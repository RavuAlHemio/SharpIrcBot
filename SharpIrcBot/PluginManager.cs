using System;
using System.Collections.Generic;
#if NETCORE
using System.IO;
#endif
using System.Linq;
using System.Reflection;
#if NETCORE
using System.Runtime.Loader;
#endif
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
                ass = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(SharpIrcBotUtil.AppDirectory, plugin.Assembly));
#else
                ass = Assembly.Load(new AssemblyName(plugin.Assembly));
#endif
                var type = ass.GetType(plugin.Class);
                if (!typeof(IPlugin).GetTypeInfo().IsAssignableFrom(type))
                {
                    throw new ArgumentException("class is not a plugin");
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
