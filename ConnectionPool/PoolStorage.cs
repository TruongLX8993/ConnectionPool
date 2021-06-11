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

        public Connection GetFreeConnection()
        {
            lock (_lock)
            {
                while (_freeConnections.Count != 0)
                {
                    var con = _freeConnections.Dequeue();
                    if (con.State != ConnectionState.Free) continue;
                    con.Active();
                    return con;
                }
                return null;
            }
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
                if (connection == null)
                    return;

                if (connection.State == ConnectionState.Free)
                    _freeConnections.Enqueue(connection);
                
                if (!_mapConnections.ContainsKey(connection.DatabaseConnection))
                    _mapConnections.Add(connection.DatabaseConnection, connection);
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
            lock (_lock)
            {
                return _mapConnections.Count;
            }
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

        private IList<Connection> GetConnections(Func<Connection, bool> condition)
        {
            lock (_lock)
            {
                return _mapConnections.Values.Where(condition).ToList();
            }
        }

        public void Clean(Func<Connection, bool> cleanCondition, bool reuseConnection = false)
        {
            var removedConnections = new List<Connection>();
            var reuseConnections = new List<Connection>();
            var expiredConnections = GetConnections(cleanCondition);

            foreach (var connection in expiredConnections)
            {
                if (reuseConnection && connection.State == ConnectionState.MissRelease)
                {
                    reuseConnections.Add(connection);
                    continue;
                }

                removedConnections.Add(connection);
            }

            foreach (var connection in removedConnections.Where(connection => connection.Close()))
            {
                Remove(connection);
            }

            foreach (var connection in reuseConnections.Where(connection => connection.Release()))
            {
                Return(connection);
            }
        }

        public void MoveToFreeQueue(Connection con)
        {
        }
    }
}