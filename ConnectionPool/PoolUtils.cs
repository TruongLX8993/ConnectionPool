

using System.Linq;

namespace ConnectionPool
{
    internal class PoolUtils
    {
        
        public static string GetSourceName(string connectionString)
        {
            var parts  = connectionString.Split(';');
           return parts.FirstOrDefault(part => part.Contains("Data Source"));
        }
    }
}