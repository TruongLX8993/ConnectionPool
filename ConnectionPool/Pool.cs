using System;
using System.Data;
using ConnectionPool.Exceptions;
using ConnectionPool.Extensions;

namespace ConnectionPool
{
    public class Pool
    {
        private readonly int _maxPoolSize;
        private readonly int _lifeTimeSeconds;
        private readonly int _cleanPoolThreshold;
        private DateTime _lastClean;
        private readonly PoolStorage _poolStorage;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly string _dbConnectionString;
        private IConnectionListener _connectionListener;

        public Pool(IDbConnectionFactory dbConnectionFactory,
            string dbConnectionString,
            int maxPoolSize,
            int lifeTimeSeconds,
            int cleanPoolThreshold)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _dbConnectionString = dbConnectionString;
            _maxPoolSize = maxPoolSize;
            _lifeTimeSeconds = lifeTimeSeconds;
            _poolStorage = new PoolStorage();
            _cleanPoolThreshold = cleanPoolThreshold;
            _lastClean = DateTime.Now;
            _connectionListener = new ConnectionListener(_poolStorage);
        }


        public IDbConnection GetConnection()
        {
            TryClean();
            var connection = _poolStorage.GetFreeConnection();
            if (connection != null) return connection.Open();
            connection = CreateNew();
            _poolStorage.AddNewConnection(connection);
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
            var connection = _poolStorage.GetConnection(dbConnection);
            if (connection != null)
            {
                connection.Release();
                _poolStorage.Return(connection);
            }
            else
            {
                dbConnection.CloseAndDispose();
            }
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

        private void TryClean()
        {
            var dur = (DateTime.Now - _lastClean).TotalSeconds;
            if (!(dur >= _lifeTimeSeconds) && GetPoolSize() <= _cleanPoolThreshold) return;
            CleanExpiredConnection();
        }

        private Connection CreateNew()
        {
            if (_maxPoolSize <= _poolStorage.GetSize())
            {
                throw new PoolLimitedException();
            }

            var dbConnection = _dbConnectionFactory.CreateConnection(_dbConnectionString);
            return new Connection(dbConnection,
                _connectionListener,
                30,
                _lifeTimeSeconds);
        }
    }
}