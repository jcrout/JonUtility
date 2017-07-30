using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Threading;
using JonUtility;
using SerUtility = JonUtility.Serialization;

namespace JonUtility.PipeService
{
    public class PipeServiceServer : IDisposable
    {
        public delegate object ProcessCommandDelegate(string name, string[] args);
        public delegate object ProcessQueryDelegate(string name, string[] args);
        public delegate void HandleEventResponseDelegate(string name, EventArgs args);
        public delegate void TraceDelegate(string text);

        public event EventHandler Disposing;

        private string namedPipe;

        private TraceDelegate _traceMethod;
        public TraceDelegate TraceMethod
        {
            get
            {
                return _traceMethod;
            }

            set
            {
                if (value == null)
                {
                    this._traceMethod = s => { };
                }
                else
                {
                    this._traceMethod = value;
                }
            }
        }
        
        private ProcessQueryDelegate _processQueryMethod;
        public ProcessQueryDelegate ProcessQueryMethod
        {
            get
            {
                return _processQueryMethod;
            }
            set
            {
                if (value == null)
                {
                    this._processQueryMethod = (o1, o2) => null;
                }
                else
                {
                    this._processQueryMethod = value;
                }
            }
        }

        private ProcessCommandDelegate _processCommandMethod;
        public ProcessCommandDelegate ProcessCommandMethod
        {
            get
            {
                return _processCommandMethod;
            }
            set
            {
                if (value == null)
                {
                    this._processCommandMethod = (o1, o2) => null;
                }
                else
                {
                    this._processCommandMethod = value;
                }
            }
        }

        private HandleEventResponseDelegate _handleEventResponse;
        public HandleEventResponseDelegate HandleEventResponse
        {
            get
            {
                return _handleEventResponse;
            }
            set
            {
                if (value == null)
                {
                    this._handleEventResponse = (o1, o2) => { };
                }
                else
                {
                    this._handleEventResponse = value;
                }
            }
        }
        
        private Dictionary<string, Type> events = new Dictionary<string, Type>();

        private Process Process { get; set; }

        public string ProcessId { get; private set; }

        public bool IsDisposed { get { return this.isDisposed; } }

        private Stream Pipe { get; set; }

        private StreamReader Reader { get; set; }

        private StreamWriter Writer { get; set; }

        private EventArgs BlockingEventArgs { get; set; }

        private string BlockingEvent { get; set; }

        private bool InSynchronousContext { get; set; }

        private bool isDisposed;

        private int _SyncronousDepth = 0;
        private int SynchronousDepth
        {
            get
            {
                return _SyncronousDepth;
            }
            set
            {
                var old = _SyncronousDepth;
                if (_SyncronousDepth < 0)
                {
                    _SyncronousDepth = 0;
                }
                else
                {
                    _SyncronousDepth = value;
                }

                if (old != _SyncronousDepth)
                {
                    this.TraceMethod($"Set SynchronousDepth to {_SyncronousDepth} from {old}");
                }
            }
        }

        private string _NextCommand = null;
        private string NextCommand
        {
            get
            {
                return _NextCommand;
            }
            set
            {
                var old = _NextCommand;
                _NextCommand = value;
                if (!String.Equals(old, value))
                {
                    this.TraceMethod($"Set NextCommand to {_NextCommand ?? "NULL"} from {old ?? "NULL"}");
                }
            }
        }
        
        public PipeServiceServer(string pipeName, string processId = null)
        {
            this.namedPipe = pipeName;
            if (!String.IsNullOrEmpty(processId))
            {
                this.SetProcessId(processId);
            }

            this.TraceMethod = null;
            this.HandleEventResponse = null;   
            this.ProcessQueryMethod = null;
        }

        public void RegisterEvent(string name, Type eventArgsType)
        {
            this.events.Add(name, eventArgsType);
        }

        public void SetProcessId(string id)
        {
            this.ProcessId = id;
            this.Process = Process.GetProcessById(Int32.Parse(id));
        }
        
        public void Start(string processId = null)
        {
            this.TraceMethod($"Initializing listener named pipe \"{namedPipe}\".");

            var pipeServer = new NamedPipeServerStream(namedPipe, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            int threadId = Thread.CurrentThread.ManagedThreadId;
            this.Pipe = pipeServer;
            if (!String.IsNullOrEmpty(processId))
            {
                this.SetProcessId(processId);
            }

            // Wait for a client to connect        
            try
            {
                pipeServer.WaitForConnection();
                Task.Run(() => HandleReading());

                this.TraceMethod($"Client has connected on thread: {threadId}");
            }
            catch
            {                
            }
        }

        private void HandleReading()
        {
            string message = null;
            try
            {
                Reader = new StreamReader(this.Pipe);
                Writer = new StreamWriter(this.Pipe);

                while (true)
                {
                    message = Reader.ReadLineAsync().Result;
                    HandleInputFirstLevel(message);
                }
            }
            // Catch the IOException that is raised if the pipe is broken
            // or disconnected.
            catch (Exception e)
            {
                this.TraceMethod($"Exiting listener pipe thread: Error: {e.Message}");
            }
        }

        public void RaisePipeEvent(string name, EventArgs args, bool synchronous)
        {
            const long MaxSyncWaitTime = 5000L;

            var process = this.Process;
            if (process == null)
            {
                this.Dispose();
                return;
            }
            else
            {
                if (synchronous)
                {
                    this.InSynchronousContext = true;
                    this.BlockingEventArgs = args;
                    this.BlockingEvent = name.ToLower();
                    this.SynchronousDepth++;
                }

                var eventArgsText = JonUtility.Serialization.SerializeToJson(args);
                var pipeProcessId = this.ProcessId;
                this.Writer.WaitWrite($"{PipeMessages.Event}|{name}|{eventArgsText}");

                if (synchronous)
                {
                    // block until response written
                    this.TraceMethod($"Blocking on event {name}");
                    var stopWatch = Stopwatch.StartNew();
                    var currentDepth = this.SynchronousDepth;
                    while (true)
                    {
                        if (!this.InSynchronousContext || this.SynchronousDepth < currentDepth)
                        {
                            this.TraceMethod($"Unblocking event {name} due to successful response reception");
                            break;
                        }
                        else if (stopWatch.ElapsedMilliseconds > MaxSyncWaitTime)
                        {
                            this.TraceMethod($"Unblocking event {name} due to timeout");
                            break;
                        }
                        else if (process.HasExited)
                        {
                            this.TraceMethod($"Unblocking event {name} due to client process {pipeProcessId} exit");
                            this.Dispose();
                            return;
                        }

                        var nextCommand = this.NextCommand;
                        if (!String.IsNullOrEmpty(nextCommand))
                        {
                            stopWatch.Stop();
                            ProcessInput(nextCommand);
                            stopWatch.Reset();
                            stopWatch.Start();
                        }

                        System.Threading.Thread.Sleep(1);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            this.Disposing.SafeRaise(this);
            this.isDisposed = true;
            this.TraceMethod("Disposing PipeServiceServer");
            if (this.Pipe != null)
            {
                JonUtility.Try.Do(() => this.Pipe.Close());
            }
        }
        
        private void HandleInputFirstLevel(string temp)
        {
            if (temp == null)
            {
                return;
            }

            if (this.SynchronousDepth > 0)
            {
                this.TraceMethod($"Enqueueing command: {temp}");
                this.NextCommand = temp;
            }
            else
            {
                this.TraceMethod($"Running command: {temp}");
                Task.Run(() => ProcessInput(temp));
            }
        }

        private void ProcessInput(string temp)
        {
            this.TraceMethod($"ProcessInput: {temp}");
            this.NextCommand = null;

            var lowerText = temp.ToLower();
            if (lowerText.StartsWith(PipeMessages.Command))
            {
                try
                {
                    this.TraceMethod($"Attempting Command: {temp}");
                    var output = ProcessCommand(temp);
                    var outputText = output != null ? $"{PipeMessages.Success}|{output}" : PipeMessages.Success;

                    this.TraceMethod($"Command Result: {outputText}");
                    this.Writer.WaitWrite(outputText);
                }
                catch (Exception ex)
                {
                    this.TraceMethod($"Command Result: Error: {ex.Message}");
                    this.Writer.WaitWrite($"{PipeMessages.Error}|" + ex.Message);
                }
            }
            else if (lowerText.StartsWith(PipeMessages.Query))
            {
                try
                {
                    this.TraceMethod($"Attempting Query: {temp}");
                    var output = ProcessQuery(temp);
                    var outputText = output != null ? $"{PipeMessages.Success}|{output}" : PipeMessages.Success;

                    this.TraceMethod($"Query Result: {outputText}");
                    this.Writer.WaitWrite(outputText);
                }
                catch (Exception ex)
                {
                    this.TraceMethod($"Query Result: Error: {ex.Message}");
                    this.Writer.WaitWrite($"{PipeMessages.Error}|" + ex.Message);
                }
            }
            else if (lowerText.StartsWith(PipeMessages.Event))
            {
                try
                {
                    this.TraceMethod($"Attempting Event Processing: {temp}");
                    ProcessEventResponse(temp);
                }
                catch (Exception ex)
                {
                    this.TraceMethod($"Event Process: Error: {ex.Message}");
                    this.Writer.WaitWrite($"{PipeMessages.Error}|" + ex.Message);
                }
            }
            else
            {
                this.TraceMethod($"Ignoring unknown message: {temp}");
                this.Writer.WaitWrite("UNKNOWN:" + temp);
            }
        }

        private void ProcessEventResponse(string input)
        {
            var parts = input.Split("|".ToArray(), StringSplitOptions.RemoveEmptyEntries);
            ValidateArgsLength(parts, minCount: 3); // Event|Name|ResponseText

            var eventName = parts[1].ToLower();
            var isBlocking = !String.IsNullOrEmpty(this.BlockingEvent);

            Type eventArgsType;
            this.events.TryGetValue(eventName, out eventArgsType);
            if (eventArgsType == null)
            {
                return;
            }

            if (BlockingEventArgs != null && parts.Length > 2)
            {
                var argsText = parts[2];
                var deserializedArgs = SerUtility.DeserializeJson(argsText, eventArgsType);
                SerUtility.AssignObjectFromOther(BlockingEventArgs, deserializedArgs);
            }

            HandleEventResponse(eventName, BlockingEventArgs);

            this.BlockingEventArgs = null;
            this.BlockingEvent = null;
            this.SynchronousDepth--;
        }

        private object ProcessQuery(string text)
        {
            var parts = text.Split("|".ToArray(), StringSplitOptions.RemoveEmptyEntries);
            ValidateArgsLength(parts, minCount: 2);

            var queryType = parts[1].ToLower();
            var args = parts.Skip(2).ToArray();


            var response = this.ProcessQueryMethod(queryType, args);
            if (response == null || response.GetType() == typeof(string))
            {
                return response;
            }
            else
            {
                return SerUtility.SerializeToJson(response);
            }
        }
        
        private object ProcessCommand(string text)
        {
            var parts = text.Split("|".ToArray(), StringSplitOptions.RemoveEmptyEntries);
            ValidateArgsLength(parts, minCount: 2);

            var commandType = parts[1].ToLower();
            var args = parts.Skip(2).ToArray();
            var response = this.ProcessCommandMethod(commandType, args);
            if (response == null || response.GetType() == typeof(string))
            {
                return response;
            }
            else
            {
                return SerUtility.SerializeToJson(response);
            }            
        }

        private void ValidateArgsLength(string[] parts, int minCount)
        {
            if (parts.Length < minCount)
            {
                throw new Exception(String.Format("Not enough arguments. Expected: {0}; Received: {1}", minCount.ToString(), parts.Length));
            }
        }
    }
}
