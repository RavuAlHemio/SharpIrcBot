using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Events.Irc;

namespace SharpIrcBot.Plugins.Sed
{
    public class SedPlugin : IPlugin, IReloadableConfiguration
    {
        protected readonly HashSet<char> Splitters = new HashSet<char>("!\"#$%&'*+,-./:;=?^_`|~");

        protected IConnectionManager ConnectionManager { get; }
        protected SedConfig Config { get; set; }

        protected Dictionary<string, List<string>> ChannelToLastBodies { get; set; }

        public SedPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new SedConfig(config);

            ChannelToLastBodies = new Dictionary<string, List<string>>();

            ConnectionManager.ChannelMessage += HandleChannelMessage;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new SedConfig(newConfig);
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs e, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            if (HandleReplacementCommand(e))
            {
                return;
            }

            // remember?
            List<string> lastBodies;
            if (!ChannelToLastBodies.TryGetValue(e.Channel, out lastBodies))
            {
                lastBodies = new List<string>();
                ChannelToLastBodies[e.Channel] = lastBodies;
            }

            lastBodies.Insert(0, e.Message);
            while (lastBodies.Count > Config.RememberLastMessages && lastBodies.Count > 0)
            {
                lastBodies.RemoveAt(lastBodies.Count - 1);
            }
        }

        protected virtual bool HandleReplacementCommand(IChannelMessageEventArgs e)
        {
            string command = e.Message.Trim();

            // shortest possible command
            if (command.Length < "s/a//".Length)
            {
                return false;
            }
            if (command[0] != 's')
            {
                return false;
            }

            char splitter = command[1];
            if (!Splitters.Contains(splitter))
            {
                return false;
            }

            // from here on, we might come across an invalid sed command that is very similar to a valid one
            // return true so that it is not remembered (and the user can try again)

            SubCommand subCommand = ParseSubCommand(command, splitter);
            if (subCommand == null)
            {
                // not a valid sed command, but very similar to one, so don't remember it
                return true;
            }

            SubFlags subFlags = ParseSubFlags(subCommand.Flags);
            if (subFlags == null)
            {
                return true;
            }

            string replacement = TransformReplacementString(subCommand.ReplacementSed);
            if (replacement == null)
            {
                return true;
            }

            Regex regex;
            try
            {
                regex = new Regex(subCommand.PatternSed, subFlags.Options);
            }
            catch (ArgumentException)
            {
                // parsing failed; never mind
                return true;
            }

            // find the message to perform a replacement in
            List<string> lastBodies;
            if (!ChannelToLastBodies.TryGetValue(e.Channel, out lastBodies))
            {
                // no last bodies for this channel; never mind
                return true;
            }

            foreach (string lastBody in lastBodies)
            {
                int matchIndex = -1;
                string replaced = regex.Replace(lastBody, match =>
                {
                    ++matchIndex;

                    if (matchIndex < subFlags.FirstMatch)
                    {
                        // unchanged
                        return match.Value;
                    }

                    if (matchIndex > subFlags.FirstMatch && !subFlags.ReplaceAll)
                    {
                        // unchanged
                        return match.Value;
                    }

                    return match.Result(replacement);
                });

                if (replaced != lastBody)
                {
                    // success!
                    ConnectionManager.SendChannelMessage(e.Channel, replaced);
                    break;
                }
            }

            return true;
        }

        protected virtual SubCommand ParseSubCommand(string command, char splitter)
        {
            string pattern = null;
            string replacement = null;
            bool escaping = false;
            var builder = new StringBuilder();
            foreach (char c in command.Substring(2))
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
                    else if (pattern == null)
                    {
                        pattern = builder.ToString();
                        builder.Clear();
                    }
                    else if (replacement == null)
                    {
                        replacement = builder.ToString();
                        builder.Clear();
                    }
                    else
                    {
                        // too many separators!
                        return null;
                    }
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

            if (pattern == null || replacement == null)
            {
                // incomplete command!
                return null;
            }

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
