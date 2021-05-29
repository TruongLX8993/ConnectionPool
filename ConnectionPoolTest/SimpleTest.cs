using ConnectionPool;
using ConnectionPool.DbConnectionFactory;
using NUnit.Framework;
using ConnectionState = System.Data.ConnectionState;

namespace ConnectionPoolTest
{
    [TestFixture]
    public class Tests
    {
        private PoolManager _poolManager;

        [OneTimeSetUp]
        public void SetUp()
        {
            _poolManager = new PoolManager(new OracleDbConnectionFactory(),
                10,
                1,
                5);
        }

        [Test]
        public void TestOpenAndRelease()
        {
            var pool = _poolManager.GetPool(Constance.ConnectionString);
            var connection = pool.GetConnection();
            var trans = connection.BeginTransaction();
            pool = _poolManager.GetPool(trans.Connection.ConnectionString);
            pool.ReleaseConnection(connection);
            Assert.True(connection != null && connection.State == ConnectionState.Open);
        }

        public void LimitPool()
        {
        }

        [TearDown]
        public void Clear()
        {
            _poolManager.CleanAll();
        }
    }
}