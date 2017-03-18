using JetBrains.Annotations;

namespace SharpIrcBot.Plugins.DontJustHighlightMe
{
    public class HighlightOccurrence
    {
        [NotNull]
        public string Perpetrator { get; }
        [NotNull]
        public string Victim { get; }
        [NotNull]
        public string Channel { get; }
        public int Countdown { get; set; }

        public HighlightOccurrence([NotNull] string perpetrator, [NotNull] string victim, [NotNull] string channel, int countdownValue)
        {
            Perpetrator = perpetrator;
            Victim = victim;
            Channel = channel;
            Countdown = countdownValue;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(HighlightOccurrence))
            {
                return false;
            }

            var other = (HighlightOccurrence) obj;
            return this.Perpetrator == other.Perpetrator
                && this.Victim == other.Victim
                && this.Channel == other.Channel
                && this.Countdown == other.Countdown;
        }

        public override int GetHashCode()
        {
            return 3*Perpetrator.GetHashCode()
                + 5*Countdown.GetHashCode()
                + 7*Channel.GetHashCode()
                + 11*Victim.GetHashCode();
        }
    }
}
