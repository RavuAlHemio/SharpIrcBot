using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Sed.Parsing;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Sed
{
    public class SedPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<SedPlugin>();

        protected IConnectionManager ConnectionManager { get; }
        protected SedConfig Config { get; set; }

        protected Dictionary<string, List<LastMessage>> ChannelToLastMessages { get; set; }
        protected SedParser Parser { get; set; }

        protected ImmutableArray<Regex> BannedUserRegexes { get; set; }

        public SedPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new SedConfig(config);

            ChannelToLastMessages = new Dictionary<string, List<LastMessage>>();
            Parser = new SedParser();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.ChannelAction += HandleChannelAction;
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new SedConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            BannedUserRegexes = Config.BannedUserRegexes
                .Select(re => new Regex(re, RegexOptions.Compiled))
                .ToImmutableArray();
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs e, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            if (BannedUserRegexes.Any(re => re.IsMatch(e.SenderNickname)))
            {
                return;
            }

            string senderUser = ConnectionManager.RegisteredNameForNick(e.SenderNickname);
            if (senderUser != null && BannedUserRegexes.Any(re => re.IsMatch(senderUser)))
            {
                return;
            }

            if (HandleReplacementCommand(e))
            {
                return;
            }

            RememberMessage(e.Channel, new LastMessage(LastMessageType.ChannelMessage, e.SenderNickname, e.Message));
        }

        protected virtual void HandleChannelAction(object sender, IChannelMessageEventArgs e, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            // remember
            RememberMessage(e.Channel, new LastMessage(LastMessageType.ChannelAction, e.SenderNickname, e.Message));
        }

        protected virtual void RememberMessage(string channel, LastMessage message)
        {
            List<LastMessage> lastMessages;
            if (!ChannelToLastMessages.TryGetValue(channel, out lastMessages))
            {
                lastMessages = new List<LastMessage>();
                ChannelToLastMessages[channel] = lastMessages;
            }

            lastMessages.Insert(0, message);
            while (lastMessages.Count > Config.RememberLastMessages && lastMessages.Count > 0)
            {
                lastMessages.RemoveAt(lastMessages.Count - 1);
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
            List<LastMessage> lastMessages;
            if (!ChannelToLastMessages.TryGetValue(e.Channel, out lastMessages))
            {
                // no last bodies for this channel; never mind
                return true;
            }

            bool foundAny = false;
            foreach (LastMessage lastMessage in lastMessages)
            {
                string replaced = lastMessage.Body;

                foreach (ITransformCommand transformation in transformations)
                {
                    replaced = transformation.Transform(replaced);
                }

                if (replaced != lastMessage.Body)
                {
                    // success!
                    if (Config.MaxResultLength >= 0 && replaced.Length > Config.MaxResultLength)
                    {
                        replaced = Config.ResultTooLongMessage;
                    }

                    if (lastMessage.Type == LastMessageType.ChannelMessage)
                    {
                        ConnectionManager.SendChannelMessage(e.Channel, replaced);
                    }
                    else if (lastMessage.Type == LastMessageType.ChannelAction)
                    {
                        ConnectionManager.SendQueryAction(e.Channel, replaced);
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
