using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Plugins.DatabaseNickMapping.ORM;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.DatabaseNickMapping
{
    public class DatabaseNickMappingPlugin : IPlugin, IReloadableConfiguration
    {
        private static readonly ILogger Logger = LogUtil.LoggerFactory.CreateLogger<DatabaseNickMappingPlugin>();

        protected IConnectionManager ConnectionManager;
        protected DatabaseNickMappingConfig Config;

        public DatabaseNickMappingPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new DatabaseNickMappingConfig(config);

            ConnectionManager.NickMapping += HandleNickMapping;

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("linknicks"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker, // first nick (base)
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // second nick (alias)
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleLinkNicksCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("unlinknick"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // nick
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleUnlinkNickCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("basenick"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // nick
                    ),
                    CommandUtil.MakeTags("fun"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleBaseNickCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("pseudoregister", "pseudounregister"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // nick
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandlePseudoRegisterCommand
            );
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new DatabaseNickMappingConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
        }

        protected virtual void HandleLinkNicksCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            string channel = msg.Channel;
            string requestor = msg.SenderNickname;
            if (ConnectionManager.GetChannelLevelForUser(channel, requestor) < ChannelUserLevel.HalfOp)
            {
                ConnectionManager.SendChannelMessage(channel, $"{requestor}: You need to be an op to do that.");
                return;
            }

            var baseNickInput = (string)cmd.Arguments[0];
            var aliasNickInput = (string)cmd.Arguments[1];

            Logger.LogInformation(
                "{Requestor} in {Channel} creating {BaseNickInput} alias {AliasNickInput}",
                requestor, channel, baseNickInput, aliasNickInput
            );

            using (var ctx = GetNewContext())
            {
                string baseNick = FindBaseNickFor(baseNickInput, ctx);
                string aliasNick = aliasNickInput;

                if (baseNick == null)
                {
                    // arguments switched around?
                    string aliasAsBaseNick = FindBaseNickFor(aliasNickInput, ctx);
                    if (aliasAsBaseNick == null)
                    {
                        Logger.LogDebug("performing new registration of nickname {BaseNickInput}", baseNick);

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
                        Logger.LogDebug(
                            "instead of adding {BaseNickInput} alias {AliasNickInput}, adding {AliasAsBaseNick} alias {BaseNickInput}",
                            baseNickInput, aliasNickInput, aliasAsBaseNick, baseNickInput
                        );
                        baseNick = aliasAsBaseNick;
                        aliasNick = baseNickInput;
                    }
                }
                else
                {
                    // already linked?
                    string aliasNickBase = FindBaseNickFor(aliasNickInput, ctx);
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

                Logger.LogDebug("adding {BaseNick} alias {AliasNick}", baseNick, aliasNick);
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

        protected virtual void HandleUnlinkNickCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            string channel = msg.Channel;
            string requestor = msg.SenderNickname;
            if (ConnectionManager.GetChannelLevelForUser(channel, requestor) < ChannelUserLevel.HalfOp)
            {
                ConnectionManager.SendChannelMessage(channel, $"{requestor}: You need to be an op to do that.");
                return;
            }

            using (var ctx = GetNewContext())
            {
                var unlinkNickInput = (string)cmd.Arguments[0];
                string unlinkNickLower = unlinkNickInput.ToLowerInvariant();

                BaseNickname unlinkBaseObject = ctx.BaseNicknames.FirstOrDefault(bn => bn.Nickname.ToLower() == unlinkNickLower);
                if (unlinkBaseObject != null)
                {
                    ConnectionManager.SendChannelMessage(
                        channel,
                        $"{requestor}: {unlinkNickInput} is the base nickname and cannot be unlinked."
                    );
                    return;
                }

                var entryToUnlink = ctx.NickMappings.FirstOrDefault(nm => nm.MappedNicknameLowercase == unlinkNickLower);
                if (entryToUnlink == null)
                {
                    ConnectionManager.SendChannelMessage(
                        channel,
                        $"{requestor}: {unlinkNickInput} is not mapped to any nickname."
                    );
                    return;
                }
                var baseNick = entryToUnlink.BaseNickname;
                ctx.NickMappings.Remove(entryToUnlink);
                ctx.SaveChanges();

                ConnectionManager.SendChannelMessage(
                    channel,
                    $"{requestor}: {unlinkNickInput} is no longer an alias for {baseNick}."
                );
            }
        }

        protected virtual void HandlePseudoRegisterCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            string channel = msg.Channel;
            string requestor = msg.SenderNickname;
            if (ConnectionManager.GetChannelLevelForUser(channel, requestor) < ChannelUserLevel.HalfOp)
            {
                ConnectionManager.SendChannelMessage(channel, $"{requestor}: You need to be an op to do that.");
                return;
            }

            using (var ctx = GetNewContext())
            {
                bool unregister = (cmd.CommandName == "pseudounregister");
                var nickToRegister = (string)cmd.Arguments[0];
                string nickToRegisterLowercase = nickToRegister.ToLowerInvariant();

                if (unregister)
                {
                    BaseNickname foundEntry = ctx.BaseNicknames
                        .FirstOrDefault(bn => bn.Nickname.ToLower() == nickToRegisterLowercase);
                    if (foundEntry == null)
                    {
                        ConnectionManager.SendChannelMessage(
                            channel,
                            $"{requestor}: The nickname {nickToRegister} is not registered."
                        );
                        return;
                    }

                    IQueryable<NickMapping> allMappings = ctx.NickMappings
                        .Where(bn => bn.BaseNickname == foundEntry.Nickname);
                    ctx.NickMappings.RemoveRange(allMappings);
                    ctx.BaseNicknames.Remove(foundEntry);
                    ctx.SaveChanges();

                    ConnectionManager.SendChannelMessage(
                        channel,
                        $"{requestor}: The nickname {nickToRegister} has been unregistered."
                    );
                }
                else
                {
                    string baseNickname = FindBaseNickFor(nickToRegister, ctx);
                    if (baseNickname != null)
                    {
                        ConnectionManager.SendChannelMessage(
                            channel,
                            $"{requestor}: The nickname {nickToRegister} is already registered as {baseNickname}."
                        );
                        return;
                    }

                    var newEntry = new BaseNickname
                    {
                        Nickname = nickToRegister
                    };
                    ctx.BaseNicknames.Add(newEntry);
                    ctx.SaveChanges();

                    ConnectionManager.SendChannelMessage(
                        channel,
                        $"{requestor}: The nickname {nickToRegister} has been registered."
                    );
                }
            }
        }

        protected virtual void HandleBaseNickCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            string channel = msg.Channel;
            string requestor = msg.SenderNickname;

            var whichNick = (string)cmd.Arguments[0];
            using (var ctx = GetNewContext())
            {
                var baseNick = FindBaseNickFor(whichNick, ctx);
                if (baseNick == null)
                {
                    ConnectionManager.SendChannelMessage(
                        channel,
                        $"{requestor}: I can't find the nickname {whichNick}."
                    );
                }
                else
                {
                    ConnectionManager.SendChannelMessage(
                        channel,
                        $"{requestor}: The base nickname for {whichNick} is {baseNick}."
                    );
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
                Logger.LogDebug(
                    "{Nickname} has a base nickname ({BaseNickname})",
                    nick, meAsTarget.BaseNickname
                );
                return meAsTarget.BaseNickname;
            }

            var meAsBase = ctx.BaseNicknames.FirstOrDefault(bn => bn.Nickname.ToLower() == lowerNickname);
            if (meAsBase != null)
            {
                Logger.LogDebug(
                    "{Nickname} is the base nickname ({BaseNickname})",
                    nick, meAsBase.Nickname
                );
                return meAsBase.Nickname;
            }

            Logger.LogDebug("{Nickname} not found in the database", nick);
            return null;
        }

        private NickMappingContext GetNewContext()
        {
            var opts = DatabaseUtil.GetContextOptions<NickMappingContext>(Config);
            return new NickMappingContext(opts);
        }
    }
}
