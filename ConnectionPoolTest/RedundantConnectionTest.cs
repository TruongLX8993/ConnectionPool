using System.Data;
using System.Threading;
using ConnectionPool;
using NUnit.Framework;

namespace ConnectionPoolTest
{
    [TestFixture(20)]
    public class RedundantConnectionTest
    {
        private readonly int _numberConnection;
        private Pool _pool;
        private IDbConnection _dbConnection;

        public RedundantConnectionTest(int numberConnection)
        {
            _numberConnection = numberConnection;
        }

        [OneTimeSetUp]
        public void SetUp()
        {
            var poolManager = new PoolManager(_numberConnection, 1);
            _pool = poolManager.GetPool(Constance.ConnectionString);
        }

        [Test, Order(1)]
        public void Open()
        {
            for (var i = 0; i < _numberConnection; i++)
            {
                _dbConnection = _pool.GetConnection();
            }
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
            Thread.Sleep(10000);
            return _pool.GetPoolSize();
        }
    }
}