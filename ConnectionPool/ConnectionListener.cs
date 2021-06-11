namespace ConnectionPool
{
    public class ConnectionListener : IConnectionListener
    {
        private readonly PoolStorage _poolStorage;

        public ConnectionListener(PoolStorage poolStorage)
        {
            _poolStorage = poolStorage;
        }

        public void OnActive(Connection con)
        {
          
        }

        public void OnOpen(Connection con)
        {
        }

        public void OnClose(Connection con)
        {
            _poolStorage.Remove(con);
        }

        public void OnRelease(Connection con)
        {
            _poolStorage.MoveToFreeQueue(con);
        }
    }
}