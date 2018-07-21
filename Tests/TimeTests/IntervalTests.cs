using System;
using System.Collections.Generic;
using SharpIrcBot.Plugins.Time;
using Xunit;

namespace SharpIrcBot.Tests.TimeTests
{
    public class IntervalTests
    {
        void AssertIntervalEqualTo(
            DateTime basePoint, DateTime target,
            int years = 0, int months = 0, int days = 0, int hours = 0, int minutes = 0, int seconds = 0,
            int milliseconds = 0, bool negative = false
        )
        {
            CalendarTimeSpan diff = TimeComparison.CalendarDifference(basePoint, target);
            Assert.Equal(
                new CalendarTimeSpan(years, months, days, hours, minutes, seconds, milliseconds, negative),
                diff
            );
        }

        [Fact]
        public void ZeroDifference()
        {
            DateTime now = DateTime.Now;
            AssertIntervalEqualTo(now, now, 0);
        }

        [Fact]
        public void SingleSubMonthUnit()
        {
            DateTime now = DateTime.Now;

            AssertIntervalEqualTo(now, now.AddSeconds(1), seconds: 1);
            AssertIntervalEqualTo(now.AddSeconds(1), now, seconds: 1, negative: true);

            AssertIntervalEqualTo(now, now.AddMinutes(1), minutes: 1);
            AssertIntervalEqualTo(now.AddMinutes(1), now, minutes: 1, negative: true);

            AssertIntervalEqualTo(now, now.AddHours(1), hours: 1);
            AssertIntervalEqualTo(now.AddDays(1), now, days: 1, negative: true);
        }

        [Fact]
        public void OneMonthSameDay()
        {
            DateTime then = new DateTime(1990, 4, 10, 8, 40, 0, DateTimeKind.Utc);
            DateTime monthLater = new DateTime(1990, 5, 10, 8, 40, 0, DateTimeKind.Utc);
            AssertIntervalEqualTo(then, monthLater, months: 1);

            DateTime december = new DateTime(2018, 12, 15, 12, 34, 56, DateTimeKind.Utc);
            DateTime january = new DateTime(2019, 1, 15, 12, 34, 56, DateTimeKind.Utc);
            AssertIntervalEqualTo(december, january, months: 1);
        }

        [Fact]
        public void OverOneShortMonth()
        {
            // 31st does not exist in February
            // => "29d" instead of "1mon 1d"
            DateTime endJan = new DateTime(2018, 1, 31, 1, 23, 45, DateTimeKind.Utc);
            DateTime begMar = new DateTime(2018, 3, 1, 1, 23, 45, DateTimeKind.Utc);
            AssertIntervalEqualTo(endJan, begMar, days: 29);
        }

        [Fact]
        public void FebruaryBehaviorNonLeap()
        {
            var jan26To31 = new DateTime[6];
            for (int i = 0; i < jan26To31.Length; ++i)
            {
                jan26To31[i] = new DateTime(2018, 1, 26 + i, 1, 11, 11, DateTimeKind.Utc);
            }
            DateTime feb28 = new DateTime(2018, 2, 28, 1, 11, 11, DateTimeKind.Utc);

            AssertIntervalEqualTo(jan26To31[0] /* 26 */, feb28, months: 1, days: 2);
            AssertIntervalEqualTo(jan26To31[1] /* 27 */, feb28, months: 1, days: 1);
            AssertIntervalEqualTo(jan26To31[2] /* 28 */, feb28, months: 1);
            AssertIntervalEqualTo(jan26To31[3] /* 29 */, feb28, days: 30);
            AssertIntervalEqualTo(jan26To31[4] /* 30 */, feb28, days: 29);
            AssertIntervalEqualTo(jan26To31[5] /* 31 */, feb28, days: 28);
        }

        [Fact]
        public void FebruaryBehaviorLeap()
        {
            var jan26To31 = new DateTime[6];
            for (int i = 0; i < jan26To31.Length; ++i)
            {
                jan26To31[i] = new DateTime(2016, 1, 26 + i, 1, 11, 11, DateTimeKind.Utc);
            }
            DateTime feb29 = new DateTime(2016, 2, 29, 1, 11, 11, DateTimeKind.Utc);

            AssertIntervalEqualTo(jan26To31[0] /* 26 */, feb29, months: 1, days: 3);
            AssertIntervalEqualTo(jan26To31[1] /* 27 */, feb29, months: 1, days: 2);
            AssertIntervalEqualTo(jan26To31[2] /* 28 */, feb29, months: 1, days: 1);
            AssertIntervalEqualTo(jan26To31[3] /* 29 */, feb29, months: 1);
            AssertIntervalEqualTo(jan26To31[4] /* 30 */, feb29, days: 30);
            AssertIntervalEqualTo(jan26To31[5] /* 31 */, feb29, days: 29);
        }

        [Fact]
        public void FutureDiffTest()
        {
            var startingPoint = new DateTime(2018, 7, 21, 23, 5, 41, DateTimeKind.Utc);
            var fifa2018 = new DateTime(2018, 6, 14, 15, 0, 0, DateTimeKind.Utc);
            var euro2020 = new DateTime(2020, 6, 12, 19, 0, 0, DateTimeKind.Utc);
            var fifa2022 = new DateTime(2022, 11, 20, 22, 0, 0, DateTimeKind.Utc);

            AssertIntervalEqualTo(startingPoint, fifa2018, months: 1, days: 7, hours: 8, minutes: 5, seconds: 41, negative: true);
            AssertIntervalEqualTo(startingPoint, euro2020, years: 1, months: 10, days: 21, hours: 19, minutes: 54, seconds: 19);
            AssertIntervalEqualTo(startingPoint, fifa2022, years: 4, months: 3, days: 29, hours: 22, minutes: 54, seconds: 19);
        }
    }
}
