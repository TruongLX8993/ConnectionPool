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
                    if (con.IsFree())
                    {
                        con.Active();
                        return con;
                    }
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
                if (!_mapConnections.ContainsKey(connection.DbConnection))
                {
                    _mapConnections.Add(connection.DbConnection, connection);
                }
            }
        }

        public void ReturnConnection(Connection connection)
        {
            lock (_lock)
            {
                if (connection.IsFree())
                {
                    _freeConnections.Enqueue(connection);
                }

                if (!_mapConnections.ContainsKey(connection.DbConnection))
                {
                    _mapConnections.Add(connection.DbConnection, connection);
                }
            }
        }

        public void RemoveConnection(Connection connection)
        {
            lock (_lock)
            {
                if (!connection.IsClosed()) return;
                if (_mapConnections.ContainsKey(connection.DbConnection))
                {
                    _mapConnections.Remove(connection.DbConnection);
                }
            }
        }

        public int GetPoolSize()
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

        public IList<Connection> GetConnections(Func<Connection, bool> condition)
        {
            lock (_lock)
            {
                return _mapConnections.Values.Where(condition).ToList();
            }
        }

        private void DebugState(string method)
        {
            Debug.WriteLine($"{method}:{GetFreeConnection()}-{GetPoolSize()}");
        }
    }
}