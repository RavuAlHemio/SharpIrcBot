using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        public static readonly string[] DateRegexGroupPrefixes;

        [NotNull]
        public static readonly string[] TimeRegexGroupSpecPrefixes = {"t", "dt"};

        [NotNull]
        public static readonly Regex DateTimeRegex;

        static TimeUtil()
        {
            DateTimeRegex = new Regex(
                "^" +
                "\\s*" +
                "(?:" +
                    // date only
                    RegexDatePattern("d") +
                "|" +
                    // time only
                    RegexTimePattern("t") +
                "|" +
                    // date and time
                    RegexDatePattern("dt") +
                    "\\s+" +
                    RegexTimePattern("dt") +
                ")" +
                "\\s*" +
                "$",
                RegexOptions.Compiled
            );

            string[] dateSpecPrefixes = {"d", "dt"};
            string[] dateFormatPrefixes = {"Ymd", "Dmy", "Mdy"};
            DateRegexGroupPrefixes = dateSpecPrefixes
                .SelectMany(
                    dsp => dateFormatPrefixes,
                    (dsp, dfp) => dsp + dfp
                )
                .ToArray()
            ;
        }

        private static string RegexDatePattern(string groupPrefix)
        {
            if (!groupPrefix.All(c => c >= 'a' && c <= 'z'))
            {
                throw new ArgumentException("group prefix may only consist of letters a-z");
            }

            return
                "(?:" +
                    // "2018-04-08", "04-08", "2018-4-8", ...
                    "(?:" +
                        $"(?<{groupPrefix}YmdYear>[0-9]+)" +
                    "-)?" +
                    $"(?<{groupPrefix}YmdMonth>0?[1-9]|1[12])" +
                    "-" +
                    $"(?<{groupPrefix}YmdDay>0?[1-9]|[12][0-9]|3[01])" +
                "|" +
                    // "08.04.2018", "08.04.", "08.04", "8.4", ...
                    $"(?<{groupPrefix}DmyDay>0?[1-9]|[12][0-9]|3[01])" +
                    "\\." +
                    "\\s*" +
                    $"(?<{groupPrefix}DmyMonth>0?[1-9]|1[12])" +
                    "(?:" +
                        "\\." +
                        "\\s*" +
                        $"(?<{groupPrefix}DmyYear>[0-9]+)?" +
                    ")?" +
                "|" +
                    // "04/08/2018", "04/08", "4/8", ...
                    $"(?<{groupPrefix}MdyMonth>0?[1-9]|1[12])" +
                    "/" +
                    $"(?<{groupPrefix}MdyDay>0?[1-9]|[12][0-9]|3[01])" +
                    "(?:" +
                        "/" +
                        $"(?<{groupPrefix}MdyYear>[0-9]+)" +
                    ")?" +
                ")"
            ;
        }

        private static string RegexTimePattern(string groupPrefix)
        {
            if (!groupPrefix.All(c => c >= 'a' && c <= 'z'))
            {
                throw new ArgumentException("group prefix may only consist of letters a-z");
            }

            return
                "(?:" +
                    // 24-hour clock
                    $"(?<{groupPrefix}Hour24>0?[0-9]|1[0-9]|2[0-3])" +
                    ":" +
                    $"(?<{groupPrefix}Minute24>[0-5][0-9])" +
                    "(?:" +
                        ":" +
                        $"(?<{groupPrefix}Second24>[0-5][0-9])" +
                    ")?" +
                "|" +
                    // 12-hour clock
                    $"(?<{groupPrefix}Hour12>0?[1-9]|1[0-2])" +
                    ":" +
                    $"(?<{groupPrefix}Minute12>[0-5][0-9])" +
                    "(?:" +
                        ":" +
                        $"(?<{groupPrefix}Second12>[0-5][0-9])" +
                    ")?" +
                    "\\s*" +
                    $"(?<{groupPrefix}AmPm>[aApP])(?:[mM]|\\.[mM]\\.)?" +
                "|" +
                    // disambiguating conventions with the 12-hour clock
                    // "12 noon", "noon", "midnight", ...
                    // (otherwise: assumption that 12:00 am is midnight
                    "(?:" +
                        $"(?<{groupPrefix}Noon>(?:12\\s+)?noon)" +
                        "|" +
                        $"(?<{groupPrefix}Midnight>(?:12\\s+)?midnight)" +
                    ")" +
                ")"
            ;
        }

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

        public static DateTime? DateTimeFromString(string dateTimeString, DateTime? customNow = null)
        {
            Match m = DateTimeRegex.Match(dateTimeString);
            if (!m.Success)
            {
                return null;
            }

            DateTime now = customNow ?? DateTime.Now;

            int? year = null;
            int month = 0;
            int day = 0;

            bool haveDate = false;
            foreach (string prefix in DateRegexGroupPrefixes)
            {
                if (m.Groups[$"{prefix}Day"].Success)
                {
                    (year, month, day) = ParseDate(m, $"{prefix}Year", $"{prefix}Month", $"{prefix}Day");
                    haveDate = true;
                    break;
                }
            }
            if (!haveDate)
            {
                // assume today
                // (check later if time is past; if so, assume tomorrow)
                year = now.Year;
                month = now.Month;
                day = now.Day;
            }

            int hour = 0, minute = 0, second = 0;
            bool haveTime = false;
            foreach (string prefix in TimeRegexGroupSpecPrefixes)
            {
                if (m.Groups[$"{prefix}Hour24"].Success)
                {
                    hour = StringUtil.MaybeParseInt(m.Groups[$"{prefix}Hour24"].Value).Value;
                    minute = StringUtil.MaybeParseInt(m.Groups[$"{prefix}Minute24"].Value).Value;
                    second = (m.Groups[$"{prefix}Second24"].Success)
                        ? StringUtil.MaybeParseInt(m.Groups[$"{prefix}Second24"].Value).Value
                        : 0
                    ;
                    haveTime = true;
                    break;
                }
                else if (m.Groups[$"{prefix}Hour12"].Success)
                {
                    hour = StringUtil.MaybeParseInt(m.Groups[$"{prefix}Hour12"].Value).Value;
                    minute = StringUtil.MaybeParseInt(m.Groups[$"{prefix}Minute12"].Value).Value;
                    second = (m.Groups[$"{prefix}Second12"].Success)
                        ? StringUtil.MaybeParseInt(m.Groups[$"{prefix}Second12"].Value).Value
                        : 0
                    ;

                    string amPm = m.Groups[$"{prefix}AmPm"].Value.ToLowerInvariant();
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

                    haveTime = true;
                    break;
                }
                else if (m.Groups[$"{prefix}Noon"].Success)
                {
                    (hour, minute, second) = (12, 0, 0);

                    haveTime = true;
                    break;
                }
                else if (m.Groups[$"{prefix}Midnight"].Success)
                {
                    (hour, minute, second) = (0, 0, 0);

                    haveTime = true;
                    break;
                }
            }
            if (!haveTime)
            {
                // assume midnight
                (hour, minute, second) = (0, 0, 0);
            }

            DateTime ret;

            if (!year.HasValue)
            {
                // assume the future
                year = now.Year;
                ret = new DateTime(year.Value, month, day, hour, minute, second);
                if (now > ret)
                {
                    // a bit more complex due to leap years
                    for (int yearAdder = 1; yearAdder < 9; ++yearAdder)
                    {
                        try
                        {
                            ret = new DateTime(year.Value + yearAdder, month, day, hour, minute, second);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // probably not a valid date in that year; try the next one
                            continue;
                        }
                        break;
                    }
                }
                return ret;
            }

            ret = new DateTime(year.Value, month, day, hour, minute, second);
            if (!haveDate && now > ret)
            {
                // assume the future
                ret = ret.AddDays(1);
            }

            return ret;
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
