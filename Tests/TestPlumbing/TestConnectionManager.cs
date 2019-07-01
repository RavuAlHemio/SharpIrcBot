using System;
using System.Collections.Generic;
using SharpIrcBot.Chunks;
using SharpIrcBot.Commands;
using SharpIrcBot.Config;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Tests.TestPlumbing.Events.Feeding;
using SharpIrcBot.Tests.TestPlumbing.Events.Logging;

namespace SharpIrcBot.Tests.TestPlumbing
{
    public class TestConnectionManager : IConnectionManager
    {
        #pragma warning disable CS0067
        public event SharpIrcBotEventHandler<IChannelMessageEventArgs> ChannelMessage;
        public event SharpIrcBotEventHandler<IChannelMessageEventArgs> ChannelAction;
        public event SharpIrcBotEventHandler<IChannelMessageEventArgs> ChannelNotice;
        public event SharpIrcBotEventHandler<IPrivateMessageEventArgs> QueryMessage;
        public event SharpIrcBotEventHandler<IPrivateMessageEventArgs> QueryAction;
        public event SharpIrcBotEventHandler<IPrivateMessageEventArgs> QueryNotice;
        public event EventHandler<EventArgs> ConnectedToServer;
        public event EventHandler<NickMappingEventArgs> NickMapping;
        public event EventHandler<IRawMessageEventArgs> RawMessage;
        public event EventHandler<INameListEventArgs> NamesInChannel;
        public event EventHandler<IUserJoinedChannelEventArgs> JoinedChannel;
        public event EventHandler<INickChangeEventArgs> NickChange;
        public event EventHandler<IUserLeftChannelEventArgs> UserLeftChannel;
        public event EventHandler<IUserQuitServerEventArgs> UserQuitServer;
        public event EventHandler<OutgoingMessageEventArgs> OutgoingChannelMessage;
        public event EventHandler<OutgoingMessageEventArgs> OutgoingChannelAction;
        public event EventHandler<OutgoingMessageEventArgs> OutgoingChannelNotice;
        public event EventHandler<OutgoingMessageEventArgs> OutgoingQueryMessage;
        public event EventHandler<OutgoingMessageEventArgs> OutgoingQueryAction;
        public event EventHandler<OutgoingMessageEventArgs> OutgoingQueryNotice;
        public event EventHandler<BaseNickChangedEventArgs> BaseNickChanged;
        public event EventHandler<IUserInvitedToChannelEventArgs> Invited;
        public event EventHandler<MessageChunkingEventArgs> SplitToChunks;
        public event SharpIrcBotEventHandler<ICTCPEventArgs> CTCPRequest;
        public event SharpIrcBotEventHandler<ICTCPEventArgs> CTCPReply;
        #pragma warning restore CS0067

        public string MyNickname => MyUsername;
        public string MyUsername => "TestBot";
        public int MaxLineLength => 510;
        public ITimerTrigger Timers { get; }
        public IReadOnlyList<string> AutoJoinChannels => new List<string>();
        public IReadOnlyList<string> JoinedChannels => ActuallyJoinedChannels;

        public List<string> ActuallyJoinedChannels { get; } = new List<string>();
        public List<ITestIrcEvent> EventLog { get; } = new List<ITestIrcEvent>();

        public CommandManager CommandManager { get; }

        public TestConnectionManager()
        {
            CommandManager = new CommandManager(new CommandConfig(), this);
        }

        public void SendChannelMessage(string channel, string message)
        {
            LogMessage(MessageType.Message, channel, message);
        }

        public void SendChannelAction(string channel, string message)
        {
            LogMessage(MessageType.Action, channel, message);
        }

        public void SendChannelNotice(string channel, string message)
        {
            LogMessage(MessageType.Notice, channel, message);
        }

        public void SendQueryMessage(string nick, string message)
        {
            LogMessage(MessageType.Message, nick, message);
        }

        public void SendQueryAction(string nick, string message)
        {
            LogMessage(MessageType.Action, nick, message);
        }

        public void SendQueryNotice(string nick, string message)
        {
            LogMessage(MessageType.Notice, nick, message);
        }

        public void SendCtcpRequest(string target, string message, string parameter = null)
        {
            string fullMessage = (parameter == null)
                ? message
                : (message + "\0" + parameter);
            LogMessage(MessageType.CTCPRequest, target, fullMessage);
        }

        public void SendCtcpResponse(string target, string message, string parameter = null)
        {
            string fullMessage = (parameter == null)
                ? message
                : (message + "\0" + parameter);
            LogMessage(MessageType.CTCPResponse, target, fullMessage);
        }

        protected void LogMessage(MessageType messageType, string target, string body)
        {
            EventLog.Add(new TestMessage
            {
                Type = messageType,
                Target = target,
                Body = body
            });
        }

        public void SendRawCommand(string cmd)
        {
            EventLog.Add(new TestRawCommand { Command = cmd });
        }

        public void KickChannelUser(string channel, string nick, string message = null)
        {
            EventLog.Add(new TestKick
            {
                Channel = channel,
                Nickname = nick,
                Message = message
            });
        }

        public void JoinChannel(string channel)
        {
            throw new NotImplementedException();
        }

        public void RequestNicknamesInChannel(string channel)
        {
            throw new NotImplementedException();
        }

        public void RequestUserInfo(params string[] nicknames)
        {
            throw new NotImplementedException();
        }

        public void ChangeChannelMode(string channel, string modeChange)
        {
            throw new NotImplementedException();
        }

        public string RegisteredNameForNick(string nick)
        {
            throw new NotImplementedException();
        }

        public void ReportBaseNickChange(string oldBaseNick, string newBaseNick)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<string> NicknamesInChannel(string channel)
        {
            throw new NotImplementedException();
        }

        public ChannelUserLevel GetChannelLevelForUser(string channel, string nick)
        {
            throw new NotImplementedException();
        }

        public List<IMessageChunk> SplitMessageToChunks(string message)
        {
            var chunks = new List<IMessageChunk>
            {
                new TextMessageChunk(message)
            };
            var eventArgs = new MessageChunkingEventArgs(chunks);
            SplitToChunks(this, eventArgs);
            return eventArgs.Chunks;
        }

        public virtual bool IsValidNickname(string nick)
        {
            throw new NotImplementedException();
        }

        public void ReloadConfiguration()
        {
            throw new NotImplementedException();
        }

        public void InjectChannelMessage(string channel, string nick, string message)
        {
            var messageArgs = new TestChannelMessageEventArgs
            {
                Channel = channel,
                SenderNickname = nick,
                Message = message
            };
            ChannelMessage.Invoke(this, messageArgs, MessageFlags.None);
        }
    }
}
