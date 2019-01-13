using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks;

namespace SharpIrcBot.Util
{
    public static class LogUtil
    {
        public static void SetupConsoleLogging([CanBeNull] LogEventLevel? minimumLevel = null, [CanBeNull] Dictionary<string, LogEventLevel> logFilter = null)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .Filter.ByIncludingOnly(ev => LogFilter(ev, minimumLevel, logFilter))
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {LoggerName}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();
        }

        static bool LogFilter(LogEvent evt, [CanBeNull] LogEventLevel? minimumLevel = null, [CanBeNull] Dictionary<string, LogEventLevel> logFilter = null)
        {
            if (minimumLevel.HasValue && evt.Level < minimumLevel.Value)
            {
                return false;
            }

            if (logFilter != null)
            {
                LogEventLevel minimumLoggerLevel;
                LogEventPropertyValue loggerName;
                if (!evt.Properties.TryGetValue("LoggerName", out loggerName))
                {
                    // event does not have a logger name; assume output is wanted
                    return true;
                }

                if (logFilter.TryGetValue(loggerName.ToString(), out minimumLoggerLevel))
                {
                    if (evt.Level < minimumLoggerLevel)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
