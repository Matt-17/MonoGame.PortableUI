using System;       
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

        public Timer(int waitTime)
        {
            WaitTime = waitTime;
        }

        public async Task Start()
        {
            int seconds = 0;
            IsRunning = true;
            while (IsRunning)
            {
                if (seconds != 0 && seconds % WaitTime == 0)
                {
                    OnTimerElapsed();
                }
                await Task.Delay(1000);
                seconds++;
            }
        }

        public void Stop()
        {
            IsRunning = false;
        }
    }
}
