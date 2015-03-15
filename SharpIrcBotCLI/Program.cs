using System;
using SharpIrcBot;

namespace SharpIrcBotCLI
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            SharpIrcBotUtil.SetupConsoleLogging();

            var config = SharpIrcBotUtil.LoadConfig();
            var connMgr = new ConnectionManager(config);
            var pluginMgr = new PluginManager(config);

            pluginMgr.LoadPlugins(connMgr);
            connMgr.Start();

            Console.Error.WriteLine("Press Enter or Esc to quit.");

            for (;;)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Enter)
                {
                    break;
                }
            }

            connMgr.Stop();
        }
    }
}
