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
            string str, int year, int month, int day, int hour = 0, int minute = 0, int second = 0
        )
        {
            Assert.Equal(
                new DateTime(year, month, day, hour, minute, second),
                TimeUtil.DateTimeFromString(str, CustomNow)
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
        }
    }
}
