using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Meebey.SmartIrc4net;
using SharpIrcBot.Events;

namespace SharpIrcBot
{
    public interface IConnectionManager
    {
		event SharpIrcBotEventHandler<IrcEventArgs> ChannelMessage;
        event SharpIrcBotEventHandler<ActionEventArgs> ChannelAction;
        event SharpIrcBotEventHandler<IrcEventArgs> ChannelNotice;
        event SharpIrcBotEventHandler<IrcEventArgs> QueryMessage;
        event SharpIrcBotEventHandler<ActionEventArgs> QueryAction;
        event SharpIrcBotEventHandler<IrcEventArgs> QueryNotice;
        event EventHandler<EventArgs> ConnectedToServer;
        event EventHandler<NickMappingEventArgs> NickMapping;
        event EventHandler<IrcEventArgs> RawMessage;
        event EventHandler<NamesEventArgs> NamesInChannel;
        event EventHandler<JoinEventArgs> JoinedChannel;
        event EventHandler<NickChangeEventArgs> NickChange;
        event EventHandler<PartEventArgs> UserLeftChannel;
        event EventHandler<QuitEventArgs> UserQuitServer;
        event EventHandler<OutgoingMessageEventArgs> OutgoingChannelMessage;
        event EventHandler<OutgoingMessageEventArgs> OutgoingChannelAction;
        event EventHandler<OutgoingMessageEventArgs> OutgoingChannelNotice;
        event EventHandler<OutgoingMessageEventArgs> OutgoingQueryMessage;
        event EventHandler<OutgoingMessageEventArgs> OutgoingQueryAction;
        event EventHandler<OutgoingMessageEventArgs> OutgoingQueryNotice;
        event EventHandler<BaseNickChangedEventArgs> BaseNickChanged;
        event EventHandler<InviteEventArgs> Invited;

        [NotNull] string MyNickname { get; }
        [NotNull] string MyUsername { get; }
        int MaxMessageLength { get; }
        [NotNull] ITimerTrigger Timers { get; }
        [NotNull, ItemNotNull] IReadOnlyList<string> AutoJoinChannels { get; }
        [NotNull] IReadOnlyList<string> JoinedChannels { get; }

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

        [CanBeNull] string RegisteredNameForNick([NotNull] string nick);
        void ReportBaseNickChange([NotNull] string oldBaseNick, [NotNull] string newBaseNick);
        [CanBeNull] IReadOnlyList<string> NicknamesInChannel([CanBeNull] string channel);
        ChannelUserLevel GetChannelLevelForUser([NotNull] string channel, [NotNull] string nick);

        void ReloadConfiguration();
    }
}
