using System;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace SharpIrcBot.Util
{
    public static class TimeUtil
    {
        [NotNull]
        public static readonly Regex TimeSpanRegex = new Regex(
            "^(?:"
            + "(?<zero>0)"
            + "|"
            + "(?:(?<days>[1-9][0-9]*)d)?"
            + "(?:(?<hours>[1-9][0-9]*)h)?"
            + "(?:(?<minutes>[1-9][0-9]*)m)?"
            + "(?:(?<seconds>[1-9][0-9]*)s)?"
            + ")$",
            RegexOptions.Compiled
        );

        public static DateTime ToUniversalTimeForDatabase(this DateTime dt)
        {
            return DateTime.SpecifyKind(dt.ToUniversalTime(), DateTimeKind.Unspecified);
        }

        public static DateTime ToLocalTimeFromDatabase(this DateTime dt, bool overrideKind = false)
        {
            var dateTime = dt;
            if (dateTime.Kind == DateTimeKind.Unspecified || overrideKind)
            {
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            return dateTime.ToLocalTime();
        }

        public static TimeSpan? TimeSpanFromString(string timeSpan)
        {
            Match m = TimeSpanRegex.Match(timeSpan);
            if (!m.Success)
            {
                return null;
            }

            if (m.Groups["zero"].Success)
            {
                return TimeSpan.Zero;
            }

            int? days = StringUtil.MaybeIntFromMatchGroup(m.Groups["days"]);
            int? hours = StringUtil.MaybeIntFromMatchGroup(m.Groups["hours"]);
            int? minutes = StringUtil.MaybeIntFromMatchGroup(m.Groups["minutes"]);
            int? seconds = StringUtil.MaybeIntFromMatchGroup(m.Groups["seconds"]);

            // don't allow overflow into a higher segment
            if (days.HasValue)
            {
                if (hours > 24 || minutes > 60 || seconds > 60)
                {
                    return null;
                }
            }
            if (hours.HasValue)
            {
                if (minutes > 60 || seconds > 60)
                {
                    return null;
                }
            }
            if (minutes.HasValue)
            {
                if (seconds > 60)
                {
                    return null;
                }
            }

            TimeSpan ret = TimeSpan.Zero;
            if (days.HasValue)
            {
                ret = ret.Add(TimeSpan.FromDays(days.Value));
            }
            if (hours.HasValue)
            {
                ret = ret.Add(TimeSpan.FromHours(hours.Value));
            }
            if (minutes.HasValue)
            {
                ret = ret.Add(TimeSpan.FromMinutes(minutes.Value));
            }
            if (seconds.HasValue)
            {
                ret = ret.Add(TimeSpan.FromSeconds(seconds.Value));
            }
            return ret;
        }
    }
}
