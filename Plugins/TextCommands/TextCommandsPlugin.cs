using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace TextCommands
{
    public class TextCommandsPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected TextCommandsConfig Config { get; set; }
        protected IConnectionManager ConnectionManager { get; }

        public TextCommandsPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new TextCommandsConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.QueryMessage += HandlePrivateMessage;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new TextCommandsConfig(newConfig);
        }

        private void HandleChannelMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            try
            {
                ActuallyHandleMessage(
                    message => ConnectionManager.SendChannelMessage(args.Data.Channel, message),
                    args,
                    flags
                );
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        private void HandlePrivateMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            try
            {
                ActuallyHandleMessage(
                    message => ConnectionManager.SendQueryMessage(args.Data.Nick, message),
                    args,
                    flags
                );
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        protected void ActuallyHandleMessage(Action<string> respond, IrcEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var message = args.Data;
            if (message.Nick == ConnectionManager.MyNickname)
            {
                return;
            }

            var lowerBody = message.Message.ToLowerInvariant();

            if (Config.CommandsResponses.ContainsKey(lowerBody))
            {
                Output(respond, lowerBody, message.Nick, message.Channel, Config.CommandsResponses[lowerBody], message.Nick);
                return;
            }

            var channelNicksEnumerable = ConnectionManager.NicknamesInChannel(args.Data.Channel);
            var channelNicks = (channelNicksEnumerable == null)
                ? new HashSet<string>()
                : new HashSet<string>(channelNicksEnumerable);

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
                    Output(respond, nickCommandResponse.Key, message.Nick, message.Channel, nickCommandResponse.Value, message.Nick);
                    return;
                }

                // trigger for someone else?
                var targetedNick = lowerBody.Substring(nickCommandResponse.Key.Length).Trim();
                foreach (string channelNick in channelNicks)
                {
                    if (channelNick.ToLowerInvariant() == targetedNick)
                    {
                        // nickname directly from user list
                        Output(respond, nickCommandResponse.Key, message.Nick, message.Channel, nickCommandResponse.Value, channelNick);
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
                        Output(respond, nickCommandResponse.Key, message.Nick, message.Channel, nickCommandResponse.Value, channelNick);
                        return;
                    }
                }
            }
        }

        protected void Output(Action<string> respond, string command, string author, string location, string targetBody, string targetNick)
        {
            Logger.DebugFormat("{0} triggered {1} in {2}", author, command, location);
            var response = targetBody.Replace("{{NICKNAME}}", targetNick);
            foreach (var line in response.Split('\n').Where(l => l.Length > 0))
            {
                respond(line);
            }
        }
    }
}
