using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Sed.Parsing;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Sed
{
    public class SedPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = LogUtil.LoggerFactory.CreateLogger<SedPlugin>();

        protected IConnectionManager ConnectionManager { get; }
        protected SedConfig Config { get; set; }

        protected Dictionary<string, List<string>> ChannelToLastBodies { get; set; }
        protected SedParser Parser { get; set; }

        public SedPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new SedConfig(config);

            ChannelToLastBodies = new Dictionary<string, List<string>>();
            Parser = new SedParser();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new SedConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs e, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            if (HandleReplacementCommand(e))
            {
                return;
            }

            // remember?
            List<string> lastBodies;
            if (!ChannelToLastBodies.TryGetValue(e.Channel, out lastBodies))
            {
                lastBodies = new List<string>();
                ChannelToLastBodies[e.Channel] = lastBodies;
            }

            lastBodies.Insert(0, e.Message);
            while (lastBodies.Count > Config.RememberLastMessages && lastBodies.Count > 0)
            {
                lastBodies.RemoveAt(lastBodies.Count - 1);
            }
        }

        protected virtual bool HandleReplacementCommand(IChannelMessageEventArgs e)
        {
            List<ITransformCommand> transformations = Parser.ParseSubCommands(e.Message);
            if (transformations == null)
            {
                // something that didn't even look like sed commands
                return false;
            }

            if (transformations.Count == 0)
            {
                // something that looked like sed commands but didn't work
                return true;
            }

            // find the message to perform a replacement in
            List<string> lastBodies;
            if (!ChannelToLastBodies.TryGetValue(e.Channel, out lastBodies))
            {
                // no last bodies for this channel; never mind
                return true;
            }

            bool foundAny = false;
            foreach (string lastBody in lastBodies)
            {
                string replaced = lastBody;

                foreach (ITransformCommand transformation in transformations)
                {
                    replaced = transformation.Transform(replaced);
                }

                if (replaced != lastBody)
                {
                    // success!
                    if (Config.MaxResultLength >= 0 && replaced.Length > Config.MaxResultLength)
                    {
                        ConnectionManager.SendChannelMessage(e.Channel, Config.ResultTooLongMessage);
                    }
                    else
                    {
                        ConnectionManager.SendChannelMessage(e.Channel, replaced);
                    }

                    foundAny = true;
                    break;
                }
            }

            if (!foundAny)
            {
                Logger.LogInformation("no recent messages found to match transformations {TransformationsString}", e.Message);
            }

            return true;
        }
    }
}
