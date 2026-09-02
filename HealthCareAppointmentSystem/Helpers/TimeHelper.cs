using System;

namespace HealthCareAppointmentSystem.Helpers
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo PakistanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");

        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PakistanTimeZone);
        
        public static DateTime UtcNow => DateTime.UtcNow;
    }
}
