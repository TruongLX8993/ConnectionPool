using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
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
        Executing,
        MissRelease,
        Broken,
        Closed,
        Expired,
    }


    public class Connection
    {
        private DateTime _lastUpdateTime;
        private readonly int _lifeTimeSeconds;
        private readonly int _maxBusySeconds;
        private readonly object _lockState = new object();
        private ConnectionState _state;
        private readonly IDbConnection _dbConnection;
        private bool _openedFlag;
        private ConnectionState? _prevState;

        public Connection(IDbConnection dbConnection,
            int lifeTimeSeconds,
            int maxBusySeconds)
        {
            State = ConnectionState.Free;
            _lifeTimeSeconds = lifeTimeSeconds;
            _maxBusySeconds = maxBusySeconds;
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
                    if (value == _prevState) return;
                    _prevState = _state;
                    _state = value;
                }
            }
        }


        public bool Active()
        {
            lock (_lockState)
            {
                var currentState = State;
                if (currentState != ConnectionState.Free) return false;
                State = ConnectionState.Active;
                return true;
            }
        }


        public IDbConnection Open()
        {
            lock (_lockState)
            {
                var currentState = State;
                if (_state != ConnectionState.Active)
                    throw new StateException(currentState, ConnectionState.Active);
                if (_dbConnection.State == System.Data.ConnectionState.Closed)
                    _dbConnection.Open();
                State = ConnectionState.Busy;
                _openedFlag = true;
                return _dbConnection;
            }
        }

        public bool Release()
        {
            lock (_lockState)
            {
                var currentState = State;
                if (currentState == ConnectionState.Closed ||
                    currentState == ConnectionState.Broken ||
                    currentState == ConnectionState.Executing)
                    return false;
                State = ConnectionState.Free;
                return true;
            }
        }

        public bool Close()
        {
            lock (_lockState)
            {
                var currentState = State;
                if (currentState == ConnectionState.Closed)
                    return true;
                
                if (CanCloseDbConnection(currentState, _prevState))
                {
                    if (!_dbConnection.CloseAndDispose())
                        return false;
                }

                State = ConnectionState.Closed;
                return true;
            }
        }

        private static bool CanCloseDbConnection(ConnectionState currentState, ConnectionState? prevState)
        {
            if (currentState == ConnectionState.Free || currentState == ConnectionState.Broken)
                return true;
            if (currentState == ConnectionState.Expired)
            {
                var prevStates = new List<ConnectionState> {ConnectionState.Broken, ConnectionState.Free};
                if (prevState == null || prevStates.Contains(prevState.Value))
                    return true;
            }

            return false;
        }

        private void UpdateState()
        {
            lock (_lockState)
            {
                if (_openedFlag && !_dbConnection.HasConnect())
                {
                    State = ConnectionState.Broken;
                    return;
                }

                if (_dbConnection.IsExecuting())
                {
                    State = ConnectionState.Executing;
                    return;
                }

                if (IsExpired())
                {
                    State = ConnectionState.Expired;
                }

                if (IsBusyTimeout())
                {
                    State = ConnectionState.MissRelease;
                }
            }
        }

        private bool IsExpired()
        {
            return _lifeTimeSeconds > 0 && DateTime.Now.Subtract(_lastUpdateTime).TotalSeconds > _lifeTimeSeconds;
        }

        private bool IsBusyTimeout()
        {
            return _maxBusySeconds > 0 &&
                   DateTime.Now.Subtract(_lastUpdateTime).TotalSeconds > _maxBusySeconds;
        }
    }
}