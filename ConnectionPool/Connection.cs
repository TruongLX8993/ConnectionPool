using System;
using System.Data;
using ConnectionPool.Exceptions;

namespace ConnectionPool
{
    public enum ConnectionState
    {
        Free,
        Active,
        Busy,
        Closed,
    }

    public class Connection
    {
        public Connection(IDbConnection dbConnection, int lifeTimeSeconds)
        {
            DbConnection = dbConnection;
            _lifeTimeSeconds = lifeTimeSeconds;
            State = ConnectionState.Free;
        }

        private DateTime _lastUpdateTime;
        private int _lifeTimeSeconds;

        private ConnectionState _state;

        public ConnectionState State
        {
            get { return _state; }
            private set
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
            State = ConnectionState.Closed;
            DbConnection.Close();
            DbConnection.Dispose();
        }

        public void Active()
        {
            State = ConnectionState.Active;
        }

        public bool IsExecuting()
        {
            return _state == ConnectionState.Busy &&
                   DbConnection.State == System.Data.ConnectionState.Executing;
        }

        public bool MissRelease()
        {
            return IsExpired() && _state == ConnectionState.Busy &&
                   DbConnection.State == System.Data.ConnectionState.Open;
        }

        public bool IsFree()
        {
            return _state == ConnectionState.Free && DbConnection.State == System.Data.ConnectionState.Open;
        }

        public IDbConnection Open()
        {
            State = ConnectionState.Busy;
            if (DbConnection.State != System.Data.ConnectionState.Open)
                DbConnection.Open();
            return DbConnection;
        }

        public bool IsClosed()
        {
            return _state == ConnectionState.Closed && DbConnection != null &&
                   DbConnection.State == System.Data.ConnectionState.Closed;
        }

        public void Release()
        {
            State = ConnectionState.Free;
        }
    }
}