using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ConnectionPool
{
    public class PoolManager
    {
        private readonly IDictionary<string, Pool> _dicPools;
        private readonly int _maxPoolSize;
        private readonly int _maxLifeTimeMin;
        private readonly int _cleanPoolThreshold;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public PoolManager(
            IDbConnectionFactory dbConnectionFactory,
            int maxPoolSize,
            int lifeTimeMin,
            int cleanPoolThreshold)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _dicPools = new Dictionary<string, Pool>();
            _maxPoolSize = maxPoolSize;
            _maxLifeTimeMin = lifeTimeMin;
            _cleanPoolThreshold = cleanPoolThreshold;
            if (_cleanPoolThreshold > _maxPoolSize)
            {
                throw new ArgumentException("Max-pool-size cannot be less clean-threshold ");
            }
        }

        public Pool GetPool(string connectionString)
        {
            var key = PoolUtils.GetSourceName(connectionString);

            if (_dicPools.ContainsKey(key)) return _dicPools[key];
            var pool = new Pool(_dbConnectionFactory,
                connectionString,
                _maxPoolSize,
                _maxLifeTimeMin,
                _cleanPoolThreshold);
            _dicPools.Add(key, pool);
            return _dicPools[key];
        }

        public void Clean()
        {
            foreach (var pool in _dicPools.Select(kp => kp.Value))
            {
                pool.CleanExpiredConnection();
            }
        }

        public void CleanAll()
        {
            foreach (var pool in _dicPools.Select(kp => kp.Value))
            {
                pool.CleanAll();
            }
        }
    }
}