/**
 * DatabaseParameters.cs
  **RTD Data Access (RTDdac)
  *** Database Parameter
  *** 
  **/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RTDDAC
{
    public class DBParameters
    {
        //DBTool使用,根據不同類型定義IDParameters
        public class OracleParameter : IDBParameters
        {
            private string paramChar = ":";
            private string sysDateTimeString = " SELECT TO_CHAR(SYSDATE,'YYYY/MM/DD HH24:MI:SS') AS DBTIME FROM DUAL";
            private string sysDateTime = "TO_DATE('{0}','YYYY/MM/DD HH24:MI:SS')";
            private string plusChar = "||";
            private string fromdual = " FROM DUAL";
            private string dbNullReplace = "NVL";
            private string subChar = "SUBSTR";

            public string ParamChar
            {
                get { return paramChar; }
            }
            public string SysDateTimeString
            {
                get { return sysDateTimeString; }
            }
            public string PlusChar
            {
                get { return plusChar; }
            }
            public string Fromdual
            {
                get { return fromdual; }
            }
            public string DBNullReplace
            {
                get { return dbNullReplace; }
            }
            public string SubChar
            {
                get { return subChar; }
            }


            public string SysDateTime
            {
                get { return sysDateTime; }
            }

        }
        public class SqlServerParameter : IDBParameters
        {
            private string paramChar = "@";
            private string sysDateTimeString = "SELECT REPLACE(CONVERT(varchar, GETDATE(), 120), '-', '/') AS DBTIME";
            private string plusChar = "+";
            private string fromdual = "";
            private string dbNullReplace = "ISNULL";
            private string subChar = "SUBSTRING";

            public string ParamChar
            {
                get { return paramChar; }
            }
            public string SysDateTimeString
            {
                get { return sysDateTimeString; }
            }
            public string PlusChar
            {
                get { return plusChar; }
            }
            public string Fromdual
            {
                get { return fromdual; }
            }
            public string DBNullReplace
            {
                get { return dbNullReplace; }
            }
            public string SubChar
            {
                get { return subChar; }
            }


            public string SysDateTime
            {
                get { throw new NotImplementedException(); }
            }

        }
        public class DB2Parameter : IDBParameters
        {
            private string paramChar = "@";
            private string sysDateTimeString = @"SELECT RTRIM(CHAR(RTRIM(CHAR(YEAR(current timestamp))) ||'/' ||substr( digits (month(current timestamp)),9) || '/' ||substr( digits (day(current timestamp)),9) || ' ' ||substr( digits (hour(current timestamp)),9)|| ':' || substr( digits (minute(current timestamp)),9)|| ':' ||substr( digits (second(current timestamp)),9)))  AS DBTIME FROM SYSIBM.SYSDUMMY1 ";

            private string plusChar = "||";
            private string fromdual = " SYSIBM.SYSDUMMY1";
            private string dbNullReplace = "COALESCE";
            private string subChar = "SUBSTR";

            public string ParamChar
            {
                get { return paramChar; }
            }
            public string SysDateTimeString
            {
                get { return sysDateTimeString; }
            }
            public string PlusChar
            {
                get { return plusChar; }
            }
            public string Fromdual
            {
                get { return fromdual; }
            }
            public string DBNullReplace
            {
                get { return dbNullReplace; }
            }
            public string SubChar
            {
                get { return subChar; }
            }


            public string SysDateTime
            {
                get { throw new NotImplementedException(); }
            }

        }


        //可以直接取
        public static class Oracle
        {
            public static string paramChar = ":";
            public static string sysDateTimeString = " SELECT TO_CHAR(SYSDATE,'YYYY/MM/DD HH24:MI:SS') AS DBTIME FROM DUAL";
            public static string plusChar = "||";
            public static string fromdual = " FROM DUAL";
            public static string dbNullReplace = "NVL";
            public static string subChar = "SUBSTR";
        }
        public static class SqlServer
        {
            public static string paramChar = "@";
            public static string sysDateTimeString = "SELECT REPLACE(CONVERT(varchar, GETDATE(), 120), '-', '/') AS DBTIME";
            public static string plusChar = "+";
            public static string fromdual = "";
            public static string dbNullReplace = "ISNULL";
            public static string subChar = "SUBSTRING";
        }
        public static class DB2
        {
            public static string paramChar = "@";
            public static string sysDateTimeString = @"SELECT RTRIM(CHAR(RTRIM(CHAR(YEAR(current timestamp))) ||'/' ||substr( digits (month(current timestamp)),9) || '/' ||substr( digits (day(current timestamp)),9) || ' ' ||substr( digits (hour(current timestamp)),9)|| ':' ||substr( digits (minute(current timestamp)),9)|| ':' ||substr( digits (second(current timestamp)),9))) AS DBTIME FROM SYSIBM.SYSDUMMY1 ";

            public static string plusChar = "||";
            public static string fromdual = " SYSIBM.SYSDUMMY1";
            public static string dbNullReplace = "COALESCE";
            public static string subChar = "SUBSTR";
        }
    }
}
