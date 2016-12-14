using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Config;

namespace SharpIrcBot
{
    public class PluginManager
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<PluginManager>();

        [NotNull]
        protected BotConfig Config;
        [NotNull, ItemNotNull]
        protected List<IPlugin> Plugins;

        public PluginManager([NotNull] BotConfig config)
        {
            Config = config;
            Plugins = new List<IPlugin>();
        }

        public void LoadPlugins([NotNull] IConnectionManager connManager)
        {
            foreach (var plugin in Config.Plugins)
            {
                Assembly ass;
#if NETCORE
                ass = SharpIrcBotAssemblyLoadContext.Instance.LoadFromAssemblyName(new AssemblyName(plugin.Assembly));
#else
                ass = Assembly.Load(new AssemblyName(plugin.Assembly));
#endif
                var type = ass.GetType(plugin.Class);
                if (!typeof(IPlugin).GetTypeInfo().IsAssignableFrom(type))
                {
                    throw new ArgumentException($"class {plugin.Class} from assembly {plugin.Assembly} is not a plugin (does not implement {typeof(IPlugin).FullName})");
                }
                var ctor = type.GetTypeInfo().GetConstructor(new [] {typeof(IConnectionManager), typeof(JObject)});
                var pluginObject = (IPlugin)ctor.Invoke(new object[] {connManager, plugin.Config});
                Plugins.Add(pluginObject);
            }
        }

        public void ReloadConfigurations([NotNull] List<PluginConfig> newPluginConfigs)
        {
            if (Plugins.Count != newPluginConfigs.Count)
            {
                throw new ArgumentException("number of plugins changed", nameof(newPluginConfigs));
            }

            foreach (var pluginPair in Enumerable.Zip(Plugins, newPluginConfigs, Tuple.Create))
            {
                IPlugin plugin = pluginPair.Item1;
                PluginConfig newConfig = pluginPair.Item2;

                var pluginType = plugin.GetType();

                if (pluginType.FullName != newConfig.Class)
                {
                    throw new ArgumentException($"plugin order changed; existing plugin of type {plugin.GetType().FullName} clashes with configured plugin of type {newConfig.Class}", nameof(newPluginConfigs));
                }

                var updatablePlugin = plugin as IReloadableConfiguration;
                if (updatablePlugin == null)
                {
                    // nope
                    continue;
                }

                Logger.LogInformation("updating configuration of plugin of type {PluginName}", pluginType.FullName);
                try
                {
                    updatablePlugin.ReloadConfiguration(newConfig.Config);
                }
                catch (Exception exc)
                {
                    Logger.LogError("failed to update configuration of plugin of type {PluginName}: {Exception}", pluginType.FullName, exc);
                }
            }
        }
    }
}
