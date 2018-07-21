using System;

namespace SharpIrcBot.Plugins.Time
{
    public static class TimeComparison
    {        
        public static int CompareAdding(
            DateTime basePoint, DateTime target, int years = 0, int months = 0, int days = 0, int hours = 0, int minutes = 0,
            int seconds = 0, int milliseconds = 0
        )
        {
            if (years < 0) { throw new ArgumentOutOfRangeException(nameof(years)); }
            if (months < 0) { throw new ArgumentOutOfRangeException(nameof(months)); }

            int newYear = basePoint.Year + years;
            int newMonth = basePoint.Month + months;
            if (newMonth > 12)
            {
                ++newYear;
                newMonth -= 12;
            }

            int newDay = basePoint.Day + days;

            DateTime daysAdded;

            // does the base day exist in the new month?
            try
            {
                daysAdded = new DateTime(newYear, newMonth, basePoint.Day, 0, 0, 0, basePoint.Kind);

                // yes; just add the added days
                daysAdded = daysAdded.AddDays(days);
            }
            catch (ArgumentOutOfRangeException)
            {
                // no; add both base day and added days to the start of the new month
                daysAdded = new DateTime(newYear, newMonth, 1, 0, 0, 0, basePoint.Kind);
                daysAdded = daysAdded.AddDays(basePoint.Day + days - 1);
            }

            DateTime sum = daysAdded
                .AddHours(basePoint.Hour + hours)
                .AddMinutes(basePoint.Minute + minutes)
                .AddSeconds(basePoint.Second + seconds)
                .AddMilliseconds(basePoint.Millisecond + milliseconds);

            return sum.CompareTo(target);
        }

        public static CalendarTimeSpan CalendarDifference(DateTime start, DateTime target)
        {
            bool negative = false;
            if (start > target)
            {
                negative = true;
                (start, target) = (target, start);
            }

            // years, months, days, hours, minutes, seconds, milliseconds
            int[] values = {0, 0, 0, 0, 0, 0, 0};
            bool equality = false;

            for (int valueIndex = 0; valueIndex < values.Length && !equality; ++valueIndex)
            {
                for (;;)
                {
                    // try increasing the value
                    ++values[valueIndex];

                    int comparison = CompareAdding(
                        start, target,
                        values[0], values[1], values[2], values[3], values[4], values[5], values[6]
                    );

                    if (comparison < 0)
                    {
                        // we have undershot; go through the process again
                        continue;
                    }

                    if (comparison == 0)
                    {
                        // found the final combination!
                        equality = true;
                        break;
                    }

                    // we have overshot; reduce this value again and continue with the next
                    --values[valueIndex];
                    break;
                }
            }

            return new CalendarTimeSpan(
                values[0], values[1], values[2], values[3], values[4], values[5], values[6],
                negative
            );
        }
    }
}
