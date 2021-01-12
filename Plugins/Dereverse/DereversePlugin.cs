using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Events.Irc;

namespace SharpIrcBot.Plugins.Dereverse
{
    public class DereversePlugin : IPlugin, IReloadableConfiguration
    {
        protected IConnectionManager ConnectionManager { get; }
        protected DereverseConfig Config { get; set; }

        public DereversePlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new DereverseConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelMessage;
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new DereverseConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs e, MessageFlags flags)
        {
            if (!Config.Channels.Contains(e.Channel))
            {
                return;
            }

            // also react to banned users

            decimal score = 0.0m;
            foreach (Scorer scorer in Config.Scorers)
            {
                MatchCollection matches = scorer.Pattern.Matches(e.Message);
                if (matches.Count > 0)
                {
                    if (scorer.KnockOut)
                    {
                        return;
                    }

                    score += matches.Count * scorer.ScoreAdjustment;
                }
            }

            if (score >= Config.ScoreThreshold)
            {
                string dereversed = string.Concat(e.Message.Reverse());
                ConnectionManager.SendChannelMessage(e.Channel, dereversed);
            }
        }
    }
}
