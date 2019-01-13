using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Serilog.Events;
using SharpIrcBot;
using SharpIrcBot.Util;

namespace SharpIrcBotCLI
{
    class Program
    {
        public static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var logFilter = new Dictionary<string, LogEventLevel>();
            string logFilterFileName = Path.Combine(SharpIrcBotUtil.AppDirectory, "LogFilter.json");
            if (File.Exists(logFilterFileName))
            {
                using (var stream = File.Open(logFilterFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamReader(stream, StringUtil.Utf8NoBom))
                {
                    JsonSerializer.Create().Populate(reader, logFilter);
                }
            }
            LogUtil.SetupConsoleLogging(logFilter: logFilter);

            var connMgr = new ConnectionManager(args.Length > 0 ? args[0] : null);
            var pluginMgr = new PluginManager(connMgr.Config);
            connMgr.PluginManager = pluginMgr;

            pluginMgr.LoadPlugins(connMgr);
            connMgr.Start();

            if (Console.IsInputRedirected)
            {
                Thread.Sleep(Timeout.Infinite);
            }
            else
            {
                Console.Error.WriteLine("Press Enter or Esc to quit.");

                for (;;)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
            }

            connMgr.Stop();
        }
    }
}
