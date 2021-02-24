using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Dereverse
{
    public class WordListDereversePlugin : IPlugin, IReloadableConfiguration
    {
        protected IConnectionManager ConnectionManager { get; }
        protected WordListDereverseConfig Config { get; set; }
        protected ImmutableHashSet<string> Words { get; set; }

        public WordListDereversePlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new WordListDereverseConfig(config);
            Words = ImmutableHashSet<string>.Empty;

            ConnectionManager.ChannelMessage += HandleChannelMessage;

            LoadWordlists();
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new WordListDereverseConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            LoadWordlists();
        }

        protected virtual void LoadWordlists()
        {
            var wordsBuilder = ImmutableHashSet.CreateBuilder<string>();
            foreach (string wordList in Config.WordListFiles)
            {
                using (var reader = new StreamReader(wordList, StringUtil.Utf8NoBom))
                {
                    string word;
                    for (;;)
                    {
                        word = reader.ReadLine();
                        if (word == null)
                        {
                            break;
                        }

                        word = NormalizeWord(word);
                        if (word == null)
                        {
                            continue;
                        }
                        wordsBuilder.Add(word);
                    }
                }
            }
            Words = wordsBuilder.ToImmutable();
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs e, MessageFlags flags)
        {
            if (!Config.Channels.Contains(e.Channel))
            {
                return;
            }

            // also react to banned users

            int messageWords = e.Message
                .Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => NormalizeWord(word))
                .Where(word => word != null)
                .Count(word => Words.Contains(word));

            string revMessage = string.Concat(e.Message.Reverse());
            int revMessageWords = revMessage
                .Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => NormalizeWord(word))
                .Where(word => word != null)
                .Count(word => Words.Contains(word));

            decimal revToForward = revMessageWords / (messageWords + 1.0m);
            if (revToForward >= Config.RatioThreshold)
            {
                // good enough
                ConnectionManager.SendChannelMessage(e.Channel, revMessage);
            }
        }

        protected virtual string NormalizeWord(string word)
        {
            // remove whitespace around the word
            word = word.Trim();
            if (word.Length == 0)
            {
                return null;
            }

            if (word.Contains(" "))
            {
                // skip (we split the input text on whitespace so this will never match)
                return null;
            }

            // lowercase
            word = word.ToLowerInvariant();

            // collect only those characters that are letters
            var wordBuilder = new StringBuilder();
            int i = 0;
            while (i < word.Length)
            {
                int val = char.ConvertToUtf32(word, i);
                if (char.IsLetter(word, i))
                {
                    wordBuilder.Append(char.ConvertFromUtf32(val));
                }

                i++;
                if (val > 0xFFFF)
                {
                    i++;
                }
            }

            if (wordBuilder.Length == 0)
            {
                return null;
            }
            return wordBuilder.ToString();
        }
    }
}
