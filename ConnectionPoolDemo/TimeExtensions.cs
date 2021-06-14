using System;

namespace ConnectionPoolDemo
{
    public static class TimeExtensions
    {
        public static long GetUnixTime(this DateTime time)
        {
            return (long) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}