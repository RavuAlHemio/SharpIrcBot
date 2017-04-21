using System;
using Newtonsoft.Json.Linq;

namespace SharpIrcBot.Plugins.CasinoBot.Player
{
    public class CasinoPlayerBotPlugin : IPlugin, IReloadableConfiguration
    {
        protected IConnectionManager ConnectionManager { get; }

        public CasinoPlayerBotPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            //Config = new CasinoPlayerConfig(config);
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            //Config = new CasinoPlayerConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }
    }
}
