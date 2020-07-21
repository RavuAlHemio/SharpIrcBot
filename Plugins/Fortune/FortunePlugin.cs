using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Fortune
{
    public class FortunePlugin : IPlugin, IReloadableConfiguration
    {
        protected IConnectionManager ConnectionManager { get; }
        protected FortuneConfig Config { get; set; }
        protected ImmutableDictionary<string, ImmutableArray<string>> CategoryToFortunes { get; set; }
        protected ImmutableArray<string> AllFortunes { get; set; }
        protected Random RNG { get; set; }

        public FortunePlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new FortuneConfig(config);

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("fortune"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        RestTaker.Instance // fortune type (optional)
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleFortuneCommand
            );

            CategoryToFortunes = ImmutableDictionary<string, ImmutableArray<string>>.Empty;
            AllFortunes = ImmutableArray<string>.Empty;
            RNG = new Random();

            ReloadFortunes();
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new FortuneConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            ReloadFortunes();
        }

        protected virtual void ReloadFortunes()
        {
            var byCategoryBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<string>>();
            var allBuilder = ImmutableArray.CreateBuilder<string>();
            foreach (string fileName in Directory.EnumerateFiles(Config.FortuneDirectory))
            {
                if (fileName.Contains("."))
                {
                    // probably a .dat file or something similar
                    continue;
                }

                string categoryName = Path.GetFileName(fileName);
                string fortuneText = File.ReadAllText(fileName, StringUtil.Utf8NoBom)
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n");
                string[] fortunes = fortuneText.Split(
                    new[] {"\n%\n"},
                    StringSplitOptions.RemoveEmptyEntries
                );

                byCategoryBuilder[categoryName] = ImmutableArray.CreateRange(fortunes);
                allBuilder.AddRange(fortunes);
            }

            CategoryToFortunes = byCategoryBuilder.ToImmutable();
            AllFortunes = allBuilder.ToImmutable();
        }

        protected virtual void HandleFortuneCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            string fortuneCategory = ((string)cmd.Arguments[0])?.Trim();

            ImmutableArray<string> fortunes;
            if (fortuneCategory != null)
            {
                if (!CategoryToFortunes.TryGetValue(fortuneCategory, out fortunes))
                {
                    string availableCategories = string.Join(
                        ", ",
                        CategoryToFortunes.Keys
                            .OrderBy(c => c)
                            .Where(c => Config.AllowedCategories == null || Config.AllowedCategories.Contains(c))
                    );

                    ConnectionManager.SendChannelMessage(
                        args.Channel,
                        $"{args.SenderNickname}: fortune category \"{fortuneCategory}\" not found; available categories are: {availableCategories}"
                    );
                    return;
                }

                if (Config.AllowedCategories != null && !Config.AllowedCategories.Contains(fortuneCategory))
                {
                    ConnectionManager.SendChannelMessage(
                        args.Channel,
                        $"{args.SenderNickname}: fortune category \"{fortuneCategory}\" has been disabled"
                    );
                    return;
                }
            }
            else
            {
                fortunes = AllFortunes;
            }

            if (fortunes.IsEmpty)
            {
                return;
            }

            int index = RNG.Next(fortunes.Length);
            ConnectionManager.SendChannelMessage(
                args.Channel,
                fortunes[index]
            );
            return;
        }
    }
}
