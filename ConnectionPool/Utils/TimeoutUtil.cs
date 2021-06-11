using System;

namespace ConnectionPool.Utils
{
    public static class TimeoutUtil
    {
        public static bool IsTimeout(DateTime fromDate, DateTime toDate, int duration)
        {
            return toDate.Subtract(fromDate).TotalSeconds > duration;
        }
    }
}