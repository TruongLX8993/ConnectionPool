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
            _poolManager = new PoolManager(new OracleDbConnectionFactory());
        }

        [Test]
        public void TestOpenAndRelease()
        {
            var pool = _poolManager.GetPool(Constance.ConnectionString);
            var dbConnection = pool.GetConnection();
            var trans = dbConnection.BeginTransaction();
            pool = _poolManager.GetPool(trans.Connection.ConnectionString);
            pool.ReturnConnection(dbConnection);
            Assert.True(dbConnection != null && dbConnection.State == ConnectionState.Open);
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