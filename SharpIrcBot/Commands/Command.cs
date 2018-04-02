using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using SharpIrcBot.Config;

namespace SharpIrcBot.Commands
{
    public class Command : IEquatable<Command>
    {
        public ImmutableList<string> CommandNames { get; }
        public ImmutableList<KeyValuePair<string, IArgumentTaker>> Options { get; }
        public ImmutableList<IArgumentTaker> Arguments { get; }
        public MessageFlags RequiredFlags { get; }
        public MessageFlags ForbiddenFlags { get; }

        public Command(
            IEnumerable<string> commandNames = null,
            IEnumerable<KeyValuePair<string, IArgumentTaker>> options = null,
            IEnumerable<IArgumentTaker> arguments = null,
            MessageFlags requiredFlags = MessageFlags.None,
            MessageFlags forbiddenFlags = MessageFlags.None
        )
        {
            CommandNames = (commandNames == null)
                ? ImmutableList.Create<string>()
                : ImmutableList.ToImmutableList(commandNames);
            Options = (options == null)
                ? ImmutableList.Create<KeyValuePair<string, IArgumentTaker>>()
                : ImmutableList.ToImmutableList(options);
            Arguments = (arguments == null)
                ? ImmutableList.Create<IArgumentTaker>()
                : ImmutableList.ToImmutableList(arguments);
            RequiredFlags = requiredFlags;
            ForbiddenFlags = forbiddenFlags;
        }

        public CommandMatch Match(CommandConfig config, string commandAndArgs, MessageFlags messageFlags)
        {
            if ((messageFlags & RequiredFlags) != RequiredFlags)
            {
                // one or more required flags not set
                return null;
            }

            if ((messageFlags & ForbiddenFlags) != 0)
            {
                // one or more forbidden flags set
                return null;
            }

            string commandMatched = null;
            foreach (string commandName in CommandNames)
            {
                if (!commandAndArgs.StartsWith(commandName))
                {
                    // not this command
                    continue;
                }

                if (commandAndArgs.Length == commandName.Length)
                {
                    // this command without arguments
                    commandMatched = commandName;
                    break;
                }

                if (char.IsWhiteSpace(commandAndArgs, commandName.Length))
                {
                    // this command followed by whitespace
                    commandMatched = commandName;
                    break;
                }
            }

            if (commandMatched == null)
            {
                // not this command
                return null;
            }

            string argumentsToGo = commandAndArgs.Substring(commandMatched.Length);

            bool keepTakingOptions = true;
            ImmutableList<KeyValuePair<string, object>>.Builder seenOptions =
                ImmutableList.CreateBuilder<KeyValuePair<string, object>>();
            ImmutableList<object>.Builder args = ImmutableList.CreateBuilder<object>();

            for (;;)
            {
                bool optionMatched = false;
                if (keepTakingOptions)
                {
                    string startTrimmedToGo = argumentsToGo.TrimStart();

                    // forced end of options?
                    if (startTrimmedToGo.StartsWith("--"))
                    {
                        if (startTrimmedToGo.Length == 2 || char.IsWhiteSpace(startTrimmedToGo, 2))
                        {
                            // lone "--" option; stop taking options
                            keepTakingOptions = false;
                            argumentsToGo = startTrimmedToGo.Substring(2);
                            continue;
                        }
                    }

                    // options
                    foreach (KeyValuePair<string, IArgumentTaker> option in Options)
                    {
                        bool thisOption = false;
                        if (startTrimmedToGo.StartsWith(option.Key))
                        {
                            // might be this option
                            if (startTrimmedToGo.Length == option.Key.Length)
                            {
                                // it is, as the last option
                                thisOption = true;
                            }
                            else
                            {
                                Debug.Assert(startTrimmedToGo.Length > option.Key.Length);
                                if (startTrimmedToGo[option.Key.Length] == '=' || char.IsWhiteSpace(startTrimmedToGo, option.Key.Length))
                                {
                                    // it is, followed by a space or an equals sign
                                    thisOption = true;
                                }
                            }
                        }

                        if (!thisOption)
                        {
                            // not this one
                            continue;
                        }

                        // attempt to match the value
                        // (the equals sign, if it follows, is sent to the taker!)
                        object optValue;
                        string optRest = option.Value.Take(startTrimmedToGo.Substring(option.Key.Length), out optValue);
                        if (optRest == null)
                        {
                            // the option name matched but its argument didn't
                            continue;
                        }

                        // matched!
                        seenOptions.Add(new KeyValuePair<string, object>(option.Key, optValue));
                        argumentsToGo = optRest;
                        optionMatched = true;
                        break;
                    }
                }

                if (optionMatched)
                {
                    continue;
                }

                // argument?
                IArgumentTaker upcomingArgumentTaker = Arguments
                    .Skip(args.Count)
                    .FirstOrDefault();
                if (upcomingArgumentTaker == null)
                {
                    // there should be no more arguments
                    if (argumentsToGo.Trim().Length > 0)
                    {
                        // but there are; it's not this command
                        return null;
                    }

                    // good; let's get out of this loop
                    break;
                }

                object argValue;
                string argRest = upcomingArgumentTaker.Take(argumentsToGo, out argValue);
                if (argRest == null)
                {
                    // taking the argument failed; it's not this command
                    return null;
                }
                args.Add(argValue);
                argumentsToGo = argRest;
            }

            // command parsed successfully
            return new CommandMatch(
                this,
                commandMatched,
                seenOptions.ToImmutable(),
                args.ToImmutable(),
                messageFlags
            );
        }

        public bool Equals(Command other)
        {
            if (!this.CommandNames.SequenceEqual(other.CommandNames))
            {
                return false;
            }

            if (this.Options.Count != other.Options.Count)
            {
                return false;
            }
            for (int i = 0; i < this.Options.Count; ++i)
            {
                if (this.Options[i].Key != other.Options[i].Key)
                {
                    return false;
                }

                if (this.Options[i].Value != other.Options[i].Value)
                {
                    return false;
                }
            }

            if (!this.Arguments.SequenceEqual(other.Arguments))
            {
                return false;
            }

            if (this.RequiredFlags != other.RequiredFlags)
            {
                return false;
            }

            if (this.ForbiddenFlags != other.ForbiddenFlags)
            {
                return false;
            }

            return true;
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

            return this.Equals((Command)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;
                foreach (string commandName in CommandNames)
                {
                    hashCode = (hashCode * 397) ^ commandName.GetHashCode();
                }
                foreach (KeyValuePair<string, IArgumentTaker> option in Options)
                {
                    hashCode = (hashCode * 397) ^ option.Key.GetHashCode();
                    hashCode = (hashCode * 397) ^ option.Value.GetHashCode();
                }
                foreach (IArgumentTaker argument in Arguments)
                {
                    hashCode = (hashCode * 397) ^ argument.GetHashCode();
                }
                hashCode = (hashCode * 397) ^ RequiredFlags.GetHashCode();
                hashCode = (hashCode * 397) ^ ForbiddenFlags.GetHashCode();
                return hashCode;
            }
        }
    }
}
