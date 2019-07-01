using System;
using System.Collections.Generic;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;

namespace SharpIrcBot
{
    public partial class ConnectionManager
    {
        #region handler lists (per event)
        protected List<SharpIrcBotEventHandler<IChannelMessageEventArgs>> ChannelMessageSubscribers = new List<SharpIrcBotEventHandler<IChannelMessageEventArgs>>();
        protected List<SharpIrcBotEventHandler<IChannelMessageEventArgs>> ChannelActionSubscribers = new List<SharpIrcBotEventHandler<IChannelMessageEventArgs>>();
        protected List<SharpIrcBotEventHandler<IChannelMessageEventArgs>> ChannelNoticeSubscribers = new List<SharpIrcBotEventHandler<IChannelMessageEventArgs>>();
        protected List<SharpIrcBotEventHandler<IPrivateMessageEventArgs>> QueryMessageSubscribers = new List<SharpIrcBotEventHandler<IPrivateMessageEventArgs>>();
        protected List<SharpIrcBotEventHandler<IPrivateMessageEventArgs>> QueryActionSubscribers = new List<SharpIrcBotEventHandler<IPrivateMessageEventArgs>>();
        protected List<SharpIrcBotEventHandler<IPrivateMessageEventArgs>> QueryNoticeSubscribers = new List<SharpIrcBotEventHandler<IPrivateMessageEventArgs>>();
        protected List<EventHandler<EventArgs>> ConnectedToServerSubscribers = new List<EventHandler<EventArgs>>();
        protected List<EventHandler<NickMappingEventArgs>> NickMappingSubscribers = new List<EventHandler<NickMappingEventArgs>>();
        protected List<EventHandler<IRawMessageEventArgs>> RawMessageSubscribers = new List<EventHandler<IRawMessageEventArgs>>();
        protected List<EventHandler<INameListEventArgs>> NamesInChannelSubscribers = new List<EventHandler<INameListEventArgs>>();
        protected List<EventHandler<IUserJoinedChannelEventArgs>> JoinedChannelSubscribers = new List<EventHandler<IUserJoinedChannelEventArgs>>();
        protected List<EventHandler<INickChangeEventArgs>> NickChangeSubscribers = new List<EventHandler<INickChangeEventArgs>>();
        protected List<EventHandler<IUserLeftChannelEventArgs>> UserLeftChannelSubscribers = new List<EventHandler<IUserLeftChannelEventArgs>>();
        protected List<EventHandler<IUserQuitServerEventArgs>> UserQuitServerSubscribers = new List<EventHandler<IUserQuitServerEventArgs>>();
        protected List<EventHandler<OutgoingMessageEventArgs>> OutgoingChannelMessageSubscribers = new List<EventHandler<OutgoingMessageEventArgs>>();
        protected List<EventHandler<OutgoingMessageEventArgs>> OutgoingChannelActionSubscribers = new List<EventHandler<OutgoingMessageEventArgs>>();
        protected List<EventHandler<OutgoingMessageEventArgs>> OutgoingChannelNoticeSubscribers = new List<EventHandler<OutgoingMessageEventArgs>>();
        protected List<EventHandler<OutgoingMessageEventArgs>> OutgoingQueryMessageSubscribers = new List<EventHandler<OutgoingMessageEventArgs>>();
        protected List<EventHandler<OutgoingMessageEventArgs>> OutgoingQueryActionSubscribers = new List<EventHandler<OutgoingMessageEventArgs>>();
        protected List<EventHandler<OutgoingMessageEventArgs>> OutgoingQueryNoticeSubscribers = new List<EventHandler<OutgoingMessageEventArgs>>();
        protected List<EventHandler<BaseNickChangedEventArgs>> BaseNickChangedSubscribers = new List<EventHandler<BaseNickChangedEventArgs>>();
        protected List<EventHandler<IUserInvitedToChannelEventArgs>> InvitedSubscribers = new List<EventHandler<IUserInvitedToChannelEventArgs>>();
        protected List<EventHandler<MessageChunkingEventArgs>> SplitToChunksSubscribers = new List<EventHandler<MessageChunkingEventArgs>>();
        protected List<SharpIrcBotEventHandler<ICTCPEventArgs>> CTCPRequestSubscribers = new List<SharpIrcBotEventHandler<ICTCPEventArgs>>();
        protected List<SharpIrcBotEventHandler<ICTCPEventArgs>> CTCPReplySubscribers = new List<SharpIrcBotEventHandler<ICTCPEventArgs>>();
        #endregion

        #region event definitions (per event)
        public event SharpIrcBotEventHandler<IChannelMessageEventArgs> ChannelMessage
        {
            add { AddSubscriber(ChannelMessageSubscribers, value); }
            remove { RemoveSubscriber(ChannelMessageSubscribers, value); }
        }

        public event SharpIrcBotEventHandler<IChannelMessageEventArgs> ChannelAction
        {
            add { AddSubscriber(ChannelActionSubscribers, value); }
            remove { RemoveSubscriber(ChannelActionSubscribers, value); }
        }

        public event SharpIrcBotEventHandler<IChannelMessageEventArgs> ChannelNotice
        {
            add { AddSubscriber(ChannelNoticeSubscribers, value); }
            remove { RemoveSubscriber(ChannelNoticeSubscribers, value); }
        }

        public event SharpIrcBotEventHandler<IPrivateMessageEventArgs> QueryMessage
        {
            add { AddSubscriber(QueryMessageSubscribers, value); }
            remove { RemoveSubscriber(QueryMessageSubscribers, value); }
        }

        public event SharpIrcBotEventHandler<IPrivateMessageEventArgs> QueryAction
        {
            add { AddSubscriber(QueryActionSubscribers, value); }
            remove { RemoveSubscriber(QueryActionSubscribers, value); }
        }

        public event SharpIrcBotEventHandler<IPrivateMessageEventArgs> QueryNotice
        {
            add { AddSubscriber(QueryNoticeSubscribers, value); }
            remove { RemoveSubscriber(QueryNoticeSubscribers, value); }
        }

        public event EventHandler<EventArgs> ConnectedToServer
        {
            add { AddSubscriber(ConnectedToServerSubscribers, value); }
            remove { RemoveSubscriber(ConnectedToServerSubscribers, value); }
        }

        public event EventHandler<NickMappingEventArgs> NickMapping
        {
            add { AddSubscriber(NickMappingSubscribers, value); }
            remove { RemoveSubscriber(NickMappingSubscribers, value); }
        }

        public event EventHandler<IRawMessageEventArgs> RawMessage
        {
            add { AddSubscriber(RawMessageSubscribers, value); }
            remove { RemoveSubscriber(RawMessageSubscribers, value); }
        }

        public event EventHandler<INameListEventArgs> NamesInChannel
        {
            add { AddSubscriber(NamesInChannelSubscribers, value); }
            remove { RemoveSubscriber(NamesInChannelSubscribers, value); }
        }

        public event EventHandler<IUserJoinedChannelEventArgs> JoinedChannel
        {
            add { AddSubscriber(JoinedChannelSubscribers, value); }
            remove { RemoveSubscriber(JoinedChannelSubscribers, value); }
        }

        public event EventHandler<INickChangeEventArgs> NickChange
        {
            add { AddSubscriber(NickChangeSubscribers, value); }
            remove { RemoveSubscriber(NickChangeSubscribers, value); }
        }

        public event EventHandler<IUserLeftChannelEventArgs> UserLeftChannel
        {
            add { AddSubscriber(UserLeftChannelSubscribers, value); }
            remove { RemoveSubscriber(UserLeftChannelSubscribers, value); }
        }

        public event EventHandler<IUserQuitServerEventArgs> UserQuitServer
        {
            add { AddSubscriber(UserQuitServerSubscribers, value); }
            remove { RemoveSubscriber(UserQuitServerSubscribers, value); }
        }

        public event EventHandler<OutgoingMessageEventArgs> OutgoingChannelMessage
        {
            add { AddSubscriber(OutgoingChannelMessageSubscribers, value); }
            remove { RemoveSubscriber(OutgoingChannelMessageSubscribers, value); }
        }

        public event EventHandler<OutgoingMessageEventArgs> OutgoingChannelAction
        {
            add { AddSubscriber(OutgoingChannelActionSubscribers, value); }
            remove { RemoveSubscriber(OutgoingChannelActionSubscribers, value); }
        }

        public event EventHandler<OutgoingMessageEventArgs> OutgoingChannelNotice
        {
            add { AddSubscriber(OutgoingChannelNoticeSubscribers, value); }
            remove { RemoveSubscriber(OutgoingChannelNoticeSubscribers, value); }
        }

        public event EventHandler<OutgoingMessageEventArgs> OutgoingQueryMessage
        {
            add { AddSubscriber(OutgoingQueryMessageSubscribers, value); }
            remove { RemoveSubscriber(OutgoingQueryMessageSubscribers, value); }
        }

        public event EventHandler<OutgoingMessageEventArgs> OutgoingQueryAction
        {
            add { AddSubscriber(OutgoingQueryActionSubscribers, value); }
            remove { RemoveSubscriber(OutgoingQueryActionSubscribers, value); }
        }

        public event EventHandler<OutgoingMessageEventArgs> OutgoingQueryNotice
        {
            add { AddSubscriber(OutgoingQueryNoticeSubscribers, value); }
            remove { RemoveSubscriber(OutgoingQueryNoticeSubscribers, value); }
        }

        public event EventHandler<BaseNickChangedEventArgs> BaseNickChanged
        {
            add { AddSubscriber(BaseNickChangedSubscribers, value); }
            remove { RemoveSubscriber(BaseNickChangedSubscribers, value); }
        }

        public event EventHandler<IUserInvitedToChannelEventArgs> Invited
        {
            add { AddSubscriber(InvitedSubscribers, value); }
            remove { RemoveSubscriber(InvitedSubscribers, value); }
        }

        public event EventHandler<MessageChunkingEventArgs> SplitToChunks
        {
            add { AddSubscriber(SplitToChunksSubscribers, value); }
            remove { RemoveSubscriber(SplitToChunksSubscribers, value); }
        }

        public event SharpIrcBotEventHandler<ICTCPEventArgs> CTCPRequest
        {
            add { AddSubscriber(CTCPRequestSubscribers, value); }
            remove { RemoveSubscriber(CTCPRequestSubscribers, value); }
        }

        public event SharpIrcBotEventHandler<ICTCPEventArgs> CTCPReply
        {
            add { AddSubscriber(CTCPReplySubscribers, value); }
            remove { RemoveSubscriber(CTCPReplySubscribers, value); }
        }
        #endregion

        #region OnEvent methods (per event)
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

        protected virtual void OnSplitToChunks(MessageChunkingEventArgs e)
        {
            HandleEvent(SplitToChunksSubscribers, e, "splitting message to chunks");
        }

        protected virtual void OnCTCPRequest(ICTCPEventArgs e)
        {
            HandleSharpIrcBotEvent(CTCPRequestSubscribers, e, "CTCP request");
        }

        protected virtual void OnCTCPReply(ICTCPEventArgs e)
        {
            HandleSharpIrcBotEvent(CTCPReplySubscribers, e, "CTCP reply");
        }
        #endregion

        #region utility functions (shared)
        private static void AddSubscriber<THandler>(List<THandler> list, THandler subscriber)
            where THandler : class
        {
            lock (list)
            {
                list.Add(subscriber);
            }
        }

        private static void RemoveSubscriber<THandler>(List<THandler> list, THandler subscriber)
            where THandler : class
        {
            lock (list)
            {
                list.RemoveAll(sub =>
                {
                    return (sub == subscriber);
                });
            }
        }

        protected virtual void HandleSharpIrcBotEvent<T>(List<SharpIrcBotEventHandler<T>> subscribers,
                T e, string description)
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

            MessageFlags flags = FlagsForNick(e.SenderNickname);
            foreach (SharpIrcBotEventHandler<T> subscriber in subscriberList)
            {
                try
                {
                    subscriber(this, e, flags);
                }
                catch (Exception exc)
                {
                    Logger.LogError("error when {Subscriber} was handling {EventType}: {Exception}", subscriber, description, exc);
                }
            }
        }

        protected virtual void HandleEvent<T>(List<EventHandler<T>> subscribers, T e, string description)
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

            foreach (EventHandler<T> subscriber in subscriberList)
            {
                try
                {
                    subscriber(this, e);
                }
                catch (Exception exc)
                {
                    Logger.LogError("error when {Subscriber} was handling {EventType}: {Exception}", subscriber, description, exc);
                }
            }
        }
        #endregion
    }
}
