using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Collections;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Demoderation.ORM;

namespace SharpIrcBot.Plugins.Demoderation
{
    /// <summary>
    /// Delivers messages to users when they return.
    /// </summary>
    public class DemoderationPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<DemoderationPlugin>();

        protected DemoderationConfig Config { get; set; }
        protected IConnectionManager ConnectionManager { get; set; }

        protected Dictionary<string, RingBuffer<ChannelMessage>> ChannelsMessages { get; set; }
        protected Dictionary<string, long> CommandCache { get; set; }
        protected Dictionary<string, Regex> RegexCache { get; set; }

        public DemoderationPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new DemoderationConfig(config);

            ChannelsMessages = new Dictionary<string, RingBuffer<ChannelMessage>>();

            ConnectionManager.ChannelMessage += HandleChannelMessage;

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("dmabuse"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker, // nickname
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // criterion name
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleAbuseCommand
            );

            UpdateCommandCache();
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new DemoderationConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            UpdateCommandCache();
        }

        protected void UpdateCommandCache()
        {
            CommandCache = new Dictionary<string, long>();
            RegexCache = new Dictionary<string, Regex>();
            using (DemoderationContext ctx = GetNewContext())
            {
                foreach (Criterion crit in ctx.Criteria)
                {
                    CommandCache[crit.Name] = crit.ID;
                    RegexCache[crit.DetectionRegex] = new Regex(crit.DetectionRegex, RegexOptions.Compiled);
                }
            }
        }

        protected void HandleAbuseCommand(CommandMatch cmd, IChannelMessageEventArgs message)
        {
            ChannelUserLevel level = ConnectionManager.GetChannelLevelForUser(message.Channel, message.SenderNickname);
            if (level < ChannelUserLevel.HalfOp)
            {
                ConnectionManager.SendChannelMessage(message.Channel, $"{message.SenderNickname}: You need to be a channel operator.");
                return;
            }

            var bannerNickname = (string)cmd.Arguments[0];
            var criterionName = (string)cmd.Arguments[1];

            Ban ban;
            using (var ctx = GetNewContext())
            {
                // identify the criterion
                Criterion crit = ctx.Criteria
                    .FirstOrDefault(c => c.Name == criterionName);
                if (crit == null)
                {
                    ConnectionManager.SendChannelMessage(message.Channel, $"{message.SenderNickname}: Unknown criterion.");
                    return;
                }

                // find the last relevant ban
                ban = ctx.Bans
                    .Where(b => b.CriterionID == crit.ID)
                    .Where(b => b.BannerNickname == bannerNickname)
                    .OrderByDescending(b => b.Timestamp)
                    .FirstOrDefault();

                if (ban == null)
                {
                    ConnectionManager.SendChannelMessage(message.Channel, $"{message.SenderNickname}: No relevant ban found.");
                    return;
                }

                // find if it has been sanctioned yet
                bool sanctioned = ctx.Abuses
                    .Any(a => a.BanID == ban.ID);
                if (sanctioned)
                {
                    ConnectionManager.SendChannelMessage(message.Channel, $"{message.SenderNickname}: This abuse has already been sanctioned.");
                    return;
                }

                // burn
                var abuse = new Abuse
                {
                    BanID = ban.ID,
                    OpNickname = message.SenderNickname,
                    OpUsername = ConnectionManager.RegisteredNameForNick(message.SenderNickname),
                    Timestamp = DateTimeOffset.Now,
                    BanUntil = DateTimeOffset.Now.AddMinutes(Config.AbuseBanMinutes),
                    LockUntil = DateTimeOffset.Now.AddMinutes(Config.AbuseLockMinutes),
                    Lifted = false
                };
                ctx.Abuses.Add(abuse);
                ctx.SaveChanges();
            }

            ConnectionManager.ChangeChannelMode(message.Channel, $"+b {ban.BannerNickname}!*@*");
            ConnectionManager.KickChannelUser(
                message.Channel,
                ban.BannerNickname,
                $"demoderation abuse sanctioned by {message.SenderNickname}"
            );
        }

        private DemoderationContext GetNewContext()
        {
            var opts = SharpIrcBotUtil.GetContextOptions<DemoderationContext>(Config);
            return new DemoderationContext(opts);
        }

        protected void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (args.SenderNickname == ConnectionManager.MyNickname)
            {
                return;
            }

            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                HandlePotentialDemoderation(args.Channel, args.Message);
            }

            RingBuffer<ChannelMessage> messages;
            if (!ChannelsMessages.TryGetValue(args.Channel, out messages))
            {
                messages = new RingBuffer<ChannelMessage>(Config.BacklogSize);
                ChannelsMessages[args.Channel] = messages;
            }
            messages.Add(new ChannelMessage
            {
                Nickname = args.SenderNickname,
                Username = ConnectionManager.RegisteredNameForNick(args.SenderNickname),
                Body = args.Message
            });
        }

        protected void HandlePotentialDemoderation(string channel, string commandString)
        {
            string commandPrefix = ConnectionManager.CommandManager.Config.CommandPrefix;
            if (!commandString.StartsWith(commandPrefix))
            {
                return;
            }

            string command = commandString.Substring(commandPrefix.Length).TrimEnd();

            // TODO
        }
    }
}
