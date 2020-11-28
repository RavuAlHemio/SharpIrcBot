using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;

namespace SharpIrcBot.Plugins.WordGen
{
    public class WordGenPlugin : IPlugin, IReloadableConfiguration
    {
        protected WordGenConfig Config { get; set; }
        protected IConnectionManager ConnectionManager { get; }
        protected Random RNG { get; set; }
        protected long LongestWordLength { get; set; }

        public WordGenPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new WordGenConfig(config);
            RNG = new Random();
            LongestWordLength = 0;

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("wordgen"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        RestTaker.Instance // substring
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleWordGenCommand
            );

            UpdateLongestWordLength();
        }

        private void HandleWordGenCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            string substr = ((string)cmd.Arguments[0]).Trim();

            if (substr.Length > LongestWordLength)
            {
                ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: that's longer than the longest word");
                return;
            }

            string foundWord;
            var wordPieces = new List<string>(Config.Rings.Count);
            var stopwatch = Stopwatch.StartNew();
            do
            {
                if (Config.MaxDurationSeconds.HasValue && stopwatch.Elapsed > TimeSpan.FromSeconds(Config.MaxDurationSeconds.Value))
                {
                    ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: sorry, can't find it :-(");
                    return;
                }

                wordPieces.Clear();
                foreach (List<string> ring in Config.Rings)
                {
                    int i = RNG.Next(ring.Count);
                    wordPieces.Add(ring[i]);
                }
                foundWord = string.Concat(wordPieces);
            } while (!foundWord.Contains(substr));

            ConnectionManager.SendChannelMessage(msg.Channel, $"{msg.SenderNickname}: {foundWord}");
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new WordGenConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            UpdateLongestWordLength();
        }

        protected void UpdateLongestWordLength()
        {
            // find the longest word
            int longestLength = 0;
            foreach (List<string> ring in Config.Rings)
            {
                int ringMax = ring.Select(piece => (int?)piece.Length)
                    .Max() ?? 0;
                longestLength += ringMax;
            }
            LongestWordLength = longestLength;
        }
    }
}
