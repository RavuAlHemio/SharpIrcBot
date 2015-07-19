using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace UnoBot.GameMaster
{
    public class UnoGameMasterPlugin : IPlugin
    {
        protected ConnectionManager ConnectionManager;
        protected GameMasterConfig Config;

        protected List<Card> DrawPile;
        protected List<Card> DiscardPile;
        protected List<Player> Players;
        protected int CurrentPlayerIndex;
        protected bool PlayerOrderReversed;

        protected static readonly CardColor[] RegularColors = { CardColor.Red, CardColor.Green, CardColor.Blue, CardColor.Yellow };

        public UnoGameMasterPlugin(ConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new GameMasterConfig(config);

            DrawPile = new List<Card>();
            DiscardPile = new List<Card>();
            Players = new List<Player>();
            CurrentPlayerIndex = 0;
            PlayerOrderReversed = false;
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
                DrawPile.Add(new Card(color, CardValue.Zero));

                // two of:
                for (int i = 0; i < 2; ++i)
                {
                    // each number except zero
                    for (CardValue value = CardValue.One; value <= CardValue.Nine; ++value)
                    {
                        DrawPile.Add(new Card(color, value));
                    }

                    // skip, reverse draw-two
                    DrawPile.Add(new Card(color, CardValue.Skip));
                    DrawPile.Add(new Card(color, CardValue.Reverse));
                    DrawPile.Add(new Card(color, CardValue.DrawTwo));
                }
            }

            // four wilds, four wild-draw-fours
            for (int i = 0; i < 4; ++i)
            {
                DrawPile.Add(new Card(CardColor.Wild, CardValue.Wild));
                DrawPile.Add(new Card(CardColor.Wild, CardValue.WildDrawFour));
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
                    DrawPile.Add(new Card(color, CardValue.Skip));
                    DrawPile.Add(new Card(color, CardValue.Reverse));
                    DrawPile.Add(new Card(color, CardValue.DrawTwo));
                }
            }

            // ... and four more of each special wild card (wild, wild-draw-four)
            for (int i = 0; i < 4; ++i)
            {
                DrawPile.Add(new Card(CardColor.Wild, CardValue.Wild));
                DrawPile.Add(new Card(CardColor.Wild, CardValue.WildDrawFour));
            }

            // assert the validity of the piles again
            AssertPileValidity();
        }

        protected virtual void SendEventTo(JObject evt, string playerNick)
        {
            var message = evt.ToString(Formatting.None);

            // split into chunks
            var maxLengthPerChunk = ConnectionManager.MaxMessageLength - 5;
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

            var currentCard = DiscardPile[DiscardPile.Count - 1];
            var evt = new JObject
            {
                { "event", "current_card" },
                { "current_card", currentCard.ToFullPlayString() }
            };
            BroadcastEventToBots(evt);
        }

        protected virtual void MulticastHandInfoEvents()
        {
            foreach (var bot in Players.Where(p => p.IsBot))
            {
                var cardNames = bot.Hand.Select(c => c.ToFullPlayString());
                var hand = new JArray(cardNames);
                var evt = new JObject
                {
                    { "event", "hand_info" },
                    { "hand", hand }
                };
                SendEventTo(evt, bot.Nick);
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

        protected virtual void BroadcastAnchorCurrentPlayerOrderEvent(string who)
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
    }
}
