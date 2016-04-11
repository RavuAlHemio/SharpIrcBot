using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using Thanks.ORM;

namespace Thanks
{
    public class ThanksPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Regex ThankRegex = new Regex("^[ ]*!(?:thank|thanks|thx)[ ]+(?<force>--force[ ]+)?(?<thankee>[^ ]+)(?:[ ]+(?<reason>.+))?$");
        private static readonly Regex ThankedRegex = new Regex("^[ ]*!thanked[ ]+(?<raw>--raw[ ]+)?(?<thankee>[^ ]+)[ ]*$");

        protected IConnectionManager ConnectionManager { get; }
        protected ThanksConfig Config { get; set; }

        public ThanksPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new ThanksConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.BaseNickChanged += HandleBaseNickChanged;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new ThanksConfig(newConfig);
        }

        private void HandleChannelMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            try
            {
                ActuallyHandleChannelMessage(sender, args, flags);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        private void HandleBaseNickChanged(object sender, BaseNickChangedEventArgs args)
        {
            try
            {
                ActuallyHandleBaseNickChanged(sender, args);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling base nick change", exc);
            }
        }

        protected void ActuallyHandleChannelMessage(object sender, IrcEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var message = args.Data;
            if (message.Type != ReceiveType.ChannelMessage || message.Nick == ConnectionManager.MyNickname)
            {
                return;
            }

            var thankMatch = ThankRegex.Match(message.Message);
            if (thankMatch.Success)
            {
                var thankerNick = message.Nick;

                bool forceThanks = thankMatch.Groups["force"].Success;
                var thankeeNick = thankMatch.Groups["thankee"].Value;
                var reason = thankMatch.Groups["reason"].Value.Trim();

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
                            message.Channel,
                            "{0}: You can't use this unless you're logged in with NickServ.",
                            thankerNick
                        );
                        return;
                    }

                    thankee = ConnectionManager.RegisteredNameForNick(thankeeNick);
                    if (thankee == null)
                    {
                        ConnectionManager.SendChannelMessageFormat(
                            message.Channel,
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
                        message.Channel,
                        "You are so full of yourself, {0}.",
                        thankerNick
                    );
                    return;
                }

                Logger.DebugFormat("{0} thanks {1}", thanker, thankee);

                long thankedCount;
                using (var ctx = GetNewContext())
                {
                    var entry = new ThanksEntry
                    {
                        Channel = message.Channel,
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
                    message.Channel,
                    "{0}: Alright! By the way, {1} has been thanked {2} until now.",
                    message.Nick,
                    thankee,
                    (thankedCount == 1) ? "once" : (thankedCount + " times")
                );
                return;
            }

            var thankedMatch = ThankedRegex.Match(message.Message);
            if (thankedMatch.Success)
            {
                bool raw = thankedMatch.Groups["raw"].Success;
                var nickname = thankedMatch.Groups["thankee"].Value;
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
                        string.Join(", ", mostGratefulStrings.Take(Config.MostGratefulCount))
                    );
                }

                ConnectionManager.SendChannelMessageFormat(
                    message.Channel,
                    "{0}: {1} has {2} until now.{3}",
                    message.Nick,
                    nickname,
                    countPhrase,
                    statsString
                );
            }

            if (message.Message == "!topthanked")
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
                    message.Channel,
                    "{0}: {1}",
                    message.Nick,
                    string.Join(", ", top.Select(NicknameAndCountString))
                );
            }

            if (message.Message == "!topgrateful")
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
                    message.Channel,
                    "{0}: {1}",
                    message.Nick,
                    string.Join(", ", top.Select(NicknameAndCountString))
                );
            }
        }

        protected virtual void ActuallyHandleBaseNickChanged(object sender, BaseNickChangedEventArgs args)
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
            var conn = SharpIrcBotUtil.GetDatabaseConnection(Config);
            return new ThanksContext(conn);
        }
    }
}
