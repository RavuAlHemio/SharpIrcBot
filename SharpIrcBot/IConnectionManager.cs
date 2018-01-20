using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SharpIrcBot.Chunks;
using SharpIrcBot.Commands;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;

namespace SharpIrcBot
{
    public interface IConnectionManager
    {
        event SharpIrcBotEventHandler<IChannelMessageEventArgs> ChannelMessage;
        event SharpIrcBotEventHandler<IChannelMessageEventArgs> ChannelAction;
        event SharpIrcBotEventHandler<IChannelMessageEventArgs> ChannelNotice;
        event SharpIrcBotEventHandler<IPrivateMessageEventArgs> QueryMessage;
        event SharpIrcBotEventHandler<IPrivateMessageEventArgs> QueryAction;
        event SharpIrcBotEventHandler<IPrivateMessageEventArgs> QueryNotice;
        event EventHandler<EventArgs> ConnectedToServer;
        event EventHandler<NickMappingEventArgs> NickMapping;
        event EventHandler<IRawMessageEventArgs> RawMessage;
        event EventHandler<INameListEventArgs> NamesInChannel;
        event EventHandler<IUserJoinedChannelEventArgs> JoinedChannel;
        event EventHandler<INickChangeEventArgs> NickChange;
        event EventHandler<IUserLeftChannelEventArgs> UserLeftChannel;
        event EventHandler<IUserQuitServerEventArgs> UserQuitServer;
        event EventHandler<OutgoingMessageEventArgs> OutgoingChannelMessage;
        event EventHandler<OutgoingMessageEventArgs> OutgoingChannelAction;
        event EventHandler<OutgoingMessageEventArgs> OutgoingChannelNotice;
        event EventHandler<OutgoingMessageEventArgs> OutgoingQueryMessage;
        event EventHandler<OutgoingMessageEventArgs> OutgoingQueryAction;
        event EventHandler<OutgoingMessageEventArgs> OutgoingQueryNotice;
        event EventHandler<BaseNickChangedEventArgs> BaseNickChanged;
        event EventHandler<IUserInvitedToChannelEventArgs> Invited;
        event EventHandler<MessageChunkingEventArgs> SplitToChunks;

        [NotNull] string MyNickname { get; }
        [NotNull] string MyUsername { get; }
        int MaxLineLength { get; }
        [NotNull] ITimerTrigger Timers { get; }
        [NotNull, ItemNotNull] IReadOnlyList<string> AutoJoinChannels { get; }
        [NotNull] IReadOnlyList<string> JoinedChannels { get; }
        [NotNull] CommandManager CommandManager { get; }

        void SendChannelMessage([NotNull] string channel, [NotNull] string message);
        void SendChannelAction([NotNull] string channel, [NotNull] string message);
        void SendChannelNotice([NotNull] string channel, [NotNull] string message);
        void SendQueryMessage([NotNull] string nick, [NotNull] string message);
        void SendQueryAction([NotNull] string nick, [NotNull] string message);
        void SendQueryNotice([NotNull] string nick, [NotNull] string message);
        void SendRawCommand([NotNull] string cmd);

        void KickChannelUser([NotNull] string channel, [NotNull] string nick, [CanBeNull] string message = null);
        void JoinChannel([NotNull] string channel);
        void RequestNicknamesInChannel([NotNull] string channel);
        void RequestUserInfo([NotNull, ItemNotNull] params string[] nicknames);
        void ChangeChannelMode([NotNull] string channel, [NotNull] string modeChange);

        [CanBeNull] string RegisteredNameForNick([NotNull] string nick);
        void ReportBaseNickChange([NotNull] string oldBaseNick, [NotNull] string newBaseNick);
        [CanBeNull] IReadOnlyList<string> NicknamesInChannel([CanBeNull] string channel);
        ChannelUserLevel GetChannelLevelForUser([NotNull] string channel, [NotNull] string nick);
        List<IMessageChunk> SplitMessageToChunks(string message);
        bool IsValidNickname(string nickname);

        void ReloadConfiguration();
    }
}
