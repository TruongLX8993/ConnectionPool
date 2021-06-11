using System.Data;
using System.Threading;
using ConnectionPool;
using ConnectionPool.DbConnectionFactory;
using NUnit.Framework;

namespace ConnectionPoolTest
{
    [TestFixture(10,10)]
    public class RedundantConnectionTest
    {
        private readonly int _numberConnection;
        private readonly int _cleanThresHold;
        private Pool _pool;
        private IDbConnection _dbConnection;

        public RedundantConnectionTest(int numberConnection,int cleanThreshold)
        {
            _numberConnection = numberConnection;
            _cleanThresHold = cleanThreshold;

        }

        [OneTimeSetUp]
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
            for (var i = 0; i < _numberConnection; i++)
            {
                _dbConnection = _pool.GetConnection();
                _dbConnection.BeginTransaction();
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