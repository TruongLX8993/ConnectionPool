using System.Data;

namespace ConnectionPool
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection(string connectionString);
    }
}