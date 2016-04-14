using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DatabaseNickMapping.ORM;
using log4net;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;

namespace DatabaseNickMapping
{
    public class DatabaseNickMappingPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly Regex LinkRegex = new Regex("^!linknicks\\s+(?<baseNick>\\S+)\\s+(?<aliasNick>\\S+)\\s*$", RegexOptions.Compiled);
        public static readonly Regex UnlinkRegex = new Regex("^!unlinknick\\s+(?<nick>\\S+)\\s*$", RegexOptions.Compiled);
        public static readonly Regex BaseNickRegex = new Regex("^!basenick\\s+(?<nick>\\S+)\\s*$", RegexOptions.Compiled);
        public static readonly Regex PseudoRegisterRegex = new Regex("^!pseudo(?<unregister>un)?register\\s+(?<nick>\\S+)\\s*$", RegexOptions.Compiled);

        protected IConnectionManager ConnectionManager;
        protected DatabaseNickMappingConfig Config;

        public DatabaseNickMappingPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new DatabaseNickMappingConfig(config);

            ConnectionManager.ChannelMessage += HandleChannelMessage;
            ConnectionManager.NickMapping += HandleNickMapping;
        }

        public void ReloadConfiguration(JObject newConfig)
        {
            Config = new DatabaseNickMappingConfig(newConfig);
        }

        protected virtual void HandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            try
            {
                ActuallyHandleChannelMessage(sender, args, flags);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        protected virtual void HandleNickMapping(object sender, NickMappingEventArgs args)
        {
            try
            {
                ActuallyHandleNickMapping(sender, args);
            }
            catch (Exception exc)
            {
                Logger.Error("error handling message", exc);
            }
        }

        protected virtual void ActuallyHandleChannelMessage(object sender, IChannelMessageEventArgs args, MessageFlags flags)
        {
            if (flags.HasFlag(MessageFlags.UserBanned))
            {
                return;
            }

            var message = args.Message;
            var channel = args.Channel;
            var requestor = args.SenderNickname;

            var linkMatch = LinkRegex.Match(message);
            var unlinkMatch = UnlinkRegex.Match(message);
            var pseudoRegisterMatch = PseudoRegisterRegex.Match(message);

            if (linkMatch.Success || unlinkMatch.Success || pseudoRegisterMatch.Success)
            {
                if (ConnectionManager.GetChannelLevelForUser(channel, requestor) < ChannelUserLevel.HalfOp)
                {
                    ConnectionManager.SendChannelMessageFormat(channel, "{0}: You need to be an op to do that.", requestor);
                    return;
                }
            }

            if (linkMatch.Success)
            {
                var baseNickInput = linkMatch.Groups["baseNick"].Value;
                var aliasNickInput = linkMatch.Groups["aliasNick"].Value;

                Logger.InfoFormat("{0} in {1} creating {2} alias {3}", requestor, channel, baseNickInput, aliasNickInput);

                using (var ctx = GetNewContext())
                {
                    var baseNick = FindBaseNickFor(baseNickInput, ctx);
                    string aliasNick = aliasNickInput;

                    if (baseNick == null)
                    {
                        // arguments switched around?
                        var aliasAsBaseNick = FindBaseNickFor(aliasNickInput, ctx);
                        if (aliasAsBaseNick == null)
                        {
                            Logger.DebugFormat("performing new registration of nickname {0}", baseNickInput);

                            // perform new registration
                            var baseNickEntry = new BaseNickname
                            {
                                Nickname = baseNickInput
                            };
                            ctx.BaseNicknames.Add(baseNickEntry);
                            ctx.SaveChanges();
                            baseNick = baseNickInput;
                        }
                        else
                        {
                            Logger.DebugFormat("instead of adding {0} alias {1}, adding {2} alias {0}", baseNickInput, aliasNickInput, aliasAsBaseNick);
                            baseNick = aliasAsBaseNick;
                            aliasNick = baseNickInput;
                        }
                    }
                    else
                    {
                        // already linked?
                        var aliasNickBase = FindBaseNickFor(aliasNickInput, ctx);
                        if (aliasNickBase != null)
                        {
                            ConnectionManager.SendChannelMessageFormat(channel, "{0}: The nickname {1} is already linked to {2}.", requestor, aliasNickInput, aliasNickBase);
                            return;
                        }
                    }

                    Logger.DebugFormat("adding {0} alias {1}", baseNick, aliasNick);
                    var mappingEntry = new NickMapping
                    {
                        BaseNickname = baseNick,
                        MappedNicknameLowercase = aliasNick.ToLowerInvariant()
                    };
                    ctx.NickMappings.Add(mappingEntry);
                    ctx.SaveChanges();

                    // trigger update
                    ConnectionManager.ReportBaseNickChange(aliasNick, baseNick);

                    ConnectionManager.SendChannelMessageFormat(channel, "{0}: {1} is now an alias for {2}.", requestor, aliasNick, baseNick);
                }
            }

            if (unlinkMatch.Success)
            {
                using (var ctx = GetNewContext())
                {
                    var unlinkNickInput = unlinkMatch.Groups["nick"].Value;
                    var unlinkNickLower = unlinkNickInput.ToLowerInvariant();

                    var unlinkBaseObject = ctx.BaseNicknames.FirstOrDefault(bn => bn.Nickname.ToLower() == unlinkNickLower);
                    if (unlinkBaseObject != null)
                    {
                        ConnectionManager.SendChannelMessageFormat(channel, "{0}: {1} is the base nickname and cannot be unlinked.", requestor, unlinkNickInput);
                        return;
                    }

                    var entryToUnlink = ctx.NickMappings.FirstOrDefault(nm => nm.MappedNicknameLowercase == unlinkNickLower);
                    if (entryToUnlink == null)
                    {
                        ConnectionManager.SendChannelMessageFormat(channel, "{0}: {1} is not mapped to any nickname.", requestor, unlinkNickInput);
                        return;
                    }
                    var baseNick = entryToUnlink.BaseNickname;
                    ctx.NickMappings.Remove(entryToUnlink);
                    ctx.SaveChanges();

                    ConnectionManager.SendChannelMessageFormat(channel, "{0}: {1} is no longer an alias for {2}.", requestor, unlinkNickInput, baseNick);
                }
            }

            if (pseudoRegisterMatch.Success)
            {
                using (var ctx = GetNewContext())
                {
                    bool unregister = pseudoRegisterMatch.Groups["unregister"].Success;
                    var nickToRegister = pseudoRegisterMatch.Groups["nick"].Value;
                    var nickToRegisterLowercase = nickToRegister.ToLowerInvariant();

                    if (unregister)
                    {
                        var foundEntry = ctx.BaseNicknames.FirstOrDefault(bn => bn.Nickname.ToLower() == nickToRegisterLowercase);
                        if (foundEntry == null)
                        {
                            ConnectionManager.SendChannelMessageFormat(channel, "{0}: The nickname {1} is not registered.", requestor, nickToRegister);
                            return;
                        }

                        var allMappings = ctx.NickMappings.Where(bn => bn.BaseNickname == foundEntry.Nickname);
                        ctx.NickMappings.RemoveRange(allMappings);
                        ctx.BaseNicknames.Remove(foundEntry);
                        ctx.SaveChanges();

                        ConnectionManager.SendChannelMessageFormat(channel, "{0}: The nickname {1} has been unregistered.", requestor, nickToRegister);
                    }
                    else
                    {
                        var baseNickname = FindBaseNickFor(nickToRegister, ctx);
                        if (baseNickname != null)
                        {
                            ConnectionManager.SendChannelMessageFormat(channel, "{0}: The nickname {1} is already registered as {2}.", requestor, baseNickname);
                            return;
                        }

                        var newEntry = new BaseNickname
                        {
                            Nickname = nickToRegister
                        };
                        ctx.BaseNicknames.Add(newEntry);
                        ctx.SaveChanges();

                        ConnectionManager.SendChannelMessageFormat(channel, "{0}: The nickname {1} has been registered.", requestor, nickToRegister);
                    }
                }
            }

            var baseNickMatch = BaseNickRegex.Match(message);
            if (baseNickMatch.Success)
            {
                var whichNick = baseNickMatch.Groups["nick"].Value;
                using (var ctx = GetNewContext())
                {
                    var baseNick = FindBaseNickFor(whichNick, ctx);
                    if (baseNick == null)
                    {
                        ConnectionManager.SendChannelMessageFormat(channel, "{0}: I can't find the nickname {1}.", requestor, whichNick);
                    }
                    else
                    {
                        ConnectionManager.SendChannelMessageFormat(channel, "{0}: The base nickname for {1} is {2}.", requestor, whichNick, baseNick);
                    }
                }
            }
        }

        protected virtual void ActuallyHandleNickMapping(object sender, NickMappingEventArgs args)
        {
            string baseNickname;
            using (var ctx = GetNewContext())
            {
                baseNickname = FindBaseNickFor(args.Nickname, ctx);
            }
            if (baseNickname != null)
            {
                args.MapsTo.Add(baseNickname);
            }
        }

        protected virtual string FindBaseNickFor(string nick, NickMappingContext ctx)
        {
            var lowerNickname = nick.ToLowerInvariant();
            var meAsTarget = ctx.NickMappings.FirstOrDefault(nm => nm.MappedNicknameLowercase == lowerNickname);
            if (meAsTarget != null)
            {
                Logger.DebugFormat("{0} has a base nickname ({1})", nick, meAsTarget.BaseNickname);
                return meAsTarget.BaseNickname;
            }

            var meAsBase = ctx.BaseNicknames.FirstOrDefault(bn => bn.Nickname.ToLower() == lowerNickname);
            if (meAsBase != null)
            {
                Logger.DebugFormat("{0} is the base nickname ({1})", nick, meAsBase.Nickname);
                return meAsBase.Nickname;
            }

            Logger.DebugFormat("{0} not found in the database", nick);
            return null;
        }

        private NickMappingContext GetNewContext()
        {
            var conn = SharpIrcBotUtil.GetDatabaseConnection(Config);
            return new NickMappingContext(conn);
        }
    }
}
