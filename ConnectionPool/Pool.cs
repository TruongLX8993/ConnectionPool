using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ConnectionPool.Exceptions;

namespace ConnectionPool
{
    public class Pool
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly string _dbConnectionString;
        private readonly int _maxPoolSize;
        private readonly int _lifeTimeSeconds;
        private readonly int _cleanPoolThreshold;
        private readonly Queue<Connection> _freeConnections;
        private readonly IDictionary<IDbConnection, Connection> _mapConnections;
        private DateTime _lastClean;

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
            _freeConnections = new Queue<Connection>();
            _mapConnections = new ConcurrentDictionary<IDbConnection, Connection>();
            _cleanPoolThreshold = cleanPoolThreshold;
            _lastClean = DateTime.Now;
        }

        public IDbConnection GetConnection()
        {
            TryClean();
            var connection = GetConnectionFromQueue();
            if (connection == null)
            {
                CreateNewConnection();
                connection = GetConnectionFromQueue();
            }

            connection.State = ConnectionState.Busy;
            // Debug.WriteLine($"GetConnection- Pool state: {GetPoolSize()}:{GetNumberConnectionFree()}");  
            return connection.DbConnection;
        }

        private Connection GetConnectionFromQueue()
        {
            while (_freeConnections.Count != 0)
            {
                var connection = _freeConnections.Dequeue();
                if (connection == null || connection.State != ConnectionState.Free) continue;
                return connection;
            }

            return null;
        }

        private void CreateNewConnection()
        {
            var currentPoolSize = GetPoolSize();
            if (currentPoolSize >= _maxPoolSize)
            {
                throw new PoolLimitedException();
            }

            var dbConnection = _dbConnectionFactory.CreateConnection(_dbConnectionString);
            var newConnection = new Connection(dbConnection, _lifeTimeSeconds) {State = ConnectionState.Free};
            _mapConnections.Add(dbConnection, newConnection);
            dbConnection.Open();
            _freeConnections.Enqueue(newConnection);
        }

        public int GetPoolSize()
        {
            return _mapConnections.Count();
        }

        public int GetNumberConnectionFree()
        {
            return _freeConnections.Count;
        }

        public void ReleaseConnection(IDbConnection dbConnection)
        {
            if (!_mapConnections.ContainsKey(dbConnection))
            {
                dbConnection.Close();
                return;
            }

            var connection = _mapConnections[dbConnection];
            if (connection != null)
            {
                connection.State = ConnectionState.Free;
                _freeConnections.Enqueue(connection);
            }
        }

        private void TryClean()
        {
            var dur = (DateTime.Now - _lastClean).TotalSeconds;
            if (!(dur >= _lifeTimeSeconds) && GetPoolSize() <= _cleanPoolThreshold) return;
            CleanExpiredConnection();
            _lastClean = DateTime.Now;
        }

        public void CleanExpiredConnection()
        {
            Clean(con => con != null && (con.IsExpired() ||
                                         con.State == ConnectionState.MarkClose ||
                                         con.State == ConnectionState.Closed), true);
        }

        public void CleanAll()
        {
            Clean(con => true);
        }

        private void Clean(Func<Connection, bool> cleanCondition, bool reuseConnection = false)
        {
            IList<Connection> expiredConnections = _mapConnections.Values
                .ToList();
            IList<Connection> removedConnection = new List<Connection>();

            expiredConnections = expiredConnections.Where(cleanCondition)
                .ToList();

            foreach (var connection in expiredConnections)
            {
                if (connection.State == ConnectionState.Busy)
                {
                    if (reuseConnection && connection.DbConnection.State == System.Data.ConnectionState.Open)
                    {
                        connection.State = ConnectionState.Free;
                        _freeConnections.Enqueue(connection);
                        continue;
                    }
                }

                if (connection.State == ConnectionState.Busy &&
                    connection.DbConnection.State == System.Data.ConnectionState.Executing)
                {
                    continue;
                }

                connection.State = ConnectionState.Closed;
                removedConnection.Add(connection);
                if (_mapConnections.ContainsKey(connection.DbConnection))
                {
                    _mapConnections.Remove(connection.DbConnection);
                }
            }

            foreach (var connection in removedConnection)
            {
                connection.DbConnection.Close();
            }

            Debug.WriteLine($"Pool-clean:{removedConnection.Count}-{GetNumberConnectionFree()}-{GetPoolSize()}");
        }
    }
}