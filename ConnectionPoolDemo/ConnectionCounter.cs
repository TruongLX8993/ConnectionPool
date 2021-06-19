using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Threading;
using log4net;
using log4net.Config;
using Oracle.DataAccess.Client;
using ConnectionState = ConnectionPool.ConnectionState;

namespace ConnectionPoolDemo
{
    public class ConnectionCounter
    {
        // private const string ServerName = "DESKTOP-GIFILU2";
        // private const string ServerName = "SERVER11";


        public static void Main(string[] args)
        {
            XmlConfigurator.ConfigureAndWatch(new FileInfo("log.config"));
            var log= LogManager.GetLogger(typeof(ConnectionState));
            var serverName = ConfigurationManager.AppSettings["ServerName"];
            var connectionString = ConfigurationManager.AppSettings["connectionString"];
            var secondInterval = float.Parse(ConfigurationManager.AppSettings["secondInterval"]);
            var con = OracleClientFactory.Instance.CreateConnection();
            if (con == null) throw new Exception("Can not create new connection");
            con.ConnectionString = connectionString;
            con.Open();
            var cmd = con.CreateCommand();

            var counter = 0;
            while (true)
            {
                cmd.CommandText =
                    $"select count(*)  from V$SESSION where MACHINE like '%{serverName}%' and PROGRAM = 'w3wp.exe'";
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var newCounter = reader.GetInt32(0);
                    var logContent = $"{DateTime.Now.ToString("hh:mm:ss")}:{DateTime.Now.GetUnixTime()}: {newCounter}: {newCounter - counter}";
                    // Console.WriteLine(logContent);
                    log.Debug(logContent);
                    counter = newCounter;
                }

                reader.Close();
                Thread.Sleep((int)(secondInterval * 1000));
            }
        }
    }
}