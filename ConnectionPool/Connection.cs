using System;
using System.Data;
using ConnectionPool.Exceptions;

namespace ConnectionPool
{
    public enum ConnectionState
    {
        Free,
        Busy,
        MarkClose,
        Closed,
    }

    public class Connection
    {
        public Connection(IDbConnection dbConnection, int lifeTimeSeconds)
        {
            DbConnection = dbConnection;
            _lifeTimeSeconds = lifeTimeSeconds;
        }

        private DateTime _lastUpdateTime;
        private int _lifeTimeSeconds;

        private ConnectionState _state;

        public ConnectionState State
        {
            get { return _state; }
            set
            {
                _lastUpdateTime = DateTime.Now;
                _state = value;
            }
        }

        public IDbConnection DbConnection { get; }

        public bool IsExpired()
        {
            return _lifeTimeSeconds > 0 && DateTime.Now.Subtract(_lastUpdateTime).TotalSeconds > _lifeTimeSeconds;
        }

        public void Close()
        {
            if (_state != ConnectionState.Closed)
            {
                throw new ConnectionPoolException("Pool is set closed state before close");
            }
            DbConnection.Close();
            DbConnection.Dispose();
        }
    }
}