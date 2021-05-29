using ConnectionPool;
using NUnit.Framework;
using ConnectionState = System.Data.ConnectionState;

namespace ConnectionPoolTest
{
    [TestFixture]
    public class Tests
    {
        private PoolManager _poolManager;

        [SetUp]
        public void SetUp()
        {
            _poolManager = new PoolManager(1, 1);
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