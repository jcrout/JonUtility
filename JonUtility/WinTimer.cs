namespace JonUtility
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;

    /// <summary>
    ///     Wrapper class around Winmm.dll timer functions. This class cannot be
    ///     inherited.
    /// </summary>
    public sealed class WinTimer : IDisposable
    {
        private const UInt32 EVENT_TYPE = 1;
        private readonly uint interval;
        private readonly SendOrPostCallback procMethod;
        private readonly SynchronizationContext syncContext;
        private readonly TimerEventHandler timerDelegate;
        private bool disposed;
        private bool stopRequested;
        private uint timerID;

        /// <summary>
        ///     Initializes a new instance of the <see cref="WinTimer" /> class.
        /// </summary>
        /// <param name="action">The action delegate to execute on each proc.</param>
        /// <param name="interval">The timer interval between procs.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action" /> is <see langword="null" />.</exception>
        public WinTimer(Action action, int interval)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            this.syncContext = SynchronizationContext.Current;
            this.interval = (uint)interval;
            this.procMethod = o => action();
            this.timerDelegate = this.TimerCallback;
        }

        ~WinTimer()
        {
            this.OnDispose(false);
        }

        private delegate void TimerEventHandler(int id, int msg, IntPtr user, int dw1, int dw2);

        public void Dispose()
        {
            this.OnDispose(true);
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            this.stopRequested = false;
            WinTimer.timeBeginPeriod(1);
            this.timerID = WinTimer.timeSetEvent(
                this.interval,
                0,
                this.timerDelegate,
                UIntPtr.Zero,
                WinTimer.EVENT_TYPE);
        }

        public void Stop()
        {
            if (this.stopRequested)
            {
                return;
            }

            this.stopRequested = true;

            if (this.timerID != 0)
            {
                WinTimer.timeKillEvent(this.timerID);
                WinTimer.timeEndPeriod(1);
                Thread.Sleep(0);
            }
        }

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        private static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        private static extern uint timeEndPeriod(uint uPeriod);

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        private static extern uint timeGetTime();

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        private static extern uint timeKillEvent(uint uTimerID);

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        private static extern uint timeSetEvent(uint uDelay, uint uResolution, TimerEventHandler lpTimeProc,
            UIntPtr dwUser, uint fuEvent);

        private void OnDispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.Stop();
        }

        private void TimerCallback(int id, int msg, IntPtr user, int dw1, int dw2)
        {
            if (this.stopRequested)
            {
                return;
            }

            if (this.timerID != 0)
            {
                this.syncContext.Post(this.procMethod, null);
            }
        }
    }
}
