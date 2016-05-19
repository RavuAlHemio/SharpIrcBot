namespace UnoBot.GameMaster
{
    public class Player
    {
        public string Nick { get; set; }
        public bool IsBot { get; set; }
        public SortedMultiset<Card> Hand { get; set; }

        protected Player()
        {
        }

        public static Player Create(string nick)
        {
            return Create(nick, isBot: false);
        }

        public static Player CreateBot(string nick)
        {
            return Create(nick, isBot: true);
        }

        public static Player Create(string nick, bool isBot)
        {
            return new Player { Nick = nick, IsBot = isBot, Hand = new SortedMultiset<Card>() };
        }
    }
}
