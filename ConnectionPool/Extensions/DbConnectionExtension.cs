using System;
using System.Data;

namespace ConnectionPool.Extensions
{
    public static class DbConnectionExtension
    {
        public static bool HasConnect(this IDbConnection dbConnection)
        {
            return dbConnection.State != System.Data.ConnectionState.Closed &&
                   dbConnection.State != System.Data.ConnectionState.Broken;
        }

        public static bool IsExecuting(this IDbConnection dbConnection)
        {
            return dbConnection.State == System.Data.ConnectionState.Executing ||
                   dbConnection.State == System.Data.ConnectionState.Fetching;
        }


        public static bool CloseAndDispose(this IDbConnection dbConnection)
        {
            try
            {
                dbConnection.Close();
                dbConnection.Dispose();
            }
            catch (Exception e)
            {
                // ignored
            }

            return dbConnection.State == System.Data.ConnectionState.Closed;
        }
    }
}