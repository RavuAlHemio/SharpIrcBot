using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using log4net;
using Meebey.SmartIrc4net;
using SharpIrcBot.Config;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Events.Irc.Specific;

namespace SharpIrcBot
{
    public class ConnectionManager : IConnectionManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool ConfigFilePathKnown { get; set; }
        public string ConfigPath { get; }
        public BotConfig Config { get; set; }
        public IrcClient Client { get; }
        protected TimerTrigger ActualTimers { get; }

        public string MyNickname => Client.Nickname;
        public string MyUsername => Client.Username;
        public ITimerTrigger Timers => ActualTimers;
        public IReadOnlyList<string> AutoJoinChannels => Config.AutoJoinChannels.ToImmutableList();
        public IReadOnlyList<string> JoinedChannels => Client.JoinedChannels.OfType<string>().ToImmutableList();

        [CanBeNull]
        protected Thread IrcThread { get; set; }
        [NotNull]
        protected CancellationTokenSource Canceller { get; }
        [NotNull, ItemNotNull]
        protected HashSet<string> SyncedChannels { get; }

        /// <remarks>For on-demand configuration reloading.</remarks>
        [CanBeNull]
        public PluginManager PluginManager { get; set; }

        public int MaxMessageLength => 230;

        #region painless event handling boilerplate
        protected IList<SharpIrcBotEventHandler<IChannelMessageEventArgs>> ChannelMessageSubscribers = new List<SharpIrcBotEventHandler<IChannelMessageEventArgs>>();
        protected IList<SharpIrcBotEventHandler<IChannelMessageEventArgs>> ChannelActionSubscribers = new List<SharpIrcBotEventHandler<IChannelMessageEventArgs>>();
        protected IList<SharpIrcBotEventHandler<IChannelMessageEventArgs>> ChannelNoticeSubscribers = new List<SharpIrcBotEventHandler<IChannelMessageEventArgs>>();
        protected IList<SharpIrcBotEventHandler<IPrivateMessageEventArgs>> QueryMessageSubscribers = new List<SharpIrcBotEventHandler<IPrivateMessageEventArgs>>();
        protected IList<SharpIrcBotEventHandler<IPrivateMessageEventArgs>> QueryActionSubscribers = new List<SharpIrcBotEventHandler<IPrivateMessageEventArgs>>();
        protected IList<SharpIrcBotEventHandler<IPrivateMessageEventArgs>> QueryNoticeSubscribers = new List<SharpIrcBotEventHandler<IPrivateMessageEventArgs>>();
        protected IList<EventHandler<EventArgs>> ConnectedToServerSubscribers = new List<EventHandler<EventArgs>>();
        protected IList<EventHandler<NickMappingEventArgs>> NickMappingSubscribers = new List<EventHandler<NickMappingEventArgs>>();
        protected IList<EventHandler<IRawMessageEventArgs>> RawMessageSubscribers = new List<EventHandler<IRawMessageEventArgs>>();
        protected IList<EventHandler<INameListEventArgs>> NamesInChannelSubscribers = new List<EventHandler<INameListEventArgs>>();
        protected IList<EventHandler<IUserJoinedChannelEventArgs>> JoinedChannelSubscribers = new List<EventHandler<IUserJoinedChannelEventArgs>>();
        protected IList<EventHandler<INickChangeEventArgs>> NickChangeSubscribers = new List<EventHandler<INickChangeEventArgs>>();
        protected IList<EventHandler<IUserLeftChannelEventArgs>> UserLeftChannelSubscribers = new List<EventHandler<IUserLeftChannelEventArgs>>();
        protected IList<EventHandler<IUserQuitServerEventArgs>> UserQuitServerSubscribers = new List<EventHandler<IUserQuitServerEventArgs>>();
        protected IList<EventHandler<OutgoingMessageEventArgs>> OutgoingChannelMessageSubscribers = new List<EventHandler<OutgoingMessageEventArgs>>();
        protected IList<EventHandler<OutgoingMessageEventArgs>> OutgoingChannelActionSubscribers = new List<EventHandler<OutgoingMessageEventArgs>>();
        protected IList<EventHandler<OutgoingMessageEventArgs>> OutgoingChannelNoticeSubscribers = new List<EventHandler<OutgoingMessageEventArgs>>();
        protected IList<EventHandler<OutgoingMessageEventArgs>> OutgoingQueryMessageSubscribers = new List<EventHandler<OutgoingMessageEventArgs>>();
        protected IList<EventHandler<OutgoingMessageEventArgs>> OutgoingQueryActionSubscribers = new List<EventHandler<OutgoingMessageEventArgs>>();
        protected IList<EventHandler<OutgoingMessageEventArgs>> OutgoingQueryNoticeSubscribers = new List<EventHandler<OutgoingMessageEventArgs>>();
        protected IList<EventHandler<BaseNickChangedEventArgs>> BaseNickChangedSubscribers = new List<EventHandler<BaseNickChangedEventArgs>>();
        protected IList<EventHandler<IUserInvitedToChannelEventArgs>> InvitedSubscribers = new List<EventHandler<IUserInvitedToChannelEventArgs>>();
        public event SharpIrcBotEventHandler<IChannelMessageEventArgs> ChannelMessage
        {
            add { lock(ChannelMessageSubscribers) { ChannelMessageSubscribers.Add(value); } }
            remove { lock (ChannelMessageSubscribers) { ChannelMessageSubscribers.Remove(value); } }
        }
        public event SharpIrcBotEventHandler<IChannelMessageEventArgs> ChannelAction
        {
            add { lock(ChannelActionSubscribers) { ChannelActionSubscribers.Add(value); } }
            remove { lock (ChannelActionSubscribers) { ChannelActionSubscribers.Remove(value); } }
        }
        public event SharpIrcBotEventHandler<IChannelMessageEventArgs> ChannelNotice
        {
            add { lock(ChannelNoticeSubscribers) { ChannelNoticeSubscribers.Add(value); } }
            remove { lock (ChannelNoticeSubscribers) { ChannelNoticeSubscribers.Remove(value); } }
        }
        public event SharpIrcBotEventHandler<IPrivateMessageEventArgs> QueryMessage
        {
            add { lock(QueryMessageSubscribers) { QueryMessageSubscribers.Add(value); } }
            remove { lock (QueryMessageSubscribers) { QueryMessageSubscribers.Remove(value); } }
        }
        public event SharpIrcBotEventHandler<IPrivateMessageEventArgs> QueryAction
        {
            add { lock(QueryActionSubscribers) { QueryActionSubscribers.Add(value); } }
            remove { lock (QueryActionSubscribers) { QueryActionSubscribers.Remove(value); } }
        }
        public event SharpIrcBotEventHandler<IPrivateMessageEventArgs> QueryNotice
        {
            add { lock(QueryNoticeSubscribers) { QueryNoticeSubscribers.Add(value); } }
            remove { lock (QueryNoticeSubscribers) { QueryNoticeSubscribers.Remove(value); } }
        }
        public event EventHandler<EventArgs> ConnectedToServer
        {
            add { lock(ConnectedToServerSubscribers) { ConnectedToServerSubscribers.Add(value); } }
            remove { lock (ConnectedToServerSubscribers) { ConnectedToServerSubscribers.Remove(value); } }
        }
        public event EventHandler<NickMappingEventArgs> NickMapping
        {
            add { lock(NickMappingSubscribers) { NickMappingSubscribers.Add(value); } }
            remove { lock (NickMappingSubscribers) { NickMappingSubscribers.Remove(value); } }
        }
        public event EventHandler<IRawMessageEventArgs> RawMessage
        {
            add { lock(RawMessageSubscribers) { RawMessageSubscribers.Add(value); } }
            remove { lock (RawMessageSubscribers) { RawMessageSubscribers.Remove(value); } }
        }
        public event EventHandler<INameListEventArgs> NamesInChannel
        {
            add { lock(NamesInChannelSubscribers) { NamesInChannelSubscribers.Add(value); } }
            remove { lock (NamesInChannelSubscribers) { NamesInChannelSubscribers.Remove(value); } }
        }
        public event EventHandler<IUserJoinedChannelEventArgs> JoinedChannel
        {
            add { lock(JoinedChannelSubscribers) { JoinedChannelSubscribers.Add(value); } }
            remove { lock (JoinedChannelSubscribers) { JoinedChannelSubscribers.Remove(value); } }
        }
        public event EventHandler<INickChangeEventArgs> NickChange
        {
            add { lock(NickChangeSubscribers) { NickChangeSubscribers.Add(value); } }
            remove { lock (NickChangeSubscribers) { NickChangeSubscribers.Remove(value); } }
        }
        public event EventHandler<IUserLeftChannelEventArgs> UserLeftChannel
        {
            add { lock(UserLeftChannelSubscribers) { UserLeftChannelSubscribers.Add(value); } }
            remove { lock (UserLeftChannelSubscribers) { UserLeftChannelSubscribers.Remove(value); } }
        }
        public event EventHandler<IUserQuitServerEventArgs> UserQuitServer
        {
            add { lock(UserQuitServerSubscribers) { UserQuitServerSubscribers.Add(value); } }
            remove { lock (UserQuitServerSubscribers) { UserQuitServerSubscribers.Remove(value); } }
        }
        public event EventHandler<OutgoingMessageEventArgs> OutgoingChannelMessage
        {
            add { lock(OutgoingChannelMessageSubscribers) { OutgoingChannelMessageSubscribers.Add(value); } }
            remove { lock (OutgoingChannelMessageSubscribers) { OutgoingChannelMessageSubscribers.Remove(value); } }
        }
        public event EventHandler<OutgoingMessageEventArgs> OutgoingChannelAction
        {
            add { lock(OutgoingChannelActionSubscribers) { OutgoingChannelActionSubscribers.Add(value); } }
            remove { lock (OutgoingChannelActionSubscribers) { OutgoingChannelActionSubscribers.Remove(value); } }
        }
        public event EventHandler<OutgoingMessageEventArgs> OutgoingChannelNotice
        {
            add { lock(OutgoingChannelNoticeSubscribers) { OutgoingChannelNoticeSubscribers.Add(value); } }
            remove { lock (OutgoingChannelNoticeSubscribers) { OutgoingChannelNoticeSubscribers.Remove(value); } }
        }
        public event EventHandler<OutgoingMessageEventArgs> OutgoingQueryMessage
        {
            add { lock(OutgoingQueryMessageSubscribers) { OutgoingQueryMessageSubscribers.Add(value); } }
            remove { lock (OutgoingQueryMessageSubscribers) { OutgoingQueryMessageSubscribers.Remove(value); } }
        }
        public event EventHandler<OutgoingMessageEventArgs> OutgoingQueryAction
        {
            add { lock(OutgoingQueryActionSubscribers) { OutgoingQueryActionSubscribers.Add(value); } }
            remove { lock (OutgoingQueryActionSubscribers) { OutgoingQueryActionSubscribers.Remove(value); } }
        }
        public event EventHandler<OutgoingMessageEventArgs> OutgoingQueryNotice
        {
            add { lock(OutgoingQueryNoticeSubscribers) { OutgoingQueryNoticeSubscribers.Add(value); } }
            remove { lock (OutgoingQueryNoticeSubscribers) { OutgoingQueryNoticeSubscribers.Remove(value); } }
        }
        public event EventHandler<BaseNickChangedEventArgs> BaseNickChanged
        {
            add { lock(BaseNickChangedSubscribers) { BaseNickChangedSubscribers.Add(value); } }
            remove { lock (BaseNickChangedSubscribers) { BaseNickChangedSubscribers.Remove(value); } }
        }
        public event EventHandler<IUserInvitedToChannelEventArgs> Invited
        {
            add { lock(InvitedSubscribers) { InvitedSubscribers.Add(value); } }
            remove { lock (InvitedSubscribers) { InvitedSubscribers.Remove(value); } }
        }
        #endregion

        public ConnectionManager([CanBeNull] string configPath)
            : this(SharpIrcBotUtil.LoadConfig(configPath))
        {
            ConfigFilePathKnown = true;
            ConfigPath = configPath;
        }

        public ConnectionManager([NotNull] BotConfig config)
        {
            SyncedChannels = new HashSet<string>();

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
            Client.OnChannelNotice += HandleChannelNotice;
            Client.OnChannelActiveSynced += HandleChannelSynced;
            Client.OnRawMessage += HandleRawMessage;
            Client.OnNames += HandleNames;
            Client.OnJoin += HandleJoin;
            Client.OnNickChange += HandleNickChange;
            Client.OnQueryMessage += HandleQueryMessage;
            Client.OnQueryAction += HandleQueryAction;
            Client.OnQueryNotice += HandleQueryNotice;
            Client.OnRegistered += HandleRegistered;
            Client.OnPart += HandlePart;
            Client.OnQuit += HandleQuit;
            Client.OnInvite += HandleInvite;
            ActualTimers = new TimerTrigger();
            Canceller = new CancellationTokenSource();

            ConfigFilePathKnown = false;
            ConfigPath = null;
        }

        public void Start()
        {
            IrcThread = new Thread(OuterProc)
            {
                Name = "IRC thread"
            };
            IrcThread.Start();

            ActualTimers.Start();
        }

        public void Stop()
        {
            ActualTimers.Stop();

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

        public void ReloadConfiguration()
        {
            if (!ConfigFilePathKnown)
            {
                Logger.Warn("cannot reload configuration: configuration file path unknown");
            }

            Config = SharpIrcBotUtil.LoadConfig(ConfigPath);

            if (PluginManager == null)
            {
                Logger.Warn("cannot reload plugin configuration: plugin manager is null");
                return;
            }
            PluginManager.ReloadConfigurations(Config.Plugins);
        }

        protected virtual void OuterProc()
        {
            var cancelToken = Canceller.Token;
            TimeSpan cooldown = TimeSpan.FromSeconds(1);
            TimeSpan cooldownIncreaseThreshold = TimeSpan.FromMinutes(Config.CooldownIncreaseThresholdMinutes);

            while (!cancelToken.IsCancellationRequested)
            {
                var connectPoint = DateTime.UtcNow;

                try
                {
                    Proc();
                }
                catch (Exception exc)
                {
                    Logger.Error("exception while running IRC", exc);
                    DisconnectOrWhatever();

                    var failPoint = DateTime.UtcNow;
                    if (cooldownIncreaseThreshold == TimeSpan.Zero || (failPoint - connectPoint) < cooldownIncreaseThreshold)
                    {
                        // increase cooldown
                        cooldown = TimeSpan.FromTicks(cooldown.Ticks * 2);
                    }
                }

                Thread.Sleep(cooldown);
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
            foreach (var channel in Config.AutoJoinChannels)
            {
                Client.RfcJoin(channel);
            }

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
            OnChannelMessage(new ChannelMessageEventArgs(e.Data));
        }

        protected virtual void HandleChannelAction(object sender, ActionEventArgs e)
        {
            if (!SyncedChannels.Contains(e.Data.Channel))
            {
                return;
            }
            OnChannelAction(new ChannelActionEventArgs(e));
        }

        protected virtual void HandleChannelNotice(object sender, IrcEventArgs e)
        {
            if (!SyncedChannels.Contains(e.Data.Channel))
            {
                return;
            }
            OnChannelNotice(new ChannelMessageEventArgs(e.Data));
        }

        protected virtual void HandleQueryMessage(object sender, IrcEventArgs e)
        {
            OnQueryMessage(new PrivateMessageEventArgs(e.Data));
        }

        protected virtual void HandleQueryAction(object sender, ActionEventArgs e)
        {
            OnQueryAction(new PrivateActionEventArgs(e));
        }

        protected virtual void HandleQueryNotice(object sender, IrcEventArgs e)
        {
            OnQueryNotice(new PrivateMessageEventArgs(e.Data));
        }

        protected virtual void HandleChannelSynced(object sender, IrcEventArgs e)
        {
            SyncedChannels.Add(e.Data.Channel);
        }

        protected virtual void HandleRawMessage(object sender, IrcEventArgs e)
        {
            OnRawMessage(new RawMessageEventArgs(e.Data));
        }

        protected virtual void HandleJoin(object sender, JoinEventArgs e)
        {
            OnJoinedChannel(new UserJoinedChannelEventArgs(e));
        }

        protected virtual void HandleNames(object sender, NamesEventArgs e)
        {
            OnNamesInChannel(new NameListEventArgs(e));
        }

        protected virtual void HandleNickChange(object sender, Meebey.SmartIrc4net.NickChangeEventArgs e)
        {
            OnNickChange(new Events.Irc.Specific.NickChangeEventArgs(e));
        }

        protected virtual void HandleRegistered(object sender, EventArgs e)
        {
            OnConnectedToServer(e);
        }

        protected virtual void HandleQuit(object sender, QuitEventArgs e)
        {
            OnUserQuitServer(new UserQuitServerEventArgs(e));
        }

        protected virtual void HandlePart(object sender, PartEventArgs e)
        {
            OnUserLeftChannel(new UserLeftChannelEventArgs(e));
        }

        protected virtual void HandleInvite(object sender, InviteEventArgs e)
        {
            OnInvited(new UserInvitedToChannelEventArgs(e));
        }

        protected virtual MessageFlags FlagsForNick([CanBeNull] string nick)
        {
            if (nick == null)
            {
                return MessageFlags.None;
            }

            if (Config.BannedUsers.Contains(nick))
            {
                return MessageFlags.UserBanned;
            }
            var regNick = RegisteredNameForNick(nick);
            if (regNick != null && Config.BannedUsers.Contains(regNick))
            {
                return MessageFlags.UserBanned;
            }
            return MessageFlags.None;
        }

        protected virtual void HandleSharpIrcBotEvent<T>(IList<SharpIrcBotEventHandler<T>> subscribers, T e, string description)
            where T : IUserMessageEventArgs
        {
            List<SharpIrcBotEventHandler<T>> subscriberList;
            lock (subscribers)
            {
                subscriberList = new List<SharpIrcBotEventHandler<T>>(subscribers);
            }

            if (subscriberList.Count == 0)
            {
                return;
            }

            var flags = FlagsForNick(e.SenderNickname);
            foreach (var subscriber in subscriberList)
            {
                try
                {
                    subscriber(this, e, flags);
                }
                catch (Exception exc)
                {
                    Logger.ErrorFormat("error when {0} was handling {1}: {2}", subscriber, description, exc);
                }
            }
        }

        protected virtual void HandleEvent<T>(IList<EventHandler<T>> subscribers, T e, string description)
        {
            List<EventHandler<T>> subscriberList;
            lock (subscribers)
            {
                subscriberList = new List<EventHandler<T>>(subscribers);
            }

            if (subscriberList.Count == 0)
            {
                return;
            }

            foreach (var subscriber in subscriberList)
            {
                try
                {
                    subscriber(this, e);
                }
                catch (Exception exc)
                {
                    Logger.ErrorFormat("error when {0} was handling {1}: {2}", subscriber, description, exc);
                }
            }
        }

        protected virtual void OnChannelMessage(IChannelMessageEventArgs e)
        {
            HandleSharpIrcBotEvent(ChannelMessageSubscribers, e, "channel message");
        }

        protected virtual void OnChannelAction(IChannelMessageEventArgs e)
        {
            HandleSharpIrcBotEvent(ChannelActionSubscribers, e, "channel action");
        }

        protected virtual void OnChannelNotice(IChannelMessageEventArgs e)
        {
            HandleSharpIrcBotEvent(ChannelNoticeSubscribers, e, "channel notice");
        }

        protected virtual void OnQueryMessage(IPrivateMessageEventArgs e)
        {
            HandleSharpIrcBotEvent(QueryMessageSubscribers, e, "query message");
        }

        protected virtual void OnQueryAction(IPrivateMessageEventArgs e)
        {
            HandleSharpIrcBotEvent(QueryActionSubscribers, e, "query action");
        }

        protected virtual void OnQueryNotice(IPrivateMessageEventArgs e)
        {
            HandleSharpIrcBotEvent(QueryNoticeSubscribers, e, "query notice");
        }

        protected virtual void OnConnectedToServer(EventArgs e)
        {
            HandleEvent(ConnectedToServerSubscribers, e, "new connection to server");
        }

        protected virtual void OnNickMapping(NickMappingEventArgs e)
        {
            HandleEvent(NickMappingSubscribers, e, "nick mapping");
        }

        protected virtual void OnRawMessage(IRawMessageEventArgs e)
        {
            HandleEvent(RawMessageSubscribers, e, "raw message");
        }

        protected virtual void OnNamesInChannel(INameListEventArgs e)
        {
            HandleEvent(NamesInChannelSubscribers, e, "names in channel");
        }

        protected virtual void OnJoinedChannel(IUserJoinedChannelEventArgs e)
        {
            HandleEvent(JoinedChannelSubscribers, e, "user joining channel");
        }

        protected virtual void OnNickChange(INickChangeEventArgs e)
        {
            HandleEvent(NickChangeSubscribers, e, "nick change");
        }

        protected virtual void OnUserLeftChannel(IUserLeftChannelEventArgs e)
        {
            HandleEvent(UserLeftChannelSubscribers, e, "user leaving channel");
        }

        protected virtual void OnUserQuitServer(IUserQuitServerEventArgs e)
        {
            HandleEvent(UserQuitServerSubscribers, e, "user quitting server");
        }

        protected virtual void OnOutgoingChannelMessage(OutgoingMessageEventArgs e)
        {
            HandleEvent(OutgoingChannelMessageSubscribers, e, "outgoing channel message");
        }

        protected virtual void OnOutgoingChannelAction(OutgoingMessageEventArgs e)
        {
            HandleEvent(OutgoingChannelActionSubscribers, e, "outgoing channel action");
        }

        protected virtual void OnOutgoingChannelNotice(OutgoingMessageEventArgs e)
        {
            HandleEvent(OutgoingChannelNoticeSubscribers, e, "outgoing channel notice");
        }

        protected virtual void OnOutgoingQueryMessage(OutgoingMessageEventArgs e)
        {
            HandleEvent(OutgoingQueryMessageSubscribers, e, "outgoing query message");
        }

        protected virtual void OnOutgoingQueryAction(OutgoingMessageEventArgs e)
        {
            HandleEvent(OutgoingQueryActionSubscribers, e, "outgoing query action");
        }

        protected virtual void OnOutgoingQueryNotice(OutgoingMessageEventArgs e)
        {
            HandleEvent(OutgoingQueryNoticeSubscribers, e, "outgoing query notice");
        }

        protected virtual void OnBaseNickChanged(BaseNickChangedEventArgs e)
        {
            HandleEvent(BaseNickChangedSubscribers, e, "base nick change");
        }

        protected virtual void OnInvited(IUserInvitedToChannelEventArgs e)
        {
            HandleEvent(InvitedSubscribers, e, "invitation");
        }

        public string RegisteredNameForNick(string nick)
        {
            // perform nick mapping
            var eventArgs = new NickMappingEventArgs(nick);
            OnNickMapping(eventArgs);

            return eventArgs.MapsTo.FirstOrDefault();
        }

        public void ReportBaseNickChange(string oldBaseNick, string newBaseNick)
        {
            // trigger update among plugins
            Logger.InfoFormat("reporting base nick change from {0} to {1}", oldBaseNick, newBaseNick);
            var e = new BaseNickChangedEventArgs(oldBaseNick, newBaseNick);
            OnBaseNickChanged(e);
        }

        /// <remarks><paramref name="words"/> will be modified.</remarks>
        [NotNull]
        protected string GetLongestWordPrefix([NotNull] IList<string> words, int length = 230)
        {
            if (words.Count == 0)
            {
                throw new ArgumentException("words is empty", "words");
            }

            var firstWord = words[0];
            words.RemoveAt(0);

            if (Client.Encoding.GetBytes(firstWord).Length >= length)
            {
                // cutting on words isn't enough
                var returnValue = new StringBuilder(firstWord);
                var newFirstWord = new StringBuilder();

                while (Client.Encoding.GetBytes(returnValue.ToString()).Length >= length)
                {
                    // move a character from the end of returnValue to the beginning of newFirstWord
                    newFirstWord.Insert(0, returnValue[returnValue.Length - 1]);
                    --returnValue.Length;
                }

                // replace the new first word and return the return value
                words.Insert(0, newFirstWord.ToString());
                return returnValue.ToString();
            }

            // start taking words
            var ret = firstWord;
            while (words.Count > 0)
            {
                var testReturn = ret + " " + words[0];
                if (Client.Encoding.GetBytes(testReturn).Length >= length)
                {
                    // nope, not this one anymore
                    return ret.ToString();
                }

                // take a word
                ret = testReturn;
                words.RemoveAt(0);
            }

            // we took all the remaining words!
            return ret;
        }

        [NotNull]
        public List<string> SplitMessageToLength([NotNull] string message, int length = 479)
        {
            // normalize newlines
            message = message.Replace("\r\n", "\n").Replace("\r", "\n");

            var lines = new List<string>();
            foreach (var origLine in message.Split('\n'))
            {
                if (Client.Encoding.GetBytes(origLine).Length < length || origLine.Length == 0)
                {
                    // short-circuit
                    lines.Add(origLine);
                    continue;
                }

                var words = origLine.Split(' ').ToList();
                while (words.Count > 0)
                {
                    var line = GetLongestWordPrefix(words, length);
                    lines.Add(line);
                }
            }
            return lines;
        }

        public void SendChannelMessage(string channel, string message)
        {
            foreach (var line in SplitMessageToLength(message, MaxMessageLength))
            {
                OnOutgoingChannelMessage(new OutgoingMessageEventArgs(channel, line));
                Client.SendMessage(SendType.Message, channel, line);
            }
        }

        public void SendChannelAction(string channel, string message)
        {
            foreach (var line in SplitMessageToLength(message, MaxMessageLength))
            {
                OnOutgoingChannelAction(new OutgoingMessageEventArgs(channel, line));
                Client.SendMessage(SendType.Action, channel, line);
            }
        }

        public void SendChannelNotice(string channel, string message)
        {
            foreach (var line in SplitMessageToLength(message, MaxMessageLength))
            {
                OnOutgoingChannelNotice(new OutgoingMessageEventArgs(channel, line));
                Client.SendMessage(SendType.Notice, channel, line);
            }
        }

        public void SendQueryMessage(string nick, string message)
        {
            foreach (var line in SplitMessageToLength(message, MaxMessageLength))
            {
                OnOutgoingQueryMessage(new OutgoingMessageEventArgs(nick, line));
                Client.SendMessage(SendType.Message, nick, line);
            }
        }

        public void SendQueryAction(string nick, string message)
        {
            foreach (var line in SplitMessageToLength(message, MaxMessageLength))
            {
                OnOutgoingQueryAction(new OutgoingMessageEventArgs(nick, line));
                Client.SendMessage(SendType.Action, nick, line);
            }
        }

        public void SendQueryNotice(string nick, string message)
        {
            foreach (var line in SplitMessageToLength(message, MaxMessageLength))
            {
                OnOutgoingQueryNotice(new OutgoingMessageEventArgs(nick, line));
                Client.SendMessage(SendType.Notice, nick, line);
            }
        }

        public void SendRawCommand(string cmd)
        {
            Client.WriteLine(cmd);
        }

        public void KickChannelUser(string channel, string nick, string message)
        {
            if (message == null)
            {
                Client.RfcKick(channel, nick);
            }
            else
            {
                Client.RfcKick(channel, nick, message);
            }
        }

        public void JoinChannel(string channel)
        {
            Client.RfcJoin(channel);
        }

        public void RequestNicknamesInChannel(string channel)
        {
            Client.RfcNames(channel);
        }

        public void RequestUserInfo(params string[] nicknames)
        {
            Client.RfcWhois(nicknames);
        }

        public IReadOnlyList<string> NicknamesInChannel(string channel)
        {
            if (channel == null)
            {
                return null;
            }

            var channelObject = Client.GetChannel(channel);
            return channelObject
                ?.Users
                .OfType<DictionaryEntry>()
                .Select(de => (string) de.Key)
                .ToImmutableList();
        }

        public ChannelUserLevel GetChannelLevelForUser(string channel, string nick)
        {
            var user = Client.GetChannelUser(channel, nick);
            var nonRfcUser = user as NonRfcChannelUser;

            if (nonRfcUser != null)
            {
                if (nonRfcUser.IsOwner)
                {
                    return ChannelUserLevel.Owner;
                }
                if (nonRfcUser.IsChannelAdmin)
                {
                    return ChannelUserLevel.ChannelAdmin;
                }
            }

            if (user.IsOp)
            {
                return ChannelUserLevel.Op;
            }

            if (nonRfcUser != null && nonRfcUser.IsHalfop)
            {
                return ChannelUserLevel.HalfOp;
            }

            if (user.IsVoice)
            {
                return ChannelUserLevel.Voice;
            }

            return ChannelUserLevel.Normal;
        }
    }
}
