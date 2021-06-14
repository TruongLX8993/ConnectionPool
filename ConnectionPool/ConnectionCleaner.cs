using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConnectionPool
{
    internal class ConnectionCleaner
    {
        private readonly IList<Pool> _pools = new List<Pool>();
        private readonly object _lockObject = new object();
        private Task _cleanTask;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly int _interval;

        /// <summary>
        /// Unit second.
        /// </summary>
        /// <param name="interval"></param>
        public ConnectionCleaner(int interval)
        {
            _interval = interval*1000;
        }

        public void AddPool(Pool pool)
        {
            lock (_lockObject)
            {
                _pools.Add(pool);
            }
        }

        public void Remove(Pool pool)
        {
            lock (_lockObject)
            {
                _pools.Remove(pool);
            }
        }

        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cleanTask = Task.Factory.StartNew(Exe, _cancellationTokenSource.Token);
            Debug.WriteLine($"Connection cleaner:{_cleanTask.Status}");
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private void Exe()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                IList<Pool> temPools = null;
                lock (_lockObject)
                {
                    temPools = _pools.ToList();
                }

                foreach (var pool in temPools)
                {
                    pool.TryClean();
                }

                Thread.Sleep(_interval);
            }
        }
    }
}