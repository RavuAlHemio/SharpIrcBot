﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Meebey.SmartIrc4net;

namespace SharpIrcBot
{
    public class ConnectionManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public BotConfig Config;
        public readonly IrcClient Client;

        protected Thread IrcThread;
        protected CancellationTokenSource Canceller;
        protected HashSet<string> SyncedChannels;
        protected Dictionary<string, string> NicksToLogins;

        public event EventHandler<IrcEventArgs> ChannelMessage;
        public event EventHandler<IrcEventArgs> ChannelAction;

        public ConnectionManager(BotConfig config)
        {
            SyncedChannels = new HashSet<string>();
            NicksToLogins = new Dictionary<string, string>();

            Config = config;
            Client = new IrcClient
            {
                UseSsl = Config.UseTls,
                ValidateServerCertificate = Config.VerifyTlsCertificate,
                AutoReconnect = false,
                AutoRejoin = false,
                AutoRelogin = false,
                Encoding = Encoding.GetEncoding(Config.Encoding),
                SendDelay = Config.SendDelay,
                SupportNonRfc = true,
                ActiveChannelSyncing = true
            };
            Client.OnCtcpRequest += HandleCtcpRequest;
            Client.OnChannelMessage += HandleChannelMessage;
            Client.OnChannelAction += HandleChannelAction;
            Client.OnChannelActiveSynced += HandleChannelSynced;
            Client.OnRawMessage += HandleRegisteredAs;
            Client.OnNames += HandleNames;
            Client.OnJoin += HandleJoin;
            Client.OnNickChange += HandleNickChange;
            Canceller = new CancellationTokenSource();
        }

        public void Start()
        {
            IrcThread = new Thread(OuterProc)
            {
                Name = "IRC thread"
            };
            IrcThread.Start();
        }

        public void Stop()
        {
            Canceller.Cancel();
            DisconnectOrWhatever();
            IrcThread.Join();
        }

        protected void DisconnectOrWhatever()
        {
            try
            {
                Client.Disconnect();
            }
            catch (NotConnectedException)
            {
            }
        }

        protected virtual void OuterProc()
        {
            var cancelToken = Canceller.Token;

            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    Proc();
                }
                catch (Exception exc)
                {
                    Logger.Error("exception while running IRC", exc);
                    DisconnectOrWhatever();
                }
            }
        }

        protected virtual void Proc()
        {
            SyncedChannels.Clear();

            Client.Connect(Config.ServerHostname, Config.ServerPort);

            Client.Login(Config.Nickname, Config.DisplayName, 0, Config.Username, Config.ServerPassword);

            // perform autocommands
            foreach (var autoCmd in Config.AutoConnectCommands)
            {
                Client.WriteLine(autoCmd);
            }

            // autojoin
            Client.RfcJoin(Config.AutoJoinChannels.ToArray());

            // listen
            Client.Listen();

            // disconnect
            DisconnectOrWhatever();
        }

        protected virtual void HandleCtcpRequest(object sender, CtcpEventArgs e)
        {
            try
            {
                switch (e.CtcpCommand)
                {
                    case "VERSION":
                        Client.SendMessage(SendType.CtcpReply, e.Data.Nick, "VERSION " + Config.CtcpVersionResponse);
                        break;
                    case "FINGER":
                        Client.SendMessage(SendType.CtcpReply, e.Data.Nick, "FINGER " + Config.CtcpFingerResponse);
                        break;
                    case "PING":
                        if (e.Data.Message.Length > 7)
                        {
                            Client.SendMessage(SendType.CtcpReply, e.Data.Nick, "PONG " + e.Data.Message.Substring(6, e.Data.Message.Length - 7));
                        }
                        else
                        {
                            Client.SendMessage(SendType.CtcpReply, e.Data.Nick, "PONG");
                        }
                        break;
                }
            }
            catch (Exception exc)
            {
                Logger.Warn("exception while handling CTCP request", exc);
            }
        }

        protected virtual void HandleChannelMessage(object sender, IrcEventArgs e)
        {
            if (!SyncedChannels.Contains(e.Data.Channel))
            {
                return;
            }
            OnChannelMessage(e);
        }

        protected virtual void HandleChannelAction(object sender, ActionEventArgs e)
        {
            if (!SyncedChannels.Contains(e.Data.Channel))
            {
                return;
            }
            OnChannelAction(e);
        }

        protected virtual void HandleChannelSynced(object sender, IrcEventArgs e)
        {
            SyncedChannels.Add(e.Data.Channel);
        }

        protected virtual void HandleRegisteredAs(object sender, IrcEventArgs e)
        {
            if ((int)e.Data.ReplyCode != 330)
            {
                return;
            }

            // :irc.example.com 330 MYNICK THEIRNICK THEIRLOGIN :is logged in as
            NicksToLogins[e.Data.RawMessageArray[3].ToLowerInvariant()] = e.Data.RawMessageArray[4];
        }

        protected virtual void HandleNames(object sender, NamesEventArgs e)
        {
            // send WHOIS for every user to get their registered name
            // do this in packages to reduce traffic
            const int packageSize = 5;

            for (int i = 0; i < e.UserList.Length; i += packageSize)
            {
                Client.RfcWhois(e.UserList.Skip(i).Take(packageSize).ToArray());
            }
        }

        protected virtual void HandleJoin(object sender, JoinEventArgs e)
        {
            // delay a few seconds, then send WHOIS to get their registered name
            DelayThenWhois(e.Who);
        }

        protected virtual void HandleNickChange(object sender, NickChangeEventArgs e)
        {
            // remove the old name
            NicksToLogins.Remove(e.OldNickname);
            // send WHOIS to get their registered name
            Client.RfcWhois(e.NewNickname);
        }

        protected virtual void OnChannelMessage(IrcEventArgs e)
        {
            if (ChannelMessage != null)
            {
                ChannelMessage(this, e);
            }
        }

        protected virtual void OnChannelAction(IrcEventArgs e)
        {
            if (ChannelAction != null)
            {
                ChannelMessage(this, e);
            }
        }

        public string RegisteredNameForNick(string nick)
        {
            if (!Config.HonorUserRegistrations)
            {
                // don't give a damn
                return nick;
            }

            var lowerNick = nick.ToLowerInvariant();
            if (NicksToLogins.ContainsKey(lowerNick))
            {
                return NicksToLogins[lowerNick];
            }

            // maybe they are registered now...
            Client.RfcWhois(nick);

            // return null for the time being
            return null;
        }

        protected void DelayThenWhois(string nick)
        {
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(Config.JoinWhoisDelay));
                Client.RfcWhois(nick);
            });
        }
    }
}
