using System;
using System.Text;
using System.Threading;
using SharpIrcBot;

namespace SharpIrcBotCLI
{
    class Program
    {
        public static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            SharpIrcBotUtil.SetupConsoleLogging();

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
