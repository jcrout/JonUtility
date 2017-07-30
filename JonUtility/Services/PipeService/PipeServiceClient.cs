using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using JonUtility;

namespace JonUtility.PipeService
{
    public class PipeServiceClient : IDisposable
    {
        private class ManagedEvents
        {
            public List<Delegate> Handlers { get; set; }

            public bool Synchronous { get; set; }

            public Type ArgsType { get; set; }
        }

        private NamedPipeClientStream pipeClient;
        private string currentProcessId;
        private StreamWriter writer;
        private StreamReader reader;
        private Dictionary<string, ManagedEvents> events = new Dictionary<string, ManagedEvents>();

        private bool isDisposed = false;
        private object syncLock = new object();
        private bool errorReturned = false;
        private string blockingResponse = null;
        private string namedPipe;

        private volatile string _nextCommand;
        private string nextCommand
        {
            get
            {
                return _nextCommand;
            }
            set
            {
                var old = _nextCommand;
                _nextCommand = value;
                if (!String.Equals(old, value))
                {
                    LogDebug($"Set NextCommand to {_nextCommand ?? "NULL"} from {old ?? "NULL"}");
                }
            }
        }

        private volatile int _synchronousDepth = 0;
        private int synchronousDepth
        {
            get
            {
                return _synchronousDepth;
            }
            set
            {
                var old = _synchronousDepth;
                if (value < 0)
                {
                    _synchronousDepth = 0;
                }
                else
                {
                    _synchronousDepth = value;
                }

                if (old != _synchronousDepth)
                {
                    LogDebug($"Set SynchronousDepth to {_synchronousDepth} from {old}");
                }
            }
        }

        private bool runningInitialCommands { get; set; }
        private bool readyForCommands { get; set; }

        public event EventHandler<PipeServerErrorArgs> ProxyError;
        public event EventHandler<EventArgs> RunningInitialCommands;

        public int MaxSyncWaitTime { get; set; } = 10000;
        public bool EnableLogging { get; set; }
        public bool IsDisposed { get { return this.isDisposed;  } }

        private Action<string> _logDebugMethod;
        public Action<string> LogDebug
        {
            get
            {
                return _logDebugMethod;
            }

            set
            {
                if (value == null)
                {
                    this._logDebugMethod = s => { };
                }
                else
                {
                    this._logDebugMethod = value;
                }
            }
        }
        
        public PipeServiceClient(string pipeName)
        {
            _logDebugMethod = this.LogDebugDefault;
            namedPipe = pipeName;
        }
        
        public T ExecuteQuery<T>(string name, string extra = null)
        {
            var command = $"{PipeMessages.Query}|{name}" + (!String.IsNullOrEmpty(extra) ? "|" + extra : "");
            var resultText = _Execute(name, command);

            return !String.IsNullOrEmpty(resultText) ? JonUtility.Serialization.DeserializeJson<T>(resultText) : default(T);
        }

        public string ExecuteQuery(string name, string extra = null)
        {
            var command = $"{PipeMessages.Query}|{name}" + (!String.IsNullOrEmpty(extra) ? "|" + extra : "");
            return _Execute(name, command);
        }

        public void ExecuteCommand(string name, object data)
        {
            var command = $"{PipeMessages.Command}|{name}" + (data != null ? "|" + JonUtility.Serialization.SerializeToJson(data) : "");
            _Execute(name, command);
        }

        public void ExecuteCommand(string name, string extra = null)
        {
            var command = $"{PipeMessages.Command}|{name}" + (!String.IsNullOrEmpty(extra) ? "|" + extra : "");
            _Execute(name, command);
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;

            if (pipeClient != null)
            {
                try
                {
                    pipeClient.Close();
                }
                catch
                {
                }
            }
        }

        public void AddEvent<T>(string name, bool synchronous, EventHandler<T> handler)
        {
            if (this.events.ContainsKey(name))
            {
                this.events[name].Handlers.Add(handler);
            }
            else
            {
                this.events.Add(name, new ManagedEvents() { Synchronous = synchronous, Handlers = new List<Delegate> { handler }, ArgsType = typeof(T) });
                this.AttachEvent(name, synchronous);
            }
        }

        public void RemoveEvent<T>(EventHandler<T> handler)
        {
            var existingEvent = this.events.FirstOrDefault(e => e.Value != null && e.Value.Handlers != null && e.Value.Handlers.Contains(handler));
            if (existingEvent.Value != null)
            {
                existingEvent.Value.Handlers.Remove(handler);
                if (existingEvent.Value.Handlers.Count == 0)
                {
                    this.events.Remove(existingEvent.Key);
                }
            }
        }

        private Tuple<ManagedEvents, EventArgs> RaiseEvent(string name, string argsText)
        {
            var subscribedEventList = this.events.ContainsKey(name) ? this.events[name] : null;
            if (subscribedEventList != null)
            {
                var args = JonUtility.Serialization.DeserializeJson(argsText, subscribedEventList.ArgsType) as EventArgs;
                foreach (var method in subscribedEventList.Handlers)
                {
                    method.DynamicInvoke(this, args);
                }

                return new Tuple<ManagedEvents, EventArgs>(subscribedEventList, args);
            }

            return null;
        }

        private ManagedEvents RaiseEvent(string name, EventArgs args)
        {
            var subscribedEventList = this.events.ContainsKey(name) ? this.events[name] : null;
            if (subscribedEventList != null)
            {
                foreach (var method in subscribedEventList.Handlers)
                {
                    method.DynamicInvoke(this, args);
                }
            }

            return subscribedEventList;
        }

        private void AttachEvent(string name, bool synchronous)
        {
            this.ExecuteCommand(PipeCommands.AttachEvent, $"{name}|{synchronous}");
        }

        private string _Execute(string name, string command)
        {
            try
            {
                WaitForConnection();
                LogDebug($"Attempting to execute: {command}");

                this.synchronousDepth++;
                var currentDepth = this.synchronousDepth;
                var wroteCommand = this.writer.WaitWrite(command);
                if (!wroteCommand)
                {
                    return null;
                }

                // block until response
                var stopWatch = Stopwatch.StartNew();
                while (true)
                {
                    System.Threading.Thread.Sleep(1);
                    var nextCommand = this.nextCommand;
                    if (!String.IsNullOrEmpty(nextCommand))
                    {
                        ProcessInput(nextCommand);
                    }
                    else if (stopWatch.ElapsedMilliseconds > MaxSyncWaitTime)
                    {
                        LogDebug($"Unblocking event {name} due to timeout");
                        break;
                    }
                    else if (this.synchronousDepth < currentDepth)
                    {
                        LogDebug($"Unblocking event {name} due to synchronous depth change");
                        break;
                    }
                }

                var response = new Tuple<bool, string>(errorReturned, blockingResponse);
                errorReturned = false;
                blockingResponse = null;

                if (response.Item1)
                {
                    var exception = new PipeServerException(response.Item2);
                    this.ProxyError.SafeRaise(this, new PipeServerErrorArgs(name, exception));
                }

                return response.Item2;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        
        private void LogDebugDefault(string text)
        {
            if (this.EnableLogging)
            {
                Debug.WriteLine(DateTime.Now.ToString("hh:mm:ss ffff") + " [" + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString("00") + "] - [PipeClient]" + text);
            }
        }

        private void WaitForConnection()
        {
            if (this.runningInitialCommands)
            {
                return;
            }

            while (!this.readyForCommands)
            {
                System.Threading.Thread.Sleep(1);
            }
        }

        public void Start()
        {
            this.pipeClient = new NamedPipeClientStream(".", namedPipe, PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation);
            this.pipeClient.Connect();

            this.writer = new StreamWriter(pipeClient);
            this.reader = new StreamReader(pipeClient);

            var task = Task.Run(() => ReadInput());

            lock (task)
            {
                this.runningInitialCommands = true;
            }

            this.RunningInitialCommands.SafeRaise(this, EventArgs.Empty);

            lock (task)
            {
                this.runningInitialCommands = false;
                this.readyForCommands = true;
            }
        }
        
        private void ReadInput()
        {
            try
            {
                while (true) // (pipeProcess != null && !pipeProcess.HasExited)
                {
                    var input = this.reader.ReadLineAsync().Result;
                    if (String.IsNullOrEmpty(input))
                    {
                        continue;
                    }

                    LogDebug($"Received input [{System.Threading.Thread.CurrentThread.ManagedThreadId}]: {input}");
                    Task.Run(() => ProcessInput(input));
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Exiting ReadInput on error: {ex.Message}");
            }
        }

        private void ProcessInput(string input)
        {
            var parts = input.Split("|".ToArray());
            if (parts.Length == 0)
            {
                this.synchronousDepth--;
                return;
            }

            var identifier = parts[0].ToLower();
            switch (identifier)
            {
                case PipeMessages.Success:
                    errorReturned = false;
                    blockingResponse = parts.Length > 1 ? parts[1] : null;
                    this.synchronousDepth--;
                    break;
                case PipeMessages.Error:
                    errorReturned = true;
                    blockingResponse = parts.Length > 1 ? parts[1] : null;
                    this.synchronousDepth--;
                    break;
                case PipeMessages.Event:
                    if (parts.Length > 1)
                    {
                        ProcessEvent(parts[1], parts.Length > 2 ? parts[2] : null);
                    }

                    break;
            }
        }

        private void ProcessEvent(string eventName, string fieldText)
        {
            var raisedEvent = this.RaiseEvent(eventName, fieldText);
            if (raisedEvent == null)
            {
                return;
            }

            var responseText = JonUtility.Serialization.SerializeToJson(raisedEvent.Item2);
            if (raisedEvent.Item1.Synchronous)
            {
                var response = String.Format("{0}|{1}|{2}", PipeMessages.Event, eventName, responseText);
                LogDebug("Sending event response: " + response);
                Task.Run(() => this.writer.WaitWrite(response));
            }
        }
    }
}
