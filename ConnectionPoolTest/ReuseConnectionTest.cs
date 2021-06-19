using System.Data;
using System.Threading;
using ConnectionPool;
using ConnectionPool.DbConnectionFactory;
using NUnit.Framework;

namespace ConnectionPoolTest
{
    [TestFixture(20, 1, 6, 5)]
    public class ReuseConnectionTest
    {
        private readonly int _maxConnectionPool;
        private readonly int _connectionLifeTime;
        private readonly int _cleanThreshold;
        private readonly int _numberConnection;
        private Pool _pool;


        public ReuseConnectionTest(int maxConnectionPool, int connectionLifeTime, int numberConnection,
            int cleanThreshold)
        {
            _maxConnectionPool = maxConnectionPool;
            _connectionLifeTime = connectionLifeTime;
            _numberConnection = numberConnection;
            _cleanThreshold = cleanThreshold;
        }

        [OneTimeSetUp]
        public void SetUp()
        {
            var poolManager = new PoolManager(new OracleDbConnectionFactory());
            _pool = poolManager.GetPool(Constance.ConnectionString);
        }

        [Test]
        public void TestClean()
        {
            IDbConnection connection = null;

            for (var i = 0; i < _numberConnection; i++)
            {
                connection = _pool.GetConnection();
            }

            var poolSize = _pool.GetPoolSize();
            if (poolSize != _numberConnection)
            {
                Assert.False(false,
                    $"Number connection in the pool not correct.Target is {_numberConnection} per {poolSize} ");
                return;
            }

            _pool.ReturnConnection(connection);
            _pool.GetConnection();
            poolSize = _pool.GetPoolSize();
            if (poolSize != _numberConnection - 1)
            {
                Assert.False(false,
                    $"Number connection in the pool not correct after clean.Target is {1} per {poolSize} ");
            }

            Assert.True(true);
        }
    }
}