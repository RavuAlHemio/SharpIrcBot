namespace SharpIrcBot.Plugins.DasIstNenFehler.ORM
{
    public class Noun
    {
        public long ID { get; set; }

        public long WordID { get; set; }

        public Word Word { get; set; }

        public long BaseWordID { get; set; }

        public Word BaseWord { get; set; }

        public GrammaticalCase Case { get; set; }

        public GrammaticalNumber Number { get; set; }

        public GrammaticalGender Gender { get; set; }
    }
}
