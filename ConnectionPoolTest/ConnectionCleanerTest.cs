using System.Threading;
using ConnectionPool;
using ConnectionPool.DbConnectionFactory;
using NUnit.Framework;

namespace ConnectionPoolTest
{
    [TestFixture]
    public class ConnectionCleanerTest
    {
        private PoolManager _poolManager;
        private Pool _pool;

        [OneTimeSetUp]
        public void SetUp()
        {
            var poolManager = new PoolManager(new OracleDbConnectionFactory(),
                10,
                1,
                5);
            _pool = poolManager.GetPool(Constance.ConnectionString);
        }

        [Test]
        public void Test()
        {
            _pool.GetConnection();
            Thread.Sleep(20000);
            _pool.GetConnection();
            Thread.Sleep(60000);
        }
    }
}