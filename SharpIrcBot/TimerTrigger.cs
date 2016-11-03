using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace SharpIrcBot
{
    public class TimerTrigger : ITimerTrigger
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<TimerTrigger>();

        private readonly SortedDictionary<DateTimeOffset, List<Action>> _whenWhat;
        private Thread _performThread;
        private readonly CancellationTokenSource _cancelSource;
        private readonly ManualResetEvent _interruptor;

        public TimerTrigger()
        {
            _whenWhat = new SortedDictionary<DateTimeOffset, List<Action>>();
            _cancelSource = new CancellationTokenSource();
            _interruptor = new ManualResetEvent(initialState: false);
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
            _interruptor.Set();
        }

        public bool Unregister(DateTimeOffset when, Action what)
        {
            lock (_whenWhat)
            {
                if (!_whenWhat.ContainsKey(when))
                {
                    return false;
                }

                if (!_whenWhat[when].Remove(what))
                {
                    return false;
                }
            }

            // wake up the sleeper thread
            _interruptor.Set();

            return true;
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
            _performThread.Join();
            _performThread = null;
        }

        private void SleepUntilInterrupt(CancellationToken cancelToken = default(CancellationToken))
        {
            bool interrupted = false;
            while (!interrupted)
            {
                _interruptor.Reset();
                int waited = WaitHandle.WaitAny(new WaitHandle[] {cancelToken.WaitHandle, _interruptor}, TimeSpan.FromMinutes(1.0));
                switch (waited)
                {
                    case 0:
                        throw new OperationCanceledException();
                    case 1:
                        interrupted = true;
                        break;
                    case WaitHandle.WaitTimeout:
                        // loop again
                        break;
                    default:
                        Trace.Fail("unexpected switch value");
                        break;
                }
            }
        }

        /// <returns><c>true</c> if interrupted, <c>false</c> if time was hit.</returns>
        private bool SleepUntilTimeOrInterrupt(DateTimeOffset when, CancellationToken cancelToken = default(CancellationToken))
        {
            while (when > DateTimeOffset.Now)
            {
                var howLong = when - DateTimeOffset.Now;

                if (howLong.TotalMilliseconds > int.MaxValue)
                {
                    howLong = TimeSpan.FromMilliseconds(int.MaxValue);
                }

                Logger.LogDebug("sleeping until {0} ({1})", when, howLong);
                _interruptor.Reset();
                int waited = WaitHandle.WaitAny(new WaitHandle[] {cancelToken.WaitHandle, _interruptor}, howLong);
                switch (waited)
                {
                    case 0:
                        throw new OperationCanceledException();
                    case 1:
                        // and stop
                        return true;
                    case WaitHandle.WaitTimeout:
                        // loop again
                        break;
                }
            }

            Logger.LogDebug("sleep done");
            return false;
        }

        protected virtual void Proc()
        {
            var token = _cancelSource.Token;

            while (!token.IsCancellationRequested)
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
                    SleepUntilInterrupt(token);
                    continue;
                }

                var first = maybeFirst.Value;

                // sleep until that event
                bool interrupted;
                try
                {
                    interrupted = SleepUntilTimeOrInterrupt(first.Key, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (interrupted)
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
                        Logger.LogWarning("invoking {0} at {1} failed: {2}", func, first.Key, ex);
                    }
                }
            }
        }
    }
}
