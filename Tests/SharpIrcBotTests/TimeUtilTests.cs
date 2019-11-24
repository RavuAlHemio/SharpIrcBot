using System;
using SharpIrcBot.Util;
using Xunit;

namespace SharpIrcBot.Tests.SharpIrcBotTests
{
    public class TimeUtilTests
    {
        readonly DateTime CustomNow = new DateTime(
            2018, 07, 22, 2, 6, 10
        );

        void TestDateTimeFromString(
            string str, int year, int month, int day, int hour = 0, int minute = 0, int second = 0,
            DateTime? specialCustomNow = null
        )
        {
            DateTime now = specialCustomNow.GetValueOrDefault(CustomNow);

            Assert.Equal(
                new DateTime(year, month, day, hour, minute, second),
                TimeUtil.DateTimeFromString(str, now)
            );
        }

        [Fact]
        public void YMDFormatTests()
        {
            TestDateTimeFromString("2018-07-22 01:54:26", 2018, 7, 22, 1, 54, 26);
            TestDateTimeFromString("   2018-07-22  01:54:26  ", 2018, 7, 22, 1, 54, 26);

            // year guesstimation
            TestDateTimeFromString(" 07-22 2:06:09  ", 2019, 7, 22, 2, 6, 9);
            TestDateTimeFromString(" 07-22 2:06:11  ", 2018, 7, 22, 2, 6, 11);

            // no seconds
            TestDateTimeFromString(" 2018-04-10 10:40  ", 2018, 4, 10, 10, 40, 0);

            // AM/PM
            TestDateTimeFromString(" 2018-04-10 10:40 AM ", 2018, 4, 10, 10, 40, 0);
            TestDateTimeFromString(" 2018-04-10 10:40 PM ", 2018, 4, 10, 22, 40, 0);
            TestDateTimeFromString(" 2018-04-10 10:40 P.M. ", 2018, 4, 10, 22, 40, 0);
            TestDateTimeFromString(" 2018-04-10    noon ", 2018, 4, 10, 12, 0, 0);
            TestDateTimeFromString(" 2018-04-10    12  noon ", 2018, 4, 10, 12, 0, 0);
            TestDateTimeFromString(" 2018-04-10    midnight ", 2018, 4, 10, 0, 0, 0);
            TestDateTimeFromString(" 2018-04-10    12  midnight ", 2018, 4, 10, 0, 0, 0);

            // gauntlet
            TestDateTimeFromString("   7-22  1:02 pm  ", 2018, 7, 22, 13, 2, 0);

            // no time
            TestDateTimeFromString("2018-07-22", 2018, 7, 22, 0, 0, 0);
            TestDateTimeFromString("   2018-07-22  ", 2018, 7, 22, 0, 0, 0);
            TestDateTimeFromString("   07-22  ", 2019, 7, 22, 0, 0, 0);
            TestDateTimeFromString("   7-22  ", 2019, 7, 22, 0, 0, 0);
        }

        [Fact]
        public void MDYFormatTests()
        {
            TestDateTimeFromString("07/22/2018 01:54:26", 2018, 7, 22, 1, 54, 26);
            TestDateTimeFromString("   07/22/2018  01:54:26  ", 2018, 7, 22, 1, 54, 26);

            // year guesstimation
            TestDateTimeFromString(" 07/22 2:06:09  ", 2019, 7, 22, 2, 6, 9);
            TestDateTimeFromString(" 07/22 2:06:11  ", 2018, 7, 22, 2, 6, 11);

            // no seconds
            TestDateTimeFromString(" 04/10/2018 10:40  ", 2018, 4, 10, 10, 40, 0);

            // AM/PM
            TestDateTimeFromString(" 04/10/2018 10:40 AM ", 2018, 4, 10, 10, 40, 0);
            TestDateTimeFromString(" 04/10/2018 10:40 PM ", 2018, 4, 10, 22, 40, 0);
            TestDateTimeFromString(" 04/10/2018 10:40 P.M. ", 2018, 4, 10, 22, 40, 0);
            TestDateTimeFromString(" 04/10/2018    noon ", 2018, 4, 10, 12, 0, 0);
            TestDateTimeFromString(" 04/10/2018    12  noon ", 2018, 4, 10, 12, 0, 0);
            TestDateTimeFromString(" 04/10/2018    midnight ", 2018, 4, 10, 0, 0, 0);
            TestDateTimeFromString(" 04/10/2018    12  midnight ", 2018, 4, 10, 0, 0, 0);

            // gauntlet
            TestDateTimeFromString("   7/22  1:02 pm  ", 2018, 7, 22, 13, 2, 0);

            // no time
            TestDateTimeFromString("07/22/2018", 2018, 7, 22, 0, 0, 0);
            TestDateTimeFromString("   07/22/2018  ", 2018, 7, 22, 0, 0, 0);
            TestDateTimeFromString("   07/22  ", 2019, 7, 22, 0, 0, 0);
            TestDateTimeFromString("   7/22  ", 2019, 7, 22, 0, 0, 0);
        }

        [Fact]
        public void DMYFormatTests()
        {
            TestDateTimeFromString("22.07.2018 01:54:26", 2018, 7, 22, 1, 54, 26);
            TestDateTimeFromString("   22.07.2018  01:54:26  ", 2018, 7, 22, 1, 54, 26);

            // extreme spacing (DMY specialty)
            TestDateTimeFromString("  22.  07.   2018  01:54:26  ", 2018, 7, 22, 1, 54, 26);

            // year guesstimation
            TestDateTimeFromString(" 22.07. 2:06:09  ", 2019, 7, 22, 2, 6, 9);
            TestDateTimeFromString(" 22.07 2:06:09  ", 2019, 7, 22, 2, 6, 9);
            TestDateTimeFromString(" 22.07. 2:06:11  ", 2018, 7, 22, 2, 6, 11);
            TestDateTimeFromString(" 22.07 2:06:11  ", 2018, 7, 22, 2, 6, 11);

            // no seconds
            TestDateTimeFromString(" 10.04.2018 10:40  ", 2018, 4, 10, 10, 40, 0);

            // AM/PM
            TestDateTimeFromString(" 10.04.2018 10:40 AM ", 2018, 4, 10, 10, 40, 0);
            TestDateTimeFromString(" 10.04.2018 10:40 PM ", 2018, 4, 10, 22, 40, 0);
            TestDateTimeFromString(" 10.04.2018 10:40 P.M. ", 2018, 4, 10, 22, 40, 0);
            TestDateTimeFromString(" 10.04.2018    noon ", 2018, 4, 10, 12, 0, 0);
            TestDateTimeFromString(" 10.04.2018    12  noon ", 2018, 4, 10, 12, 0, 0);
            TestDateTimeFromString(" 10.04.2018    midnight ", 2018, 4, 10, 0, 0, 0);
            TestDateTimeFromString(" 10.04.2018    12  midnight ", 2018, 4, 10, 0, 0, 0);

            // gauntlet
            TestDateTimeFromString("   22.7.  1:02 pm  ", 2018, 7, 22, 13, 2, 0);

            // no time
            TestDateTimeFromString("22.07.2018", 2018, 7, 22, 0, 0, 0);
            TestDateTimeFromString("   22.07.2018  ", 2018, 7, 22, 0, 0, 0);
            TestDateTimeFromString("   22.07.  ", 2019, 7, 22, 0, 0, 0);
            TestDateTimeFromString("   22.7.  ", 2019, 7, 22, 0, 0, 0);
        }

        [Fact]
        public void DatelessFormatTests()
        {
            TestDateTimeFromString("03:54:26", 2018, 7, 22, 3, 54, 26);
            TestDateTimeFromString("    03:54:26  ", 2018, 7, 22, 3, 54, 26);

            // assume future
            TestDateTimeFromString(" 2:06:09  ", 2018, 7, 23, 2, 6, 9);
            TestDateTimeFromString(" 2:06:09  ", 2018, 7, 23, 2, 6, 9);
            TestDateTimeFromString(" 2:06:11  ", 2018, 7, 22, 2, 6, 11);
            TestDateTimeFromString(" 2:06:11  ", 2018, 7, 22, 2, 6, 11);

            // no seconds (+ assume future)
            TestDateTimeFromString(" 10:40  ", 2018, 7, 22, 10, 40, 0);
            TestDateTimeFromString(" 2:05  ", 2018, 7, 23, 2, 5, 0);

            // AM/PM (+ assume future)
            TestDateTimeFromString(" 10:40 AM ", 2018, 7, 22, 10, 40, 0);
            TestDateTimeFromString(" 10:40 PM ", 2018, 7, 22, 22, 40, 0);
            TestDateTimeFromString(" 10:40 P.M. ", 2018, 7, 22, 22, 40, 0);
            TestDateTimeFromString("  noon ", 2018, 7, 22, 12, 0, 0);
            TestDateTimeFromString("   12  noon ", 2018, 7, 22, 12, 0, 0);
            TestDateTimeFromString("  midnight ", 2018, 7, 23, 0, 0, 0);
            TestDateTimeFromString(" 12  midnight ", 2018, 7, 23, 0, 0, 0);

            // gauntlet
            TestDateTimeFromString("   1:02 pm  ", 2018, 7, 22, 13, 2, 0);
        }

        [Fact]
        public void EmptyStringTests()
        {
            Assert.Null(TimeUtil.DateTimeFromString("", CustomNow));
            Assert.Null(TimeUtil.DateTimeFromString("    ", CustomNow));
        }

        [Fact]
        public void LeapYearTests()
        {
            var nowLeapDay = new DateTime(
                2016, 2, 29, 1, 23, 45
            );
            var nowTrickyLeapDay = new DateTime(
                2096, 2, 29, 1, 23, 45
            );

            // near future vs. far future
            TestDateTimeFromString("02-29 1:23:46", 2016, 2, 29, 1, 23, 46, nowLeapDay);
            TestDateTimeFromString("02-29 1:23:44", 2020, 2, 29, 1, 23, 44, nowLeapDay);

            // 2100 is not a leap year
            TestDateTimeFromString("02-29 1:23:46", 2096, 2, 29, 1, 23, 46, nowTrickyLeapDay);
            TestDateTimeFromString("02-29 1:23:44", 2104, 2, 29, 1, 23, 44, nowTrickyLeapDay);
        }
    }
}
