using System.Linq;
using System.Text.RegularExpressions;
using DatabaseNickMapping.ORM;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;

namespace DatabaseNickMapping
{
    public class DatabaseNickMappingPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<DatabaseNickMappingPlugin>();
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
                    ConnectionManager.SendChannelMessage(channel, $"{requestor}: You need to be an op to do that.");
                    return;
                }
            }

            if (linkMatch.Success)
            {
                var baseNickInput = linkMatch.Groups["baseNick"].Value;
                var aliasNickInput = linkMatch.Groups["aliasNick"].Value;

                Logger.LogInformation($"{requestor} in {channel} creating {baseNickInput} alias {aliasNickInput}");

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
                            Logger.LogDebug($"performing new registration of nickname {baseNickInput}");

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
                            Logger.LogDebug($"instead of adding {baseNickInput} alias {aliasNickInput}, adding {aliasAsBaseNick} alias {baseNickInput}");
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
                            if (aliasNickBase == baseNick)
                            {
                                ConnectionManager.SendChannelMessage(channel, $"{requestor}: The nicknames {baseNickInput} and {aliasNickInput} are already linked; the base nick is {baseNick}.");
                            }
                            else
                            {
                                ConnectionManager.SendChannelMessage(channel, $"{requestor}: The nickname {baseNickInput} is already linked to {baseNick} and {aliasNickInput} is already linked to {aliasNickBase}.");
                            }
                            return;
                        }
                    }

                    Logger.LogDebug($"adding {baseNick} alias {aliasNick}");
                    var mappingEntry = new NickMapping
                    {
                        BaseNickname = baseNick,
                        MappedNicknameLowercase = aliasNick.ToLowerInvariant()
                    };
                    ctx.NickMappings.Add(mappingEntry);
                    ctx.SaveChanges();

                    // trigger update
                    ConnectionManager.ReportBaseNickChange(aliasNick, baseNick);

                    ConnectionManager.SendChannelMessage(channel, $"{requestor}: {aliasNick} is now an alias for {baseNick}.");
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
                        ConnectionManager.SendChannelMessage(channel, $"{requestor}: {unlinkNickInput} is the base nickname and cannot be unlinked.");
                        return;
                    }

                    var entryToUnlink = ctx.NickMappings.FirstOrDefault(nm => nm.MappedNicknameLowercase == unlinkNickLower);
                    if (entryToUnlink == null)
                    {
                        ConnectionManager.SendChannelMessage(channel, $"{requestor}: {unlinkNickInput} is not mapped to any nickname.");
                        return;
                    }
                    var baseNick = entryToUnlink.BaseNickname;
                    ctx.NickMappings.Remove(entryToUnlink);
                    ctx.SaveChanges();

                    ConnectionManager.SendChannelMessage(channel, $"{requestor}: {unlinkNickInput} is no longer an alias for {baseNick}.");
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
                            ConnectionManager.SendChannelMessage(channel, $"{requestor}: The nickname {nickToRegister} is not registered.");
                            return;
                        }

                        var allMappings = ctx.NickMappings.Where(bn => bn.BaseNickname == foundEntry.Nickname);
                        ctx.NickMappings.RemoveRange(allMappings);
                        ctx.BaseNicknames.Remove(foundEntry);
                        ctx.SaveChanges();

                        ConnectionManager.SendChannelMessage(channel, $"{requestor}: The nickname {nickToRegister} has been unregistered.");
                    }
                    else
                    {
                        var baseNickname = FindBaseNickFor(nickToRegister, ctx);
                        if (baseNickname != null)
                        {
                            ConnectionManager.SendChannelMessage(channel, $"{requestor}: The nickname {nickToRegister} is already registered as {baseNickname}.");
                            return;
                        }

                        var newEntry = new BaseNickname
                        {
                            Nickname = nickToRegister
                        };
                        ctx.BaseNicknames.Add(newEntry);
                        ctx.SaveChanges();

                        ConnectionManager.SendChannelMessage(channel, $"{requestor}: The nickname {nickToRegister} has been registered.");
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
                        ConnectionManager.SendChannelMessage(channel, $"{requestor}: I can't find the nickname {whichNick}.");
                    }
                    else
                    {
                        ConnectionManager.SendChannelMessage(channel, $"{requestor}: The base nickname for {whichNick} is {baseNick}.");
                    }
                }
            }
        }

        protected virtual void HandleNickMapping(object sender, NickMappingEventArgs args)
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
                Logger.LogDebug($"{nick} has a base nickname ({meAsTarget.BaseNickname})");
                return meAsTarget.BaseNickname;
            }

            var meAsBase = ctx.BaseNicknames.FirstOrDefault(bn => bn.Nickname.ToLower() == lowerNickname);
            if (meAsBase != null)
            {
                Logger.LogDebug($"{nick} is the base nickname ({meAsBase.Nickname})");
                return meAsBase.Nickname;
            }

            Logger.LogDebug($"{nick} not found in the database");
            return null;
        }

        private NickMappingContext GetNewContext()
        {
            var opts = SharpIrcBotUtil.GetContextOptions<NickMappingContext>(Config);
            return new NickMappingContext(opts);
        }
    }
}
