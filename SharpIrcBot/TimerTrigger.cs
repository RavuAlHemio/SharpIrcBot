using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using log4net;

namespace SharpIrcBot
{
    public class TimerTrigger
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private SortedDictionary<DateTimeOffset, List<Action>> _whenWhat;
        private Thread _performThread;
        private CancellationTokenSource _cancelSource;

        public TimerTrigger()
        {
            _whenWhat = new SortedDictionary<DateTimeOffset, List<Action>>();
            _cancelSource = new CancellationTokenSource();
        }

        public void Register(DateTimeOffset when, Action what)
        {
            lock (_whenWhat)
            {
                if (!_whenWhat.ContainsKey(when))
                {
                    _whenWhat[when] = new List<Action>();
                }

                _whenWhat[when].Add(what);
            }

            // wake up the sleeper thread
            _performThread?.Interrupt();
        }

        public void Start()
        {
            _performThread = new Thread(Proc)
            {
                IsBackground = true,
                Name = "TimerTrigger Proc"
            };
            _performThread.Start();
        }

        public void Stop()
        {
            _cancelSource.Cancel();
            _performThread.Interrupt();
            _performThread.Join();
            _performThread = null;
        }

        private void SleepUntilInterrupt()
        {
            bool interrupted = false;
            while (!interrupted)
            {
                try
                {
                    Thread.Sleep(TimeSpan.FromMinutes(1.0));
                }
                catch (ThreadInterruptedException)
                {
                    interrupted = true;
                }
            }
        }

        /// <returns><c>true</c> if interrupted, <c>false</c> if time was hit.</returns>
        private bool SleepUntilTimeOrInterrupt(DateTimeOffset when)
        {
            while (when > DateTimeOffset.Now)
            {
                var howLong = when - DateTimeOffset.Now;
                try
                {
                    Logger.DebugFormat("sleeping until {0} ({1})", when, howLong);
                    Thread.Sleep(howLong);
                }
                catch (TargetInvocationException)
                {
                    // and stop
                    return true;
                }
            }

            Logger.Debug("sleep done");
            return false;
        }

        protected virtual void Proc()
        {
            var token = _cancelSource.Token;

            while (!_cancelSource.IsCancellationRequested)
            {
                KeyValuePair<DateTimeOffset, List<Action>>? maybeFirst;

                // find (and remove) the next event
                lock (_whenWhat)
                {
                    if (_whenWhat.Count > 0)
                    {
                        maybeFirst = _whenWhat.First();
                        _whenWhat.Remove(maybeFirst.Value.Key);
                    }
                    else
                    {
                        maybeFirst = null;
                    }
                }

                if (!maybeFirst.HasValue)
                {
                    SleepUntilInterrupt();
                    continue;
                }

                var first = maybeFirst.Value;

                // sleep until that event
                if (SleepUntilTimeOrInterrupt(first.Key))
                {
                    // interrupted; something might have changed

                    // put back our values
                    lock (_whenWhat)
                    {
                        if (_whenWhat.ContainsKey(first.Key))
                        {
                            // already have other events at this timestamp; append
                            _whenWhat[first.Key].AddRange(first.Value);
                        }
                        else
                        {
                            // no events here; store my list
                            _whenWhat[first.Key] = first.Value;
                        }
                    }

                    // start again
                    continue;
                }

                // go through the list, calling the actions
                foreach (var func in first.Value)
                {
                    try
                    {
                        func.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Logger.WarnFormat("invoking {0} at {1} failed: {2}", func, first.Key, ex);
                    }
                }
            }
        }
    }
}
