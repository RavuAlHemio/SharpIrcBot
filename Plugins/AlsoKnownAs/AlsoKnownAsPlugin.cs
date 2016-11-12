﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using SharpIrcBot.Collections;
using SharpIrcBot.Events.Irc;

namespace AlsoKnownAs
{
    public class AlsoKnownAsPlugin : IPlugin, IReloadableConfiguration
    {
        public static readonly Regex AlsoKnownAsRegex = new Regex("^!aka\\s+(?<nickname>\\S+)\\s*", RegexOptions.Compiled);

        protected IConnectionManager ConnectionManager { get; set; }
        protected AlsoKnownAsConfig Config { get; set; }

        protected DrillDownTree<string, HashSet<string>> HostToNicks { get; }
        protected Dictionary<string, UserIdentifier> NickToMostRecentHost { get; }

        public AlsoKnownAsPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new AlsoKnownAsConfig(config);

            HostToNicks = new DrillDownTree<string, HashSet<string>>();
            NickToMostRecentHost = new Dictionary<string, UserIdentifier>(StringComparer.OrdinalIgnoreCase);

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

        private void HandleChannelMessage([CanBeNull] object sender, [NotNull] IChannelMessageEventArgs e, MessageFlags flags)
        {
            ActuallyHandleChannelOrQueryMessage(
                sender, e, flags,
                txt => ConnectionManager.SendChannelMessage(e.Channel, $"{e.SenderNickname}: {txt}")
            );
        }

        private void HandleQueryMessage([CanBeNull] object sender, [NotNull] IPrivateMessageEventArgs e, MessageFlags flags)
        {
            ActuallyHandleChannelOrQueryMessage(
                sender, e, flags,
                txt => ConnectionManager.SendQueryMessage(e.SenderNickname, txt)
            );
        }

        protected virtual void ActuallyHandleChannelOrQueryMessage([CanBeNull] object sender, [NotNull] IUserMessageEventArgs e, MessageFlags flags, [NotNull] Action<string> respond)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var akaMatch = AlsoKnownAsRegex.Match(e.Message);
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

            ImmutableList<HashSet<string>> fuzzyMatches = null;
            int fuzzyMatchDepth = -1;
            if (matchDepth == identifierParts.Count)
            {
                // do a fuzzy match too
                // replace the last item in identifierParts with the empty string
                var fuzzyIdentifierParts = identifierParts.SetItem(identifierParts.Count - 1, "");
                fuzzyMatchDepth = HostToNicks.GetBestMatches(fuzzyIdentifierParts, out fuzzyMatches);
            }

            var otherNicks = new SortedSet<string>(matches.SelectMany(x => x));
            var fuzzyOtherNicks = (fuzzyMatches == null)
                ? null
                : new SortedSet<string>(fuzzyMatches.SelectMany(x => x));
            fuzzyOtherNicks?.ExceptWith(otherNicks);

            if (matchDepth == identifierParts.Count)
            {
                if (fuzzyOtherNicks != null && fuzzyOtherNicks.Count > 0)
                {
                    respond($"{identifier}: {string.Join(", ", otherNicks)}; fuzzy match ({fuzzyMatchDepth}/{identifierParts.Count}) also: {string.Join(", ", fuzzyOtherNicks)}");
                }
                else
                {
                    respond($"{identifier}: {string.Join(", ", otherNicks)}");
                }
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
                HostToNicks[identifierParts] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { nick };
            }

            NickToMostRecentHost[nick] = identifier;
        }

        protected virtual void HandleJoinedChannel(object sender, [NotNull] IUserJoinedChannelEventArgs e)
        {
            RegisterNickname(e.Host, e.Nickname);
        }

        protected virtual void HandleNickChange(object sender, [NotNull] INickChangeEventArgs e)
        {
            RegisterNickname(e.Host, e.NewNickname);
        }

        protected virtual void HandleRawMessage([CanBeNull] object sender, [NotNull] IRawMessageEventArgs e)
        {
            if (e.ReplyCode != 311)
            {
                return;
            }

            // :irc.example.com 311 MYNICK THEIRNICK THEIRUSER THEIRHOST * :REALNAME
            var theirNick = e.RawMessageParts[3];
            var theirHost = e.RawMessageParts[5];

            RegisterNickname(theirHost, theirNick);
        }
    }
}
