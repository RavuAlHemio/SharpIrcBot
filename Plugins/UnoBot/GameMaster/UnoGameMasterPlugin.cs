using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Commands;
using SharpIrcBot.Events.Irc;
using SharpIrcBot.Util;
using Timer = System.Threading.Timer;

namespace SharpIrcBot.Plugins.UnoBot.GameMaster
{
    public class UnoGameMasterPlugin : IPlugin
    {
        private static readonly ILogger Logger = LogUtil.LoggerFactory.CreateLogger<UnoGameMasterPlugin>();

        protected static readonly CardColor[] RegularColors = { CardColor.Red, CardColor.Green, CardColor.Blue, CardColor.Yellow };

        protected IConnectionManager ConnectionManager;
        protected GameMasterConfig Config;

        protected bool AttackMode;
        protected bool ExtremeMode;
        protected Pile<Card> DrawPile;
        protected Pile<Card> DiscardPile;
        protected List<Player> Players;
        protected int CurrentPlayerIndex;
        protected bool PlayerOrderReversed;
        protected bool DrewLast;
        protected GameState CurrentGameState;
        protected DateTime? TurnStartedUtc;
        protected Timer TurnTickTimer;
        protected object TurnLock;
        protected Random Randomizer;
        protected long BotTestCount;
        protected DateTime? BotTestJoinRequested;

        public UnoGameMasterPlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new GameMasterConfig(config);

            AttackMode = false;
            ExtremeMode = false;
            DrawPile = new Pile<Card>();
            DiscardPile = new Pile<Card>();
            Players = new List<Player>();
            CurrentPlayerIndex = 0;
            PlayerOrderReversed = false;
            DrewLast = true;
            CurrentGameState = GameState.NoGame;
            TurnStartedUtc = null;
            TurnTickTimer = new Timer(TurnTickTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            TurnLock = new object();
            Randomizer = new Random();
            BotTestCount = 0;
            BotTestJoinRequested = null;

            ConnectionManager.NickChange += HandleNickChange;
            ConnectionManager.UserLeftChannel += HandleUserLeftChannel;
            ConnectionManager.UserQuitServer += HandleUserQuitServer;

            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("uno"),
                    CommandUtil.MakeOptions(
                        CommandUtil.MakeFlag("+a"),
                        CommandUtil.MakeFlag("+e")
                    ),
                    CommandUtil.NoArguments,
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleUnoCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("play", "p"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker, // color or wildcard value
                        CommandUtil.NonzeroStringMatcherRequiredWordTaker // value or change-to-color
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandlePlayCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("bottest"),
                    CommandUtil.NoOptions,
                    CommandUtil.MakeArguments(
                        new LongMatcher().ToRequiredWordTaker() // number of rounds
                    ),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleBotTestCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("join", "botjoin"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleJoinCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("leave"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleLeaveCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("deal"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleDealCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("draw"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleDrawCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("pass"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandlePassCommand
            );
            ConnectionManager.CommandManager.RegisterChannelMessageCommandHandler(
                new Command(
                    CommandUtil.MakeNames("endgame"),
                    forbiddenFlags: MessageFlags.UserBanned
                ),
                HandleEndGameCommand
            );
        }

        public virtual Player CurrentPlayer
        {
            get
            {
                return Players[CurrentPlayerIndex];
            }
        }

        public virtual Card TopCard
        {
            get
            {
                return DiscardPile.Peek();
            }
        }

        protected virtual void AssertPileValidity()
        {
            Debug.Assert(DrawPile.All(CardUtils.IsValid));
            Debug.Assert(DiscardPile.All(CardUtils.IsValid));
        }

        protected virtual void PrepareRegularPiles()
        {
            DrawPile.Clear();
            DiscardPile.Clear();

            // of each color:
            foreach (var color in RegularColors)
            {
                // one zero
                DrawPile.Push(new Card(color, CardValue.Zero));

                // two of:
                for (int i = 0; i < 2; ++i)
                {
                    // each number except zero
                    for (CardValue value = CardValue.One; value <= CardValue.Nine; ++value)
                    {
                        DrawPile.Push(new Card(color, value));
                    }

                    // skip, reverse draw-two
                    DrawPile.Push(new Card(color, CardValue.Skip));
                    DrawPile.Push(new Card(color, CardValue.Reverse));
                    DrawPile.Push(new Card(color, CardValue.DrawTwo));
                }
            }

            // four wilds, four wild-draw-fours
            for (int i = 0; i < 4; ++i)
            {
                DrawPile.Push(new Card(CardColor.Wild, CardValue.Wild));
                DrawPile.Push(new Card(CardColor.Wild, CardValue.WildDrawFour));
            }

            // assert the validity of the piles
            AssertPileValidity();
        }

        protected virtual void PrepareExtremePiles()
        {
            // in addition to the regular version...
            PrepareRegularPiles();

            // ... two more of each special color card (skip, reverse, draw-two)
            foreach (var color in RegularColors)
            {
                for (int i = 0; i < 2; ++i)
                {
                    DrawPile.Push(new Card(color, CardValue.Skip));
                    DrawPile.Push(new Card(color, CardValue.Reverse));
                    DrawPile.Push(new Card(color, CardValue.DrawTwo));
                }
            }

            // ... and four more of each special wild card (wild, wild-draw-four)
            for (int i = 0; i < 4; ++i)
            {
                DrawPile.Push(new Card(CardColor.Wild, CardValue.Wild));
                DrawPile.Push(new Card(CardColor.Wild, CardValue.WildDrawFour));
            }

            // assert the validity of the piles again
            AssertPileValidity();
        }

        protected virtual List<Card> DrawCards(int count)
        {
            var ret = new List<Card>();
            int remainingCount = count;

            if (DrawPile.Count < remainingCount)
            {
                // draw the whole pile
                ret.AddRange(DrawPile.DrawAll());

                // shift all the cards from the discard into the draw pile except the top one
                // wild cards become discolored during this
                var discardTop = DiscardPile.Draw();
                DrawPile.AddRange(DiscardPile.DrawAll().Select(c => c.Value.IsWild() ? new Card(CardColor.Wild, c.Value) : c));
                DiscardPile.Push(discardTop);

                // shuffle the draw pile
                DrawPile.Shuffle(Randomizer);
            }

            remainingCount = Math.Max(count - ret.Count, 0);

            if (DrawPile.Count < remainingCount)
            {
                // oh well, take what's left
                ret.AddRange(DrawPile.DrawAll());
            }
            else
            {
                // take only what we need
                ret.AddRange(DrawPile.DrawMany(remainingCount));
            }

            return ret;
        }

        protected virtual Card DrawCard()
        {
            return DrawCards(1)[0];
        }

        protected virtual void SendEventTo(JObject evt, string playerNick)
        {
            var message = evt.ToString(Formatting.None);

            // split into chunks
            var maxLengthPerChunk = ConnectionManager.MaxLineLength / 2;
            var messageChunks = new List<string>();
            while (message.Length > 0)
            {
                var chunk = message.Substring(0, Math.Min(message.Length, maxLengthPerChunk));
                messageChunks.Add(chunk);
                message = message.Substring(chunk.Length);
            }

            // first chunk is prefixed with the total number of chunks
            if (messageChunks.Count > 0)
            {
                ConnectionManager.SendQueryMessageFormat(playerNick, "{0} {1}", messageChunks.Count.ToString(CultureInfo.InvariantCulture), messageChunks[0]);
            }

            // all the other chunks are, well, the chunks themselves
            foreach (var chunk in messageChunks.Skip(1))
            {
                ConnectionManager.SendQueryMessage(playerNick, chunk);
            }
        }

        protected virtual void BroadcastEventToBots(JObject evt)
        {
            foreach (var bot in Players.Where(p => p.IsBot))
            {
                SendEventTo(evt, bot.Nick);
            }
        }

        protected virtual void BroadcastCurrentCardEvent()
        {
            Debug.Assert(DiscardPile.Count > 0);

            var evt = new JObject
            {
                { "event", "current_card" },
                { "current_card", TopCard.ToFullPlayString() }
            };
            BroadcastEventToBots(evt);
        }

        protected virtual void SendPlayerHandInfoEvent(Player player)
        {
            if (!player.IsBot)
            {
                // never mind
                return;
            }

            var cardNames = player.Hand.Select(c => c.ToFullPlayString());
            var hand = new JArray(cardNames);
            var evt = new JObject
            {
                { "event", "hand_info" },
                { "hand", hand }
            };
            SendEventTo(evt, player.Nick);
        }

        protected virtual void MulticastHandInfoEvents()
        {
            foreach (var bot in Players.Where(p => p.IsBot))
            {
                SendPlayerHandInfoEvent(bot);
            }
        }

        protected virtual void BroadcastCardCountsEvent()
        {
            var cardCountsEnumerable = Players.Select(p => new JObject
            {
                { "player", p.Nick },
                { "count", p.Hand.Count }
            });
            var cardCounts = new JArray(cardCountsEnumerable);
            var evt = new JObject
            {
                { "event", "card_counts" },
                { "counts", cardCounts }
            };
            BroadcastEventToBots(evt);
        }

        protected virtual void BroadcastPlayerPassedEvent(string who)
        {
            var evt = new JObject
            {
                { "event", "player_passed" },
                { "player", who }
            };
            BroadcastEventToBots(evt);
        }

        protected virtual void BroadcastCardPlayedEvent(string who, Card which)
        {
            var evt = new JObject
            {
                { "event", "card_played" },
                { "player", who },
                { "card", which.ToFullPlayString() }
            };
            BroadcastEventToBots(evt);
        }

        protected virtual void BroadcastAnchorPlayerDrewCardEvent(string who)
        {
            var evt = new JObject
            {
                { "event", "player_drew_card" },
                { "player", who }
            };
            BroadcastEventToBots(evt);
        }

        protected virtual void BroadcastAnchorCurrentPlayerOrderEvent()
        {
            var players = new List<Player>(Players);
            int currentPlayerIndex = CurrentPlayerIndex;

            if (PlayerOrderReversed)
            {
                // reverse F E D [C] B A (where cPI == 3)
                // to A B [C] D E F (where cPI == 2)
                players.Reverse();
                currentPlayerIndex = players.Count - currentPlayerIndex - 1;
            }

            // reorder A B [C] D E F (where cPI == 2):
            var order = new List<Player>(players.Count);
            // add [C] D E F (Skip cPI)
            order.AddRange(players.Skip(currentPlayerIndex));
            // add A B (Take cPI)
            order.AddRange(players.Take(currentPlayerIndex));

            var evt = new JObject
            {
                { "event", "current_player_order" },
                { "order", new JArray(order.Select(p => p.Nick)) }
            };
            BroadcastEventToBots(evt);
        }

        protected virtual void BroadcastAnchorGameEndedEvent()
        {
            var evt = new JObject { { "event", "game_ended" } };
            BroadcastEventToBots(evt);
        }

        protected virtual void BroadcastAnchorCardsDealtEvent()
        {
            var evt = new JObject { { "event", "cards_dealt" } };
            BroadcastEventToBots(evt);
        }

        protected virtual void SendPlayerHandNotice(Player player)
        {
            // if the current player is human, private-NOTICE them their hand
            if (CurrentPlayer.IsBot)
            {
                return;
            }

            ConnectionManager.SendQueryNoticeFormat(
                player.Nick,
                "[{0}]",
                player.Hand.Select(c => c.ToFullPlayString()).StringJoin(", ")
            );
        }

        protected virtual void TurnTickTimerElapsed(object sender)
        {
            lock (TurnLock)
            {
                var nowUtc = DateTime.UtcNow;

                if (CurrentGameState == GameState.Preparation && BotTestJoinRequested.HasValue)
                {
                    if ((nowUtc - BotTestJoinRequested.Value) >= TimeSpan.FromSeconds(Config.BotJoinWaitSeconds))
                    {
                        // bots should be joined now; start the game!
                        ConnectionManager.SendChannelMessage(Config.UnoChannel, "That should do it; dealing!");
                        DealGame();
                    }
                }

                if (CurrentGameState != GameState.InProgress || !TurnStartedUtc.HasValue)
                {
                    return;
                }

                if ((nowUtc - TurnStartedUtc.Value) >= TimeSpan.FromSeconds(Config.SecondsPerTurn))
                {
                    // bzzt -- time's up!

                    // make current player draw card
                    ConnectionManager.SendChannelMessageFormat(Config.UnoChannel, "{0} ran out of time and drew a card!", CurrentPlayer.Nick);
                    CurrentPlayer.Hand.Add(DrawCard());
                    SendPlayerHandInfoEvent(CurrentPlayer);

                    // next player
                    AdvanceToNextPlayer();
                }
            }
        }

        protected virtual void HandlePlayCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            if (msg.Channel != Config.UnoChannel)
            {
                return;
            }

            lock (TurnLock)
            {
                if (CurrentPlayer.Nick != msg.SenderNickname)
                {
                    ConnectionManager.SendChannelMessageFormat(Config.UnoChannel, "It's not your turn, {0}.", msg.SenderNickname);
                    return;
                }

                var primaryString = (string)cmd.Arguments[0];
                var secondaryString = (string)cmd.Arguments[1];

                // !play COLOR VALUE for colors or !play VALUE CHOSENCOLOR for wilds
                CardColor? color = CardUtils.ParseColor(primaryString);
                if (!color.HasValue || color.Value == CardColor.Wild)
                {
                    // try parsing it as a value instead (wild card)
                    CardValue? value = CardUtils.ParseValue(primaryString);
                    CardColor? chosenColor = CardUtils.ParseColor(secondaryString);
                    if (!value.HasValue || (value.Value != CardValue.Wild && value.Value != CardValue.WildDrawFour) || !chosenColor.HasValue)
                    {
                        ConnectionManager.SendChannelMessageFormat(
                            Config.UnoChannel,
                            "{0}: Play a card using !play COLOR VALUE for normal cards or !play VALUE CHOSENCOLOR for wild cards!",
                            msg.SenderNickname
                        );
                        return;
                    }

                    // wild cards can be played independent of top card's color or value

                    // take the card from the player's hand
                    var requestedCard = new Card(CardColor.Wild, value.Value);
                    if (!CurrentPlayer.Hand.Remove(requestedCard))
                    {
                        ConnectionManager.SendChannelMessageFormat(
                            Config.UnoChannel,
                            "{0}: You don't have that card.",
                            msg.SenderNickname
                        );
                        return;
                    }

                    // toss its colorful equivalent on top of the discard pile
                    var disCard = new Card(chosenColor.Value, value.Value);
                    DiscardPile.Push(disCard);

                    // if this is a WD4, feed the next player with 4 cards
                    if (value.Value == CardValue.WildDrawFour)
                    {
                        // advancing here and advancing later simply skips this player
                        AdvanceCurrentPlayerIndex();
                        ConnectionManager.SendChannelMessageFormat(
                            Config.UnoChannel,
                            "{0} draws four cards.",
                            CurrentPlayer.Nick
                        );
                        CurrentPlayer.Hand.UnionWith(DrawCards(4));

                        // inform the player of their new hand
                        SendPlayerHandInfoEvent(CurrentPlayer);
                    }
                }
                else
                {
                    CardValue? value = CardUtils.ParseValue(secondaryString);
                    if (!value.HasValue)
                    {
                        ConnectionManager.SendChannelMessageFormat(
                            Config.UnoChannel,
                            "{0}: Play a card using !play COLOR VALUE for normal cards or !play VALUE CHOSENCOLOR for wild cards!",
                            msg.SenderNickname
                        );
                        return;
                    }

                    // see if this card is playable at all
                    if (TopCard.Color != color.Value && TopCard.Value != value.Value)
                    {
                        ConnectionManager.SendChannelMessageFormat(
                            Config.UnoChannel,
                            "{0}: You cannot play this card.",
                            msg.SenderNickname
                        );
                        return;
                    }

                    // take the card from the player's hand
                    var requestedCard = new Card(color.Value, value.Value);
                    if (!CurrentPlayer.Hand.Remove(requestedCard))
                    {
                        ConnectionManager.SendChannelMessageFormat(
                            Config.UnoChannel,
                            "{0}: You don't have that card.",
                            msg.SenderNickname
                        );
                        return;
                    }

                    // toss it on the top of the discard pile
                    DiscardPile.Push(requestedCard);

                    // if this is a Skip (or a Reverse with less than three players), skip the next player
                    if (value.Value == CardValue.Skip || (value.Value == CardValue.Reverse && Players.Count < 3))
                    {
                        AdvanceCurrentPlayerIndex();
                        ConnectionManager.SendChannelMessageFormat(
                            Config.UnoChannel,
                            "{0} has been skipped.",
                            CurrentPlayer.Nick
                        );
                    }
                    // if this is a Reverse (with at least three players), reverse the direction
                    else if (value.Value == CardValue.Reverse)
                    {
                        PlayerOrderReversed = !PlayerOrderReversed;
                        ConnectionManager.SendChannelMessageFormat(
                            Config.UnoChannel,
                            "{0} reversed the player order.",
                            CurrentPlayer.Nick
                        );
                    }
                    // if this is a D2, feed the next player with 2 cards
                    else if (value.Value == CardValue.DrawTwo)
                    {
                        // advancing here and advancing later simply skips this player
                        AdvanceCurrentPlayerIndex();
                        ConnectionManager.SendChannelMessageFormat(
                            Config.UnoChannel,
                            "{0} draws two cards.",
                            CurrentPlayer.Nick
                        );
                        CurrentPlayer.Hand.UnionWith(DrawCards(2));

                        // inform the player of their new hand
                        SendPlayerHandInfoEvent(CurrentPlayer);
                    }
                }

                // is any player out of cards?
                var firstPlayerWithNoCards = Players.FirstOrDefault(p => p.Hand.Count == 0);
                if (firstPlayerWithNoCards != null)
                {
                    // this player wins!
                    ConnectionManager.SendChannelMessageFormat(
                        Config.UnoChannel,
                        "{0} is the winner!",
                        firstPlayerWithNoCards.Nick
                    );
                    Logger.LogInformation("Uno game won by {0}", firstPlayerWithNoCards.Nick);

                    // clean up the game
                    StopGame();

                    if (BotTestCount > 0)
                    {
                        --BotTestCount;
                        if (BotTestCount > 0)
                        {
                            // start another round, inviting all bots!
                            ConnectionManager.SendChannelMessageFormat(
                                Config.UnoChannel,
                                "I'm in bot test mode; {0} game{1} left!",
                                BotTestCount,
                                (BotTestCount == 1) ? "" : "s"
                            );
                            Logger.LogInformation("{Count} bot test game/s left", BotTestCount);
                            PrepareGame();
                            ConnectionManager.SendChannelMessage(Config.UnoChannel, "?join");
                            BotTestJoinRequested = DateTime.UtcNow;
                        }
                    }

                    return;
                }

                // next player's turn!
                AdvanceToNextPlayer();
            }
        }

        protected virtual void HandleUnoCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            if (msg.Channel != Config.UnoChannel)
            {
                return;
            }

            lock (TurnLock)
            {
                switch (CurrentGameState)
                {
                    case GameState.Preparation:
                    case GameState.InProgress:
                        Logger.LogDebug(
                            "{Nickname} is trying to start a game although one is already in progress",
                            msg.SenderNickname
                        );
                        return;
                    case GameState.NoGame:
                        // continue below
                        break;
                    default:
                        Logger.LogError("invalid game state when trying to start new game");
                        return;
                }

                // process options
                AttackMode = cmd.Options.Any(o => o.Key.ToLowerInvariant() == "+a");
                ExtremeMode = cmd.Options.Any(o => o.Key.ToLowerInvariant() == "+e");

                // prepare game
                PrepareGame();

                // add player who launched the game
                Players.Add(Player.Create(msg.SenderNickname));

                ConnectionManager.SendChannelMessageFormat(
                    Config.UnoChannel,
                    "{0} started a game of Uno!",
                    msg.SenderNickname
                );
            }
        }

        protected virtual void HandleBotTestCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            if (msg.Channel != Config.UnoChannel)
            {
                return;
            }

            lock (TurnLock)
            {
                BotTestCount = (long)cmd.Arguments[0];

                ConnectionManager.SendChannelMessageFormat(
                    Config.UnoChannel,
                    "{0} engaged bot test mode; {1} games left!",
                    msg.SenderNickname,
                    BotTestCount
                );

                // prepare a game
                PrepareGame();

                // trigger bot joinage
                ConnectionManager.SendChannelMessage(Config.UnoChannel, "?join");

                // wait for bot joinage
                BotTestJoinRequested = DateTime.UtcNow;
            }
        }

        protected virtual void HandleJoinCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            if (msg.Channel != Config.UnoChannel)
            {
                return;
            }

            bool isBot = (cmd.CommandName == "botjoin");

            lock (TurnLock)
            {
                switch (CurrentGameState)
                {
                    case GameState.NoGame:
                        Logger.LogDebug("{Nickname} is trying to join no game", msg.SenderNickname);
                        return;
                    default:
                        Logger.LogError("invalid game state when trying to add player to game");
                        return;
                    case GameState.Preparation:
                    case GameState.InProgress:
                        // continue below
                        break;
                }

                var existingPlayer = Players.FirstOrDefault(p => p.Nick == msg.SenderNickname);
                if (existingPlayer != null)
                {
                    // player already joined
                    // they become what they chose
                    existingPlayer.IsBot = isBot;
                    ConnectionManager.SendChannelMessageFormat(
                        Config.UnoChannel,
                        "{0} became {1}!",
                        msg.SenderNickname,
                        isBot ? "a bot" : "human"
                    );
                }
                else
                {
                    // add them
                    var newPlayer = Player.Create(msg.SenderNickname, isBot);

                    if (CurrentGameState == GameState.InProgress)
                    {
                        // deal cards to them
                        var drawnCards = DrawCards(Config.InitialDealSize);
                        newPlayer.Hand.UnionWith(drawnCards);
                    }

                    Players.Add(newPlayer);

                    ConnectionManager.SendChannelMessageFormat(
                        Config.UnoChannel,
                        "{0} joined the fray!",
                        msg.SenderNickname
                    );
                }
            }
        }

        protected virtual void HandleLeaveCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            if (msg.Channel != Config.UnoChannel)
            {
                return;
            }

            lock (TurnLock)
            {
                RemovePlayerFromGame(msg.SenderNickname);
            }
        }

        protected virtual void HandleDealCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            if (msg.Channel != Config.UnoChannel)
            {
                return;
            }

            lock (TurnLock)
            {
                switch (CurrentGameState)
                {
                    case GameState.NoGame:
                        Logger.LogDebug("{Nickname} is trying to deal no game", msg.SenderNickname);
                        return;
                    default:
                        Logger.LogError("invalid game state when trying to add player to game");
                        return;
                    case GameState.Preparation:
                    case GameState.InProgress:
                        // continue below
                        break;
                }

                DealGame();
            }
        }

        protected virtual void HandleDrawCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            if (msg.Channel != Config.UnoChannel)
            {
                return;
            }

            lock (TurnLock)
            {
                if (CurrentPlayer.Nick != msg.SenderNickname)
                {
                    ConnectionManager.SendChannelMessageFormat(
                        Config.UnoChannel,
                        "It's not your turn, {0}.",
                        msg.SenderNickname
                    );
                    return;
                }

                if (DrewLast)
                {
                    ConnectionManager.SendChannelMessageFormat(
                        Config.UnoChannel,
                        "You already drew, {0}.",
                        msg.SenderNickname
                    );
                    return;
                }

                // mark this
                DrewLast = true;

                bool drawn = false;
                if (AttackMode)
                {
                    bool wasAttacked = (Randomizer.Next(5) == 0);
                    if (wasAttacked)
                    {
                        int drawCount = Randomizer.Next(7);
                        ConnectionManager.SendChannelMessageFormat(
                            Config.UnoChannel,
                            "{0} has been Uno-attacked! They have to draw {1} cards!",
                            CurrentPlayer.Nick,
                            drawCount
                        );
                        CurrentPlayer.Hand.UnionWith(DrawCards(drawCount));
                        drawn = true;
                    }
                }

                if (!drawn)
                {
                    // draw a card
                    CurrentPlayer.Hand.Add(DrawCard());
                }

                // tell them what they drew
                SendPlayerHandInfoEvent(CurrentPlayer);
                SendPlayerHandNotice(CurrentPlayer);

                // broadcast that they drew
                BroadcastAnchorPlayerDrewCardEvent(CurrentPlayer.Nick);

                // restart counting down
                TurnStartedUtc = DateTime.UtcNow;
            }
        }

        protected virtual void HandlePassCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            if (msg.Channel != Config.UnoChannel)
            {
                return;
            }

            lock (TurnLock)
            {
                if (CurrentPlayer.Nick != msg.SenderNickname)
                {
                    ConnectionManager.SendChannelMessageFormat(
                        Config.UnoChannel,
                        "It's not your turn, {0}.",
                        msg.SenderNickname
                    );
                    return;
                }

                if (!DrewLast)
                {
                    ConnectionManager.SendChannelMessageFormat(
                        Config.UnoChannel,
                        "You have to draw first, {0}.",
                        msg.SenderNickname
                    );
                    return;
                }

                // skip to the next player
                AdvanceToNextPlayer();
            }
        }

        protected virtual void HandleEndGameCommand(CommandMatch cmd, IChannelMessageEventArgs msg)
        {
            if (msg.Channel != Config.UnoChannel)
            {
                return;
            }

            lock (TurnLock)
            {
                StopGame();
                ConnectionManager.SendChannelMessage(
                    Config.UnoChannel,
                    "Game stopped."
                );
            }
        }

        protected virtual void AdvanceCurrentPlayerIndex()
        {
            if (PlayerOrderReversed)
            {
                --CurrentPlayerIndex;
                if (CurrentPlayerIndex < 0)
                {
                    CurrentPlayerIndex = Players.Count - 1;
                }
            }
            else
            {
                ++CurrentPlayerIndex;
                if (CurrentPlayerIndex >= Players.Count)
                {
                    CurrentPlayerIndex = 0;
                }
            }
        }

        protected static string EscapeHighlight(string s)
        {
            if (s.Length < 2)
            {
                return '\uFEFF' + s;
            }

            // escape nickname or other highlight-y string by adding ZWNBSP in between first and second character
            var ret = new StringBuilder(s.Length + 1);
            ret.Append(s[0]);
            ret.Append('\uFEFF');
            ret.Append(s, 1, s.Length - 1);
            return ret.ToString();
        }

        protected virtual void StartNewTurn()
        {
            if (CurrentGameState != GameState.InProgress)
            {
                return;
            }

            // broadcast info
            BroadcastCardCountsEvent();
            BroadcastCurrentCardEvent();
            SendPlayerHandInfoEvent(CurrentPlayer);

            // write out human-readable info into the channel
            var playerListPieces = Players
                .Select(p => (p.Nick == CurrentPlayer.Nick)
                    ? string.Format("[ {0}: {1} ]", p.Nick, p.Hand.Count)
                    : string.Format("{0}: {1}", EscapeHighlight(p.Nick), p.Hand.Count));
            var playerListString = playerListPieces.StringJoin(PlayerOrderReversed ? " <-- " : " --> ");
            ConnectionManager.SendChannelMessageFormat(Config.UnoChannel, "{0} || {1}", TopCard.ToFullPlayString(), playerListString);

            SendPlayerHandNotice(CurrentPlayer);

            // trigger a move
            BroadcastAnchorCurrentPlayerOrderEvent();

            // the player did not draw last by default
            DrewLast = false;

            // start counting down
            TurnStartedUtc = DateTime.UtcNow;
        }

        protected virtual void AdvanceToNextPlayer()
        {
            AdvanceCurrentPlayerIndex();
            StartNewTurn();
        }

        protected virtual void HandleNickChange(object sender, INickChangeEventArgs e)
        {
            lock (TurnLock)
            {
                // update player's nickname in player list
                var player = Players.FirstOrDefault(p => p.Nick == e.OldNickname);
                if (player != null)
                {
                    player.Nick = e.NewNickname;
                }
            }
        }

        protected virtual void HandleUserLeftChannel(object sender, IUserLeftChannelEventArgs e)
        {
            ActuallyHandleUserLeaving(sender, e.User);
        }

        protected virtual void HandleUserQuitServer(object sender, IUserQuitServerEventArgs e)
        {
            ActuallyHandleUserLeaving(sender, e.User);
        }

        protected virtual void ActuallyHandleUserLeaving(object sender, string nick)
        {
            lock (TurnLock)
            {
                RemovePlayerFromGame(nick);
            }
        }

        /// <remarks>You must be holding <see cref="TurnLock"/>.</remarks>
        protected virtual void RemovePlayerFromGame(string nick)
        {
            // is the player a player?
            var player = Players.FirstOrDefault(p => p.Nick == nick);
            if (player == null)
            {
                // nope
                return;
            }

            // is it this player's turn?
            bool removed = false;
            if (CurrentPlayerIndex < Players.Count)
            {
                var currentPlayer = Players[CurrentPlayerIndex];
                if (player == currentPlayer)
                {
                    // yes -.-

                    // remove the player
                    Players.RemoveAt(CurrentPlayerIndex);

                    // make it the next player's turn
                    AdvanceToNextPlayer();

                    removed = true;
                }
            }

            if (!removed)
            {
                // simply remove the player
                Players.Remove(player);
            }

            ConnectionManager.SendChannelMessageFormat(Config.UnoChannel, "{0} has left the game.", player.Nick);

            if (Players.Count == 0)
            {
                ConnectionManager.SendChannelMessage(Config.UnoChannel, "With nobody to play, I'm terminating the game.");

                StopGame();
            }
        }

        protected virtual void PrepareGame()
        {
            if (ExtremeMode)
            {
                PrepareExtremePiles();
            }
            else
            {
                PrepareRegularPiles();
            }

            Players.Clear();
            PlayerOrderReversed = false;
            DrewLast = false;
            TurnStartedUtc = null;

            // we're accepting applications!
            CurrentGameState = GameState.Preparation;
        }

        protected virtual void DealGame()
        {
            // shuffle the draw pile
            DrawPile.Shuffle(Randomizer);

            if (Config.ShufflePlayerList)
            {
                Pile<Player>.Shuffle(Players, Randomizer);
            }

            // distribute the cards
            foreach (var player in Players)
            {
                var drawnCards = DrawCards(Config.InitialDealSize);
                player.Hand.UnionWith(drawnCards);
            }

            // draw one card and discard it; this will be our top card
            var newTopCard = DrawCard();
            if (newTopCard.Color == CardColor.Wild)
            {
                // it's a wild card; give it a fixed color at random
                newTopCard = new Card((CardColor)Randomizer.Next(0, 4), newTopCard.Value);
            }
            DiscardPile.Push(newTopCard);

            CurrentGameState = GameState.InProgress;

            // we're getting ready to start!
            BroadcastAnchorCardsDealtEvent();

            // start a new turn!
            StartNewTurn();
        }

        protected virtual void StopGame()
        {
            BroadcastAnchorGameEndedEvent();
            CurrentGameState = GameState.NoGame;
            TurnStartedUtc = null;
        }
    }
}
