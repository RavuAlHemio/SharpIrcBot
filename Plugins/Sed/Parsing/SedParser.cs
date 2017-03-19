using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpIrcBot.Plugins.Sed.Parsing
{
    public class SedParser
    {
        protected readonly HashSet<char> Splitters = new HashSet<char>("!\"#$%&'*+,-./:;=?^_`|~");

        public virtual List<ReplacementSpec> ParseSubCommands(string message)
        {
            string trimmedMessage = message.Trim();

            // shortest possible command
            if (trimmedMessage.Length < "s/a//".Length)
            {
                // if we fail at this stage, it's probably not supposed to be a sed command
                return null;
            }
            if (trimmedMessage[0] != 's')
            {
                return null;
            }
            if (!Splitters.Contains(trimmedMessage[1]))
            {
                return null;
            }

            var subCommands = new List<SubCommand>();
            for (;;)
            {
                string rest;
                SubCommand subCommand = TakeSubCommand(trimmedMessage, out rest);
                if (subCommand == null)
                {
                    break;
                }

                // ensure that the string is getting shorter
                Debug.Assert(rest.Length < trimmedMessage.Length);

                subCommands.Add(subCommand);
                trimmedMessage = rest;
            }

            // probably is supposed to be a sed command but they are doing it wrong
            // return an empty list
            if (subCommands.Count == 0)
            {
                return new List<ReplacementSpec>();
            }

            var ret = new List<ReplacementSpec>(subCommands.Count);
            foreach (SubCommand subCommand in subCommands)
            {
                SubFlags subFlags = ParseSubFlags(subCommand.Flags);
                if (subFlags == null)
                {
                    // invalid flags
                    return new List<ReplacementSpec>();
                }

                string replacementString = TransformReplacementString(subCommand.ReplacementSed);
                Regex pattern;
                try
                {
                    pattern = new Regex(subCommand.PatternSed, subFlags.Options);
                }
                catch (ArgumentException)
                {
                    // syntactic error in pattern
                    return new List<ReplacementSpec>();
                }

                ret.Add(new ReplacementSpec
                {
                    Pattern = pattern,
                    Replacement = replacementString,
                    FirstMatch = subFlags.FirstMatch,
                    ReplaceAll = subFlags.ReplaceAll
                });
            }

            return ret;
        }

        protected virtual SubCommand TakeSubCommand(string command, out string rest)
        {
            string pattern = null;
            string replacement = null;
            char splitter = '\0';

            var state = ParserState.AwaitingCommand;
            bool escaping = false;
            var builder = new StringBuilder();

            // trim initial whitespace
            command = command.TrimStart();

            foreach (Tuple<char, int> tuple in command.Select(Tuple.Create<char, int>))
            {
                char c = tuple.Item1;
                int i = tuple.Item2;

                if (state == ParserState.AwaitingCommand)
                {
                    if (c == 's')
                    {
                        state = ParserState.AwaitingSeparatorAfterCommand;
                    }
                    else
                    {
                        // wrong command
                        rest = command;
                        return null;
                    }
                }
                else if (state == ParserState.AwaitingSeparatorAfterCommand)
                {
                    if (!Splitters.Contains(c))
                    {
                        // invalid splitter
                        rest = command;
                        return null;
                    }
                    splitter = c;
                    state = ParserState.AwaitingPattern;
                }
                else
                {
                    if (c == '\\')
                    {
                        if (escaping)
                        {
                            builder.Append("\\\\");
                            escaping = false;
                        }
                        else
                        {
                            escaping = true;
                        }
                    }
                    else if (c == splitter)
                    {
                        if (escaping)
                        {
                            builder.Append('\\');
                            builder.Append(c);
                            escaping = false;
                        }
                        else if (state == ParserState.AwaitingPattern)
                        {
                            pattern = builder.ToString();
                            builder.Clear();
                            state = ParserState.AwaitingReplacement;
                        }
                        else if (state == ParserState.AwaitingReplacement)
                        {
                            replacement = builder.ToString();
                            builder.Clear();
                            state = ParserState.AwaitingFlags;
                        }
                        else
                        {
                            // too many separators!
                            rest = command;
                            return null;
                        }
                    }
                    else if (state == ParserState.AwaitingFlags && char.IsWhiteSpace(c))
                    {
                        // we're done

                        // rest should include the current (whitespace) character!
                        rest = command.Substring(i);
                        return new SubCommand(pattern, replacement, builder.ToString());
                    }
                    else
                    {
                        if (escaping)
                        {
                            builder.Append('\\');
                            builder.Append(c);
                            escaping = false;
                        }
                        else
                        {
                            builder.Append(c);
                        }
                    }
                }
            }

            if (pattern == null || replacement == null)
            {
                // incomplete command!
                rest = command;
                return null;
            }

            // fell out of the loop: nothing left
            rest = "";

            return new SubCommand(pattern, replacement, builder.ToString());
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
                if (c >= '0' && c <= '9')
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
                    firstMatchBuilder.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out firstMatch
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
