using System;
using System.Text.RegularExpressions;
using log4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using SharpIrcBot.Events.Irc;

namespace WhoDoYouThinkIs
{
    public class WhoDoYouThinkIsPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static readonly Regex WhoDoYouThinkIsRegex = new Regex("^!wdyti\\s+(?<nick>[^ ]+)\\s*$", RegexOptions.Compiled);

        protected IConnectionManager ConnectionManager { get; }

        public WhoDoYouThinkIsPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.QueryMessage += HandleQueryMessage;
        }

        private void HandleQueryMessage(object sender, IPrivateMessageEventArgs e, MessageFlags flags)
        {
            HandleMessage(
                e.Message,
                flags,
                msg => ConnectionManager.SendQueryMessage(e.SenderNickname, msg)
            );
        }

        private void HandleChannelMessage(object sender, IChannelMessageEventArgs e, MessageFlags flags)
        {
            HandleMessage(
                e.Message,
                flags,
                msg => ConnectionManager.SendChannelMessage(e.Channel, $"{e.SenderNickname}: {msg}")
            );
        }

        private void HandleMessage(string messageBody, MessageFlags flags, Action<string> respond)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            Match wdytiMatch = WhoDoYouThinkIsRegex.Match(messageBody);
            if (!wdytiMatch.Success)
            {
                return;
            }

            string nick = wdytiMatch.Groups["nick"].Value;
            string regName = ConnectionManager.RegisteredNameForNick(nick);

            respond(regName == null ? $"I don't know {nick}." : $"I think {nick} is {regName}.");
        }
    }
}
