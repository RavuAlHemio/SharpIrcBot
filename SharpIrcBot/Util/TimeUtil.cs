using System;
using System.Diagnostics;
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

        [NotNull]
        public static readonly Regex DateTimeRegex = new Regex(
            "\\s*" +
            "(?:" +
                // "2018-04-08", "04-08", "2018-4-8", ...
                "(?:" +
                    "(?<ymdYear>[0-9]+)" +
                "-)?" +
                "(?<ymdMonth>0?[1-9]|1[12])" +
                "-" +
                "(?<ymdDay>0?[1-9]|[12][0-9]|3[01])" +
            "|" +
                // "08.04.2018", "08.04.", "08.04", "8.4", ...
                "(?<dmyDay>0?[1-9]|[12][0-9]|3[01])" +
                "\\." +
                "\\s*" +
                "(?<dmyMonth>0?[1-9]|1[12])" +
                "(?:" +
                    "\\." +
                    "\\s*" +
                    "(?<dmyYear>[0-9]+)?" +
                ")?" +
            "|" +
                // "04/08/2018", "04/08", "4/8", ...
                "(?<mdyMonth>0?[1-9]|1[12])" +
                "/" +
                "(?<mdyDay>0?[1-9]|[12][0-9]|3[01])" +
                "(?:" +
                    "/" +
                    "(?<mdyYear>[0-9]+)" +
                ")?" +
            ")" +
            "(?:" +
                "\\s+" +
                "(?:" +
                    // 24-hour clock
                    "(?<hour24>0?[0-9]|1[0-9]|2[0-3])" +
                    ":" +
                    "(?<minute24>[0-5][0-9])" +
                    "(?:" +
                        ":" +
                        "(?<second24>[0-5][0-9])" +
                    ")?" +
                "|" +
                    // 12-hour clock
                    "(?<hour12>0?[1-9]|1[0-2])" +
                    ":" +
                    "(?<minute12>[0-5][0-9])" +
                    "(?:" +
                        ":" +
                        "(?<second12>[0-5][0-9])" +
                    ")?" +
                    "\\s*" +
                    "(?<amPm>[aApP])(?:[mM]|\\.[mM]\\.)?" +
                "|" +
                    // disambiguating conventions with the 12-hour clock
                    // "12 noon", "noon", "midnight", ...
                    // (otherwise: assumption that 12:00 am is midnight
                    "(?:" +
                        "(?<noon>(?:12\\s+)?noon)" +
                        "|" +
                        "(?<midnight>(?:12\\s+)?midnight)" +
                    ")" +
                ")" +
            ")?" +
            "\\s*",
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

        public static DateTime? DateTimeFromString(string dateTimeString)
        {
            Match m = DateTimeRegex.Match(dateTimeString);
            if (!m.Success)
            {
                return null;
            }

            int? year;
            int month;
            int day;

            if (m.Groups["ymdDay"].Success)
            {
                (year, month, day) = ParseDate(m, "ymdYear", "ymdMonth", "ymdDay");
            }
            else if (m.Groups["dmyDay"].Success)
            {
                (year, month, day) = ParseDate(m, "dmyYear", "dmyMonth", "dmyDay");
            }
            else if (m.Groups["mdyDay"].Success)
            {
                (year, month, day) = ParseDate(m, "mdyYear", "mdyMonth", "mdyDay");
            }
            else
            {
                Debug.Fail("unexpected date format: " + dateTimeString);
                return null;
            }

            int hour, minute, second;
            if (m.Groups["hour24"].Success)
            {
                hour = StringUtil.MaybeParseInt(m.Groups["hour24"].Value).Value;
                minute = StringUtil.MaybeParseInt(m.Groups["minute24"].Value).Value;
                second = (m.Groups["second24"].Success)
                    ? StringUtil.MaybeParseInt(m.Groups["second24"].Value).Value
                    : 0
                ;
            }
            else if (m.Groups["hour12"].Success)
            {
                hour = StringUtil.MaybeParseInt(m.Groups["hour12"].Value).Value;
                minute = StringUtil.MaybeParseInt(m.Groups["minute12"].Value).Value;
                second = (m.Groups["second12"].Success)
                    ? StringUtil.MaybeParseInt(m.Groups["second12"].Value).Value
                    : 0
                ;

                string amPm = m.Groups["amPm"].Value.ToLowerInvariant();
                if (amPm == "a")
                {
                    if (hour == 12)
                    {
                        hour = 0;
                    }
                }
                else if (amPm == "p")
                {
                    if (hour < 12)
                    {
                        hour += 12;
                    }
                }
                else
                {
                    Debug.Fail("unexpected AM/PM value in " + dateTimeString);
                    return null;
                }
            }
            else if (m.Groups["noon"].Success)
            {
                (hour, minute, second) = (12, 0, 0);
            }
            else if (m.Groups["midnight"].Success)
            {
                (hour, minute, second) = (0, 0, 0);
            }
            else
            {
                // assume midnight
                (hour, minute, second) = (0, 0, 0);
            }

            if (!year.HasValue)
            {
                // assume the future
                year = DateTime.Now.Year;
                DateTime ret = new DateTime(year.Value, month, day, hour, minute, second);
                if (DateTime.Now > ret)
                {
                    ret = new DateTime(year.Value + 1, month, day, hour, minute, second);
                }
                return ret;
            }

            return new DateTime(year.Value, month, day, hour, minute, second);
        }

        static (int? Year, int Month, int Day) ParseDate(Match dateTimeMatch, string yearGroup, string monthGroup, string dayGroup)
        {
            int month = StringUtil.MaybeParseInt(dateTimeMatch.Groups[monthGroup].Value).Value;
            int day = StringUtil.MaybeParseInt(dateTimeMatch.Groups[dayGroup].Value).Value;

            int? year = null;
            if (dateTimeMatch.Groups[yearGroup].Success)
            {
                year = StringUtil.MaybeParseInt(dateTimeMatch.Groups[yearGroup].Value).Value;
            }

            return (year, month, day);
        }
    }
}
