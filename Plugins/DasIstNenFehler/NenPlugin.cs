using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using DasIstNenFehler.ORM;
using SharpIrcBot;
using SharpIrcBot.Events.Irc;

namespace DasIstNenFehler
{
    public class NenPlugin : IPlugin, IReloadableConfiguration
    {
        public static readonly Regex NenRegex = new Regex("(?i)\\bnen\\b", RegexOptions.Compiled);
        public static readonly Regex WordBoundaryRegex = new Regex("[\\W-[-]]", RegexOptions.Compiled);

        public static readonly ImmutableHashSet<char> GermanAlphabet = ImmutableHashSet.CreateRange("ßqwertzuiopüasdfghjklöäyxcvbnmQWERTZUIOPÜASDFGHJKLÖÄYXCVBNM");

        protected ImmutableDictionary<long, string> IDsToWords { get; set; }
        protected ImmutableDictionary<string, ImmutableList<Noun>> LowerWordToNouns { get; set; }
        protected ImmutableDictionary<string, ImmutableList<Adjective>> LowerWordToAdjectives { get; set; }

        protected IConnectionManager ConnectionManager { get; }
        protected NenConfig Config { get; set; }

        public NenPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new NenConfig(config);

            LoadWords();

            ConnectionManager.ChannelMessage += HandleChannelMessageOrAction;
            ConnectionManager.ChannelAction += HandleChannelMessageOrAction;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new NenConfig(newConfig);

            LoadWords();
        }

        protected virtual void LoadWords()
        {
            using (var ctx = GetNewContext())
            {
                IDsToWords = ImmutableDictionary.CreateRange(
                    ctx.Words.ToList().Select(kvp => CreateKVP(kvp.ID, kvp.WordString))
                );

                var wordsToNouns = new Dictionary<string, List<Noun>>();
                foreach (Noun noun in ctx.Nouns)
                {
                    string lowerWord = IDsToWords[noun.WordID].ToLowerInvariant();

                    List<Noun> existingNouns;
                    if (!wordsToNouns.TryGetValue(lowerWord, out existingNouns))
                    {
                        existingNouns = new List<Noun>();
                        wordsToNouns[lowerWord] = existingNouns;
                    }

                    existingNouns.Add(noun);
                }
                LowerWordToNouns = ImmutableDictionary.CreateRange(
                    wordsToNouns.Select(kvp => CreateKVP(kvp.Key, ImmutableList.CreateRange(kvp.Value)))
                );

                var wordsToAdjectives = new Dictionary<string, List<Adjective>>();
                foreach (Adjective adj in ctx.Adjectives)
                {
                    string lowerWord = IDsToWords[adj.WordID].ToLowerInvariant();

                    List<Adjective> existingAdjectives;
                    if (!wordsToAdjectives.TryGetValue(lowerWord, out existingAdjectives))
                    {
                        existingAdjectives = new List<Adjective>();
                        wordsToAdjectives[lowerWord] = existingAdjectives;
                    }

                    existingAdjectives.Add(adj);
                }
                LowerWordToAdjectives = ImmutableDictionary.CreateRange(
                    wordsToAdjectives.Select(kvp => CreateKVP(kvp.Key, ImmutableList.CreateRange(kvp.Value)))
                );
            }
        }

        protected virtual void HandleChannelMessageOrAction(object sender, IChannelMessageEventArgs e, MessageFlags flags)
        {
            var body = e.Message;

            if (!NenRegex.IsMatch(body))
            {
                // not interested
                return;
            }

            string[] pieces = WordBoundaryRegex
                .Split(body)
                .Where(s => s.Any(c => GermanAlphabet.Contains(c)))
                .ToArray();
            int nenIndex = -1;
            for (int i = 0; i < pieces.Length; ++i)
            {
                if (pieces[i].ToLowerInvariant() == "nen")
                {
                    nenIndex = i;
                    break;
                }
            }

            if (nenIndex == -1)
            {
                // wtf... k
                return;
            }

            // for all the following words...
            for (int i = nenIndex + 1; i < pieces.Length; ++i)
            {
                // "nen" is only valid for masculine singular accusative words
                bool correctUsage = false;
                bool potentiallyIncorrectUsage = false;

                // take the last part after a hyphen
                string lastHyphenatedChunk = pieces[i].Split('-').Last();
                if (lastHyphenatedChunk.Length == 0)
                {
                    // a lone hyphen or a prefix like "groß-"; don't bother
                    return;
                }

                // can it be considered a word?
                if (!lastHyphenatedChunk.All(c => GermanAlphabet.Contains(c)))
                {
                    // nope
                    return;
                }

                // find the word
                string searchWord = lastHyphenatedChunk;
                while (searchWord.Length > 0)
                {
                    ImmutableList<Noun> nouns;
                    ImmutableList<Adjective> adjectives;
                    if (!LowerWordToNouns.TryGetValue(searchWord.ToLowerInvariant(), out nouns))
                    {
                        nouns = null;
                    }
                    if (!LowerWordToAdjectives.TryGetValue(searchWord.ToLowerInvariant(), out adjectives))
                    {
                        adjectives = null;
                    }

                    string thisSearchWord = searchWord;
                    searchWord = searchWord.Substring(1);

                    if (nouns == null && adjectives == null)
                    {
                        // not found
                        continue;
                    }

                    // found it! is it a noun?
                    if (nouns != null)
                    {
                        // yes!

                        // is any of those nouns masculine singular accusative?
                        Noun correctNoun = nouns
                            .Where(n => n.Case == GrammaticalCase.Accusative)
                            .Where(n => n.Number == GrammaticalNumber.Singular)
                            .Where(n => n.Gender == GrammaticalGender.Masculine)
                            .FirstOrDefault()
                        ;
                        if (correctNoun != null)
                        {
                            correctUsage = true;

                            string baseWord = IDsToWords[correctNoun.BaseWordID];
                            NotifyUsers($"NenPlugin: {e.SenderNickname} in {e.Channel}: \"nen {lastHyphenatedChunk}\" trimmed to \"{thisSearchWord}\" matches noun {baseWord}");
                        }

                        // is any of them masculine singular nominative?
                        Noun potentiallyIncorrectNoun = nouns
                            .Where(n => n.Case == GrammaticalCase.Nominative)
                            .Where(n => n.Number == GrammaticalNumber.Singular)
                            .Where(n => n.Gender == GrammaticalGender.Masculine)
                            .FirstOrDefault()
                        ;
                        if (potentiallyIncorrectNoun != null)
                        {
                            potentiallyIncorrectUsage = true;
                        }
                    }

                    // is it an adjective?
                    if (adjectives != null)
                    {
                        // is any of those adjectives masculine singular accusative?
                        Adjective correctAdjective = adjectives
                            .Where(n => n.Case == GrammaticalCase.Accusative)
                            .Where(n => n.Number == GrammaticalNumber.Singular)
                            .Where(n => n.Gender == GrammaticalGender.Masculine)
                            .FirstOrDefault()
                        ;
                        if (correctAdjective != null)
                        {
                            correctUsage = true;

                            string baseWord = IDsToWords[correctAdjective.BaseWordID];
                            NotifyUsers($"NenPlugin: {e.SenderNickname} in {e.Channel}: \"nen {lastHyphenatedChunk}\" trimmed to \"{thisSearchWord}\" matches adjective {baseWord}");
                        }

                        // is any of them masculine singular nominative?
                        Adjective potentiallyIncorrectAdjective = adjectives
                            .Where(n => n.Case == GrammaticalCase.Nominative)
                            .Where(n => n.Number == GrammaticalNumber.Singular)
                            .Where(n => n.Gender == GrammaticalGender.Masculine)
                            .FirstOrDefault()
                        ;
                        if (potentiallyIncorrectAdjective != null)
                        {
                            potentiallyIncorrectUsage = true;
                        }
                    }

                    if (potentiallyIncorrectUsage)
                    {
                        if (!correctUsage)
                        {
                            NotifyUsers($"NenPlugin: {e.SenderNickname} in {e.Channel}: \"nen {lastHyphenatedChunk}\" is incorrect");
                        }

                        // correct usage
                        return;
                    }
                    else if (nouns.Count + adjectives.Count > 0)
                    {
                        NotifyUsers($"NenPlugin: {e.SenderNickname} in {e.Channel}: \"nen {lastHyphenatedChunk}\" might be incorrect");
                        return;
                    }
                }

                // possibly TODO: try matching the next word instead
                // for the time being, assume it's wrong
                NotifyUsers($"NenPlugin: {e.SenderNickname} in {e.Channel}: \"nen {lastHyphenatedChunk}\" is unknown");
                break;
            }
        }

        protected void NotifyUsers(string message)
        {
            foreach (string userToNotify in Config.UsersToNotify)
            {
                ConnectionManager.SendQueryNotice(userToNotify, message);
            }
        }

        private GermanWordsContext GetNewContext()
        {
            var opts = SharpIrcBotUtil.GetContextOptions<GermanWordsContext>(Config);
            return new GermanWordsContext(opts);
        }

        // {CreateKVP("one", 2)} is shorter than {new KeyValuePair<string, int>("one", 2)}
        protected static KeyValuePair<TKey, TValue> CreateKVP<TKey, TValue>(TKey key, TValue value)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }
}
