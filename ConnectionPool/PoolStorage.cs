using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace ConnectionPool
{
    public class PoolStorage
    {
        public PoolStorage()
        {
            _freeConnections = new Queue<Connection>();
            _mapConnections = new Dictionary<IDbConnection, Connection>();
        }

        private readonly Queue<Connection> _freeConnections;
        private readonly object _lock = new object();
        private readonly IDictionary<IDbConnection, Connection> _mapConnections;
        private int _globalCounter = 0;
        private int _recycleCounter = 0;

        public Connection GetFreeConnection()
        {
            lock (_lock)
            {
                _globalCounter++;
                while (_freeConnections.Count != 0)
                {
                    var con = GetFromQueue();
                    if (con.State != ConnectionState.Free) continue;
                    con.Active();
                    _recycleCounter++;
                    Debug.WriteLine($"{_recycleCounter}:{_globalCounter}");
                    return con;
                }

                Debug.WriteLine($"{_recycleCounter}:{_globalCounter}");
                return null;
            }
        }

        private Connection GetFromQueue()
        {
            var con = _freeConnections.Dequeue();
            if (con == null) return null;
            if (_mapConnections.ContainsKey(con.DatabaseConnection))
                _mapConnections.Remove(con.DatabaseConnection);
            return con;
        }

        public void AddNewConnection(Connection connection, bool enqueue = false)
        {
            lock (_lock)
            {
                if (enqueue)
                    _freeConnections.Enqueue(connection);

                if (!_mapConnections.ContainsKey(connection.DatabaseConnection))
                    _mapConnections.Add(connection.DatabaseConnection, connection);
            }
        }

        public void Return(Connection connection)
        {
            lock (_lock)
            {
                var currentState = connection.State;
                if (currentState == ConnectionState.Free)
                {
                    AddNewConnection(connection, true);
                }
            }
        }

        public void Remove(Connection connection)
        {
            lock (_lock)
            {
                if (_mapConnections.ContainsKey(connection.DatabaseConnection))
                {
                    _mapConnections.Remove(connection.DatabaseConnection);
                }
            }
        }

        public int GetSize()
        {
            return _mapConnections.Count;
        }

        public int GetNumberConnectionFree()
        {
            return _freeConnections.Count;
        }

        public Connection GetConnection(IDbConnection dbConnection)
        {
            lock (_lock)
            {
                return _mapConnections.ContainsKey(dbConnection) ? _mapConnections[dbConnection] : null;
            }
        }

        private IEnumerable<Connection> GetConnections(Func<Connection, bool> condition)
        {
            lock (_lock)
            {
                return _mapConnections.Values.Where(condition).ToList();
            }
        }

        public void Clean(Func<Connection, bool> cleanCondition, bool reuseConnection = false)
        {
            var removedConnections = new List<Connection>();
            lock (_lock)
            {
                var expiredConnections = GetConnections(cleanCondition);
                removedConnections.AddRange(expiredConnections);

                foreach (var connection in removedConnections.Where(connection => connection.Close()))
                    Remove(connection);
            }
        }
    }
}