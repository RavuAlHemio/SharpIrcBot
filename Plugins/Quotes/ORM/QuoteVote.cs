namespace SharpIrcBot.Plugins.Quotes.ORM
{
    public class QuoteVote
    {
        public long ID { get; set; }

        public long QuoteID { get; set; }

        public Quote Quote { get; set; }

        public string VoterLowercase { get; set; }

        public short Points { get; set; }
    }
}
