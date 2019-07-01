using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using Meebey.SmartIrc4net;
using SharpIrcBot.Chunks;
using SharpIrcBot.Commands;
using SharpIrcBot.Config;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc.Specific;
using SharpIrcBot.Util;

namespace SharpIrcBot
{
    public partial class ConnectionManager : IConnectionManager
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<ConnectionManager>();

        private bool ConfigFilePathKnown { get; set; }
        public string ConfigPath { get; }
        public BotConfig Config { get; set; }
        public IrcClient Client { get; protected set; }
        public bool DefaultCTCPHandlerEnabled { get; set; }
        protected TimerTrigger ActualTimers { get; }

        public string MyNickname => Client?.Nickname;
        public string MyUsername => Client?.Username;
        public ITimerTrigger Timers => ActualTimers;
        public IReadOnlyList<string> AutoJoinChannels => Config.AutoJoinChannels.ToImmutableList();
        public IReadOnlyList<string> JoinedChannels =>
            Client?.JoinedChannels.OfType<string>().ToImmutableList() ?? ImmutableList<string>.Empty;

        [CanBeNull]
        protected Thread IrcThread { get; set; }
        [NotNull]
        protected CancellationTokenSource Canceller { get; }
        [NotNull, ItemNotNull]
        protected HashSet<string> SyncedChannels { get; }

        /// <remarks>For on-demand configuration reloading.</remarks>
        [CanBeNull]
        public PluginManager PluginManager { get; set; }
        public CommandManager CommandManager { get; set; }

        public int MaxLineLength => 510;

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
            ActualTimers = new TimerTrigger();
            Canceller = new CancellationTokenSource();
            CommandManager = new CommandManager(Config.Commands, this);

            DefaultCTCPHandlerEnabled = true;

            ConfigFilePathKnown = false;
            ConfigPath = null;
        }

        protected virtual IrcClient GetNewIrcClient()
        {
            var client = new IrcClient
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

            client.OnCtcpRequest += HandleCtcpRequest;
            client.OnCtcpReply += HandleCtcpReply;
            client.OnChannelMessage += HandleChannelMessage;
            client.OnChannelAction += HandleChannelAction;
            client.OnChannelNotice += HandleChannelNotice;
            client.OnChannelActiveSynced += HandleChannelSynced;
            client.OnRawMessage += HandleRawMessage;
            client.OnNames += HandleNames;
            client.OnJoin += HandleJoin;
            client.OnNickChange += HandleNickChange;
            client.OnQueryMessage += HandleQueryMessage;
            client.OnQueryAction += HandleQueryAction;
            client.OnQueryNotice += HandleQueryNotice;
            client.OnRegistered += HandleRegistered;
            client.OnPart += HandlePart;
            client.OnQuit += HandleQuit;
            client.OnInvite += HandleInvite;

            return client;
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
                Client?.Disconnect();
            }
            catch (NotConnectedException)
            {
            }
        }

        public void ReloadConfiguration()
        {
            if (!ConfigFilePathKnown)
            {
                Logger.LogWarning("cannot reload configuration: configuration file path unknown");
            }

            Config = SharpIrcBotUtil.LoadConfig(ConfigPath);
            CommandManager.Config = Config.Commands;

            if (PluginManager == null)
            {
                Logger.LogWarning("cannot reload plugin configuration: plugin manager is null");
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
                    Logger.LogError("exception while running IRC: {Exception}", exc);
                    DisconnectOrWhatever();

                    var failPoint = DateTime.UtcNow;
                    if (cooldownIncreaseThreshold == TimeSpan.Zero || (failPoint - connectPoint) < cooldownIncreaseThreshold)
                    {
                        // increase cooldown
                        cooldown = TimeSpan.FromTicks(cooldown.Ticks * 2);
                    }
                }

                Logger.LogInformation("reconnect cooldown: sleeping for {Seconds} seconds", cooldown.TotalSeconds);

                Thread.Sleep(cooldown);
            }
        }

        protected virtual void Proc()
        {
            SyncedChannels.Clear();

            // renew the client
            Client = GetNewIrcClient();

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
            OnCTCPRequest(new CTCPEventArgs(e));

            if (!DefaultCTCPHandlerEnabled)
            {
                return;
            }

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
                Logger.LogWarning("exception while handling CTCP request: {Exception}", exc);
            }
        }

        protected virtual void HandleCtcpReply(object sender, CtcpEventArgs e)
        {
            OnCTCPReply(new CTCPEventArgs(e));
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
            Logger.LogInformation("reporting base nick change from {OldBaseNickname} to {NewBaseNickname}", oldBaseNick, newBaseNick);
            var e = new BaseNickChangedEventArgs(oldBaseNick, newBaseNick);
            OnBaseNickChanged(e);
        }

        public List<IMessageChunk> SplitMessageToChunks(string message)
        {
            var chunks = new List<IMessageChunk>
            {
                new TextMessageChunk(message)
            };
            var eventArgs = new MessageChunkingEventArgs(chunks);
            OnSplitToChunks(eventArgs);
            return eventArgs.Chunks;
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
            foreach (string origLine in message.Split('\n'))
            {
                if (Client.Encoding.GetBytes(origLine).Length < length || origLine.Length == 0)
                {
                    // short-circuit
                    lines.Add(origLine);
                    continue;
                }

                List<string> words = origLine.Split(' ').ToList();
                while (words.Count > 0)
                {
                    string line = GetLongestWordPrefix(words, length);
                    lines.Add(line);
                }
            }
            return lines;
        }

        public void SendChannelMessage(string channel, string message)
        {
            string commonPrefix = $"PRIVMSG {channel} :";
            int myMaxLineLength = MaxLineLength - commonPrefix.Length;

            foreach (string line in SplitMessageToLength(message, myMaxLineLength))
            {
                OnOutgoingChannelMessage(new OutgoingMessageEventArgs(channel, line));
                Client.WriteLine(commonPrefix + line);
            }
        }

        public void SendChannelAction(string channel, string message)
        {
            string commonPrefix = $"PRIVMSG {channel} :\u0001ACTION ";
            const string commonSuffix = "\u0001";
            int myMaxLineLength = MaxLineLength - (commonPrefix.Length + commonSuffix.Length);

            foreach (string line in SplitMessageToLength(message, myMaxLineLength))
            {
                OnOutgoingChannelAction(new OutgoingMessageEventArgs(channel, line));
                Client.WriteLine(commonPrefix + line + commonSuffix);
            }
        }

        public void SendChannelNotice(string channel, string message)
        {
            string commonPrefix = $"NOTICE {channel} :";
            int myMaxLineLength = MaxLineLength - commonPrefix.Length;

            foreach (string line in SplitMessageToLength(message, myMaxLineLength))
            {
                OnOutgoingChannelNotice(new OutgoingMessageEventArgs(channel, line));
                Client.WriteLine(commonPrefix + line);
            }
        }

        public void SendQueryMessage(string nick, string message)
        {
            string commonPrefix = $"PRIVMSG {nick} :";
            int myMaxLineLength = MaxLineLength - commonPrefix.Length;

            foreach (var line in SplitMessageToLength(message, myMaxLineLength))
            {
                OnOutgoingQueryMessage(new OutgoingMessageEventArgs(nick, line));
                Client.WriteLine(commonPrefix + line);
            }
        }

        public void SendQueryAction(string nick, string message)
        {
            string commonPrefix = $"PRIVMSG {nick} :\u0001ACTION ";
            const string commonSuffix = "\u0001";
            int myMaxLineLength = MaxLineLength - (commonPrefix.Length + commonSuffix.Length);

            foreach (string line in SplitMessageToLength(message, myMaxLineLength))
            {
                OnOutgoingQueryAction(new OutgoingMessageEventArgs(nick, line));
                Client.WriteLine(commonPrefix + line + commonSuffix);
            }
        }

        public void SendQueryNotice(string nick, string message)
        {
            string commonPrefix = $"NOTICE {nick} :";
            int myMaxLineLength = MaxLineLength - commonPrefix.Length;

            foreach (var line in SplitMessageToLength(message, myMaxLineLength))
            {
                OnOutgoingQueryNotice(new OutgoingMessageEventArgs(nick, line));
                Client.WriteLine(commonPrefix + line);
            }
        }

        public void SendCtcpRequest(string target, string command, string parameters = null)
        {
            string message = (parameters == null)
                ? $"PRIVMSG {target} :\u0001{command}\u0001"
                : $"PRIVMSG {target} :\u0001{command} {parameters}\u0001";
            Client.WriteLine(message);
        }

        public void SendCtcpResponse(string target, string command, string parameters = null)
        {
            string message = (parameters == null)
                ? $"NOTICE {target} :\u0001{command}\u0001"
                : $"NOTICE {target} :\u0001{command} {parameters}\u0001";
            Client.WriteLine(message);
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

        public void ChangeChannelMode(string channel, string modeChange)
        {
            Client.RfcMode(channel, modeChange);
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

        public virtual bool IsValidNickname(string nick)
        {
            return Config.NicknameRegex.IsMatch(nick);
        }
    }
}
