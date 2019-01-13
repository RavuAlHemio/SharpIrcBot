using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Sed.Parsing
{
    public class SubFactory : IReplacementFactory
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<SubFactory>();

        protected class SubFlags
        {
            public RegexOptions Options { get; set; }
            public int FirstMatch { get; set; }
            public bool ReplaceAll { get; set; }

            public SubFlags(RegexOptions options, int firstMatch, bool replaceAll)
            {
                Options = options;
                FirstMatch = firstMatch;
                ReplaceAll = replaceAll;
            }
        }

        public ITransformCommand Construct(GenericReplacementCommand command)
        {
            SubFlags subFlags = ParseSubFlags(command.Flags);
            if (subFlags == null)
            {
                // invalid flags
                Logger.LogInformation("invalid flags {Flags}", command.Flags);
                return null;
            }

            string replacementString = TransformReplacementString(command.NewString);
            Regex pattern;
            try
            {
                pattern = new Regex(command.OldString, subFlags.Options);
            }
            catch (ArgumentException)
            {
                // syntactic error in pattern
                Logger.LogInformation("syntactic error in pattern {Pattern}", command.OldString);
                return null;
            }

            return new SubCommand(
                pattern,
                replacementString,
                subFlags.FirstMatch,
                subFlags.ReplaceAll
            );
        }

        protected virtual SubFlags ParseSubFlags(string flags)
        {
            RegexOptions options = RegexOptions.None;
            int firstMatch = 0;
            bool replaceAll = false;

            bool readingNumber = false;
            var firstMatchBuilder = new StringBuilder();

            foreach (char c in flags)
            {
                if (c == '-')
                {
                    if (firstMatchBuilder.Length > 0)
                    {
                        // minus midway through a number => invalid
                        return null;
                    }
                    readingNumber = true;
                    firstMatchBuilder.Append(c);
                }
                else if (c >= '0' && c <= '9')
                {
                    if (!readingNumber && firstMatchBuilder.Length > 0)
                    {
                        // i123n456 => invalid
                        return null;
                    }
                    readingNumber = true;
                    firstMatchBuilder.Append(c);
                }
                else
                {
                    readingNumber = false;

                    switch (c)
                    {
                        case 'i':
                            options |= RegexOptions.IgnoreCase;
                            break;
                        case 'n':
                            options |= RegexOptions.ExplicitCapture;
                            break;
                        case 'x':
                            options |= RegexOptions.IgnorePatternWhitespace;
                            break;
                        case 'g':
                            replaceAll = true;
                            break;
                        default:
                            // invalid flag
                            return null;
                    }
                }
            }

            if (firstMatchBuilder.Length > 0)
            {
                if (!int.TryParse(
                    firstMatchBuilder.ToString(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture,
                    out firstMatch
                ))
                {
                    // invalid count
                    return null;
                }
            }

            return new SubFlags(options, firstMatch, replaceAll);
        }

        protected virtual string TransformReplacementString(string replacementStringSed)
        {
            var ret = new StringBuilder();

            bool escaping = false;
            foreach (char c in replacementStringSed)
            {
                if (c == '\\')
                {
                    if (escaping)
                    {
                        ret.Append(c);
                        escaping = false;
                    }
                    else
                    {
                        escaping = true;
                    }
                }
                else if (c == '$')
                {
                    ret.Append("$$");
                    escaping = false;
                }
                else if (c >= '0' && c <= '9' && escaping)
                {
                    // group reference
                    ret.Append('$');
                    ret.Append(c);
                    escaping = false;
                }
                else
                {
                    ret.Append(c);
                    escaping = false;
                }
            }

            return ret.ToString();
        }
    }
}
