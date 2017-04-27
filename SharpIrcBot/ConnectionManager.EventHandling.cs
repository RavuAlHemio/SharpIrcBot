using System;
using System.Collections.Generic;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;
using Microsoft.Extensions.Logging;

namespace SharpIrcBot
{
    public partial class ConnectionManager
    {
        #region handler lists (per event)
        protected List<WeakReference<SharpIrcBotEventHandler<IChannelMessageEventArgs>>> ChannelMessageSubscribers = new List<WeakReference<SharpIrcBotEventHandler<IChannelMessageEventArgs>>>();
        protected List<WeakReference<SharpIrcBotEventHandler<IChannelMessageEventArgs>>> ChannelActionSubscribers = new List<WeakReference<SharpIrcBotEventHandler<IChannelMessageEventArgs>>>();
        protected List<WeakReference<SharpIrcBotEventHandler<IChannelMessageEventArgs>>> ChannelNoticeSubscribers = new List<WeakReference<SharpIrcBotEventHandler<IChannelMessageEventArgs>>>();
        protected List<WeakReference<SharpIrcBotEventHandler<IPrivateMessageEventArgs>>> QueryMessageSubscribers = new List<WeakReference<SharpIrcBotEventHandler<IPrivateMessageEventArgs>>>();
        protected List<WeakReference<SharpIrcBotEventHandler<IPrivateMessageEventArgs>>> QueryActionSubscribers = new List<WeakReference<SharpIrcBotEventHandler<IPrivateMessageEventArgs>>>();
        protected List<WeakReference<SharpIrcBotEventHandler<IPrivateMessageEventArgs>>> QueryNoticeSubscribers = new List<WeakReference<SharpIrcBotEventHandler<IPrivateMessageEventArgs>>>();
        protected List<WeakReference<EventHandler<EventArgs>>> ConnectedToServerSubscribers = new List<WeakReference<EventHandler<EventArgs>>>();
        protected List<WeakReference<EventHandler<NickMappingEventArgs>>> NickMappingSubscribers = new List<WeakReference<EventHandler<NickMappingEventArgs>>>();
        protected List<WeakReference<EventHandler<IRawMessageEventArgs>>> RawMessageSubscribers = new List<WeakReference<EventHandler<IRawMessageEventArgs>>>();
        protected List<WeakReference<EventHandler<INameListEventArgs>>> NamesInChannelSubscribers = new List<WeakReference<EventHandler<INameListEventArgs>>>();
        protected List<WeakReference<EventHandler<IUserJoinedChannelEventArgs>>> JoinedChannelSubscribers = new List<WeakReference<EventHandler<IUserJoinedChannelEventArgs>>>();
        protected List<WeakReference<EventHandler<INickChangeEventArgs>>> NickChangeSubscribers = new List<WeakReference<EventHandler<INickChangeEventArgs>>>();
        protected List<WeakReference<EventHandler<IUserLeftChannelEventArgs>>> UserLeftChannelSubscribers = new List<WeakReference<EventHandler<IUserLeftChannelEventArgs>>>();
        protected List<WeakReference<EventHandler<IUserQuitServerEventArgs>>> UserQuitServerSubscribers = new List<WeakReference<EventHandler<IUserQuitServerEventArgs>>>();
        protected List<WeakReference<EventHandler<OutgoingMessageEventArgs>>> OutgoingChannelMessageSubscribers = new List<WeakReference<EventHandler<OutgoingMessageEventArgs>>>();
        protected List<WeakReference<EventHandler<OutgoingMessageEventArgs>>> OutgoingChannelActionSubscribers = new List<WeakReference<EventHandler<OutgoingMessageEventArgs>>>();
        protected List<WeakReference<EventHandler<OutgoingMessageEventArgs>>> OutgoingChannelNoticeSubscribers = new List<WeakReference<EventHandler<OutgoingMessageEventArgs>>>();
        protected List<WeakReference<EventHandler<OutgoingMessageEventArgs>>> OutgoingQueryMessageSubscribers = new List<WeakReference<EventHandler<OutgoingMessageEventArgs>>>();
        protected List<WeakReference<EventHandler<OutgoingMessageEventArgs>>> OutgoingQueryActionSubscribers = new List<WeakReference<EventHandler<OutgoingMessageEventArgs>>>();
        protected List<WeakReference<EventHandler<OutgoingMessageEventArgs>>> OutgoingQueryNoticeSubscribers = new List<WeakReference<EventHandler<OutgoingMessageEventArgs>>>();
        protected List<WeakReference<EventHandler<BaseNickChangedEventArgs>>> BaseNickChangedSubscribers = new List<WeakReference<EventHandler<BaseNickChangedEventArgs>>>();
        protected List<WeakReference<EventHandler<IUserInvitedToChannelEventArgs>>> InvitedSubscribers = new List<WeakReference<EventHandler<IUserInvitedToChannelEventArgs>>>();
        protected List<WeakReference<EventHandler<MessageChunkingEventArgs>>> SplitToChunksSubscribers = new List<WeakReference<EventHandler<MessageChunkingEventArgs>>>();
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
        #endregion

        #region utility functions (shared)
        private static void AddSubscriber<THandler>(List<WeakReference<THandler>> list, THandler subscriber)
            where THandler : class
        {
            lock (list)
            {
                list.Add(new WeakReference<THandler>(subscriber));
            }
        }

        private static void RemoveSubscriber<THandler>(List<WeakReference<THandler>> list, THandler subscriber)
            where THandler : class
        {
            lock (list)
            {
                list.RemoveAll(sub =>
                {
                    THandler handler;
                    if (!sub.TryGetTarget(out handler))
                    {
                        // handler has been destroyed; remove
                        return true;
                    }

                    return (handler == subscriber);
                });
            }
        }

        private static void RemoveDestroyed<THandler>(List<WeakReference<THandler>> handlers)
            where THandler : class
        {
            lock (handlers)
            {
                handlers.RemoveAll(sub =>
                {
                    THandler handler;
                    return !sub.TryGetTarget(out handler);
                });
            }
        }

        protected virtual void HandleSharpIrcBotEvent<T>(List<WeakReference<SharpIrcBotEventHandler<T>>> subscribers,
                T e, string description)
            where T : IUserMessageEventArgs
        {
            List<WeakReference<SharpIrcBotEventHandler<T>>> subscriberList;
            lock (subscribers)
            {
                subscriberList = new List<WeakReference<SharpIrcBotEventHandler<T>>>(subscribers);
            }

            if (subscriberList.Count == 0)
            {
                return;
            }

            bool needsDestroyedCleanup = false;
            MessageFlags flags = FlagsForNick(e.SenderNickname);
            foreach (WeakReference<SharpIrcBotEventHandler<T>> subscriberReference in subscriberList)
            {
                SharpIrcBotEventHandler<T> subscriber;
                if (!subscriberReference.TryGetTarget(out subscriber))
                {
                    // destroyed in the meantime
                    needsDestroyedCleanup = true;
                    continue;
                }

                try
                {
                    subscriber(this, e, flags);
                }
                catch (Exception exc)
                {
                    Logger.LogError("error when {Subscriber} was handling {EventType}: {Exception}", subscriber, description, exc);
                }
            }

            if (needsDestroyedCleanup)
            {
                RemoveDestroyed(subscribers);
            }
        }

        protected virtual void HandleEvent<T>(List<WeakReference<EventHandler<T>>> subscribers, T e, string description)
        {
            List<WeakReference<EventHandler<T>>> subscriberList;
            lock (subscribers)
            {
                subscriberList = new List<WeakReference<EventHandler<T>>>(subscribers);
            }

            if (subscriberList.Count == 0)
            {
                return;
            }

            bool needsDestroyedCleanup = false;
            foreach (WeakReference<EventHandler<T>> subscriberReference in subscriberList)
            {
                EventHandler<T> subscriber;
                if (!subscriberReference.TryGetTarget(out subscriber))
                {
                    // destroyed in the meantime
                    needsDestroyedCleanup = true;
                    continue;
                }

                try
                {
                    subscriber(this, e);
                }
                catch (Exception exc)
                {
                    Logger.LogError("error when {Subscriber} was handling {EventType}: {Exception}", subscriber, description, exc);
                }
            }

            if (needsDestroyedCleanup)
            {
                RemoveDestroyed(subscribers);
            }
        }
        #endregion
    }
}
