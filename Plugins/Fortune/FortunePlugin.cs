using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Fortune
{
    public class FortunePlugin : IPlugin, IReloadableConfiguration
    {
        protected IConnectionManager ConnectionManager { get; }
        protected FortuneConfig Config { get; set; }
        protected ImmutableDictionary<string, ImmutableArray<string>> CategoryToFortunes { get; set; }
        protected ImmutableArray<string> AllFortunes { get; set; }
        protected Random RNG { get; set; }

        public FortunePlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new FortuneConfig(config);

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("fortune"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        RestTaker.Instance // fortune type (optional)
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleFortuneCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("forfortunetune"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        RestTaker.Instance // fortune type (optional)
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleForForTuneTuneCommand
            );

            CategoryToFortunes = ImmutableDictionary<string, ImmutableArray<string>>.Empty;
            AllFortunes = ImmutableArray<string>.Empty;
            RNG = new Random();

            ReloadFortunes();
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new FortuneConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            ReloadFortunes();
        }

        protected virtual void ReloadFortunes()
        {
            var byCategoryBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<string>>();
            var allBuilder = ImmutableArray.CreateBuilder<string>();
            foreach (string fileName in Directory.EnumerateFiles(Config.FortuneDirectory))
            {
                if (fileName.Contains("."))
                {
                    // probably a .dat file or something similar
                    continue;
                }

                string categoryName = Path.GetFileName(fileName);
                string fortuneText = File.ReadAllText(fileName, StringUtil.Utf8NoBom)
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n");
                List<string> fortunes = fortuneText.Split(
                    new[] {"\n%\n"},
                    StringSplitOptions.RemoveEmptyEntries
                ).ToList();

                if (Config.MaxChars.HasValue)
                {
                    fortunes.RemoveAll(f => f.Length > Config.MaxChars.Value);
                }
                if (Config.MaxLines.HasValue)
                {
                    fortunes.RemoveAll(f => f.Count(c => c == '\n') >= Config.MaxLines.Value);
                }

                if (fortunes.Count > 0)
                {
                    byCategoryBuilder[categoryName] = ImmutableArray.CreateRange(fortunes);
                }
                allBuilder.AddRange(fortunes);
            }

            CategoryToFortunes = byCategoryBuilder.ToImmutable();
            AllFortunes = allBuilder.ToImmutable();
        }

        protected virtual ImmutableArray<string>? GetFortunesForCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            string fortuneCategory = ((string)cmd.Arguments[0])?.Trim();

            if (!string.IsNullOrEmpty(fortuneCategory))
            {
                ImmutableArray<string> fortunes;
                if (!CategoryToFortunes.TryGetValue(fortuneCategory, out fortunes))
                {
                    string availableCategories = string.Join(
                        ", ",
                        CategoryToFortunes.Keys
                            .OrderBy(c => c)
                            .Where(c => Config.AllowedCategories == null || Config.AllowedCategories.Contains(c))
                    );

                    ConnectionManager.SendChannelMessage(
                        args.Channel,
                        $"{args.SenderNickname}: fortune category \"{fortuneCategory}\" not found; available categories are: {availableCategories}"
                    );
                    return null;
                }

                if (Config.AllowedCategories != null && !Config.AllowedCategories.Contains(fortuneCategory))
                {
                    ConnectionManager.SendChannelMessage(
                        args.Channel,
                        $"{args.SenderNickname}: fortune category \"{fortuneCategory}\" has been disabled"
                    );
                    return null;
                }

                return fortunes;
            }

            return AllFortunes;
        }

        protected virtual void HandleFortuneCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            var fortunes = GetFortunesForCommand(cmd, args);
            if (!fortunes.HasValue || fortunes.Value.IsEmpty)
            {
                return;
            }

            int index = RNG.Next(fortunes.Value.Length);
            ConnectionManager.SendChannelMessage(
                args.Channel,
                fortunes.Value[index]
            );
        }

        protected virtual void HandleForForTuneTuneCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            const int fortuneCount = 2;

            var fortunes = GetFortunesForCommand(cmd, args);
            if (!fortunes.HasValue || fortunes.Value.IsEmpty)
            {
                return;
            }

            // pick out multiple fortunes
            // make sure we have enough to choose from, even if that means duplicates
            var fortuneList = new List<string>();
            while (fortuneList.Count < fortuneCount)
            {
                fortuneList.AddRange(fortunes);
            }
            fortuneList.Shuffle(RNG);
            fortuneList.RemoveRange(fortuneCount, fortuneList.Count - fortuneCount);

            // split each fortune into lines
            List<string[]> fortuneLinesList = fortuneList
                .Select(fortune => fortune.Split('\n'))
                .ToList();

            // add the index of each fortune n times into the index list
            // where n is the number of its lines
            var fortuneIndexList = new List<int>();
            for (int i = 0; i < fortuneLinesList.Count; ++i)
            {
                foreach (string line in fortuneLinesList[i])
                {
                    fortuneIndexList.Add(i);
                }
            }
            fortuneIndexList.Shuffle(RNG);

            // get the enumerators for the line arrays
            List<IEnumerator<string>> lineEtorList = fortuneLinesList
                .Select(fortuneLines => ((IEnumerable<string>)fortuneLines).GetEnumerator())
                .ToList();
            var lines = new List<string>();
            foreach (int index in fortuneIndexList)
            {
                IEnumerator<string> etor = lineEtorList[index];
                bool moveSucceeded = etor.MoveNext();
                Trace.Assert(moveSucceeded, "fortune line enumerator ended earlier than expected");
                lines.Add(etor.Current);
            }

            foreach (string line in lines)
            {
                ConnectionManager.SendChannelMessage(args.Channel, line);
            }
        }
    }
}
