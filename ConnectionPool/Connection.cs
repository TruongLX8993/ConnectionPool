using System;
using System.Data;
using System.Data.Odbc;
using ConnectionPool.Exceptions;
using ConnectionPool.Extensions;
using ConnectionPool.Utils;

namespace ConnectionPool
{
    public enum ConnectionState
    {
        Free,
        Active,
        Busy,
        MissRelease,
        ReadyToClose,
        Closed,
        Expired,
    }


    public class Connection
    {
        private DateTime _lastUpdateTime;
        private readonly int _lifeTimeSeconds;
        private readonly int _timeoutReturnSeconds;
        private readonly object _lockState = new object();
        private ConnectionState _state;
        private readonly IConnectionListener _listener;
        private readonly IDbConnection _dbConnection;

        public Connection(IDbConnection dbConnection,
            IConnectionListener listener,
            int lifeTimeSeconds,
            int timeoutReturnSeconds)
        {
            State = ConnectionState.Free;
            _listener = listener;
            _lifeTimeSeconds = lifeTimeSeconds;
            _timeoutReturnSeconds = timeoutReturnSeconds;
            _dbConnection = dbConnection;
        }

        public IDbConnection DatabaseConnection
        {
            get { return _dbConnection; }
        }

        public ConnectionState State
        {
            get
            {
                lock (_lockState)
                {
                    UpdateState();
                    return _state;
                }
            }
            private set
            {
                lock (_lockState)
                {
                    _lastUpdateTime = DateTime.Now;
                    _state = value;
                }
            }
        }

        public bool Close()
        {
            lock (_lockState)
            {
                if (_dbConnection.State == System.Data.ConnectionState.Executing)
                {
                    return false;
                }

                if (!_dbConnection.CloseAndDispose()) return true;
                State = ConnectionState.Closed;
                _listener.OnClose(this);
            }

            return true;
        }

        public bool Active()
        {
            lock (_lockState)
            {
                if (!_dbConnection.HasConnect()) return false;
                if (_state != ConnectionState.Free) return false;
                State = ConnectionState.Active;
                _listener.OnActive(this);
                return true;
            }
        }


        public IDbConnection Open()
        {
            lock (_lockState)
            {
                State = ConnectionState.Busy;
                if (_dbConnection.State == System.Data.ConnectionState.Closed)
                    _dbConnection.Open();
                _listener.OnClose(this);
                return _dbConnection;
            }
        }


        /// <summary>
        /// State switch to free
        /// </summary>
        /// <returns></returns>
        public bool Release()
        {
            lock (_lockState)
            {
                if (_dbConnection.State != System.Data.ConnectionState.Open) return false;
                State = ConnectionState.Free;
                _listener.OnRelease(this);
                return true;
            }
        }

        private void UpdateState()
        {
            lock (_lockState)
            {
                if (IsClosed())
                {
                    State = ConnectionState.Closed;
                    return;
                }

                if (IsExpired())
                {
                    State = ConnectionState.Expired;
                }

                if (IsTimeoutReturnPool())
                {
                    State = ConnectionState.MissRelease;
                }
            }
        }

        public bool IsExpired()
        {
            return _lifeTimeSeconds > 0 && DateTime.Now.Subtract(_lastUpdateTime).TotalSeconds > _lifeTimeSeconds;
        }

        public bool IsTimeoutReturnPool()
        {
            return _timeoutReturnSeconds > 0 &&
                   DateTime.Now.Subtract(_lastUpdateTime).TotalSeconds > _timeoutReturnSeconds;
        }

        private bool IsMissRelease()
        {
            return TimeoutUtil.IsTimeout(_lastUpdateTime, DateTime.Now, _timeoutReturnSeconds) &&
                   _state == ConnectionState.Busy &&
                   _dbConnection.State == System.Data.ConnectionState.Open;
        }

        public bool IsFree()
        {
            return _state == ConnectionState.Free &&
                   _dbConnection.State == System.Data.ConnectionState.Open;
        }

        public bool IsClosed()
        {
            return _state == ConnectionState.Closed || (_dbConnection != null &&
                                                        _dbConnection.State == System.Data.ConnectionState.Closed);
        }
    }
}