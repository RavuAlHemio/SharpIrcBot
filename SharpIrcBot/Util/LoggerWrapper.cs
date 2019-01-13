using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace SharpIrcBot.Util
{
    public class LoggerWrapper
    {
        public string LoggerName { get; set; }

        public LoggerWrapper(string loggerName)
        {
            LoggerName = loggerName;
        }

        public static LoggerWrapper Create<T>()
            => new LoggerWrapper(typeof(T).FullName);

        public void LogTrace(string message)
            => Write(LogEventLevel.Verbose, message);
        public void LogTrace<TP1>(string message, TP1 param1)
            => Write(LogEventLevel.Verbose, message, param1);
        public void LogTrace<TP1, TP2>(string message, TP1 param1, TP2 param2)
            => Write(LogEventLevel.Verbose, message, param1, param2);
        public void LogTrace<TP1, TP2, TP3>(string message, TP1 param1, TP2 param2, TP3 param3)
            => Write(LogEventLevel.Verbose, message, param1, param2, param3);
        public void LogTrace(string message, params object[] pars)
            => Write(LogEventLevel.Verbose, message, pars);

        public void LogDebug(string message)
            => Write(LogEventLevel.Debug, message);
        public void LogDebug<TP1>(string message, TP1 param1)
            => Write(LogEventLevel.Debug, message, param1);
        public void LogDebug<TP1, TP2>(string message, TP1 param1, TP2 param2)
            => Write(LogEventLevel.Debug, message, param1, param2);
        public void LogDebug<TP1, TP2, TP3>(string message, TP1 param1, TP2 param2, TP3 param3)
            => Write(LogEventLevel.Debug, message, param1, param2, param3);
        public void LogDebug(string message, params object[] pars)
            => Write(LogEventLevel.Debug, message, pars);

        public void LogInformation(string message)
            => Write(LogEventLevel.Information, message);
        public void LogInformation<TP1>(string message, TP1 param1)
            => Write(LogEventLevel.Information, message, param1);
        public void LogInformation<TP1, TP2>(string message, TP1 param1, TP2 param2)
            => Write(LogEventLevel.Information, message, param1, param2);
        public void LogInformation<TP1, TP2, TP3>(string message, TP1 param1, TP2 param2, TP3 param3)
            => Write(LogEventLevel.Information, message, param1, param2, param3);
        public void LogInformation(string message, params object[] pars)
            => Write(LogEventLevel.Information, message, pars);

        public void LogWarning(string message)
            => Write(LogEventLevel.Warning, message);
        public void LogWarning<TP1>(string message, TP1 param1)
            => Write(LogEventLevel.Warning, message, param1);
        public void LogWarning<TP1, TP2>(string message, TP1 param1, TP2 param2)
            => Write(LogEventLevel.Warning, message, param1, param2);
        public void LogWarning<TP1, TP2, TP3>(string message, TP1 param1, TP2 param2, TP3 param3)
            => Write(LogEventLevel.Warning, message, param1, param2, param3);
        public void LogWarning(string message, params object[] pars)
            => Write(LogEventLevel.Warning, message, pars);

        public void LogError(string message)
            => Write(LogEventLevel.Error, message);
        public void LogError<TP1>(string message, TP1 param1)
            => Write(LogEventLevel.Error, message, param1);
        public void LogError<TP1, TP2>(string message, TP1 param1, TP2 param2)
            => Write(LogEventLevel.Error, message, param1, param2);
        public void LogError<TP1, TP2, TP3>(string message, TP1 param1, TP2 param2, TP3 param3)
            => Write(LogEventLevel.Error, message, param1, param2, param3);
        public void LogError(string message, params object[] pars)
            => Write(LogEventLevel.Error, message, pars);

        public void Write(LogEventLevel level, string message)
        {
            using (var prop = LogContext.PushProperty("LoggerName", LoggerName))
            {
                Log.Write(level, message);
            }
        }
        public void Write<TP1>(LogEventLevel level, string message, TP1 param1)
        {
            using (var prop = LogContext.PushProperty("LoggerName", LoggerName))
            {
                Log.Write(level, message, param1);
            }
        }
        public void Write<TP1, TP2>(LogEventLevel level, string message, TP1 param1, TP2 param2)
        {
            using (var prop = LogContext.PushProperty("LoggerName", LoggerName))
            {
                Log.Write(level, message, param1, param2);
            }
        }
        public void Write<TP1, TP2, TP3>(LogEventLevel level, string message, TP1 param1, TP2 param2, TP3 param3)
        {
            using (var prop = LogContext.PushProperty("LoggerName", LoggerName))
            {
                Log.Write(level, message, param1, param2, param3);
            }
        }
        public void Write(LogEventLevel level, string message, params object[] pars)
        {
            using (var prop = LogContext.PushProperty("LoggerName", LoggerName))
            {
                Log.Write(level, message, pars);
            }
        }
    }
}
