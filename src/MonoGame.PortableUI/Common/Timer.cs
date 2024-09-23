using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MonoGame.PortableUI.Common
{
    public class Timer
    {
        public int WaitTime { get; set; }
        public bool IsRunning { get; set; }
        public event EventHandler Elapsed;
        protected virtual void OnTimerElapsed()
        {
            Elapsed?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Creates a new Timer with desired waiting time in ms.
        /// </summary>
        /// <param name="waitTime">Time in milliseconds</param>
        public Timer(int waitTime)
        {
            WaitTime = waitTime;
        }

        public async void Start()
        {
            IsRunning = true;
            await Task.Delay(WaitTime);
            if (IsRunning)
                OnTimerElapsed();
        }

        public void Stop()
        {
            IsRunning = false;
        }
    }
}
