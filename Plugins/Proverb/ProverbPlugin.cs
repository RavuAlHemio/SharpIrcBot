using System;
using System.IO;
using System.Net.Http;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.Proverb
{
    public class ProverbPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<ProverbPlugin>();

        protected IConnectionManager ConnectionManager { get; set; }
        protected ProverbConfig Config { get; set; }

        public ProverbPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new ProverbConfig(config);

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("sprichwort"),
                    CommandUtil.NoOptions,
                    CommandUtil.NoArguments,
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleProverbCommand
            );
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new ProverbConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            // nothing currently
        }

        protected virtual void HandleProverbCommand(CommandMatch cmd, IChannelMessageEventArgs args)
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(Config.TimeoutSeconds)
            };

            using (var request = new HttpRequestMessage(HttpMethod.Get, Config.ProverbURI))
            {
                var htmlDoc = new HtmlDocument();

                using (var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).SyncWait())
                using (Stream responseStream = response.Content.ReadAsStreamAsync().SyncWait())
                {
                    htmlDoc.Load(responseStream);
                }

                string proverb = htmlDoc.DocumentNode
                    .SelectSingleNode(Config.NodeSelector)
                    ?.InnerText;

                if (proverb != null)
                {
                    ConnectionManager.SendChannelMessage(args.Channel, $"{args.SenderNickname}: {proverb}");
                }
            }
        }
    }
}
