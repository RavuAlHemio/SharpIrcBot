using System;
using System.Collections.Generic;
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

        protected ConnectionManager ConnectionManager { get; set; }
        protected AlsoKnownAsConfig Config { get; set; }

        protected Dictionary<UserIdentifier, HashSet<string>> HostToNicks { get; }
        protected Dictionary<string, UserIdentifier> NickToMostRecentHost { get; }

        public AlsoKnownAsPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new AlsoKnownAsConfig(config);

            HostToNicks = new Dictionary<UserIdentifier, HashSet<string>>();
            NickToMostRecentHost = new Dictionary<string, UserIdentifier>();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.QueryMessage += HandleQueryMessage;
            ConnectionManager.JoinedChannel += HandleJoinedChannel;
            ConnectionManager.NickChange += HandleNickChange;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new AlsoKnownAsConfig(newConfig);
        }

        private void HandleChannelMessage([CanBeNull] object sender, [CanBeNull] IrcEventArgs e, MessageFlags flags)
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

        private void HandleQueryMessage([CanBeNull] object sender, [CanBeNull] IrcEventArgs e, MessageFlags flags)
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

        private void HandleJoinedChannel([CanBeNull] object sender, [CanBeNull] JoinEventArgs e)
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

        private void HandleNickChange([CanBeNull] object sender, [CanBeNull] NickChangeEventArgs e)
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

        protected virtual void ActuallyHandleChannelOrQueryMessage([CanBeNull] object sender, [CanBeNull] IrcEventArgs e, MessageFlags flags, [NotNull] Action<string> respond)
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
            if (!HostToNicks.ContainsKey(identifier))
            {
                respond($"I don\u2019t remember any other nickname from {identifier} than {nickToSearch}.");
                return;
            }

            var otherNicks = HostToNicks[identifier]
                .Select(id => id.ToString())
                .OrderBy(id => id);
            respond($"{identifier}: {string.Join(", ", otherNicks)}");
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

        protected virtual void RegisterNickname(string host, string nick)
        {
            var identifier = GetUserIdentifier(host);

            if (HostToNicks.ContainsKey(identifier))
            {
                HostToNicks[identifier].Add(nick);
            }
            else
            {
                HostToNicks[identifier] = new HashSet<string> { nick };
            }

            NickToMostRecentHost[nick] = identifier;
        }

        protected virtual void ActuallyHandleJoinedChannel(object sender, JoinEventArgs e)
        {
            RegisterNickname(e.Data.Host, e.Data.Nick);
        }

        protected virtual void ActuallyHandleNickChange(object sender, NickChangeEventArgs e)
        {
            RegisterNickname(e.Data.Host, e.Data.Nick);
        }
    }
}
