using System;
using System.Linq;
using System.Reflection;
using log4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using SharpIrcBot.Events.Irc;

namespace Punt
{
    public class PuntPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected IConnectionManager ConnectionManager { get; }
        protected PuntConfig Config { get; set; }
        protected Random Randomizer { get; }

        public PuntPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new PuntConfig(config);
            Randomizer = new Random();

            ConnectionManager.ChannelAction += HandleAnyChannelMessage;
            ConnectionManager.ChannelMessage += HandleAnyChannelMessage;
            ConnectionManager.ChannelNotice += HandleAnyChannelMessage;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new PuntConfig(newConfig);
        }

        protected virtual void HandleAnyChannelMessage(object sender, IChannelMessageEventArgs e, MessageFlags flags)
        {
            if (!Config.ChannelsPatterns.ContainsKey(e.Channel))
            {
                // don't police this channel
                return;
            }

            var relevantPatterns = Config.CommonPatterns
                .Concat(Config.ChannelsPatterns[e.Channel]);
            foreach (var pattern in relevantPatterns)
            {
                var normalizedNick = ConnectionManager.RegisteredNameForNick(e.SenderNickname);

                if (!pattern.NickPattern.IsMatch(e.SenderNickname) && (normalizedNick == null || !pattern.NickPattern.IsMatch(normalizedNick)))
                {
                    // wrong user
                    continue;
                }

                if (pattern.ChancePercent.HasValue)
                {
                    var val = Randomizer.Next(100);
                    if (val >= pattern.ChancePercent.Value)
                    {
                        // luck is on their side
                        continue;
                    }
                }

                if (pattern.BodyPattern.IsMatch(e.Message))
                {
                    // match! kick 'em!
                    ConnectionManager.KickChannelUser(e.Channel, e.SenderNickname, pattern.KickMessage);
                    return;
                }
            }
        }
    }
}
