namespace ConnectionPool
{
    public interface IConnectionListener
    {
         void OnActive(Connection con);
         void OnOpen(Connection con);
         void OnClose(Connection con);
         void OnRelease(Connection con);
    }
}