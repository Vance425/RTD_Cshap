using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace demoWebAPI.Models
{
    public class DatabaseAccess: baseDatabaseAccess
    {
        public override void Start() {
            var builder = new ConfigurationBuilder()
                              .SetBasePath(Directory.GetCurrentDirectory())
                              .AddJsonFile("appsettings.json");
            var config = builder.Build();

            connString = config["DBconnect:Oracle:connectionString"];
            connString = string.Format(connString, config["DBconnect:Oracle:ip"], config["DBconnect:Oracle:port"], config["DBconnect:Oracle:Name"], config["DBconnect:Oracle:user"], config["DBconnect:Oracle:pwd"]);
            Console.WriteLine(connString);
            oracleConnection = new OracleConnection(connString);

            oracleConnection.Open();
            Console.WriteLine(string.Format("Connection state is {0}", oracleConnection.State));



            // 查詢資料
            string sql = "select * from gyro_lot_carrier_associate";
            OracleCommand cmd = new OracleCommand(sql, oracleConnection);
            OracleDataAdapter DataAdapter = new OracleDataAdapter();
            DataAdapter.SelectCommand = cmd;
            DataSet ds = new DataSet();
            DataAdapter.Fill(ds);
            Console.WriteLine(ds.Tables[0].Rows.Count);
        }

        public override bool DeleteSyntax(string sqlSyntax)
        {
            return true;
        }
        public override bool SQLSentence(string sqlSyntax) {
            return true; 
        }
    }
}
