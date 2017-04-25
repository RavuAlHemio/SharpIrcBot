using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SharpIrcBot.Commands
{
    public class MultiMatchTaker : IArgumentTaker, IEquatable<MultiMatchTaker>
    {
        public const string DefaultSplitterPattern = "\\s+";
        public static readonly Regex DefaultSplitterRegex = new Regex(DefaultSplitterPattern, RegexOptions.Compiled);

        public Regex MatchRegex { get; }
        public Regex SplitterRegex { get; }
        public int MinimumMatchCount { get; }

        public MultiMatchTaker(string matchPattern, string splitterPattern = DefaultSplitterPattern, int minimumMatchCount = 0)
        {
            MatchRegex = new Regex(matchPattern, RegexOptions.Compiled);
            SplitterRegex = new Regex(splitterPattern, RegexOptions.Compiled);
            MinimumMatchCount = minimumMatchCount;
        }

        public MultiMatchTaker(Regex matchRegex, Regex splitterRegex, int minimumMatchCount = 0)
        {
            MatchRegex = matchRegex;
            SplitterRegex = splitterRegex;
            MinimumMatchCount = minimumMatchCount;
        }

        public string Take(string input, out object value)
        {
            input = input.TrimStart();

            string inputRest = input;
            var matches = new List<Match>();
            for (;;)
            {
                if (matches.Count > 0)
                {
                    // match splitter (first)
                    Match splitterMatch = SplitterRegex.Match(inputRest);
                    if (!splitterMatch.Success || splitterMatch.Index != 0)
                    {
                        // we're done
                        if (matches.Count >= MinimumMatchCount)
                        {
                            value = matches;
                            return inputRest;
                        }
                        else
                        {
                            value = null;
                            return null;
                        }
                    }

                    inputRest = inputRest.Substring(splitterMatch.Length);
                }

                // match match
                Match match = MatchRegex.Match(inputRest);
                if (!match.Success || match.Index != 0)
                {
                    // we're done
                    if (matches.Count >= MinimumMatchCount)
                    {
                        value = matches;
                        return inputRest;
                    }
                    else
                    {
                        value = null;
                        return null;
                    }
                }

                matches.Add(match);
                inputRest = inputRest.Substring(match.Length);
            }
        }

        public bool Equals(MultiMatchTaker other)
        {
            if (other == null)
            {
                return false;
            }

            return
                this.MatchRegex.ToString() == other.MatchRegex.ToString()
                && this.MatchRegex.Options == other.MatchRegex.Options
                && this.MatchRegex.MatchTimeout == other.MatchRegex.MatchTimeout
                && this.SplitterRegex.ToString() == other.SplitterRegex.ToString()
                && this.SplitterRegex.Options == other.SplitterRegex.Options
                && this.SplitterRegex.MatchTimeout == other.SplitterRegex.MatchTimeout
                && this.MinimumMatchCount == other.MinimumMatchCount
            ;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (this.GetType() != obj.GetType())
            {
                return false;
            }

            return this.Equals((MultiMatchTaker)obj);
        }

        public override int GetHashCode()
        {
            return unchecked(
                103 * this.MatchRegex.ToString().GetHashCode()
                + 139 * this.MatchRegex.Options.GetHashCode()
                + 239 * this.MatchRegex.MatchTimeout.GetHashCode()
                + 367 * this.SplitterRegex.ToString().GetHashCode()
                + 383 * this.SplitterRegex.Options.GetHashCode()
                + 487 * this.SplitterRegex.MatchTimeout.GetHashCode()
                + 563 * this.MinimumMatchCount.GetHashCode()
            );
        }
    }
}
