using System;
using ConnectionPool;
using ConnectionPool.DbConnectionFactory;

namespace ConnectionPoolDemo
{
    internal class Program
    {
        private const int MaxConn = 10;

        private static string ConnectionString =
            $"Data Source=(DESCRIPTION =(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.1.8)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=ONEMES3KCB)));Password=O1234#ONEMES3KCB;User ID=ONEMES3KCB; Connection Timeout=10;Max Pool Size=7;Min Pool Size=3;Incr Pool Size=1;Decr Pool Size=1;Pooling=true";

        private const int MaxNumberConnection = 2000;
        private const int ConnectionLifeTime = 3;
        private const int ConnectionThreshold = 100;

        public static void Main(string[] args)
        {
            var poolManager = new PoolManager(new OracleDbConnectionFactory(),
                MaxNumberConnection,
                ConnectionLifeTime,
                ConnectionThreshold);
            var pool = poolManager.GetPool(ConnectionString);
         
            
            Console.WriteLine("open new pool-----------------------");
            var connection1 = pool.GetConnection();
            Console.WriteLine($"State: {connection1.State}");
            Console.WriteLine($"PoolSize: {pool.GetPoolSize()}");
            
            Console.WriteLine("open new pool----------------------");
            var connection2 = pool.GetConnection();
            Console.WriteLine($"State: {connection2.State}");
            Console.WriteLine($"PoolSize: {pool.GetPoolSize()}");
            
            Console.WriteLine("Release pool-----------------------");
            pool.ReleaseConnection(connection1);
            Console.WriteLine($"State: {connection2.State}");
            Console.WriteLine($"PoolSize: {pool.GetPoolSize()}");
            
            Console.WriteLine("open new pool----------------------");
            var connection3 = pool.GetConnection();
            Console.WriteLine($"State: {connection2.State}");
            Console.WriteLine($"PoolSize: {pool.GetPoolSize()}");
        }
    }
}