using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Vitals
{
    public class VitalsPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<VitalsPlugin>();

        protected IConnectionManager ConnectionManager { get; }
        protected VitalsConfig Config { get; set; }
        
        protected Dictionary<string, IVitalsReader> NameToReader { get; set; }

        public VitalsPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new VitalsConfig(config);
            NameToReader = new Dictionary<string, IVitalsReader>();

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("vitals", "vit"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        RestTaker.Instance // location
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleVitalsCommand
            );

            RecreateReaders();
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new VitalsConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            RecreateReaders();
        }

        protected virtual void RecreateReaders()
        {
            NameToReader.Clear();

            foreach (KeyValuePair<string, VitalsTarget> nameTarget in Config.Targets)
            {
                string name = nameTarget.Key;
                VitalsTarget target = nameTarget.Value;

                Assembly ass = (target.ReaderAssembly == null)
                    ? Assembly.GetCallingAssembly()
                    : Assembly.Load(target.ReaderAssembly);

                Type cls = ass.GetType(target.ReaderClass, throwOnError: true, ignoreCase: false);
                ConstructorInfo ctor = cls.GetConstructor(
                    bindingAttr: BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    callConvention: CallingConventions.Any,
                    types: new[] { typeof(JObject) },
                    modifiers: null
                );

                var reader = (IVitalsReader)ctor.Invoke(new object[] { target.ReaderOptions });
                NameToReader[name.ToLowerInvariant()] = reader;
            }
        }

        protected virtual void HandleVitalsCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            string name = ((string)cmd.Arguments[0]).Trim();
            if (name.Length == 0)
            {
                name = Config.DefaultTarget;
            }

            IVitalsReader reader;
            if (!NameToReader.TryGetValue(name.ToLowerInvariant(), out reader))
            {
                ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: Unknown vital '{name}'.");
            }

            string vital = reader.Read();
            ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: {vital}");
        }
    }
}
