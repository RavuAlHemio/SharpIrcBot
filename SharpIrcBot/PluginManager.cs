using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Config;

namespace SharpIrcBot
{
    public class PluginManager
    {
        [NotNull]
        protected BotConfig Config;
        [NotNull, ItemNotNull]
        protected List<IPlugin> Plugins;

        public PluginManager([NotNull] BotConfig config)
        {
            Config = config;
            Plugins = new List<IPlugin>();
        }

        public void LoadPlugins([NotNull] ConnectionManager connManager)
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
