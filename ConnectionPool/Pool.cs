using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using ConnectionPool.Exceptions;

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
        }


        private Connection CreateNew()
        {
            if (_maxPoolSize <= _poolStorage.GetPoolSize())
            {
                throw new PoolLimitedException();
            }

            var dbConnection = _dbConnectionFactory.CreateConnection(_dbConnectionString);
            return new Connection(dbConnection, _lifeTimeSeconds);
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
            return _poolStorage.GetPoolSize();
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
                _poolStorage.ReturnConnection(connection);
            }
            else
            {
                dbConnection.Close();
            }
        }


        public void CleanExpiredConnection()
        {
            Clean(con => con != null && (con.IsExpired() ||
                                         con.State == ConnectionState.Closed), true);
        }

        public void CleanAll()
        {
            Clean(con => true);
        }

        private void Clean(Func<Connection, bool> cleanCondition, bool reuseConnection = false)
        {
            var removedConnections = new List<Connection>();
            var reuseConnections = new List<Connection>();
            var expiredConnections = _poolStorage.GetConnections(cleanCondition);

            foreach (var connection in expiredConnections)
            {
                if (reuseConnection && connection.MissRelease())
                {
                    reuseConnections.Add(connection);
                    continue;
                }

                if (connection.IsExecuting())
                    continue;
                removedConnections.Add(connection);
            }

            foreach (var connection in removedConnections)
            {
                connection.Close();
                _poolStorage.RemoveConnection(connection);
            }

            foreach (var connection in reuseConnections)
            {
                connection.Release();
                _poolStorage.ReturnConnection(connection);
            }

            Debug.WriteLine($"Pool-clean:{removedConnections.Count}-{GetNumberConnectionFree()}-{GetPoolSize()}");
        }

        private void TryClean()
        {
            var dur = (DateTime.Now - _lastClean).TotalSeconds;
            if (!(dur >= _lifeTimeSeconds) && GetPoolSize() <= _cleanPoolThreshold) return;
            CleanExpiredConnection();
            _lastClean = DateTime.Now;
        }
    }
}