using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Sed.Parsing;

namespace SharpIrcBot.Plugins.Sed
{
    public class SedPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<SedPlugin>();

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

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new SedConfig(newConfig);
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
            List<ReplacementSpec> replacements = Parser.ParseSubCommands(e.Message);
            if (replacements == null)
            {
                // something that didn't even look like sed commands
                return false;
            }

            if (replacements.Count == 0)
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

                foreach (ReplacementSpec spec in replacements)
                {
                    int matchIndex = -1;
                    replaced = spec.Pattern.Replace(replaced, match =>
                    {
                        ++matchIndex;

                        if (matchIndex < spec.FirstMatch)
                        {
                            // unchanged
                            return match.Value;
                        }

                        if (matchIndex > spec.FirstMatch && !spec.ReplaceAll)
                        {
                            // unchanged
                            return match.Value;
                        }

                        return match.Result(spec.Replacement);
                    });
                }

                if (replaced != lastBody)
                {
                    // success!
                    ConnectionManager.SendChannelMessage(e.Channel, replaced);
                    foundAny = true;
                    break;
                }
            }

            if (!foundAny)
            {
                Logger.LogInformation("no recent messages found to match replacements {ReplacementsString}", e.Message);
            }

            return true;
        }
    }
}
