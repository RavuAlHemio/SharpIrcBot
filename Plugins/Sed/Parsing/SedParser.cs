using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Sed.Parsing
{
    public class SedParser
    {
        private static readonly ILogger Logger = LogUtil.LoggerFactory.CreateLogger<SedParser>();

        protected readonly HashSet<char> Splitters = new HashSet<char>("!\"#$%&'*+,-./:;=?^_`|~");
        protected readonly Dictionary<string, IReplacementFactory> CommandsToFactories
                = new Dictionary<string, IReplacementFactory>
        {
            ["s"] = new SubFactory(),
            ["tr"] = new TransposeFactory()
        };

        public virtual List<ITransformCommand> ParseSubCommands(string message)
        {
            string trimmedMessage = message.Trim();

            // shortest possible command
            if (trimmedMessage.Length < "s/a//".Length)
            {
                // too short
                // (if we fail at this stage, it's probably not supposed to be a sed command)
                return null;
            }
            if (trimmedMessage.Count(c => Splitters.Contains(c)) < 2)
            {
                // not enough splitter characters: not a command
                return null;
            }
            if (Splitters.Max(splitter => trimmedMessage.Count(c => c == splitter)) < 2)
            {
                // not enough of the same splitter character: not a command
                return null;
            }

            var replacementCommands = new List<GenericReplacementCommand>();
            for (;;)
            {
                string rest;
                bool invalidCommand;
                GenericReplacementCommand subCommand = TakeReplacementCommand(trimmedMessage, out rest, out invalidCommand);

                if (subCommand == null)
                {
                    if (invalidCommand)
                    {
                        // assume it's not supposed to be a sed command
                        return null;
                    }
                    else
                    {
                        // assume it's a syntactically incorrect sed command
                        break;
                    }
                }
                else if (subCommand.Flags == null)
                {
                    // no flags: assume syntactically incorrect sed command as well
                    break;
                }

                // ensure that the string is getting shorter
                Debug.Assert(rest.Length < trimmedMessage.Length);

                replacementCommands.Add(subCommand);
                trimmedMessage = rest;
            }

            // probably is supposed to be a sed command but they are doing it wrong
            // return an empty list
            if (replacementCommands.Count == 0)
            {
                Logger.LogInformation("already the first replacement command was invalid in {ReplacementsString}", trimmedMessage);
                return new List<ITransformCommand>();
            }

            var ret = new List<ITransformCommand>(replacementCommands.Count);
            foreach (GenericReplacementCommand replacementCommand in replacementCommands)
            {
                IReplacementFactory factory;
                if (!CommandsToFactories.TryGetValue(replacementCommand.Command, out factory))
                {
                    return new List<ITransformCommand>();
                }

                ITransformCommand command = factory.Construct(replacementCommand);
                if (command == null)
                {
                    return new List<ITransformCommand>();
                }

                ret.Add(command);
            }

            return ret;
        }

        protected virtual GenericReplacementCommand TakeReplacementCommand(string fullCommand, out string rest, out bool invalidCommand)
        {
            string command = null;
            string pattern = null;
            string replacement = null;
            char splitter = '\0';

            var state = ParserState.AwaitingCommand;
            bool escaping = false;
            var builder = new StringBuilder();

            // trim initial whitespace
            fullCommand = fullCommand.TrimStart();

            invalidCommand = false;

            foreach (Tuple<char, int> tuple in fullCommand.Select(Tuple.Create<char, int>))
            {
                char c = tuple.Item1;
                int i = tuple.Item2;

                if (state == ParserState.AwaitingCommand)
                {
                    if (c >= 'a' && c <= 'z')
                    {
                        builder.Append(c);
                    }
                    else if (Splitters.Contains(c))
                    {
                        splitter = c;
                        command = builder.ToString();
                        builder.Clear();

                        if (!CommandsToFactories.ContainsKey(command))
                        {
                            // unknown command
                            invalidCommand = true;
                            rest = fullCommand;
                            return null;
                        }

                        state = ParserState.AwaitingPattern;
                    }
                    else
                    {
                        // obviously not a command
                        invalidCommand = true;
                        rest = fullCommand;
                        return null;
                    }
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
                            rest = fullCommand;
                            return null;
                        }
                    }
                    else if (state == ParserState.AwaitingFlags && char.IsWhiteSpace(c))
                    {
                        // we're done

                        // rest should include the current (whitespace) character!
                        rest = fullCommand.Substring(i);
                        return new GenericReplacementCommand(command, pattern, replacement, builder.ToString());
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

            if (command == null || pattern == null)
            {
                // incomplete command!
                rest = fullCommand;
                return null;
            }

            // fell out of the loop: nothing left
            rest = "";

            // allow null flags; let the caller take care of it
            if (replacement == null)
            {
                return new GenericReplacementCommand(command, pattern, builder.ToString(), null);
            }

            return new GenericReplacementCommand(command, pattern, replacement, builder.ToString());
        }
    }
}
