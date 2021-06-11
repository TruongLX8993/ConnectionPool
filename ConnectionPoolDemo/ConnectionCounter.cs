using System;
using System.Threading;
using Oracle.DataAccess.Client;

namespace ConnectionPoolDemo
{
    public class ConnectionCounter
    {
        private static string ConnectionString =
            $"Data Source=(DESCRIPTION =(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.1.8)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=ONEMES3KCB)));Password=O1234#ONEMES3KCB;User ID=ONEMES3KCB; Connection Timeout=10;Max Pool Size=7;Min Pool Size=3;Incr Pool Size=1;Decr Pool Size=1;Pooling=true";

        public static void Main(string[] args)
        {
            var con = OracleClientFactory.Instance.CreateConnection();
            if (con == null) throw new Exception("Can not create new connection");
            con.ConnectionString = ConnectionString;
            con.Open();
            var cmd = con.CreateCommand();

            var counter = 0;
            while (true)
            {
                cmd.CommandText = @"select count(*)
                                from V$SESSION
                                where MACHINE like '%SERVER11%' and PROGRAM = 'w3wp.exe'";
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var newCounter = reader.GetInt32(0);
                    Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")}: {newCounter}: {newCounter - counter}");
                    counter = newCounter;
                }
                reader.Close(); 
                Thread.Sleep(1000);
            }
        }
    }
}