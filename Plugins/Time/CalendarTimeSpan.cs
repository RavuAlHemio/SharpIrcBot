using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace SharpIrcBot.Plugins.Time
{
    public struct CalendarTimeSpan
    {
        public int Years { get; }
        public int Months { get; }
        public int Days { get; }
        public int Hours { get; }
        public int Minutes { get; }
        public int Seconds { get; }
        public int Milliseconds { get; }
        public bool Negative { get; } // ToString uses: false = ago, true = in

        public CalendarTimeSpan(
            int years, int months, int days, int hours, int minutes, int seconds, int milliseconds = 0,
            bool negative = false
        )
        {
            if (years < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(years), years, "value out of range (>= 0)");
            }
            if (months < 0 || months > 11)
            {
                throw new ArgumentOutOfRangeException(nameof(months), months, "value out of range (0..11)");
            }
            if (days < 0 || days > 30)
            {
                throw new ArgumentOutOfRangeException(nameof(days), days, "value out of range (0..30)");
            }
            if (hours < 0 || hours > 23)
            {
                throw new ArgumentOutOfRangeException(nameof(hours), hours, "value out of range (0..23)");
            }
            if (minutes < 0 || minutes > 59)
            {
                throw new ArgumentOutOfRangeException(nameof(minutes), minutes, "value out of range (0..59)");
            }
            if (seconds < 0 || seconds > 59)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "value out of range (0..59)");
            }
            if (milliseconds < 0 || milliseconds > 999)
            {
                throw new ArgumentOutOfRangeException(nameof(milliseconds), milliseconds, "value out of range (0..999)");
            }
            Contract.EndContractBlock();

            Years = years;
            Months = months;
            Days = days;
            Hours = hours;
            Minutes = minutes;
            Seconds = seconds;
            Milliseconds = milliseconds;
            Negative = negative;
        }

        public bool Equals(CalendarTimeSpan other)
        {
            return (this.Negative == other.Negative)
                && (this.Years == other.Years)
                && (this.Months == other.Months)
                && (this.Days == other.Days)
                && (this.Hours == other.Hours)
                && (this.Minutes == other.Minutes)
                && (this.Seconds == other.Seconds)
                && (this.Milliseconds == other.Milliseconds);
        }

        public override bool Equals(object o)
        {
            if (o == null)
            {
                return false;
            }

            if (this.GetType() != o.GetType())
            {
                return false;
            }

            return this.Equals((CalendarTimeSpan)o);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;
                hashCode = (397 * hashCode) ^ this.Years.GetHashCode();
                hashCode = (397 * hashCode) ^ this.Months.GetHashCode();
                hashCode = (397 * hashCode) ^ this.Days.GetHashCode();
                hashCode = (397 * hashCode) ^ this.Hours.GetHashCode();
                hashCode = (397 * hashCode) ^ this.Minutes.GetHashCode();
                hashCode = (397 * hashCode) ^ this.Seconds.GetHashCode();
                hashCode = (397 * hashCode) ^ this.Milliseconds.GetHashCode();
                hashCode = (397 * hashCode) ^ this.Negative.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            var pieces = new List<string>();
            pieces.Add(Negative ? "-" : "+");

            if (Years > 0)
            {
                pieces.Add($"{Years}y");
            }
            if (Months > 0)
            {
                pieces.Add($"{Months}mon");
            }
            if (Days > 0)
            {
                pieces.Add($"{Days}d");
            }
            if (Hours > 0)
            {
                pieces.Add($"{Hours}h");
            }
            if (Minutes > 0)
            {
                pieces.Add($"{Minutes}min");
            }
            if (Seconds > 0)
            {
                pieces.Add($"{Seconds}s");
            }
            if (Milliseconds > 0)
            {
                pieces.Add($"{Milliseconds}ms");
            }

            if (pieces.Count == 1)
            {
                // just the sign...
                pieces.Add("0");
            }

            return string.Join(" ", pieces);
        }

        public static bool operator==(CalendarTimeSpan one, CalendarTimeSpan other)
            => one.Equals(other);
        public static bool operator!=(CalendarTimeSpan one, CalendarTimeSpan other)
            => !(one == other);
    }
}
