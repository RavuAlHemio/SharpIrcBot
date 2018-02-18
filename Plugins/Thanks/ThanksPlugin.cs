using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Thanks.ORM;

namespace SharpIrcBot.Plugins.Thanks
{
    public class ThanksPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<ThanksPlugin>();

        protected IConnectionManager ConnectionManager { get; }
        protected ThanksConfig Config { get; set; }

        public ThanksPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new ThanksConfig(config);

            ConnectionManager.BaseNickChanged += HandleBaseNickChanged;

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("thank", "thanks", "thx"),
                    CommandUtil.MakeOptions(
                        CommandUtil.MakeFlag("--force")
                    ),
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker, // target
                        RestTaker.Instance // reason
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleThankCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("thanked"),
                    CommandUtil.MakeOptions(
                        CommandUtil.MakeFlag("--raw")
                    ),
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // target
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleThankedCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("grateful"),
                    CommandUtil.MakeOptions(
                        CommandUtil.MakeFlag("--raw")
                    ),
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // target
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleGratefulCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("topthanked"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleTopThankedCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("topgrateful"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleTopGratefulCommand
            );
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new ThanksConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        protected virtual void HandleThankCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            string thankerNick = msg.SenderNickname;

            bool forceThanks = cmd.Options.Any(f => f.Key == "--force");
            string thankeeNick = ((string)cmd.Arguments[0]).TrimEnd(':', ',', ';');
            string reason = ((string)cmd.Arguments[1]).Trim();

            if (reason.Length == 0)  // trimmed!
            {
                reason = null;
            }

            string thanker;
            string thankee;
            if (forceThanks)
            {
                thanker = thankerNick;
                thankee = thankeeNick;
            }
            else
            {
                thanker = ConnectionManager.RegisteredNameForNick(thankerNick);
                if (thanker == null)
                {
                    ConnectionManager.SendChannelMessageFormat(
                        msg.Channel,
                        "{0}: You can't use this unless you're logged in with NickServ.",
                        thankerNick
                    );
                    return;
                }

                thankee = ConnectionManager.RegisteredNameForNick(thankeeNick);
                if (thankee == null)
                {
                    ConnectionManager.SendChannelMessageFormat(
                        msg.Channel,
                        "{0}: Unfortunately, {1} doesn't seem to be logged in with NickServ.",
                        thankerNick,
                        thankeeNick
                    );
                    return;
                }
            }

            var thankerLower = thanker.ToLowerInvariant();
            var thankeeLower = thankee.ToLowerInvariant();

            if (thankeeLower == thankerLower)
            {
                ConnectionManager.SendChannelMessageFormat(
                    msg.Channel,
                    "You are so full of yourself, {0}.",
                    thankerNick
                );
                return;
            }

            Logger.LogDebug("{Thanker} thanks {Thankee}", thanker, thankee);

            long thankedCount;
            using (var ctx = GetNewContext())
            {
                var entry = new ThanksEntry
                {
                    Channel = msg.Channel,
                    ThankerLowercase = thankerLower,
                    ThankeeLowercase = thankeeLower,
                    Timestamp = DateTime.Now.ToUniversalTimeForDatabase(),
                    Deleted = false,
                    Reason = reason
                };
                ctx.ThanksEntries.Add(entry);
                ctx.SaveChanges();

                thankedCount = ctx.ThanksEntries.Count(te => te.ThankeeLowercase == thankeeLower && !te.Deleted);
            }

            ConnectionManager.SendChannelMessageFormat(
                msg.Channel,
                "{0}: Alright! By the way, {1} has been thanked {2} until now.",
                msg.SenderNickname,
                thankee,
                (thankedCount == 1) ? "once" : (thankedCount + " times")
            );
        }

        protected virtual void HandleThankedCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            bool raw = cmd.Options.Any(f => f.Key == "--raw");
            var nickname = (string)cmd.Arguments[0];
            if (!raw)
            {
                var username = ConnectionManager.RegisteredNameForNick(nickname);
                if (username != null)
                {
                    nickname = username;
                }
            }

            var lowerNickname = nickname.ToLowerInvariant();

            long thankedCount;
            using (var ctx = GetNewContext())
            {
                thankedCount = ctx.ThanksEntries.Count(te => te.ThankeeLowercase == lowerNickname && !te.Deleted);
            }

            string countPhrase;
            bool showStats = (thankedCount != 0);

            if (thankedCount == 0)
            {
                countPhrase = "not been thanked";
            }
            else if (thankedCount == 1)
            {
                countPhrase = "been thanked once";
            }
            else
            {
                countPhrase = string.Format("been thanked {0} times", thankedCount);
            }

            var statsString = "";
            if (showStats)
            {
                List<string> mostGratefulStrings;
                using (var ctx = GetNewContext())
                {
                    mostGratefulStrings = ctx.ThanksEntries
                        .Where(te => te.ThankeeLowercase == lowerNickname && !te.Deleted)
                        .GroupBy(te => te.ThankerLowercase, (thanker, thanksEnumerable) => new NicknameAndCount { Nickname = thanker, Count = thanksEnumerable.Count() })
                        .OrderByDescending(te => te.Count)
                        .Take(Config.MostGratefulCount + 1)
                        .ToList()
                        .Select(te => string.Format("{0}: {1}\u00D7", te.Nickname, te.Count))
                        .ToList();
                }

                // mention that the list is truncated if there are more than MostGratefulCount entries
                var countString = (mostGratefulStrings.Count <= Config.MostGratefulCount) ? "" : (" " + Config.MostGratefulCountText);
                statsString = string.Format(
                    " (Most grateful{0}: {1})",
                    countString,
                    mostGratefulStrings.Take(Config.MostGratefulCount).StringJoin(", ")
                );
            }

            ConnectionManager.SendChannelMessageFormat(
                msg.Channel,
                "{0}: {1} has {2} until now.{3}",
                msg.SenderNickname,
                nickname,
                countPhrase,
                statsString
            );
        }

        protected virtual void HandleGratefulCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            bool raw = cmd.Options.Any(f => f.Key == "--raw");
            var nickname = (string)cmd.Arguments[0];
            if (!raw)
            {
                var username = ConnectionManager.RegisteredNameForNick(nickname);
                if (username != null)
                {
                    nickname = username;
                }
            }

            var lowerNickname = nickname.ToLowerInvariant();

            long gratefulCount;
            using (var ctx = GetNewContext())
            {
                gratefulCount = ctx.ThanksEntries.Count(te => te.ThankerLowercase == lowerNickname && !te.Deleted);
            }

            string countPhrase;
            bool showStats = (gratefulCount != 0);

            if (gratefulCount == 0)
            {
                countPhrase = "not thanked anybody";
            }
            else if (gratefulCount == 1)
            {
                countPhrase = "thanked once";
            }
            else
            {
                countPhrase = string.Format("thanked {0} times", gratefulCount);
            }

            var statsString = "";
            if (showStats)
            {
                List<string> mostThankedStrings;
                using (var ctx = GetNewContext())
                {
                    mostThankedStrings = ctx.ThanksEntries
                        .Where(te => te.ThankerLowercase == lowerNickname && !te.Deleted)
                        .GroupBy(te => te.ThankeeLowercase, (thankee, thanksEnumerable) => new NicknameAndCount { Nickname = thankee, Count = thanksEnumerable.Count() })
                        .OrderByDescending(te => te.Count)
                        .Take(Config.MostGratefulCount + 1)
                        .ToList()
                        .Select(te => string.Format("{0}: {1}\u00D7", te.Nickname, te.Count))
                        .ToList();
                }

                // mention that the list is truncated if there are more than MostGratefulCount entries
                var countString = (mostThankedStrings.Count <= Config.MostGratefulCount) ? "" : (" " + Config.MostGratefulCountText);
                statsString = string.Format(
                    " (Most thanked{0}: {1})",
                    countString,
                    mostThankedStrings.Take(Config.MostGratefulCount).StringJoin(", ")
                );
            }

            ConnectionManager.SendChannelMessageFormat(
                msg.Channel,
                "{0}: {1} has {2} until now.{3}",
                msg.SenderNickname,
                nickname,
                countPhrase,
                statsString
            );
        }

        protected virtual void HandleTopThankedCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            List<NicknameAndCount> top;
            using (var ctx = GetNewContext())
            {
                top = ctx.ThanksEntries
                    .Where(te => !te.Deleted)
                    .GroupBy(te => te.ThankeeLowercase, (thankee, thanksEntries) => new NicknameAndCount
                    {
                        Nickname = thankee,
                        Count = thanksEntries.Count()
                    })
                    .OrderByDescending(teg => teg.Count)
                    .Take(Config.MostThankedCount)
                    .ToList()
                ;
            }

            ConnectionManager.SendChannelMessageFormat(
                msg.Channel,
                "{0}: {1}",
                msg.SenderNickname,
                top.Select(NicknameAndCountString).StringJoin(", ")
            );
        }

        protected virtual void HandleTopGratefulCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            List<NicknameAndCount> top;
            using (var ctx = GetNewContext())
            {
                top = ctx.ThanksEntries
                    .Where(te => !te.Deleted)
                    .GroupBy(te => te.ThankerLowercase, (thanker, thanksEntries) => new NicknameAndCount
                    {
                        Nickname = thanker,
                        Count = thanksEntries.Count()
                    })
                    .OrderByDescending(teg => teg.Count)
                    .Take(Config.MostThankedCount)
                    .ToList()
                ;
            }

            ConnectionManager.SendChannelMessageFormat(
                msg.Channel,
                "{0}: {1}",
                msg.SenderNickname,
                top.Select(NicknameAndCountString).StringJoin(", ")
            );
        }

        protected virtual void HandleBaseNickChanged(object sender, BaseNickChangedEventArgs args)
        {
            var oldNickLower = args.OldBaseNick.ToLowerInvariant();
            var newNickLower = args.NewBaseNick.ToLowerInvariant();

            using (var ctx = GetNewContext())
            {
                var thanksEntries = ctx.ThanksEntries
                    .Where(te => te.ThankerLowercase == oldNickLower || te.ThankeeLowercase == oldNickLower);
                foreach (var thanksEntry in thanksEntries)
                {
                    if (thanksEntry.ThankerLowercase == oldNickLower)
                    {
                        if (thanksEntry.ThankeeLowercase == newNickLower)
                        {
                            // self-thanks are not allowed; remove
                            ctx.ThanksEntries.Remove(thanksEntry);
                        }
                        else
                        {
                            thanksEntry.ThankerLowercase = newNickLower;
                        }
                    }
                    else if (thanksEntry.ThankeeLowercase == oldNickLower)
                    {
                        if (thanksEntry.ThankerLowercase == newNickLower)
                        {
                            // self-thanks are not allowed; remove
                            ctx.ThanksEntries.Remove(thanksEntry);
                        }
                        else
                        {
                            thanksEntry.ThankeeLowercase = newNickLower;
                        }
                    }
                }
                ctx.SaveChanges();
            }
        }

        protected string NicknameAndCountString(NicknameAndCount nickAndCount)
        {
            var actualNickname = ConnectionManager.RegisteredNameForNick(nickAndCount.Nickname) ?? nickAndCount.Nickname;
            var noHighlightNickname = (actualNickname.Length < 2)
                ? actualNickname
                : actualNickname[0] + "\uFEFF" + actualNickname.Substring(1);
            return string.Format("{0}: {1}", noHighlightNickname, nickAndCount.Count);
        }

        protected ThanksContext GetNewContext()
        {
            var opts = SharpIrcBotUtil.GetContextOptions<ThanksContext>(Config);
            return new ThanksContext(opts);
        }
    }
}
