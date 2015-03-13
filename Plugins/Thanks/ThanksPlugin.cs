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
    public class ThanksPlugin : IPlugin
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Regex ThankRegex = new Regex("^[ ]*!(?:thank|thanks|thx)[ ]+([^ ]+)[ ]*$");
        private static readonly Regex ThankedRegex = new Regex("^[ ]*!thanked[ ]+([^ ]+)[ ]*$");

        protected IrcClient Client;
        protected ThanksConfig Config;

        public ThanksPlugin(IrcClient client, JObject config)
        {
            Client = client;
            Config = new ThanksConfig(config);

            Client.OnChannelMessage += HandleChannelMessage;
        }

        private void HandleChannelMessage(object sender, IrcEventArgs args)
        {
            try
            {
                ActuallyHandleChannelMessage(sender, args);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        protected void ActuallyHandleChannelMessage(object sender, IrcEventArgs args)
        {
            var message = args.Data;
            if (message.Type != ReceiveType.ChannelMessage || message.Nick == Client.Nickname)
            {
                return;
            }

            var thankMatch = ThankRegex.Match(message.Message);
            if (thankMatch.Success)
            {
                var thanker = message.Nick;
                var thankerLower = thanker.ToLowerInvariant();
                var thankee = thankMatch.Groups[1].Value;
                var thankeeLower = thankee.ToLowerInvariant();

                if (thankeeLower == thankerLower)
                {
                    Client.SendMessage(SendType.Message, message.Channel, string.Format(
                        "You are so full of yourself, {0}.",
                        thanker
                    ));
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
                        Timestamp = DateTime.Now.ToUniversalTimeForDatabase()
                    };
                    ctx.ThanksEntries.Add(entry);
                    ctx.SaveChanges();

                    thankedCount = ctx.ThanksEntries.Count(te => te.ThankeeLowercase == thankeeLower);
                }

                Client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "{0}: Alright! By the way, {1} has been thanked {2} until now.",
                    message.Nick,
                    thankee,
                    (thankedCount == 1) ? "once" : (thankedCount + " times")
                ));
                return;
            }

            var thankedMatch = ThankedRegex.Match(message.Message);
            if (thankedMatch.Success)
            {
                var nickname = thankedMatch.Groups[1].Value;
                var lowerNickname = nickname.ToLowerInvariant();

                long thankedCount;
                using (var ctx = GetNewContext())
                {
                    thankedCount = ctx.ThanksEntries.Count(te => te.ThankeeLowercase == lowerNickname);
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
                            .Where(te => te.ThankeeLowercase == lowerNickname)
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

                Client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "{0}: {1} has {2} until now.{3}",
                    message.Nick,
                    nickname,
                    countPhrase,
                    statsString
                ));
            }

            if (message.Message == "!topthanked")
            {
                List<NicknameAndCount> top;
                using (var ctx = GetNewContext())
                {
                    top = ctx.ThanksEntries
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

                var topStrings = new List<string>();
                foreach (var thankeeAndCount in top)
                {
                    topStrings.Add(string.Format("{0}: {1}", thankeeAndCount.Nickname, thankeeAndCount.Count));
                }

                Client.SendMessage(SendType.Message, message.Channel, string.Format(
                    "{0}: {1}",
                    message.Nick,
                    string.Join(", ", topStrings)
                ));
            }
        }

        private ThanksContext GetNewContext()
        {
            var conn = SharpIrcBotUtil.GetDatabaseConnection(Config);
            return new ThanksContext(conn);
        }
    }
}
