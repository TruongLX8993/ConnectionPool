using System;
using System.Data;
using System.Linq;

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
        private readonly object _lockTryClean = new object();


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
            _poolStorage.AddNewConnection(connection);
            connection.Active();
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

        public void ReturnConnection(IDbConnection dbConnection)
        {
            var connection = _poolStorage.GetConnection(dbConnection);
            if (connection == null) return;
            if (connection.Release())
                _poolStorage.Return(connection);
        }


        private void CleanExpiredConnection()
        {
            var removeStates = new[]
            {
                ConnectionState.Closed,
                ConnectionState.Expired,
                ConnectionState.Broken
            };

            var reUseStates = new[]
            {
                ConnectionState.MissRecycle
            };

            var connections = _poolStorage.GetConnections();
            foreach (var connection in connections)
            {
                connection.UpdateState();
                var state = connection.State;
                if (removeStates.Contains(state))
                {
                    if (connection.Close())
                        _poolStorage.Remove(connection);
                }

                if (reUseStates.Contains(state))
                {
                    if (connection.Release())
                    {
                        _poolStorage.Return(connection);
                    }
                }
            }

            _lastClean = DateTime.Now;
        }

        public void CleanAll()
        {
            var connections = _poolStorage.GetConnections();
            foreach (var connection in connections)
            {
                if (connection.Close())
                    _poolStorage.Remove(connection);
            }
        }

        public void TryClean()
        {
            lock (_lockTryClean)
            {
                var dur = (DateTime.Now - _lastClean).TotalSeconds;
                if (!(dur >= _lifeTimeSeconds) && GetPoolSize() <= _poolSizeThreshold) return;
                CleanExpiredConnection();
            }
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