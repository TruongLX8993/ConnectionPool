using System.Data;
using System.Threading;
using ConnectionPool;
using ConnectionPool.DbConnectionFactory;
using NUnit.Framework;

namespace ConnectionPoolTest
{
    [TestFixture(20, 1,6, 5)]
    public class CleanPoolTest
    {
        private readonly int _maxConnectionPool;
        private readonly int _connectionLifeTime;
        private readonly int _cleanThreshold;
        private readonly int _numberConnection;
        private Pool _pool;


        public CleanPoolTest(int maxConnectionPool, int connectionLifeTime,int numberConnection, int cleanThreshold)
        {
            _maxConnectionPool = maxConnectionPool;
            _connectionLifeTime = connectionLifeTime;
            _numberConnection = numberConnection;
            _cleanThreshold = cleanThreshold;
        }

        [OneTimeSetUp]
        public void SetUp()
        {
            var poolManager = new PoolManager(
                new OracleDbConnectionFactory(),
                _maxConnectionPool,
                _connectionLifeTime,
                _cleanThreshold);
            _pool = poolManager.GetPool(Constance.ConnectionString);
        }

        [Test]
        public void TestClean()
        {
            for (var i = 0; i < _numberConnection; i++)
            {
                _pool.GetConnection();
            }

            Thread.Sleep(30000);
            var poolSize = _pool.GetPoolSize();
            if (poolSize != _numberConnection)
            {
                Assert.False(false,
                    $"Number connection in the pool not correct.Target is {_numberConnection} per {poolSize} ");
                return;
            }

            Thread.Sleep(40000);
            _pool.GetConnection();
            poolSize = _pool.GetPoolSize();
            if (poolSize != 1)
            {
                Assert.False(false,
                    $"Number connection in the pool not correct after clean.Target is {1} per {poolSize} ");
            }

            Assert.True(true);
        }
    }
}