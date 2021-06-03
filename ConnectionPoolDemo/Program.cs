using System;
using ConnectionPool;
using ConnectionPool.DbConnectionFactory;

namespace ConnectionPoolDemo
{
    internal class Program
    {
        private static string ConnectionString =
            $"Data Source=(DESCRIPTION =(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.1.8)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=ONEMES3KCB)));Password=O1234#ONEMES3KCB;User ID=ONEMES3KCB; Connection Timeout=10;Max Pool Size=7;Min Pool Size=3;Incr Pool Size=1;Decr Pool Size=1;Pooling=true";

        private const int MaxNumberConnection = 2000; // so luong connection lon nhat trong  pool..
        private const int ConnectionLifeTime = 30; // thoi gian ton tai lon nhat trong pool.
        private const int ConnectionThreshold = 100; // nguong gioi han so connection.

        public static void Main(string[] args)
        {
            var poolManager = new PoolManager(new OracleDbConnectionFactory(),
                MaxNumberConnection,
                ConnectionLifeTime,
                ConnectionThreshold);
            var pool = poolManager.GetPool(ConnectionString);
         
            
            var connection1 = pool.GetConnection(); //  tao mot connection moi
            Console.WriteLine("open new pool-----------------------");
            Console.WriteLine($"State: {connection1.State}");
            Console.WriteLine($"PoolSize: {pool.GetPoolSize()}");
            
            var connection2 = pool.GetConnection(); // tao them mot connection moi
            Console.WriteLine("open new pool----------------------");
            Console.WriteLine($"State: {connection2.State}");
            Console.WriteLine($"PoolSize: {pool.GetPoolSize()}");
            
            pool.ReleaseConnection(connection1);  // giai phong connection.
            Console.WriteLine("Release pool-----------------------");
            Console.WriteLine($"State: {connection2.State}");
            Console.WriteLine($"PoolSize: {pool.GetPoolSize()}");
            
            var connection3 = pool.GetConnection(); // tao them conntion thu 3
            Console.WriteLine("open new pool----------------------");
            Console.WriteLine($"State: {connection2.State}");
            Console.WriteLine($"PoolSize: {pool.GetPoolSize()}");
            
            
            /*
             * Result: So luong connection sau khi thuc hien la 2
             * 
             */
        }
    }
}