using System;

namespace SharpIrcBot.Util
{
    public static class TimeUtil
    {
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
    }
}
