using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.WhoisLoginNickMapping
{
    public class WhoisLoginPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = LogUtil.LoggerFactory.CreateLogger<WhoisLoginPlugin>();

        protected IConnectionManager ConnectionManager { get; }
        protected WhoisLoginConfig Config { get; set; }
        protected Timer WhoisEveryoneTimer { get; set; }
        protected Dictionary<string, string> NicksToLogins { get; }

        public WhoisLoginPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new WhoisLoginConfig(config);
            WhoisEveryoneTimer = null;
            NicksToLogins = new Dictionary<string, string>();

            ConnectionManager.ConnectedToServer += HandleConnectedToServer;
            ConnectionManager.NickMapping += HandleNickMapping;
            ConnectionManager.JoinedChannel += HandleJoinedChannel;
            ConnectionManager.NamesInChannel += HandleNamesInChannel;
            ConnectionManager.NickChange += HandleNickChange;
            ConnectionManager.RawMessage += HandleRawMessage;
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new WhoisLoginConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            WhoisEveryoneTimer?.Change(TimeSpan.Zero, TimeSpan.FromMinutes(Config.ChannelSyncPeriodMinutes));
        }

        protected virtual void HandleConnectedToServer(object sender, EventArgs args)
        {
            if (WhoisEveryoneTimer == null)
            {
                WhoisEveryoneTimer = new Timer(WhoisEveryone, null, TimeSpan.Zero, TimeSpan.FromMinutes(Config.ChannelSyncPeriodMinutes));
            }
        }

        protected virtual void HandleNickMapping(object sender, NickMappingEventArgs args)
        {
            var lowerNick = args.Nickname.ToLowerInvariant();
            lock (NicksToLogins)
            {
                if (NicksToLogins.ContainsKey(lowerNick))
                {
                    if (NicksToLogins[lowerNick] == null)
                    {
                        Logger.LogDebug("regname: {Nickname} is not registered (null)", lowerNick);
                    }
                    else
                    {
                        Logger.LogDebug("regname: {Nickname} is registered as {Username}", lowerNick, NicksToLogins[lowerNick]);
                        args.MapsTo.Add(NicksToLogins[lowerNick]);
                    }
                    return;
                }
            }

            Logger.LogDebug("regname: {Nickname} is not registered (not contained)", lowerNick);
        }

        protected virtual void HandleNamesInChannel(object sender, INameListEventArgs args)
        {
            CheckRegistrationsOn(args.Nicknames);
        }

        protected virtual void HandleJoinedChannel(object sender, IUserJoinedChannelEventArgs args)
        {
            CheckRegistrationsOn(args.Nickname);
        }

        protected virtual void HandleNickChange(object sender, INickChangeEventArgs args)
        {
            CheckRegistrationsOn(args.OldNickname, args.NewNickname);
        }

        protected virtual void HandleRawMessage(object sender, IRawMessageEventArgs args)
        {
            if (args.ReplyCode == 330)
            {
                // :irc.example.com 330 MYNICK THEIRNICK THEIRLOGIN :is logged in as
                lock (NicksToLogins)
                {
                    NicksToLogins[args.RawMessageParts[3].ToLowerInvariant()] = args.RawMessageParts[4];
                }
                Logger.LogDebug("registered that {Nickname} is logged in as {Username}", args.RawMessageParts[3], args.RawMessageParts[4]);
            }
            else if (args.ReplyCode == 311)
            {
                // :irc.example.com 311 MYNICK THEIRNICK THEIRUSER THEIRHOST * :REALNAME
                // mark that we have at least seen this user
                lock (NicksToLogins)
                {
                    NicksToLogins[args.RawMessageParts[3].ToLowerInvariant()] = null;
                }
                Logger.LogDebug("registered that {Nickname} exists (and might not be logged in)", args.RawMessageParts[3]);
            }
            else if (args.ReplyCode == 401)
            {
                // :irc.example.com 401 MYNICK THEIRNICK :No such nick/channel
                // remove that user
                lock (NicksToLogins)
                {
                    NicksToLogins[args.RawMessageParts[3].ToLowerInvariant()] = null;
                }
                Logger.LogDebug("registered that {Nickname} is gone and thereby not logged in", args.RawMessageParts[3]);
            }
        }

        protected virtual void WhoisEveryone(object blah)
        {
            foreach (var channel in ConnectionManager.JoinedChannels)
            {
                // perform NAMES on the channel; the names response triggers the WHOIS waterfall
                ConnectionManager.RequestNicknamesInChannel(channel);
            }
        }

        protected virtual void CheckRegistrationsOn(params string[] nicknames)
        {
            CheckRegistrationsOn((IReadOnlyList<string>) nicknames);
        }

        protected virtual void CheckRegistrationsOn(IReadOnlyList<string> nicknames)
        {
            Logger.LogDebug("performing reg check on: {Nicknames}", nicknames.StringJoin(" "));

            // send WHOIS for every user to get their registered name
            // do this in packages to reduce traffic
            const int packageSize = 10;

            for (int i = 0; i < nicknames.Count; i += packageSize)
            {
                ConnectionManager.RequestUserInfo(nicknames.Skip(i).Take(packageSize).ToArray());
            }
        }
    }
}
