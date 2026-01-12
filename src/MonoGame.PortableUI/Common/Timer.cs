using System;

namespace MonoGame.PortableUI.Common
{
    public class Timer
    {
        private TimeSpan _startedAt;

        public Timer(int waitTime)
        {
            WaitTime = waitTime;
        }

        public int WaitTime { get; set; }
        public bool IsRunning { get; set; }
        public event EventHandler Elapsed;

        public void Start()
        {
            IsRunning = true;
            _startedAt = ScreenSystem.TotalTime;
        }

        public void Update()
        {
            if (!IsRunning || ScreenSystem.TotalTime - _startedAt < TimeSpan.FromMilliseconds(WaitTime))
                return;

            IsRunning = false;
            OnTimerElapsed();
        }

        public void Stop()
        {
            IsRunning = false;
        }

        protected virtual void OnTimerElapsed()
        {
            Elapsed?.Invoke(this, EventArgs.Empty);
        }
    }
}
