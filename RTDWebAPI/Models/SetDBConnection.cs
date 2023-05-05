using System;

namespace RTDWebAPI.Models
{
    public class SetDBConnection
    {
        public string DBConnect { get; set; }
        public SetDatabase Database { get; set; }
    }

    /*
  "DBConnect": {
"Oracle": {
  "Name": "CIMDB1",
  "ip": "192.168.0.252",
  "port": 1521,
  "user": "AMHS",
  "pwd": "gsi5613686",
  "connectionString": "DATA SOURCE={0};USER ID={1};PASSWORD={2};PERSIST SECURITY INFO=True;",
  "providerName": "Oracle.ManagedDataAccess.Client"
}
*/
}
