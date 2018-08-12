using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using SharpIrcBot.Collections;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.Libraries.RegularExpressionReplacement;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Catchword
{
    public class CatchwordPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = LogUtil.LoggerFactory.CreateLogger<CatchwordPlugin>();

        protected CatchwordConfig Config { get; set; }
        protected Random Random { get; }
        protected IConnectionManager ConnectionManager { get; }
        protected List<Command> Commands { get; }
        protected Dictionary<string, List<(ReplacerRegex, decimal)>> CatchmentRegexes { get; }

        public CatchwordPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new CatchwordConfig(config);
            Random = new Random();
            Commands = new List<Command>();
            CatchmentRegexes = new Dictionary<string, List<(ReplacerRegex, decimal)>>();

            ProcessConfig();
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new CatchwordConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            ProcessConfig();
        }

        protected virtual void ProcessConfig()
        {
            // unregister commands
            foreach (Command cmd in Commands)
            {
                ConnectionManager.CommandManager.UnregisterChannelMessageCommandHandler(
                    cmd, HandleCatchwordCommand
                );
            }

            // clear regexes
            CatchmentRegexes.Clear();

            foreach (KeyValuePair<string, List<CatchwordConfig.Replacement>> catchment in Config.Catchments)
            {
                // register command
                string commandName = catchment.Key;
                var command = new Command(
                    CommandUtil.MakeNames(commandName),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        RestTaker.Instance // message
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                );

                ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                    command, HandleCatchwordCommand
                );
                Commands.Add(command);

                // store catchment regexes
                List<(ReplacerRegex, decimal)> regexes = catchment.Value
                    .Select(r => (new ReplacerRegex(r.Regex, r.ReplacementString), r.SkipChancePercent))
                    .ToList();
                CatchmentRegexes[commandName] = regexes;
            }
        }

        protected virtual void HandleCatchwordCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            List<(ReplacerRegex, decimal)> replacements;
            if (!CatchmentRegexes.TryGetValue(cmd.CommandName, out replacements))
            {
                return;
            }

            string message = ((string)cmd.Arguments[0]).Trim();

            int messageIndex = 0;
            var ret = new StringBuilder();
            for (;;)
            {
                Match firstMatch = null;
                ReplacerRegex firstMatchRegex = null;
                foreach ((ReplacerRegex repl, decimal skipChancePercent) in replacements)
                {
                    Match m = repl.Regex.Match(message, messageIndex);
                    if (!m.Success)
                    {
                        continue;
                    }

                    Logger.LogDebug("matched {Regex} at {Index}", repl.Regex, m.Index);

                    // RNG skippage
                    if (skipChancePercent > 0.0m)
                    {
                        decimal skipValue = (decimal)(100 * Random.NextDouble());
                        if (skipValue < skipChancePercent)
                        {
                            Logger.LogDebug("skipValue {SkipValue} < skipChance {SkipChance}; skipping", skipValue, skipChancePercent);
                            continue;
                        }
                        else
                        {
                            Logger.LogDebug("skipValue {SkipValue} >= skipChance {SkipChance}; continuing", skipValue, skipChancePercent);
                        }
                    }

                    if (firstMatch == null || firstMatch.Index > m.Index)
                    {
                        firstMatch = m;
                        firstMatchRegex = repl;
                        Logger.LogDebug("new first match elected");
                    }
                }

                if (firstMatch == null)
                {
                    // we're done; copy the rest
                    ret.Append(message, messageIndex, message.Length - messageIndex);
                    Logger.LogDebug("done; ret is now {Ret}", ret);
                    break;
                }

                Logger.LogDebug("first match is {Match} matching {Regex} at {Index}", firstMatch.Value, firstMatchRegex.Regex, firstMatch.Index);

                // copy verbatim between messageIndex and index of firstMatch
                ret.Append(message, messageIndex, firstMatch.Index - messageIndex);
                Logger.LogDebug("adding verbatim chunk; ret is now {Ret}", ret);

                // replace only within the matched string
                string replacedChunk = firstMatchRegex.Replace(firstMatch.Value);

                // append that too
                ret.Append(replacedChunk);
                Logger.LogDebug("adding replaced chunk; ret is now {Ret}", ret);

                // walk foward
                messageIndex = firstMatch.Index + firstMatch.Length;
                Logger.LogDebug("walked forth to {Index}; to process: {MessageRest}", messageIndex, message.Substring(messageIndex));
            }

            ConnectionManager.SendChannelMessage(msg.Channel, ret.ToString());
        }
    }
}
