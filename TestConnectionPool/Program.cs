using System;
using System.Data;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace TestConnectionPool
{
    internal class Program
    {
        private const string ConnectionString =
            "Data Source=(DESCRIPTION =(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.1.8)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=ONEMES3KCB)));Password=O1234#ONEMES3KCB;User ID=ONEMES3KCB;Connection Timeout=30;Max Pool Size=4;Min Pool Size=3;Incr Pool Size=5;Decr Pool Size=1;";

        private const int NumberConnection = 100;

        public static void Main(string[] args)
        {
            
            Open();
            Open();
            Open();
            Open();
            Open();
            Open();

        }


        private static IDbConnection Open()
        {
            var result = OracleClientFactory.Instance.CreateConnection();
            if (result == null)
                throw new Exception("Can not create new connection");
            result.ConnectionString = ConnectionString;
            result.Open();
            var db = result.BeginTransaction();
            return result;
        }

        private static void Close(IDbConnection dbConnection)
        {
            dbConnection.Close();
       //     dbConnection.Dispose();
        }
    }
}