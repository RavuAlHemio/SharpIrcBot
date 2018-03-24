using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace SharpIrcBot.Util
{
    public static class LogUtil
    {
        public static readonly ILoggerFactory LoggerFactory = new LoggerFactory();

        public static void SetupFileLogging([CanBeNull] LogLevel? level = null)
        {
            var serilogLevelMapping = new Dictionary<LogLevel, LogEventLevel>
            {
                [LogLevel.Critical] = LogEventLevel.Fatal,
                [LogLevel.Error] = LogEventLevel.Error,
                [LogLevel.Warning] = LogEventLevel.Warning,
                [LogLevel.Information] = LogEventLevel.Information,
                [LogLevel.Debug] = LogEventLevel.Debug,
                [LogLevel.Trace] = LogEventLevel.Verbose
            };

            var serilogLoggerConfig = new LoggerConfiguration();
            if (level.HasValue)
            {
                serilogLoggerConfig = serilogLoggerConfig
                    .MinimumLevel.Is(serilogLevelMapping[level.Value]);
            }
            serilogLoggerConfig = serilogLoggerConfig
                .WriteTo.RollingFile(Path.Combine(SharpIrcBotUtil.AppDirectory, "SharpIrcBot-{Date}.log"));

            LoggerFactory.AddProvider(new SerilogLoggerProvider(serilogLoggerConfig.CreateLogger()));
        }

        public static void SetupConsoleLogging([CanBeNull] LogLevel? minimumLevel = null, [CanBeNull] Dictionary<string, LogLevel> logFilter = null)
        {
            var consoleProvider = new ConsoleLoggerProvider(
                (text, logLevel) => LogFilter(text, logLevel, minimumLevel, logFilter),
                true
            );
            LoggerFactory.AddProvider(consoleProvider);
        }

        static bool LogFilter(string logger, LogLevel level, [CanBeNull] LogLevel? minimumLevel = null, [CanBeNull] Dictionary<string, LogLevel> logFilter = null)
        {
            if (minimumLevel.HasValue && level < minimumLevel.Value)
            {
                return false;
            }

            if (logFilter != null)
            {
                LogLevel minimumLoggerLevel;
                if (logFilter.TryGetValue(logger, out minimumLoggerLevel))
                {
                    if (level < minimumLoggerLevel)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
