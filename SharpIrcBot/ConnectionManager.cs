using System;
using System.Reflection;
using System.Text;
using System.Threading;
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

        public ConnectionManager(BotConfig config)
        {
            Config = config;
            Client = new IrcClient();

            Client.AutoReconnect = true;
            Client.AutoRejoin = true;
            Client.Encoding = Encoding.GetEncoding(Config.Encoding);
            Client.SendDelay = Config.SendDelay;
            Client.ActiveChannelSyncing = true;
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
            Client.AutoReconnect = false;
            Client.Disconnect();
            IrcThread.Join();
        }

        protected virtual void OuterProc()
        {
            try
            {
                Proc();
            }
            catch (Exception exc)
            {
                Logger.Error("exception while running IRC", exc);
            }
        }

        protected virtual void Proc()
        {
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
            Client.Disconnect();
        }
    }
}
