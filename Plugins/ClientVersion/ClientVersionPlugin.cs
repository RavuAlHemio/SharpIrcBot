using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.ClientVersion.ORM;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.ClientVersion
{
    public class ClientVersionPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<ClientVersionPlugin>();

        protected CVConfig Config { get; set; }
        protected IConnectionManager ConnectionManager { get; }
        protected bool UpdateScheduled { get; set; }

        public ClientVersionPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new CVConfig(config);

            ConnectionManager.CTCPReply += HandleCTCPReply;
            ConnectionManager.JoinedChannel += HandleChannelJoined;

            UpdateScheduled = false;

            ProcessAging();
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new CVConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            ProcessAging();
        }

        protected virtual void HandleChannelJoined(object sender, IUserJoinedChannelEventArgs e)
        {
            using (var ctx = GetNewContext())
            {
                VersionEntry found = ctx.VersionEntries
                    .FirstOrDefault(ve => ve.Nickname == e.Nickname);
                if (found != null && Config.RescanIntervalDays.HasValue)
                {
                    if (found.Timestamp <= DateTimeOffset.Now.AddDays(-Config.RescanIntervalDays.Value))
                    {
                        // aged out
                        found = null;
                    }
                }

                if (found == null)
                {
                    ConnectionManager.SendCtcpRequest(e.Nickname, "VERSION");
                }
            }
        }

        protected virtual void HandleCTCPReply(object sender, ICTCPEventArgs e, MessageFlags flags)
        {
            if (e.CTCPCommand.ToUpperInvariant() != "VERSION")
            {
                return;
            }

            // new or updated version entry!
            using (var ctx = GetNewContext())
            {
                VersionEntry curVer = ctx.VersionEntries
                    .FirstOrDefault(ve => ve.Nickname == e.SenderNickname);
                if (curVer == null)
                {
                    curVer = new VersionEntry
                    {
                        Nickname = e.SenderNickname,
                    };
                    ctx.VersionEntries.Add(curVer);
                }

                curVer.VersionInfo = e.CTCPParameter;
                curVer.Timestamp = DateTimeOffset.Now;

                ctx.SaveChanges();

                MaybeScheduleUpdate(ctx);
            }
        }

        protected virtual void ProcessAging()
        {
            if (!Config.RescanIntervalDays.HasValue)
            {
                return;
            }

            using (var ctx = GetNewContext())
            {
                DateTimeOffset oldCutoff = DateTimeOffset.Now
                    .AddDays(-Config.RescanIntervalDays.Value);

                // update those that need rechecking
                List<string> recheckUs = ctx.VersionEntries
                    .Where(ve => ve.Timestamp <= oldCutoff)
                    .Select(ve => ve.Nickname)
                    .ToList();

                foreach (string nick in recheckUs)
                {
                    ConnectionManager.SendCtcpRequest(nick, "VERSION");
                }

                MaybeScheduleUpdate(ctx);
            }
        }

        protected virtual void MaybeScheduleUpdate(ClientVersionContext ctx)
        {
            if (UpdateScheduled)
            {
                return;
            }
            if (!Config.RescanIntervalDays.HasValue)
            {
                return;
            }

            DateTimeOffset oldCutoff = DateTimeOffset.Now
                .AddDays(-Config.RescanIntervalDays.Value);

            // find out when to check next
            DateTimeOffset? oldestFresh = ctx.VersionEntries
                .Where(ve => ve.Timestamp > oldCutoff)
                .OrderBy(ve => ve.Timestamp)
                .Select(ve => (DateTimeOffset?)ve.Timestamp)
                .FirstOrDefault();

            if (!oldestFresh.HasValue)
            {
                // nothing...
                UpdateScheduled = false;
                return;
            }

            ConnectionManager.Timers.Register(
                oldestFresh.Value.AddDays(Config.RescanIntervalDays.Value),
                ProcessAging
            );
            UpdateScheduled = true;
        }

        private ClientVersionContext GetNewContext()
        {
            var opts = DatabaseUtil.GetContextOptions<ClientVersionContext>(Config);
            return new ClientVersionContext(opts);
        }
    }
}
