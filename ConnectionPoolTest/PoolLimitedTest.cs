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
            pool.ReturnConnection(connection);
            connection = pool.GetConnection();
            pool.ReturnConnection(connection);
            Assert.True(true);
        }

        [TearDown]
        public void Clear()
        {
            _poolManager.CleanAll();
        }
    }
}