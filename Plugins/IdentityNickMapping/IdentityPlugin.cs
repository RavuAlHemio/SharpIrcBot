﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Events;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.IdentityNickMapping
{
    public class IdentityPlugin : IPlugin
    {
        private static readonly ILogger Logger = LogUtil.LoggerFactory.CreateLogger<IdentityPlugin>();

        protected IConnectionManager ConnectionManager;

        public IdentityPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;

            ConnectionManager.NickMapping += HandleNickMapping;
        }

        protected virtual void HandleNickMapping(object sender, NickMappingEventArgs e)
        {
            e.MapsTo.Add(e.Nickname);
        }
    }
}
