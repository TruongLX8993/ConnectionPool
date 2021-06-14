using System;
using System.Data;
using System.Diagnostics;
using ConnectionPool.Extensions;

namespace ConnectionPool
{
    public class Pool
    {
        private readonly int _busynessSeconds;
        private readonly int _lifeTimeSeconds;
        private readonly int _poolSizeThreshold;
        private DateTime _lastClean;
        private readonly PoolStorage _poolStorage;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly string _dbConnectionString;

        public Pool(IDbConnectionFactory dbConnectionFactory,
            string dbConnectionString,
            int lifeTimeSeconds,
            int busynessSeconds,
            int poolSizeThreshold)
        {
            _lifeTimeSeconds = lifeTimeSeconds;
            _busynessSeconds = busynessSeconds;
            _dbConnectionFactory = dbConnectionFactory;
            _dbConnectionString = dbConnectionString;
            _poolStorage = new PoolStorage();
            _poolSizeThreshold = poolSizeThreshold;
            _lastClean = DateTime.Now;
        }


        public IDbConnection GetConnection()
        {
            TryClean();
            var connection = _poolStorage.GetFreeConnection();
            if (connection != null) return connection.Open();
            connection = CreateNew();
            connection.Active();
            // _poolStorage.AddNewConnection(connection);
            return connection.Open();
        }

        public int GetPoolSize()
        {
            return _poolStorage.GetSize();
        }

        public int GetNumberConnectionFree()
        {
            return _poolStorage.GetNumberConnectionFree();
        }

        public void ReleaseConnection(IDbConnection dbConnection)
        {
            var connection = new Connection(dbConnection, _lifeTimeSeconds, _busynessSeconds);
            _poolStorage.Return(connection);
        }


        public void CleanExpiredConnection()
        {
            _poolStorage.Clean(con =>
            {
                var state = con.State;
                return state == ConnectionState.Closed ||
                       state == ConnectionState.Expired ||
                       state == ConnectionState.MissRelease;
            }, true);
            _lastClean = DateTime.Now;
        }

        public void CleanAll()
        {
            _poolStorage.Clean(con => true);
            _lastClean = DateTime.Now;
        }

        public void TryClean()
        {
            var dur = (DateTime.Now - _lastClean).TotalSeconds;
            if (!(dur >= _lifeTimeSeconds) && GetPoolSize() <= _poolSizeThreshold) return;
            CleanExpiredConnection();
        }

        private Connection CreateNew()
        {
            var dbConnection = _dbConnectionFactory.CreateConnection(_dbConnectionString);
            return new Connection(dbConnection,
                _lifeTimeSeconds,
                _busynessSeconds
            );
        }
    }
}