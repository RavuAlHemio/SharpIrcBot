namespace SharpIrcBot.Plugins.Demoderation.ORM
{
    public class Permaban
    {
        public long ID { get; set; }
        public string NicknameOrUsername { get; set; }
        public string Channel { get; set; }
    }
}
