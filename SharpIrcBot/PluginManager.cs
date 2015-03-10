using System;
using System.Collections.Generic;
using System.Reflection;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;

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

        public void LoadPlugins(IrcClient client)
        {
            foreach (var plugin in Config.Plugins)
            {
                var ass = Assembly.Load(plugin.Assembly);
                var type = ass.GetType(plugin.Class);
                if (!typeof(IPlugin).IsAssignableFrom(type))
                {
                    throw new ArgumentException("class is not a plugin");
                }
                var ctor = type.GetConstructor(new [] {typeof(IrcClient), typeof(JObject)});
                var pluginObject = (IPlugin)ctor.Invoke(new object[] {client, plugin.Config});
                Plugins.Add(pluginObject);
            }
        }
    }
}
