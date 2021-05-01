using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.GrammarGen.AST;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.GrammarGen
{
    public class GrammarGenPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<GrammarGenPlugin>();

        protected GGConfig Config { get; set; }
        protected Random Random { get; }
        protected IConnectionManager ConnectionManager { get; }
        protected List<Command> Commands { get; }
        protected Dictionary<string, Grammar> Grammars { get; }

        public GrammarGenPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new GGConfig(config);
            Random = new Random();
            Commands = new List<Command>();
            Grammars = new Dictionary<string, Grammar>();

            ProcessConfig();
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new GGConfig(newConfig);
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
                    cmd, HandleGenerationCommand
                );
            }

            // clear grammars and commands
            Grammars.Clear();
            Commands.Clear();

            // load the grammars from the directory
            foreach (string path in Directory.GetFiles(Config.GrammarDir, "*.grammar", SearchOption.TopDirectoryOnly))
            {
                string definition;
                using (var reader = new StreamReader(path, StringUtil.Utf8NoBom))
                {
                    definition = reader.ReadToEnd();
                }

                string name = Path.GetFileNameWithoutExtension(path);

                // generate the built-in rules
                var builder = ImmutableDictionary.CreateBuilder<string, Rule>();
                builder["__IRC_channel"] = new Rule("__IRC_channel", new DynPropertyProduction("channel"));
                builder["__IRC_nick"] = new Rule("__IRC_nick", new DynPersonProduction());
                builder["__IRC_chosen_nick"] = new Rule("__IRC_chosen_nick", new DynPersonProduction(chosenPerson: true));
                var builtInRules = new Rulebook(builder.ToImmutable());

                // create and store the grammer
                var grammar = new Grammar(definition, name, builtInRules);
                Grammars[name] = grammar;

                // create, register and store the corresponding command
                var command = new Command(
                    CommandUtil.MakeNames(name),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        RestTaker.Instance // message
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                );

                ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                    command, HandleGenerationCommand
                );
                Commands.Add(command);
            }
        }

        protected virtual void HandleGenerationCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            Grammar grammar;
            if (!Grammars.TryGetValue(cmd.CommandName, out grammar))
            {
                return;
            }

            string message = ((string)cmd.Arguments[0]).Trim();

            var paramBuilder = ImmutableDictionary.CreateBuilder<string, object>();

            var messagePieces = message.Split(' ');
            var nonFlagPieces = new List<string>();
            bool flagsEnded = false;
            foreach (string piece in messagePieces)
            {
                if (flagsEnded)
                {
                    nonFlagPieces.Add(piece);
                }
                else if (piece == "--")
                {
                    flagsEnded = true;
                }
                else if (piece.StartsWith("--") && piece.Length > 2)
                {
                    paramBuilder[$"opt_{piece.Substring(2)}"] = true;
                }
                else if (piece.StartsWith("-") && piece.Length > 1)
                {
                    foreach (char opt in piece.Substring(1))
                    {
                        paramBuilder[$"opt_{opt}"] = true;
                    }
                }
                else
                {
                    nonFlagPieces.Add(piece);
                }
            }

            paramBuilder["channel"] = msg.Channel;
            paramBuilder["message"] = string.Join(" ", nonFlagPieces);
            paramBuilder["fullMessage"] = message;
            paramBuilder["nicknames"] = ConnectionManager.NicknamesInChannel(msg.Channel);

            string response = grammar.Generate(Random, paramBuilder.ToImmutable());
            ConnectionManager.SendChannelMessage(msg.Channel, response);
        }
    }
}
