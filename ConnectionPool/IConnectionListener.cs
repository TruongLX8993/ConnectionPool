namespace ConnectionPool
{
    public interface IConnectionListener
    {
        void OnClose(Connection connection);
        void OnRecycle(Connection connection);
    }
}