using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Config;

namespace SharpIrcBot
{
    public class PluginManager
    {
        protected BotConfig Config;
        protected List<IPlugin> Plugins;

        public PluginManager(BotConfig config)
        {
            Config = config;
            Plugins = new List<IPlugin>();
        }

        public void LoadPlugins(ConnectionManager connManager)
        {
            foreach (var plugin in Config.Plugins)
            {
                var ass = Assembly.Load(plugin.Assembly);
                var type = ass.GetType(plugin.Class);
                if (!typeof(IPlugin).IsAssignableFrom(type))
                {
                    throw new ArgumentException("class is not a plugin");
                }
                var ctor = type.GetConstructor(new [] {typeof(ConnectionManager), typeof(JObject)});
                var pluginObject = (IPlugin)ctor.Invoke(new object[] {connManager, plugin.Config});
                Plugins.Add(pluginObject);
            }
        }
    }
}
