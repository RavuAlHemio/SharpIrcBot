using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Config;
using SharpIrcBot.Events.Irc;

namespace SharpIrcBot.Plugins.Fact
{
    public class FactPlugin : IPlugin, IReloadableConfiguration
    {
        protected IConnectionManager ConnectionManager { get; set; }
        protected FactConfig Config { get; set; }
        protected Dictionary<Command, List<IFactSourcePlugin>> CommandToSourcePlugins { get; set; }
        protected Random RNG { get; set; }

        public FactPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new FactConfig(config);
            CommandToSourcePlugins = new Dictionary<Command, List<IFactSourcePlugin>>();
            RNG = new Random();

            RepopulatePluginList();
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new FactConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            RepopulatePluginList();
        }

        protected virtual void RepopulatePluginList()
        {
            // remove old plugins
            foreach (Command cmd in CommandToSourcePlugins.Keys)
            {
                ConnectionManager.CommandManager.UnregisterChannelMessageCommandHandler(cmd, HandleFactCommand);
            }
            CommandToSourcePlugins.Clear();

            // assemble new plugins
            foreach (KeyValuePair<string, List<PluginConfig>> kvp in Config.CommandToSources)
            {
                bool anyEnabled = kvp.Value.Any(pc => pc.Enabled);
                if (!anyEnabled)
                {
                    continue;
                }

                var command = new Command(
                    CommandUtil.MakeNames(kvp.Key),
                    CommandUtil.NoOptions,
                    CommandUtil.NoArguments,
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                );

                var sources = new List<IFactSourcePlugin>();
                foreach (PluginConfig config in kvp.Value)
                {
                    Assembly ass = Assembly.Load(new AssemblyName(config.Assembly));
                    Type type = ass.GetType(config.Class);
                    if (!typeof(IFactSourcePlugin).GetTypeInfo().IsAssignableFrom(type))
                    {
                        throw new ArgumentException($"class {type.FullName} is not a fact source plugin");
                    }
                    ConstructorInfo ctor = type.GetTypeInfo().GetConstructor(new [] {typeof(JObject)});
                    var pluginObject = (IFactSourcePlugin)ctor.Invoke(new object[] {config.Config});
                    sources.Add(pluginObject);
                }

                ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(command, HandleFactCommand);

                CommandToSourcePlugins[command] = sources;
            }
        }

        protected virtual void HandleFactCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            if (!Config.Channels.Contains(args.Channel))
            {
                // ignore this channel
                return;
            }

            List<IFactSourcePlugin> sources;
            if (!CommandToSourcePlugins.TryGetValue(cmd.Command, out sources))
            {
                // command not found
                return;
            }

            if (!sources.Any())
            {
                // empty list of sources for this command
                return;
            }

            // pick a source at random
            var sourceIndex = RNG.Next(0, sources.Count);
            var chosenSource = sources[sourceIndex];

            string fact = chosenSource.GetRandomFact(RNG);

            ConnectionManager.SendChannelMessage(args.Channel, fact);
        }
    }
}
