using System;

namespace IT15_INATO_POS.Utilities
{
    public static class TimeZoneHelper
    {
        private static readonly TimeZoneInfo PhilippineTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

        public static DateTime ToPhilippineTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Unspecified)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, PhilippineTimeZone);
        }
    }
}