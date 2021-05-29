using System.Data;
using System.Threading;
using ConnectionPool;
using ConnectionPool.DbConnectionFactory;
using NUnit.Framework;

namespace ConnectionPoolTest
{
    [TestFixture]
    public class PoolSequenceTest
    {
        private Pool _pool;
        private IDbConnection _dbConnection;

        [SetUp]
        public void SetUp()
        {
            var poolManager = new PoolManager(new OracleDbConnectionFactory(),
                10,
                1,
                5);
            _pool = poolManager.GetPool(Constance.ConnectionString);
        }

        [Test, Order(1)]
        public void Open()
        {
            _dbConnection = _pool.GetConnection();
        }

        [Test, Order(2)]
        public void Release()
        {
            _pool.ReleaseConnection(_dbConnection);
        }

        [Test(ExpectedResult = 0), Order(3)]
        public int Clean()
        {
            _pool.CleanAll();
            Thread.Sleep(20000);
            return _pool.GetPoolSize();
        }
    }
}