using System.Data;
using ConnectionPool.Exceptions;
using Oracle.DataAccess.Client;

namespace ConnectionPool.DbConnectionFactory
{
    public class OracleDbConnectionFactory :IDbConnectionFactory
    {
        public IDbConnection CreateConnection(string connectionString)
        {
            var connection = OracleClientFactory.Instance.CreateConnection();
            if (connection == null) throw new ConnectionPoolException("Can not create new connection");
            connection.ConnectionString = connectionString;
            return connection;
        }
    }
}