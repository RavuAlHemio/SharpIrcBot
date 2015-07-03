using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using log4net;
using Meebey.SmartIrc4net;
using Timer = System.Timers.Timer;

namespace SharpIrcBot
{
    public class ConnectionManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static int MaxMessageLength = 230;

        public BotConfig Config;
        public readonly IrcClient Client;

        protected Thread IrcThread;
        protected CancellationTokenSource Canceller;
        protected HashSet<string> SyncedChannels;
        protected Dictionary<string, string> NicksToLogins;
        protected Timer WhoisUpdateTimer;

        public event EventHandler<IrcEventArgs> ChannelMessage;
        public event EventHandler<ActionEventArgs> ChannelAction;
        public event EventHandler<IrcEventArgs> ChannelNotice;
        public event EventHandler<IrcEventArgs> QueryMessage;
        public event EventHandler<ActionEventArgs> QueryAction;
        public event EventHandler<IrcEventArgs> QueryNotice;

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
            Client.OnChannelNotice += HandleChannelNotice;
            Client.OnChannelActiveSynced += HandleChannelSynced;
            Client.OnRawMessage += HandleRegisteredAs;
            Client.OnNames += HandleNames;
            Client.OnJoin += HandleJoin;
            Client.OnNickChange += HandleNickChange;
            Client.OnQueryMessage += HandleQueryMessage;
            Client.OnQueryAction += HandleQueryAction;
            Client.OnQueryNotice += HandleQueryNotice;
            Canceller = new CancellationTokenSource();

            WhoisUpdateTimer = new Timer(Config.WhoisUpdateIntervalSeconds * 1000.0);
            WhoisUpdateTimer.Elapsed += (sender, args) =>
            {
                foreach (var channel in SyncedChannels)
                {
                    WhoisEveryoneInChannel(channel);
                }
            };
        }

        public void Start()
        {
            IrcThread = new Thread(OuterProc)
            {
                Name = "IRC thread"
            };
            IrcThread.Start();

            WhoisUpdateTimer.Start();
        }

        public void Stop()
        {
            Canceller.Cancel();
            WhoisUpdateTimer.Stop();
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

        protected virtual void HandleChannelNotice(object sender, IrcEventArgs e)
        {
            if (!SyncedChannels.Contains(e.Data.Channel))
            {
                return;
            }
            OnChannelNotice(e);
        }

        protected virtual void HandleQueryMessage(object sender, IrcEventArgs e)
        {
            OnQueryMessage(e);
        }

        protected virtual void HandleQueryAction(object sender, ActionEventArgs e)
        {
            OnQueryAction(e);
        }

        protected virtual void HandleQueryNotice(object sender, IrcEventArgs e)
        {
            OnQueryNotice(e);
        }

        protected virtual void HandleChannelSynced(object sender, IrcEventArgs e)
        {
            SyncedChannels.Add(e.Data.Channel);
        }

        protected virtual void HandleRegisteredAs(object sender, IrcEventArgs e)
        {
            if ((int)e.Data.ReplyCode == 330)
            {
                // :irc.example.com 330 MYNICK THEIRNICK THEIRLOGIN :is logged in as
                lock (NicksToLogins)
                {
                    NicksToLogins[e.Data.RawMessageArray[3].ToLowerInvariant()] = e.Data.RawMessageArray[4];
                }
                Logger.DebugFormat("registered that {0} is logged in as {1}", e.Data.RawMessageArray[3], e.Data.RawMessageArray[4]);
            }
            else if (e.Data.ReplyCode == ReplyCode.WhoIsUser)
            {
                // :irc.example.com 311 MYNICK THEIRNICK THEIRHOST * :REALNAME
                // mark that we have at least seen this user
                lock (NicksToLogins)
                {
                    NicksToLogins[e.Data.RawMessageArray[3].ToLowerInvariant()] = null;
                }
                Logger.DebugFormat("registered that {0} exists (and might not be logged in)", e.Data.RawMessageArray[3]);
            }
            else if (e.Data.ReplyCode == ReplyCode.ErrorNoSuchNickname)
            {
                // :irc.example.com 311 MYNICK THEIRNICK :No such nick/channel
                // remove that user
                lock (NicksToLogins)
                {
                    NicksToLogins[e.Data.RawMessageArray[3].ToLowerInvariant()] = null;
                }
                Logger.DebugFormat("registered that {0} is gone and thereby not logged in", e.Data.RawMessageArray[3]);
            }
        }

        protected virtual void HandleNames(object sender, NamesEventArgs e)
        {
            // update all the names
            RunCheckRegistrationsOn(e.UserList);
        }

        protected virtual void HandleNickChange(object sender, NickChangeEventArgs e)
        {
            // update both old and new nickname
            RunCheckRegistrationsOn(e.OldNickname, e.NewNickname);
        }

        protected virtual void HandleJoin(object sender, JoinEventArgs e)
        {
            // update this person
            RunCheckRegistrationsOn(e.Who);
        }

        protected virtual void OnChannelMessage(IrcEventArgs e)
        {
            if (ChannelMessage != null)
            {
                ChannelMessage(this, e);
            }
        }

        protected virtual void OnChannelAction(ActionEventArgs e)
        {
            if (ChannelAction != null)
            {
                ChannelAction(this, e);
            }
        }

        protected virtual void OnChannelNotice(IrcEventArgs e)
        {
            if (ChannelNotice != null)
            {
                ChannelNotice(this, e);
            }
        }

        protected virtual void OnQueryMessage(IrcEventArgs e)
        {
            if (QueryMessage != null)
            {
                QueryMessage(this, e);
            }
        }

        protected virtual void OnQueryAction(ActionEventArgs e)
        {
            if (QueryAction != null)
            {
                QueryAction(this, e);
            }
        }

        protected virtual void OnQueryNotice(IrcEventArgs e)
        {
            if (QueryNotice != null)
            {
                QueryNotice(this, e);
            }
        }

        public string RegisteredNameForNick(string nick)
        {
            if (!Config.HonorUserRegistrations)
            {
                // don't give a damn
                Logger.DebugFormat("regname: not honoring user registration; pretending that {0} is logged in as {0}", nick);
                return nick;
            }

            var lowerNick = nick.ToLowerInvariant();
            lock (NicksToLogins)
            {
                if (NicksToLogins.ContainsKey(lowerNick))
                {
                    if (Logger.IsDebugEnabled)
                    {
                        if (NicksToLogins[lowerNick] == null)
                        {
                            Logger.DebugFormat("regname: {0} is not registered (null)", lowerNick);
                        }
                        else
                        {
                            Logger.DebugFormat("regname: {0} is registered as {1}", lowerNick, NicksToLogins[lowerNick]);
                        }
                    }
                    return NicksToLogins[lowerNick];
                }
            }

            Logger.DebugFormat("regname: {0} is not registered (not contained)", lowerNick);

            // return null for the time being
            return null;
        }

        public void WhoisEveryoneInChannel(string channel)
        {
            // perform NAMES on the channel; the names response triggers the WHOIS waterfall
            Client.RfcNames(channel);
        }

        protected bool RunCheckRegistrationsOn(params string[] nicknames)
        {
            if (Logger.IsDebugEnabled)
            {
                Logger.DebugFormat("performing reg check on: {0}", string.Join(" ", nicknames));
            }

            // send WHOIS for every user to get their registered name
            // do this in packages to reduce traffic
            const int packageSize = 5;

            for (int i = 0; i < nicknames.Length; i += packageSize)
            {
                Client.RfcWhois(nicknames.Skip(i).Take(packageSize).ToArray());
            }
            return true;
        }

        /// <remarks><paramref name="words"/> will be modified.</remarks>
        protected string GetLongestWordPrefix(IList<string> words, int length = 230)
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

        public List<string> SplitMessageToLength(string message, int length = 479)
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
                Client.SendMessage(SendType.Message, channel, line);
            }
        }

        public void SendChannelMessageFormat(string channel, string format, params object[] args)
        {
            SendChannelMessage(channel, string.Format(format, args));
        }

        public void SendChannelAction(string channel, string message)
        {
            foreach (var line in SplitMessageToLength(message, MaxMessageLength))
            {
                Client.SendMessage(SendType.Action, channel, line);
            }
        }

        public void SendChannelActionFormat(string channel, string format, params object[] args)
        {
            SendChannelAction(channel, string.Format(format, args));
        }

        public void SendChannelNotice(string channel, string message)
        {
            foreach (var line in SplitMessageToLength(message, MaxMessageLength))
            {
                Client.SendMessage(SendType.Notice, channel, line);
            }
        }

        public void SendChannelNoticeFormat(string channel, string format, params object[] args)
        {
            SendChannelNotice(channel, string.Format(format, args));
        }

        public void SendQueryMessage(string nick, string message)
        {
            foreach (var line in SplitMessageToLength(message, MaxMessageLength))
            {
                Client.SendMessage(SendType.Message, nick, line);
            }
        }

        public void SendQueryMessageFormat(string nick, string format, params object[] args)
        {
            SendQueryMessage(nick, string.Format(format, args));
        }

        public void SendQueryAction(string nick, string message)
        {
            foreach (var line in SplitMessageToLength(message, MaxMessageLength))
            {
                Client.SendMessage(SendType.Action, nick, line);
            }
        }

        public void SendQueryActionFormat(string nick, string format, params object[] args)
        {
            SendQueryAction(nick, string.Format(format, args));
        }

        public void SendQueryNotice(string nick, string message)
        {
            foreach (var line in SplitMessageToLength(message, MaxMessageLength))
            {
                Client.SendMessage(SendType.Notice, nick, line);
            }
        }

        public void SendQueryNoticeFormat(string nick, string format, params object[] args)
        {
            SendQueryNotice(nick, string.Format(format, args));
        }

        public void SendRawCommand(string cmd)
        {
            Client.WriteLine(cmd);
        }
    }
}
