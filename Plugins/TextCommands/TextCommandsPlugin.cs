using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using SharpIrcBot.Events.Irc;

namespace TextCommands
{
    public class TextCommandsPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<TextCommandsPlugin>();

        protected IConnectionManager ConnectionManager { get; }
        protected TextCommandsConfig Config { get; set; }
        protected Random RNG { get; }
        protected int LastRNGValue { get; set; }

        public TextCommandsPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new TextCommandsConfig(config);
            RNG = new Random();
            LastRNGValue = -1;

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.QueryMessage += HandlePrivateMessage;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new TextCommandsConfig(newConfig);
        }

        private void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            ActuallyHandleMessage(
                message => ConnectionManager.SendChannelMessage(args.Channel, message),
                args,
                flags
            );
        }

        private void HandlePrivateMessage(object sender, IPrivateMessageEventArgs args, MessageFlags flags)
        {
            ActuallyHandleMessage(
                message => ConnectionManager.SendQueryMessage(args.SenderNickname, message),
                args,
                flags
            );
        }

        protected void ActuallyHandleMessage(Action<string> respond, IUserMessageEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            if (args.SenderNickname == ConnectionManager.MyNickname)
            {
                return;
            }

            var lowerBody = args.Message.ToLowerInvariant();

            List<string> commandResponses;
            if (Config.CommandsResponses.TryGetValue(lowerBody, out commandResponses))
            {
                Output(respond, args, lowerBody, commandResponses, args.SenderNickname);
                return;
            }

            var channelNicks = new HashSet<string>();
            var channelMessage = args as IChannelMessageEventArgs;
            if (channelMessage != null)
            {
                var nicknamesInChannel = ConnectionManager.NicknamesInChannel(channelMessage.Channel);
                if (nicknamesInChannel != null)
                {
                    channelNicks.UnionWith(nicknamesInChannel);
                }
            }

            foreach (var nickCommandResponse in Config.NicknamableCommandsResponses)
            {
                if (!lowerBody.StartsWith(nickCommandResponse.Key))
                {
                    // not this command
                    continue;
                }

                if (lowerBody.TrimEnd() == nickCommandResponse.Key)
                {
                    // command on its own; trigger for self
                    Output(respond, args, nickCommandResponse.Key, nickCommandResponse.Value, args.SenderNickname);
                    return;
                }

                // trigger for someone else?
                var targetedNick = lowerBody.Substring(nickCommandResponse.Key.Length).Trim();
                foreach (string channelNick in channelNicks)
                {
                    if (channelNick.ToLowerInvariant() == targetedNick)
                    {
                        // nickname directly from user list
                        Output(respond, args, nickCommandResponse.Key, nickCommandResponse.Value, channelNick);
                        return;
                    }
                }

                // registered nickname?
                var registeredTargetNick = ConnectionManager.RegisteredNameForNick(targetedNick);
                if (registeredTargetNick == null)
                {
                    // nope, targeted nick is not registered
                    return;
                }

                foreach (string channelNick in channelNicks)
                {
                    var registeredChannelNick = ConnectionManager.RegisteredNameForNick(channelNick);
                    if (registeredChannelNick == null)
                    {
                        // this channel nickname is not registered
                        continue;
                    }

                    if (registeredTargetNick == registeredChannelNick)
                    {
                        // registered nicknames match
                        Output(respond, args, nickCommandResponse.Key, nickCommandResponse.Value, channelNick);
                        return;
                    }
                }
            }
        }

        protected void Output(Action<string> respond, IUserMessageEventArgs message, string command, List<string> targetBodyCandidates, string targetNick)
        {
            var channelMessage = message as IChannelMessageEventArgs;

            string targetBody;
            switch (targetBodyCandidates.Count)
            {
                case 0:
                    return;
                case 1:
                    targetBody = targetBodyCandidates[0];
                    break;
                default:
                    targetBody = targetBodyCandidates[NextRNGValueNoRepeat(targetBodyCandidates.Count)];
                    break;
            }

            Logger.LogDebug("{Sender} triggered {Command} in {Location}", message.SenderNickname, command, channelMessage?.Channel ?? "private message");
            var response = targetBody.Replace("{{NICKNAME}}", targetNick);
            foreach (var line in response.Split('\n').Where(l => l.Length > 0))
            {
                respond(line);
            }
        }

        protected virtual int NextRNGValueNoRepeat(int maxValueExclusive)
        {
            if (maxValueExclusive < 2)
            {
                return 0;
            }

            int ret;
            do
            {
                ret = RNG.Next(maxValueExclusive);
            } while (ret == LastRNGValue);

            LastRNGValue = ret;
            return ret;
        }
    }
}
