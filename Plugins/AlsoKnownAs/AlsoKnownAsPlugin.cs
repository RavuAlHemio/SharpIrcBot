using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace AlsoKnownAs
{
    public class AlsoKnownAsPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected static readonly Regex AlsoKnownAsRegex = new Regex("^!aka\\s+(?<nickname>[^ ]+)\\s*");

        protected IConnectionManager ConnectionManager { get; set; }
        protected AlsoKnownAsConfig Config { get; set; }

        protected DrillDownTree<string, HashSet<string>> HostToNicks { get; }
        protected Dictionary<string, UserIdentifier> NickToMostRecentHost { get; }

        public AlsoKnownAsPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new AlsoKnownAsConfig(config);

            HostToNicks = new DrillDownTree<string, HashSet<string>>();
            NickToMostRecentHost = new Dictionary<string, UserIdentifier>();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.QueryMessage += HandleQueryMessage;
            ConnectionManager.JoinedChannel += HandleJoinedChannel;
            ConnectionManager.NickChange += HandleNickChange;
            ConnectionManager.RawMessage += HandleRawMessage;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new AlsoKnownAsConfig(newConfig);
        }

        private void HandleChannelMessage([CanBeNull] object sender, [NotNull] IrcEventArgs e, MessageFlags flags)
        {
            try
            {
                ActuallyHandleChannelOrQueryMessage(
                    sender, e, flags,
                    txt => ConnectionManager.SendChannelMessage(e.Data.Channel, $"{e.Data.Nick}: {txt}")
                );
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        private void HandleQueryMessage([CanBeNull] object sender, [NotNull] IrcEventArgs e, MessageFlags flags)
        {
            try
            {
                ActuallyHandleChannelOrQueryMessage(
                    sender, e, flags,
                    txt => ConnectionManager.SendQueryMessage(e.Data.Nick, txt)
                );
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        private void HandleJoinedChannel([CanBeNull] object sender, [NotNull] JoinEventArgs e)
        {
            try
            {
                ActuallyHandleJoinedChannel(sender, e);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling join", exc);
            }
        }

        private void HandleNickChange([CanBeNull] object sender, [NotNull] NickChangeEventArgs e)
        {
            try
            {
                ActuallyHandleNickChange(sender, e);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling nick change", exc);
            }
        }

        private void HandleRawMessage([CanBeNull] object sender, [NotNull] IrcEventArgs e)
        {
            try
            {
                ActuallyHandleRawMessage(sender, e);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling raw message", exc);
            }
        }

        protected virtual void ActuallyHandleChannelOrQueryMessage([CanBeNull] object sender, [NotNull] IrcEventArgs e, MessageFlags flags, [NotNull] Action<string> respond)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var akaMatch = AlsoKnownAsRegex.Match(e.Data.Message);
            if (!akaMatch.Success)
            {
                return;
            }

            var nickToSearch = akaMatch.Groups["nickname"].Value;

            if (!NickToMostRecentHost.ContainsKey(nickToSearch))
            {
                respond($"I don\u2019t remember {nickToSearch}.");
                return;
            }

            var identifier = NickToMostRecentHost[nickToSearch];
            var identifierParts = identifier.Parts;

            ImmutableList<HashSet<string>> matches;
            int matchDepth = HostToNicks.GetBestMatches(identifierParts, out matches);
            if (matchDepth == -1)
            {
                respond($"I don\u2019t remember any other nickname from {identifier} than {nickToSearch}.");
                return;
            }

            var otherNicks = matches
                .SelectMany(x => x)
                .OrderBy(id => id);

            if (matchDepth == identifierParts.Count)
            {
                respond($"{identifier}: {string.Join(", ", otherNicks)}");
            }
            else
            {
                respond($"{identifier} fuzzy match ({matchDepth}/{identifierParts.Count}): {string.Join(", ", otherNicks)}");
            }
        }

        protected virtual UserIdentifier GetUserIdentifier(string host)
        {
            var ip = IPAddressUserIdentifier.TryParse(host);
            if (ip != null)
            {
                return ip;
            }

            var cloakMatch = Config.CloakedAddressRegex.Match(host);
            if (cloakMatch.Success)
            {
                return new CloakedAddressUserIdentifier(cloakMatch.Groups[1].Value);
            }

            return new HostnameUserIdentifier(host);
        }

        protected virtual void RegisterNickname([NotNull] string host, [NotNull] string nick)
        {
            var identifier = GetUserIdentifier(host);
            var identifierParts = identifier.Parts;

            if (HostToNicks.ContainsKey(identifierParts))
            {
                HostToNicks[identifierParts].Add(nick);
            }
            else
            {
                HostToNicks[identifierParts] = new HashSet<string> { nick };
            }

            NickToMostRecentHost[nick] = identifier;
        }

        protected virtual void ActuallyHandleJoinedChannel(object sender, [NotNull] JoinEventArgs e)
        {
            RegisterNickname(e.Data.Host, e.Data.Nick);
        }

        protected virtual void ActuallyHandleNickChange(object sender, [NotNull] NickChangeEventArgs e)
        {
            RegisterNickname(e.Data.Host, e.Data.Nick);
        }

        protected virtual void ActuallyHandleRawMessage([CanBeNull] object sender, [NotNull] IrcEventArgs e)
        {
            if (e.Data.ReplyCode != ReplyCode.WhoIsUser)
            {
                return;
            }

            // :irc.example.com 311 MYNICK THEIRNICK THEIRUSER THEIRHOST * :REALNAME
            var theirNick = e.Data.RawMessageArray[3];
            var theirHost = e.Data.RawMessageArray[5];

            RegisterNickname(theirHost, theirNick);
        }
    }
}
