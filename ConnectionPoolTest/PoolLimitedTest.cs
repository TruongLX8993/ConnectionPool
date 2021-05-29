using ConnectionPool;
using ConnectionPool.DbConnectionFactory;
using ConnectionPool.Exceptions;
using NUnit.Framework;

namespace ConnectionPoolTest
{
    [TestFixture]
    public class PoolLimitedTest
    {
        private PoolManager _poolManager;

        [SetUp]
        public void SetUp()
        {
            _poolManager = new PoolManager(new OracleDbConnectionFactory(),
                1,
                1, 
                1);
        }

        [Test]
        public void OpenAndRelease()
        {
            var pool = _poolManager.GetPool(Constance.ConnectionString);
            var connection = pool.GetConnection();
            pool.ReleaseConnection(connection);
            connection = pool.GetConnection();
            pool.ReleaseConnection(connection);
            Assert.True(true);
        }

        [Test]
        public void LimitedPool()
        {
            Assert.Throws<PoolLimitedException>(() =>
            {
                var pool = _poolManager.GetPool(Constance.ConnectionString);
                pool.GetConnection();
                pool.GetConnection();
            });
        }

        [TearDown]
        public void Clear()
        {
            _poolManager.CleanAll();
        }
    }
}