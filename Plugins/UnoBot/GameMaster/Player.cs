using System;
using System.Collections.Generic;

namespace UnoBot.GameMaster
{
    public class Player
    {
        public string Nick { get; set; }
        public bool IsBot { get; set; }
        public SortedSet<Card> Hand { get; set; }
    }
}
