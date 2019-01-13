using System;
using System.Globalization;

namespace SharpIrcBot.Plugins.Vitals.Nightscout
{
    public static class NightscoutUtils
    {
        public const string DateFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffzzz";

        public static DateTimeOffset ParseTimestamp(string text)
        {
            // add colon to timezone
            text = text.Insert(text.Length - 2, ":");

            // standard parse
            return DateTimeOffset.ParseExact(text, DateFormat, CultureInfo.InvariantCulture);
        }

        public static string TimestampToString(DateTimeOffset timestamp)
        {
            string text = timestamp.ToString(DateFormat, CultureInfo.InvariantCulture);

            // remove colon from timezone
            return text.Remove(text.Length - 3, 1);
        }
    }
}
