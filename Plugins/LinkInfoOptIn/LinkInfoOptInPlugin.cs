using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LinkInfo;
using LinkInfoOptIn.ORM;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;

namespace LinkInfoOptIn
{
    public class LinkInfoOptInPlugin : LinkInfoPlugin
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<LinkInfoOptInPlugin>();
        public static readonly Regex AutoLinkInfoRegex = new Regex("^!(?<unsub>no)?autolinkinfo\\s*$", RegexOptions.Compiled);

        public Uri LastBroadcastLink { get; set; }

        protected LinkInfoOptInConfig OptInConfig
        {
            get { return (LinkInfoOptInConfig) Config; }
            set { Config = value; }
        }

        public LinkInfoOptInPlugin(IConnectionManager connMgr, JObject config)
            : base(connMgr, config)
        {
            Config = new LinkInfoOptInConfig(config);

            ConnectionManager.BaseNickChanged += HandleBaseNickChanged;
            ConnectionManager.ChannelMessage += HandleChannelMessageOptIn;
        }

        public override void ReloadConfiguration(JObject newConfig)
        {
            Config = new LinkInfoOptInConfig(newConfig);
        }

        protected override void LinksAction(IChannelMessageEventArgs args, MessageFlags flags, IList<Uri> links)
        {
            bool postToChannel = false;

            if (links.Count == 0)
            {
                // no links here; never mind
                return;
            }

            // post the regular response?
            if (args.Message.StartsWith("!link "))
            {
                postToChannel = true;
            }

            // broadcast to users who have opted in
            var optedInNicks = new List<string>();

            // who is in the channel?
            var channelNicks = ConnectionManager.NicknamesInChannel(args.Channel);
            if (channelNicks != null)
            {
                // what are their registered names? (consider only registered users)
                var channelUsers = channelNicks
                    .Select(nick => new KeyValuePair<string, string>(nick, ConnectionManager.RegisteredNameForNick(nick)))
                    .Where(n => n.Value != null)
                    .ToList();

                // which users have opted in?
                using (var ctx = GetNewContext())
                {
                    foreach (var user in channelUsers)
                    {
                        string username = user.Value;
                        if (ctx.OptedInUsers.Any(u => u.UserName == username))
                        {
                            optedInNicks.Add(user.Key);
                        }
                    }
                }
            }

            if (!postToChannel && optedInNicks.Count == 0)
            {
                // nothing to fetch, nothing to post
                return;
            }

            foreach (var linkAndInfo in links.Select(ObtainLinkInfo))
            {
                if (postToChannel)
                {
                    PostLinkInfoToChannel(linkAndInfo, args.Channel);
                }

                if (LastBroadcastLink == null || LastBroadcastLink != linkAndInfo.Link)
                {
                    foreach (var nick in optedInNicks)
                    {
                        PostLinkInfo(linkAndInfo, message => ConnectionManager.SendQueryNotice(nick, message));
                    }
                    LastBroadcastLink = linkAndInfo.Link;
                }
                else
                {
                    Logger.LogDebug($"not multicasting link info for {LastBroadcastLink.AbsoluteUri}; same as the last");
                }
            }
        }

        protected void HandleChannelMessageOptIn(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var match = AutoLinkInfoRegex.Match(args.Message);
            if (!match.Success)
            {
                return;
            }

            var senderUsername = ConnectionManager.RegisteredNameForNick(args.SenderNickname);
            if (senderUsername == null)
            {
                ConnectionManager.SendChannelMessageFormat(args.Channel, "{0}: You must be registered to use this feature.", args.SenderNickname);
                return;
            }

            bool removeSubscription = match.Groups["unsub"].Success;

            using (var ctx = GetNewContext())
            {
                var currentSub = ctx.OptedInUsers.FirstOrDefault(u => u.UserName == senderUsername);

                if (removeSubscription)
                {
                    if (currentSub == null)
                    {
                        ConnectionManager.SendChannelMessageFormat(args.Channel, "{0}: You are not subscribed to auto link info.", args.SenderNickname);
                    }
                    else
                    {
                        Logger.LogInformation("{0} ({1}) is unsubscribing from auto link info", args.SenderNickname, senderUsername);

                        ctx.OptedInUsers.Remove(currentSub);
                        ctx.SaveChanges();
                        ConnectionManager.SendChannelMessageFormat(args.Channel, "{0}: You have been unsubscribed from auto link info.", args.SenderNickname);
                    }
                }
                else
                {
                    // add subscription
                    if (currentSub == null)
                    {
                        Logger.LogInformation("{0} ({1}) is subscribing to auto link info", args.SenderNickname, senderUsername);

                        ctx.OptedInUsers.Add(new OptedInUser {UserName = senderUsername});
                        ctx.SaveChanges();
                        ConnectionManager.SendChannelMessageFormat(args.Channel, "{0}: You are now subscribed to auto link info.", args.SenderNickname);
                    }
                    else
                    {
                        ConnectionManager.SendChannelMessageFormat(args.Channel, "{0}: You are already subscribed to auto link info.", args.SenderNickname);
                    }
                }
            }
        }

        private LinkInfoOptInContext GetNewContext()
        {
            var opts = SharpIrcBotUtil.GetContextOptions<LinkInfoOptInContext>(OptInConfig);
            return new LinkInfoOptInContext(opts);
        }

        protected virtual void HandleBaseNickChanged(object sender, BaseNickChangedEventArgs e)
        {
            string oldBaseNick = e.OldBaseNick;
            string newBaseNick = e.NewBaseNick;

            using (var ctx = GetNewContext())
            {
                // fix up subscription
                var oldOptedInUser = ctx.OptedInUsers
                    .FirstOrDefault(u => u.UserName == oldBaseNick);
                if (oldOptedInUser == null)
                {
                    // no subscription to transfer
                    return;
                }

                ctx.OptedInUsers.Remove(oldOptedInUser);

                var newOptedInUser = ctx.OptedInUsers
                    .FirstOrDefault(u => u.UserName == newBaseNick);
                if (newOptedInUser == null)
                {
                    // add this subscription
                    ctx.OptedInUsers.Add(new OptedInUser {UserName = newBaseNick});
                }

                ctx.SaveChanges();
            }
        }
    }
}
