using System;
using System.Data;

namespace ConnectionPool
{
    public enum ConnectionState
    {
        Free,
        Busy,
        Closed,
    }

    public class Connection
    {
        public Connection(IDbConnection dbConnection, int lifeTimeMinutes)
        {
            DbConnection = dbConnection;
            _lifeTimeMinutes = lifeTimeMinutes;
        }

        private DateTime _lastUpdateTime;
        private int _lifeTimeMinutes;

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
            return DateTime.Now.Subtract(_lastUpdateTime).TotalMinutes > _lifeTimeMinutes;
        }

        public void Close()
        {
            if (_state == ConnectionState.Closed)
            {
                DbConnection.Close();
                DbConnection.Dispose();
            }
        }
    }
}