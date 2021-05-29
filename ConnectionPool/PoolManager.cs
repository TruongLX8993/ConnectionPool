using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ConnectionPool
{
    public class PoolManager
    {
        private readonly IDictionary<string, Pool> _dicPools;
        private readonly int _maxSizePool;
        private readonly int _maxLifeTimeMin;

        public PoolManager(int maxPoolSize, int lifeTimeMin)
        {
            _dicPools = new Dictionary<string, Pool>();
            _maxSizePool = maxPoolSize;
            _maxLifeTimeMin = lifeTimeMin;
        }


        public Pool GetPool(string connectionString)
        {
            var key = PoolUtils.GetSourceName(connectionString);
            lock (_dicPools)
            {
                if (_dicPools.ContainsKey(key)) return _dicPools[key];
                _dicPools.Add(key, new Pool(connectionString, _maxSizePool, _maxLifeTimeMin));
                return _dicPools[key];
            }
        }
        
        public void Clean()
        {
            lock (_dicPools)
            {
                foreach (var pool in _dicPools.Select(kp => kp.Value))
                {
                    pool.CleanExpiredConnection();
                }
            }
        }

        public void CleanAll()
        {
            lock (_dicPools)
            {
                foreach (var pool in _dicPools.Select(kp => kp.Value))
                {
                    pool.CleanAll();
                }
            }
        }
    }
}