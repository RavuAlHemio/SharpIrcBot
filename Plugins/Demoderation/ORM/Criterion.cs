namespace SharpIrcBot.Plugins.Demoderation.ORM
{
    public class Criterion
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public string DetectionRegex { get; set; }
    }
}
