﻿using System;
using System.Text.RegularExpressions;
using log4net;
using Meebey.SmartIrc4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace Reinvite
{
    public class ReinvitePlugin : IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Regex InviteRegex = new Regex("^!invite[ ]+(?<channel>[#&][^ ]{1,256})[ ]*$");

        protected ConnectionManager ConnectionManager { get; }
        protected ReinviteConfig Config { get; set; }

        public ReinvitePlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new ReinviteConfig(config);
            
            ConnectionManager.QueryMessage += HandleQueryMessage;
            ConnectionManager.Invited += HandleInvite;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new ReinviteConfig(newConfig);
        }

        protected void HandleQueryMessage(object sender, IrcEventArgs e, MessageFlags flags)
        {
            try
            {
                ActuallyHandleQueryMessage(sender, e, flags);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling query message", exc);
            }
        }

        protected void HandleInvite(object sender, InviteEventArgs e)
        {
            try
            {
                ActuallyHandleInvite(sender, e);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling invite", exc);
            }
        }

        protected virtual void ActuallyHandleQueryMessage(object sender, IrcEventArgs e, MessageFlags flags)
        {
            if (!Config.RejoinOnPrivateMessage)
            {
                return;
            }

            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var match = InviteRegex.Match(e.Data.Message);
            if (!match.Success)
            {
                return;
            }

            var channel = match.Groups["channel"].Value;
            if (Config.AutoJoinedChannelsOnly && !ConnectionManager.Config.AutoJoinChannels.Contains(channel))
            {
                return;
            }

            ConnectionManager.Client.RfcJoin(channel);
        }

        protected void ActuallyHandleInvite(object sender, InviteEventArgs e)
        {
            if (!Config.RejoinOnInvite)
            {
                return;
            }

            if (Config.AutoJoinedChannelsOnly && !ConnectionManager.Config.AutoJoinChannels.Contains(e.Channel))
            {
                return;
            }

            ConnectionManager.Client.RfcJoin(e.Channel);
        }
    }
}
