using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace SharpIrcBot.Plugins.Sed.Parsing
{
    public class TransposeFactory : IReplacementFactory
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<TransposeFactory>();

        public const int MaxRangeDifference = 128;

        public ITransformCommand Construct(GenericReplacementCommand command)
        {
            // currently, no flags are supported
            if (command.Flags.Length > 0)
            {
                Logger.LogInformation("we don't accept flags; got {Flags}", command.Flags);
                return null;
            }

            Dictionary<int, int> transpositionDictionary = ParseTranspositions(command.OldString, command.NewString);
            if (transpositionDictionary == null)
            {
                return null;
            }

            return new TransposeCommand(transpositionDictionary);
        }

        protected virtual Dictionary<int, int> ParseTranspositions(string fromString, string toString)
        {
            List<int> fromStringChars = new UnicodeStringBuilder(fromString).GetCharacterListCopy();
            List<int> toStringChars = new UnicodeStringBuilder(toString).GetCharacterListCopy();
            
            List<int> froms = ParseWithRanges(fromStringChars);
            if (froms == null)
            {
                return null;
            }

            List<int> tos = ParseWithRanges(toStringChars);
            if (tos == null)
            {
                return null;
            }

            if (froms.Count != tos.Count)
            {
                Logger.LogInformation(
                    "from characters ({FromCount}) and to characters ({ToCount}) differ in count",
                    froms.Count, tos.Count
                );
                return null;
            }

            var ret = new Dictionary<int, int>();
            for (int i = 0; i < froms.Count; ++i)
            {
                ret[froms[i]] = tos[i];
            }
            return ret;
        }

        protected virtual List<int> ParseWithRanges(IList<int> spec)
        {
            var ret = new List<int>();

            int i = 0;
            while (i < spec.Count)
            {
                int thisChar = spec[i];
                int? nextChar = (i < spec.Count - 1) ? (int?)spec[i+1] : null;
                int? nextButOneChar = (i < spec.Count - 2) ? (int?)spec[i+2] : null;

                if (thisChar == '\\')
                {
                    if (nextChar.HasValue)
                    {
                        // escaped character
                        // "\a"
                        ret.Add(nextChar.Value);

                        i += 2;
                    }
                    else
                    {
                        // open escape at the end of the string
                        Logger.LogInformation("open escape at end");
                        return null;
                    }
                }
                else if (nextChar == '-')
                {
                    if (nextButOneChar.HasValue)
                    {
                        // it's a range!
                        // "a-z"

                        if (nextButOneChar.Value < thisChar)
                        {
                            // except it's an invalid one, ffs
                            // "z-a"
                            Logger.LogInformation(
                                "character range from {FirstChar} to {LastChar} is inverted",
                                thisChar,
                                nextButOneChar.Value
                            );
                            return null;
                        }

                        if (nextButOneChar.Value - thisChar > MaxRangeDifference)
                        {
                            // this range is far too long
                            // "\u0000-\uFFFF"
                            Logger.LogInformation(
                                "character range from {FirstChar} to {LastChar} is greater than limit {MaxRangeDifference}",
                                thisChar,
                                nextButOneChar.Value,
                                MaxRangeDifference
                            );
                            return null;
                        }

                        for (int c = thisChar; c <= nextButOneChar.Value; ++c)
                        {
                            ret.Add(c);
                        }

                        // advance by three characters
                        i += 3;
                    }
                    else
                    {
                        // string ends before the range end; assume the "-" is meant as a singular character
                        // "a-"
                        ret.Add(thisChar);
                        ret.Add(nextChar.Value);
                        break;
                    }
                }
                else
                {
                    // "-ab" or "abc"

                    // just add the current character
                    ret.Add(thisChar);

                    // advance by one
                    ++i;
                }
            }

            return ret;
        }
    }
}
