using System;
using System.Data;
using ConnectionPool.Exceptions;
using ConnectionPool.Extensions;

namespace ConnectionPool
{
    public enum ConnectionState
    {
        Free,
        Active,
        Busy,
        Executing,
        MissRecycle,
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
        private bool _openedFlag;
        private ConnectionState? _prevState;
        private IConnectionListener _listener;


        public Connection(IDbConnection dbConnection,
            int lifeTimeSeconds,
            int maxBusySeconds)
        {
            State = ConnectionState.Free;
            _lifeTimeSeconds = lifeTimeSeconds;
            _maxBusySeconds = maxBusySeconds;
            DatabaseConnection = dbConnection;
        }

        public IDbConnection DatabaseConnection { get; }


        public void Update()
        {
            lock (_lockState)
            {
                var newState = GetNewState();
                if (newState == ConnectionState.Expired || newState == ConnectionState.Closed || newState == ConnectionState.Broken)
                {
                    if (Close())
                    {
                        _listener.OnClose(this);
                    }
                }

                if (newState == ConnectionState.MissRecycle)
                {
                    State = ConnectionState.Free;
                    _listener.OnRecycle(this);
                }
                
                if()
                
            }
        }

        public ConnectionState GetAndUpdateState()
        {
            UpdateState();
            return _state;
        }

        public void UpdateState()
        {
            lock (_lockState)
            {
                var newState = GetNewState();
                if (newState == null || newState.Value == _state)
                    return;
                State = newState.Value;
            }
        }

        public ConnectionState State
        {
            get => _state;
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


        public IDbConnection Open(bool freshState = false)
        {
            lock (_lockState)
            {
                if (freshState)
                    UpdateState();
                if (_state != ConnectionState.Active)
                    throw new StateException(_state, ConnectionState.Active);
                if (DatabaseConnection.State == System.Data.ConnectionState.Closed)
                    DatabaseConnection.Open();
                State = ConnectionState.Busy;
                _openedFlag = true;
                return DatabaseConnection;
            }
        }

        public bool Release(bool freshState = false)
        {
            lock (_lockState)
            {
                if (freshState)
                    UpdateState();

                if (_state == ConnectionState.Closed ||
                    _state == ConnectionState.Broken ||
                    _state == ConnectionState.Executing)
                    return false;
                State = ConnectionState.Free;
                return true;
            }
        }

        public bool Close(bool freshState = false)
        {
            lock (_lockState)
            {
                if (freshState)
                    UpdateState();

                if (_state == ConnectionState.Executing)
                    return false;

                if (!DatabaseConnection.CloseAndDispose())
                    return false;
                State = ConnectionState.Closed;
                return true;
            }
        }

        private ConnectionState? GetNewState()
        {
            lock (_lockState)
            {
                if (_state == ConnectionState.Closed)
                    return _state;

                if (_openedFlag && !DatabaseConnection.HasConnect())
                {
                    return ConnectionState.Broken;
                }

                if (DatabaseConnection.IsExecuting())
                {
                    return ConnectionState.Executing;
                }


                if (IsBusyTimeout() && (_state == ConnectionState.Busy || _state == ConnectionState.Active))
                {
                    return ConnectionState.MissRecycle;
                }

                if (IsExpired())
                {
                    return ConnectionState.Expired;
                }

                return null;
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