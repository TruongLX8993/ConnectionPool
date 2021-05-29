using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ConnectionPool.Exceptions;

namespace ConnectionPool
{
    public class Pool
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly string _dbConnectionString;
        private readonly int _maxPoolSize;
        private readonly int _lifeTimeMinutes;
        private readonly int _cleanPoolThreshold;

        private readonly Queue<Connection> _freeConnections;
        private readonly IDictionary<IDbConnection, Connection> _mapConnections;

        public Pool(IDbConnectionFactory dbConnectionFactory,
            string dbConnectionString,
            int maxPoolSize,
            int lifeTimeMinutes,
            int cleanPoolThreshold)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _dbConnectionString = dbConnectionString;
            _maxPoolSize = maxPoolSize;
            _lifeTimeMinutes = lifeTimeMinutes;
            _freeConnections = new Queue<Connection>();
            _mapConnections = new Dictionary<IDbConnection, Connection>();
            _cleanPoolThreshold = cleanPoolThreshold;
        }

        public bool MatchConnectionString(string connection)
        {
            return connection == _dbConnectionString ||
                   _mapConnections.FirstOrDefault().Value.DbConnection.ConnectionString == connection;
        }

        public IDbConnection GetConnection()
        {
            var dbConnection = GetConnectionFromQueue();
            if (dbConnection == null)
            {
                CreateNewConnection();
                dbConnection = GetConnectionFromQueue();
            }

            TryClean();
            return dbConnection;
        }

        private IDbConnection GetConnectionFromQueue()
        {
            while (_freeConnections.Count != 0)
            {
                var connection = _freeConnections.Dequeue();
                if (connection.State == ConnectionState.Free)
                {
                    return connection.DbConnection;
                }
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
            var newConnection = new Connection(dbConnection, _lifeTimeMinutes) {State = ConnectionState.Free};
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
            if (_mapConnections.ContainsKey(dbConnection))
            {
                var connection = _mapConnections[dbConnection];
                connection.State = ConnectionState.Free;
                _freeConnections.Enqueue(connection);
            }
            else
            {
                dbConnection.Close();
            }
        }

        private void TryClean()
        {
            if (_cleanPoolThreshold > 1 &&
                GetPoolSize() >= _cleanPoolThreshold)
            {
                CleanExpiredConnection();
            }
        }

        public void CleanExpiredConnection()
        {
            Clean(con => con.IsExpired());
        }

        public void CleanAll()
        {
            Clean(con => true);
        }

        private void Clean(Func<Connection, bool> cleanCondition)
        {
            IList<KeyValuePair<IDbConnection, Connection>> expiredConnectionKeyPairs;
            lock (_mapConnections)
            {
                expiredConnectionKeyPairs = _mapConnections.Where(keyPair => cleanCondition(keyPair.Value))
                    .ToList();
                foreach (var expiredConnectionKeyPair in expiredConnectionKeyPairs)
                {
                    cleanCondition(expiredConnectionKeyPair.Value);
                    expiredConnectionKeyPair.Value.State = ConnectionState.Closed;
                    _mapConnections.Remove(expiredConnectionKeyPair.Key);
                }
            }

            foreach (var expiredConnectionKeyPair in expiredConnectionKeyPairs)
            {
                try
                {
                    expiredConnectionKeyPair.Value.Close();
                }
                catch (Exception e)
                {
                    // ignored
                }
            }
        }
    }
}