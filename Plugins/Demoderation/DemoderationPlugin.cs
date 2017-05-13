﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.EntityFrameworkCore;
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
        protected Dictionary<string, Dictionary<string, long>> ChannelCriterionCommandCache { get; set; }
        protected Dictionary<string, Regex> RegexCache { get; set; }
        protected Timer CleanupTimer { get; set; }

        public DemoderationPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new DemoderationConfig(config);

            ChannelsMessages = new Dictionary<string, RingBuffer<ChannelMessage>>();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            CleanupTimer = new Timer(
                CleanupTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromMinutes(Config.CleanupPeriodMinutes)
            );

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

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("dmnew"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker, // criterion name
                        RestTaker.Instance // criterion detection regex
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleNewCommand
            );

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("dmdel"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // criterion name
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleDeleteCommand
            );

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("dmrestore"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // criterion name
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleRestoreCommand
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
            CleanupTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(Config.CleanupPeriodMinutes));
            UpdateCommandCache();
        }

        protected void UpdateCommandCache()
        {
            ChannelCriterionCommandCache = new Dictionary<string, Dictionary<string, long>>();
            RegexCache = new Dictionary<string, Regex>();
            using (DemoderationContext ctx = GetNewContext())
            {
                foreach (Criterion crit in ctx.Criteria.Where(c => c.Enabled))
                {
                    Dictionary<string, long> commandCacheForChannel = ObtainCommandCacheForChannel(crit.Channel);
                    commandCacheForChannel[crit.Name] = crit.ID;

                    RegexCache[crit.DetectionRegex] = new Regex(crit.DetectionRegex, RegexOptions.Compiled);
                }
            }
        }

        protected virtual void HandleAbuseCommand(CommandMatch cmd, IChannelMessageEventArgs message)
        {
            if (!EnsureOp(message))
            {
                return;
            }

            var bannerNickname = (string)cmd.Arguments[0];
            var criterionName = (string)cmd.Arguments[1];

            Criterion crit;
            Ban ban;
            Abuse abuse;
            using (var ctx = GetNewContext())
            {
                // identify the criterion
                crit = ctx.Criteria
                    .FirstOrDefault(c => c.Name == criterionName && c.Channel == message.Channel);
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

                // lift the running ban
                ban.Lifted = true;

                // burn
                abuse = new Abuse
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

            Logger.LogDebug(
                "{OpNickname} is sanctioning {BannerNickname} for abusing criterion {CriterionID} ({CriterionName}) " +
                "in channel {Channel} banning {OffenderNickname} (ban {BanID}), creating abuse entry {AbuseID}",
                message.SenderNickname, ban.BannerNickname, ban.CriterionID, crit.Name, message.Channel,
                ban.OffenderNickname, ban.ID, abuse.ID
            );

            ConnectionManager.ChangeChannelMode(message.Channel, $"-b {ban.OffenderNickname}!*@*");
            ConnectionManager.ChangeChannelMode(message.Channel, $"+b {ban.BannerNickname}!*@*");
            ConnectionManager.KickChannelUser(
                message.Channel,
                ban.BannerNickname,
                $"demoderation abuse sanctioned by {message.SenderNickname}"
            );
        }

        protected virtual void HandleNewCommand(CommandMatch cmd, IChannelMessageEventArgs message)
        {
            if (!EnsureOp(message))
            {
                return;
            }

            var criterionName = (string)cmd.Arguments[0];
            string detectionRegexString = ((string)cmd.Arguments[1]).Trim();

            try
            {
                ObtainRegex(detectionRegexString);
            }
            catch (ArgumentException)
            {
                ConnectionManager.SendChannelMessage(message.Channel, $"{message.SenderNickname}: Invalid regular expression.");
                return;
            }

            using (var ctx = GetNewContext())
            {
                // see if a criterion already matches
                Criterion crit = ctx.Criteria
                    .FirstOrDefault(c => c.Name == criterionName && c.Channel == message.Channel);
                if (crit == null)
                {
                    // create a new criterion
                    crit = new Criterion
                    {
                        Name = criterionName,
                        Channel = message.Channel,
                        DetectionRegex = detectionRegexString,
                        Enabled = true
                    };
                }
                else if (crit.Enabled)
                {
                    ConnectionManager.SendChannelMessage(
                        message.Channel,
                        $"{message.SenderNickname}: That criterion name is already in use."
                    );
                    return;
                }
                else
                {
                    // modify the existing criterion and re-enable it
                    crit.DetectionRegex = detectionRegexString;
                    crit.Enabled = true;
                }
                ctx.SaveChanges();

                // update the cache
                Dictionary<string, long> commandsIDs = ObtainCommandCacheForChannel(message.Channel);
                commandsIDs[crit.Name] = crit.ID;
            }
        }

        protected virtual void HandleDeleteCommand(CommandMatch cmd, IChannelMessageEventArgs message)
        {
            if (!EnsureOp(message))
            {
                return;
            }

            var criterionName = (string)cmd.Arguments[0];

            using (DemoderationContext ctx = GetNewContext())
            {
                Criterion crit = ctx.Criteria
                    .FirstOrDefault(c => c.Name == criterionName && c.Channel == message.Channel && c.Enabled);
                if (crit == null)
                {
                    ConnectionManager.SendChannelMessage(message.Channel, $"{message.SenderNickname}: Cannot find that criterion.");
                    return;
                }

                Logger.LogDebug(
                    "disabling criterion {ID} ({Name} in {Channel}) on the behest of {Nickname}",
                    crit.ID,
                    crit.Name,
                    crit.Channel,
                    message.SenderNickname
                );

                crit.Enabled = false;
                ctx.SaveChanges();

                // remove from cache too
                Dictionary<string, long> channelCriteria = ObtainCommandCacheForChannel(message.Channel);
                channelCriteria.Remove(crit.Name);
            }

            ConnectionManager.SendChannelMessage(message.Channel, $"{message.SenderNickname}: Criterion deleted.");
        }

        protected virtual void HandleRestoreCommand(CommandMatch cmd, IChannelMessageEventArgs message)
        {
            if (!EnsureOp(message))
            {
                return;
            }

            var criterionName = (string)cmd.Arguments[0];

            using (DemoderationContext ctx = GetNewContext())
            {
                Criterion crit = ctx.Criteria
                    .FirstOrDefault(c => c.Name == criterionName && c.Channel == message.Channel);
                if (crit == null)
                {
                    ConnectionManager.SendChannelMessage(message.Channel, $"{message.SenderNickname}: Cannot find that criterion.");
                    return;
                }
                else if (crit.Enabled)
                {
                    ConnectionManager.SendChannelMessage(message.Channel, $"{message.SenderNickname}: That criterion is alive and kicking.");
                    return;
                }

                Logger.LogDebug(
                    "re-enabling criterion {ID} ({Name} in {Channel}) on the behest of {Nickname}",
                    crit.ID,
                    crit.Name,
                    crit.Channel,
                    message.SenderNickname
                );

                crit.Enabled = true;
                ctx.SaveChanges();

                // restore to cache as well
                Dictionary<string, long> channelCriteria = ObtainCommandCacheForChannel(message.Channel);
                channelCriteria[crit.Name] = crit.ID;
            }

            ConnectionManager.SendChannelMessage(message.Channel, $"{message.SenderNickname}: Criterion restored.");
        }

        protected virtual bool EnsureOp(IChannelMessageEventArgs message)
        {
            ChannelUserLevel level = ConnectionManager.GetChannelLevelForUser(message.Channel, message.SenderNickname);
            if (level < ChannelUserLevel.HalfOp)
            {
                ConnectionManager.SendChannelMessage(message.Channel, $"{message.SenderNickname}: You need to be a channel operator.");
                return false;
            }

            return true;
        }

        protected DemoderationContext GetNewContext()
        {
            var opts = SharpIrcBotUtil.GetContextOptions<DemoderationContext>(Config);
            return new DemoderationContext(opts);
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (args.SenderNickname == ConnectionManager.MyNickname)
            {
                return;
            }

            if (!flags.HasFlag(MessageFlags.UserBanned))
            {
                HandlePotentialDemoderation(args.Channel, args.SenderNickname, args.Message);
            }

            if (args.Message.StartsWith(ConnectionManager.CommandManager.Config.CommandPrefix))
            {
                if (args.Message.TrimEnd().IndexOf(' ') == -1)
                {
                    // starts with a command character and has no spaces
                    // do not consider this message relevant
                    return;
                }
            }

            RingBuffer<ChannelMessage> messages = GetOrCreateValue(
                ChannelsMessages, args.Channel, chan => new RingBuffer<ChannelMessage>(Config.BacklogSize)
            );
            messages.Add(new ChannelMessage
            {
                Nickname = args.SenderNickname,
                Username = ConnectionManager.RegisteredNameForNick(args.SenderNickname),
                Body = args.Message
            });
        }

        protected virtual void HandlePotentialDemoderation(string channel, string senderNickname, string commandString)
        {
            string commandPrefix = ConnectionManager.CommandManager.Config.CommandPrefix;
            if (!commandString.StartsWith(commandPrefix))
            {
                return;
            }

            string command = commandString.Substring(commandPrefix.Length).TrimEnd();

            // do we know this channel?
            Dictionary<string, long> criteriaForChannel;
            if (!ChannelCriterionCommandCache.TryGetValue(channel, out criteriaForChannel))
            {
                // no
                return;
            }

            // do we know this criterion?
            long criterionID;
            if (!criteriaForChannel.TryGetValue(command, out criterionID))
            {
                // no
                return;
            }

            string senderUsername = ConnectionManager.RegisteredNameForNick(senderNickname);
            ChannelMessage matchedMessage = null;

            Criterion crit;
            Ban ban;
            using (DemoderationContext ctx = GetNewContext())
            {
                // obtain the criterion
                crit = ctx.Criteria
                    .Where(c => c.Enabled && c.Channel == channel)
                    .FirstOrDefault(c => c.ID == criterionID);
                if (crit == null)
                {
                    // not found in database; remove from cache and return
                    criteriaForChannel.Remove(command);
                    return;
                }

                // is the sender being sanctioned for abuse?
                bool hasAbuseLock;
                IQueryable<Abuse> activeAbuseLocks = ctx.Abuses
                    .Include(a => a.Ban)
                    .Where(a => a.LockUntil >= DateTimeOffset.Now);

                hasAbuseLock = (senderUsername == null)
                    ? activeAbuseLocks.Any(a => a.Ban.BannerNickname == senderNickname)
                    : activeAbuseLocks.Any(
                        a => a.Ban.BannerNickname == senderNickname
                        || a.Ban.BannerUsername == senderUsername
                    );

                if (hasAbuseLock)
                {
                    ConnectionManager.SendChannelMessage(
                        channel,
                        $"{senderNickname}: You are currently being sanctioned for demod abuse."
                    );
                    return;
                }

                // find the last match in the channel
                Regex critRegex = ObtainRegex(crit.DetectionRegex);
                RingBuffer<ChannelMessage> messages;
                if (ChannelsMessages.TryGetValue(channel, out messages))
                {
                    foreach (ChannelMessage message in messages.Reverse())
                    {
                        if (critRegex.IsMatch(message.Body))
                        {
                            matchedMessage = message;
                            break;
                        }
                    }
                }

                if (matchedMessage == null)
                {
                    // can't see anyone to sanction there...
                    ConnectionManager.SendChannelMessage(
                        channel,
                        $"{senderNickname}: Can't find a message to sanction."
                    );
                    return;
                }

                // is that user already being sanctioned for this?
                IQueryable<Ban> runningBans = ctx.Bans
                    .Where(b => b.BanUntil >= DateTimeOffset.Now);
                bool alreadyBanned = (matchedMessage.Username == null)
                    ? runningBans.Any(b => b.OffenderNickname == matchedMessage.Nickname)
                    : runningBans.Any(b =>
                        b.OffenderNickname == matchedMessage.Nickname
                        || b.OffenderUsername == matchedMessage.Username
                    );
                if (alreadyBanned)
                {
                    ConnectionManager.SendChannelMessage(
                        channel,
                        $"{senderNickname}: This user is already being sanctioned."
                    );
                    return;
                }

                // halt. hammerzeit.
                ban = new Ban
                {
                    CriterionID = crit.ID,
                    Channel = channel,
                    OffenderNickname = matchedMessage.Nickname,
                    OffenderUsername = matchedMessage.Username,
                    BannerNickname = senderNickname,
                    BannerUsername = senderUsername,
                    Timestamp = DateTimeOffset.Now,
                    BanUntil = DateTimeOffset.Now.AddMinutes(Config.BanMinutes),
                    Lifted = false
                };
                ctx.Bans.Add(ban);
                ctx.SaveChanges();
            }

            Logger.LogDebug(
                "{BannerNickname} matched criterion {CriterionID} ({CriterionName}) in channel {Channel} banning " +
                "{OffenderNickname}, creating ban {BanID}",
                senderNickname, crit.ID, crit.Name, channel, ban.OffenderNickname, ban.ID
            );

            ConnectionManager.ChangeChannelMode(channel, $"+b {matchedMessage.Nickname}!*@*");
            ConnectionManager.KickChannelUser(
                channel,
                matchedMessage.Nickname,
                $"demoderation by {senderNickname}"
            );
        }

        protected Dictionary<string, long> ObtainCommandCacheForChannel(string channel)
        {
            return GetOrCreateValue(ChannelCriterionCommandCache, channel, c => new Dictionary<string, long>());
        }

        protected Regex ObtainRegex(string regexText)
        {
            return GetOrCreateValue(RegexCache, regexText, k => new Regex(k, RegexOptions.Compiled));
        }

        static TValue GetOrCreateValue<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey key,
                Func<TKey, TValue> generator)
        {
            TValue ret;
            if (!dict.TryGetValue(key, out ret))
            {
                ret = generator.Invoke(key);
                dict[key] = ret;
            }
            return ret;
        }

        protected virtual void CleanupTimerElapsed(object state)
        {
            using (var ctx = GetNewContext())
            {
                IQueryable<Ban> bansToLift = ctx.Bans
                    .Where(b => b.BanUntil < DateTimeOffset.Now && !b.Lifted);
                foreach (Ban ban in bansToLift)
                {
                    ConnectionManager.ChangeChannelMode(ban.Channel, $"-b {ban.OffenderNickname}!*@*");
                    ban.Lifted = true;
                }
                ctx.SaveChanges();

                IQueryable<Abuse> abuseBansToLift = ctx.Abuses
                    .Include(a => a.Ban)
                    .Where(a => a.BanUntil < DateTimeOffset.Now && !a.Lifted);
                foreach (Abuse abuse in abuseBansToLift)
                {
                    ConnectionManager.ChangeChannelMode(abuse.Ban.Channel, $"-b {abuse.Ban.BannerNickname}!*@*");
                    abuse.Lifted = true;
                }
                ctx.SaveChanges();
            }
        }
    }
}
