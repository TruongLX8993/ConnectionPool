using System;
using System.Data;
using ConnectionPool.Exceptions;
using Oracle.DataAccess.Client;

namespace ConnectionPool
{
    internal class DbConnectionFactory
    {
        public static IDbConnection Create(string connectionString)
        {
            var connection = OracleClientFactory.Instance.CreateConnection();
            if (connection == null) throw new ConnectionPoolException("Can not create new connection");
            connection.ConnectionString = connectionString;
            return connection;
        }
    }
}