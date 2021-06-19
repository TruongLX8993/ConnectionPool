using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using ConnectionPool.Utils;
using Newtonsoft.Json;

namespace ConnectionPool
{
    public class PoolManager
    {
        private const int DefaultLifeTime = 120;
        private const int DefaultBusynessSeconds = 60;
        private const int DefaultPoolSizeThreshold = 300;
        private const int DefaultCleanInterval = 30;

        private readonly IDictionary<string, Pool> _dicPools;
        private readonly int _lifeTimeSeconds;
        private readonly int _busynessSeconds;
        private readonly int _poolSizeThreshold;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ConnectionCleaner _cleaner;


        public PoolManager(
            IDbConnectionFactory dbConnectionFactory,
            int lifeTimeSeconds,
            int busynessSeconds,
            int poolSizeThreshold)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _dicPools = new Dictionary<string, Pool>();
            _lifeTimeSeconds = lifeTimeSeconds;
            _busynessSeconds = busynessSeconds;
            _poolSizeThreshold = poolSizeThreshold;
            _cleaner = new ConnectionCleaner(DefaultCleanInterval);
            // _cleaner.Start();
        }

        public PoolManager(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _cleaner = new ConnectionCleaner(DefaultCleanInterval);
            // _cleaner.Start();
            try
            {
                _dicPools = new Dictionary<string, Pool>();
                var cfgPath = AppDomain.CurrentDomain.BaseDirectory + "pool.cfg";
                if (File.Exists(cfgPath))
                {
                    var cfg =
                        JsonConvert.DeserializeObject<IDictionary<string, object>>(File.ReadAllText(cfgPath));
                    _busynessSeconds = (int) cfg["busynessSeconds"];
                    _lifeTimeSeconds = (int) cfg["lifeTimeSeconds"];
                    _poolSizeThreshold = (int) cfg["cleanPoolThreshold"];
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
            }

            _lifeTimeSeconds = DefaultLifeTime;
            _poolSizeThreshold = DefaultPoolSizeThreshold;
            _busynessSeconds = DefaultBusynessSeconds;
        }

        public Pool GetPool(string connectionString)
        {
            var key = GetPoolKey(connectionString);
            if (string.IsNullOrEmpty(key))
                throw new Exception("Can not get pool key");
            Debug.Print($"pool number:{_dicPools.Count}");
            if (_dicPools.ContainsKey(key)) return _dicPools[key];
            var pool = new Pool(_dbConnectionFactory,
                connectionString,
                _lifeTimeSeconds,
                _busynessSeconds,
                _poolSizeThreshold);
            _dicPools.Add(key, pool);
            _cleaner.AddPool(pool);
            return _dicPools[key];
        }

        private static string GetPoolKey(string connectionString)
        {
            return PoolUtils.GetSourceName(connectionString);
        }

        public void Clean()
        {
            foreach (var pool in _dicPools.Select(kp => kp.Value))
            {
                pool.TryClean();
            }
        }

        public void CleanAll()
        {
            foreach (var pool in _dicPools.Select(kp => kp.Value))
                pool.CleanAll();
        }
    }
}