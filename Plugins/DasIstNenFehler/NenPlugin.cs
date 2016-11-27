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
        public static readonly Regex WordBoundaryRegex = new Regex("\\b", RegexOptions.Compiled);

        public static readonly ImmutableHashSet<char> GermanAlphabet = ImmutableHashSet.CreateRange("ßqwertzuiopüasdfghjklöäyxcvbnmQWERTZUIOPÜASDFGHJKLÖÄYXCVBNM");

        protected IConnectionManager ConnectionManager { get; }
        protected NenConfig Config { get; set; }

        public NenPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new NenConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelMessageOrAction;
            ConnectionManager.ChannelAction += HandleChannelMessageOrAction;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new NenConfig(newConfig);
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
                using (var ctx = GetNewContext())
                {
                    string searchWord = lastHyphenatedChunk;
                    Word word;
                    while (searchWord.Length > 0)
                    {
                        word = ctx.Words
                            .FirstOrDefault(w => w.WordString.ToLowerInvariant() == searchWord.ToLowerInvariant());
                        searchWord = searchWord.Substring(1);

                        if (word == null)
                        {
                            // not found
                            continue;
                        }

                        // found it! is it a noun?
                        List<Noun> nouns = ctx.Nouns
                            .Where(w => w.WordID == word.ID)
                            .ToList();
                        if (nouns.Count != 0)
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

                                string baseWord = ctx.Words.FirstOrDefault(w => w.ID == correctNoun.BaseWordID).WordString;
                                NotifyUsers($"{e.SenderNickname} in {e.Channel}: \"nen {lastHyphenatedChunk}\" trimmed to \"{word.WordString}\" matches noun {baseWord}");
                            }
                        }

                        // is it an adjective?
                        List<Adjective> adjs = ctx.Adjectives
                            .Where(w => w.WordID == word.ID)
                            .ToList();
                        if (adjs.Count != 0)
                        {
                            // is any of those adjectives masculine singular accusative?
                            Adjective correctAdjective = adjs
                                .Where(n => n.Case == GrammaticalCase.Accusative)
                                .Where(n => n.Number == GrammaticalNumber.Singular)
                                .Where(n => n.Gender == GrammaticalGender.Masculine)
                                .FirstOrDefault()
                            ;
                            if (correctAdjective != null)
                            {
                                correctUsage = true;

                                string baseWord = ctx.Words.FirstOrDefault(w => w.ID == correctAdjective.BaseWordID).WordString;
                                NotifyUsers($"{e.SenderNickname} in {e.Channel}: \"nen {lastHyphenatedChunk}\" trimmed to \"{word.WordString}\" matches adjective {baseWord}");
                            }
                        }

                        if (correctUsage)
                        {
                            // we're done
                            return;
                        }
                        else if (nouns.Count + adjs.Count > 0)
                        {
                            NotifyUsers($"{e.SenderNickname} in {e.Channel}: \"nen {lastHyphenatedChunk}\" found but unmatched");
                            return;
                        }
                    }

                    // possibly TODO: try matching the next word instead
                    // for the time being, assume it's wrong
                    NotifyUsers($"{e.SenderNickname} in {e.Channel}: \"nen {lastHyphenatedChunk}\" utterly unmatched");
                    break;
                }
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
    }
}
