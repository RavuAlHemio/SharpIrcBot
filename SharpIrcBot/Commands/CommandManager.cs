using System;
using System.Collections.Generic;
using SharpIrcBot.Config;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;

namespace SharpIrcBot.Commands
{
    public class CommandManager
    {
        private static readonly LoggerWrapper Logger = LoggerWrapper.Create<CommandManager>();

        public delegate void ChannelCommandHandler(CommandMatch commandMatch, IChannelMessageEventArgs msg);
        public delegate void PrivateMessageCommandHandler(CommandMatch commandMatch, IPrivateMessageEventArgs msg);
        public delegate bool GlobalCommandCallback(CommandMatch commandMatch, IUserMessageEventArgs msg);

        public CommandConfig Config { get; set; }

        protected Dictionary<Command, List<ChannelCommandHandler>> ChannelActionHandlers { get; set; }
        protected Dictionary<Command, List<ChannelCommandHandler>> ChannelMessageHandlers { get; set; }
        protected Dictionary<Command, List<ChannelCommandHandler>> ChannelNoticeHandlers { get; set; }
        protected Dictionary<Command, List<PrivateMessageCommandHandler>> QueryActionHandlers { get; set; }
        protected Dictionary<Command, List<PrivateMessageCommandHandler>> QueryMessageHandlers { get; set; }
        protected Dictionary<Command, List<PrivateMessageCommandHandler>> QueryNoticeHandlers { get; set; }
        protected List<GlobalCommandCallback> GlobalCommandCallbacks { get; set; }

        public CommandManager(CommandConfig config, IConnectionManager connMgr)
        {
            Config = config;

            ChannelActionHandlers = new Dictionary<Command, List<ChannelCommandHandler>>();
            ChannelMessageHandlers = new Dictionary<Command, List<ChannelCommandHandler>>();
            ChannelNoticeHandlers = new Dictionary<Command, List<ChannelCommandHandler>>();
            QueryActionHandlers = new Dictionary<Command, List<PrivateMessageCommandHandler>>();
            QueryMessageHandlers = new Dictionary<Command, List<PrivateMessageCommandHandler>>();
            QueryNoticeHandlers = new Dictionary<Command, List<PrivateMessageCommandHandler>>();
            GlobalCommandCallbacks = new List<GlobalCommandCallback>();

            connMgr.ChannelAction += HandleChannelAction;
            connMgr.ChannelMessage += HandleChannelMessage;
            connMgr.ChannelNotice += HandleChannelNotice;
            connMgr.QueryAction += HandleQueryAction;
            connMgr.QueryMessage += HandleQueryMessage;
            connMgr.QueryNotice += HandleQueryNotice;
        }

        protected virtual void HandleChannelAction(object sender, IChannelMessageEventArgs msg, MessageFlags flags)
        {
            HandleChannelEntry(ChannelActionHandlers, sender, msg, flags);
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs msg, MessageFlags flags)
        {
            HandleChannelEntry(ChannelMessageHandlers, sender, msg, flags);
        }

        protected virtual void HandleChannelNotice(object sender, IChannelMessageEventArgs msg, MessageFlags flags)
        {
            HandleChannelEntry(ChannelNoticeHandlers, sender, msg, flags);
        }

        protected virtual void HandleQueryAction(object sender, IPrivateMessageEventArgs msg, MessageFlags flags)
        {
            HandleQueryEntry(QueryActionHandlers, sender, msg, flags);
        }

        protected virtual void HandleQueryMessage(object sender, IPrivateMessageEventArgs msg, MessageFlags flags)
        {
            HandleQueryEntry(QueryMessageHandlers, sender, msg, flags);
        }

        protected virtual void HandleQueryNotice(object sender, IPrivateMessageEventArgs msg, MessageFlags flags)
        {
            HandleQueryEntry(QueryNoticeHandlers, sender, msg, flags);
        }

        protected virtual bool ApplyGlobalCallbacks(CommandMatch match, IUserMessageEventArgs msg)
        {
            bool callbackSaidNo = false;
            foreach (GlobalCommandCallback callback in GlobalCommandCallbacks)
            {
                try
                {
                    if (!callback.Invoke(match, msg))
                    {
                        callbackSaidNo = true;
                    }
                }
                catch (Exception exc)
                {
                    Logger.LogError(
                        "error when global callback {GlobalCommandCallback} was processing command {Command}: {Exception}",
                        callback, match.CommandName, exc
                    );
                }
            }
            return !callbackSaidNo;
        }

        protected virtual void HandleChannelEntry(Dictionary<Command, List<ChannelCommandHandler>> commandsHandlers,
                object sender, IChannelMessageEventArgs msg, MessageFlags flags)
        {
            string line = msg.Message;
            if (Config.AllowWhitespaceBeforeCommandPrefix)
            {
                line = line.TrimStart();
            }
            if (!line.StartsWith(Config.CommandPrefix))
            {
                // not a command
                return;
            }
            string commandAndArgs = line.Substring(Config.CommandPrefix.Length);

            foreach (KeyValuePair<Command, List<ChannelCommandHandler>> commandHandler in commandsHandlers)
            {
                // attempt to parse this command
                CommandMatch match = commandHandler.Key.Match(Config, commandAndArgs, flags);
                if (match == null)
                {
                    continue;
                }

                // check result of global callbacks
                if (!ApplyGlobalCallbacks(match, msg))
                {
                    continue;
                }

                // distribute
                foreach (ChannelCommandHandler handler in commandHandler.Value)
                {
                    try
                    {
                        handler.Invoke(match, msg);
                    }
                    catch (Exception exc)
                    {
                        Logger.LogError(
                            "error when {Handler} was handling command {Command}: {Exception}",
                            handler, match.CommandName, exc
                        );
                    }
                }
            }
        }

        protected virtual void HandleQueryEntry(
                Dictionary<Command, List<PrivateMessageCommandHandler>> commandsHandlers, object sender,
                IPrivateMessageEventArgs msg, MessageFlags flags)
        {
            string line = msg.Message;
            if (Config.AllowWhitespaceBeforeCommandPrefix)
            {
                line = line.TrimStart();
            }
            if (!line.StartsWith(Config.CommandPrefix))
            {
                // not a command
                return;
            }
            string commandAndArgs = line.Substring(Config.CommandPrefix.Length);

            foreach (KeyValuePair<Command, List<PrivateMessageCommandHandler>> commandHandler in commandsHandlers)
            {
                // attempt to parse this command
                CommandMatch match = commandHandler.Key.Match(Config, commandAndArgs, flags);
                if (match == null)
                {
                    continue;
                }

                // check result of global callbacks
                if (!ApplyGlobalCallbacks(match, msg))
                {
                    continue;
                }

                // distribute
                foreach (PrivateMessageCommandHandler handler in commandHandler.Value)
                {
                    try
                    {
                        handler.Invoke(match, msg);
                    }
                    catch (Exception exc)
                    {
                        Logger.LogError(
                            "error when {Handler} was handling command {Command}: {Exception}",
                            handler, match.CommandName, exc
                        );
                    }
                }
            }
        }

        public void RegisterChannelActionCommandHandler(Command command, ChannelCommandHandler handler)
        {
            RegisterHandler(ChannelActionHandlers, command, handler);
        }

        public void RegisterChannelMessageCommandHandler(Command command, ChannelCommandHandler handler)
        {
            RegisterHandler(ChannelMessageHandlers, command, handler);
        }

        public void RegisterChannelNoticeCommandHandler(Command command, ChannelCommandHandler handler)
        {
            RegisterHandler(ChannelNoticeHandlers, command, handler);
        }

        public void RegisterQueryActionCommandHandler(Command command, PrivateMessageCommandHandler handler)
        {
            RegisterHandler(QueryActionHandlers, command, handler);
        }

        public void RegisterQueryMessageCommandHandler(Command command, PrivateMessageCommandHandler handler)
        {
            RegisterHandler(QueryMessageHandlers, command, handler);
        }

        public void RegisterQueryNoticeCommandHandler(Command command, PrivateMessageCommandHandler handler)
        {
            RegisterHandler(QueryNoticeHandlers, command, handler);
        }

        public void UnregisterChannelActionCommandHandler(Command command, ChannelCommandHandler handler)
        {
            UnregisterHandler(ChannelActionHandlers, command, handler);
        }

        public void UnregisterChannelMessageCommandHandler(Command command, ChannelCommandHandler handler)
        {
            UnregisterHandler(ChannelMessageHandlers, command, handler);
        }

        public void UnregisterChannelNoticeCommandHandler(Command command, ChannelCommandHandler handler)
        {
            UnregisterHandler(ChannelNoticeHandlers, command, handler);
        }

        public void UnregisterQueryActionCommandHandler(Command command, PrivateMessageCommandHandler handler)
        {
            UnregisterHandler(QueryActionHandlers, command, handler);
        }

        public void UnregisterQueryMessageCommandHandler(Command command, PrivateMessageCommandHandler handler)
        {
            UnregisterHandler(QueryMessageHandlers, command, handler);
        }

        public void UnregisterQueryNoticeCommandHandler(Command command, PrivateMessageCommandHandler handler)
        {
            UnregisterHandler(QueryNoticeHandlers, command, handler);
        }

        protected void RegisterHandler<THandler>(Dictionary<Command, List<THandler>> handlers, Command command,
                THandler handler)
        {
            List<THandler> handlerList;
            if (handlers.TryGetValue(command, out handlerList))
            {
                handlerList.Add(handler);
            }
            else
            {
                handlerList = new List<THandler> { handler };
                handlers[command] = handlerList;
            }
        }

        protected bool UnregisterHandler<THandler>(Dictionary<Command, List<THandler>> handlers, Command command,
                THandler handler)
        {
            List<THandler> handlerList;
            if (!handlers.TryGetValue(command, out handlerList))
            {
                return false;
            }
            return handlerList.Remove(handler);
        }

        public void RegisterGlobalCommandCallback(GlobalCommandCallback callback)
        {
            GlobalCommandCallbacks.Add(callback);
        }

        public bool UnregisterGlobalCommandCallback(GlobalCommandCallback callback)
        {
            return GlobalCommandCallbacks.Remove(callback);
        }
    }
}
