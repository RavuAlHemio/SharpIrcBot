using System;
using System.Diagnostics;
using STT = System.Threading.Timeout;

namespace SharpIrcBot.Plugins.Calc
{
    public class CalcTimer
    {
        private static Lazy<CalcTimer> _noTimeoutTimer
            = new Lazy<CalcTimer>(() => new CalcTimer(STT.InfiniteTimeSpan));
        public static CalcTimer NoTimeoutTimer => _noTimeoutTimer.Value;

        public TimeSpan Timeout { get; }
        protected Stopwatch Timekeeper { get; set; }

        public CalcTimer(TimeSpan timeout)
        {
            Timeout = timeout;
            Timekeeper = new Stopwatch();
        }

        public void Start()
        {
            if (Timeout.Ticks > 0)
            {
                Timekeeper.Start();
            }
        }

        public void Stop()
        {
            if (Timeout.Ticks > 0)
            {
                Timekeeper.Stop();
            }
        }

        public void Reset()
        {
            if (Timeout.Ticks > 0)
            {
                Timekeeper.Reset();
            }
        }

        public void ThrowIfTimedOut()
        {
            if (Timekeeper.Elapsed > Timeout && Timeout.Ticks > 0)
            {
                throw new TimeoutException();
            }
        }
    }
}
