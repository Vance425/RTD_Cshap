/**
 * DatabaseMethodConfig.cs
  **RTD Data Access (RTDdac)
  *** Database Method Config
  *** 
  **/
using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace RTDDAC
{
    public struct DBConnect
    {
        public string ConnectionString;
        public string ProviderName;

    }

    public class DBMethodConfig
    {

        #region
        private static string strDBMethodConfigPath = "DBMethod" + ".config";
        public static Dictionary<string, DBConnect> strDBConnString = new Dictionary<string, DBConnect>();
        public static string GYRO;
        private static int inDBCount = 0;

        #endregion

        static DBMethodConfig()
        {
            LoadDBMethodConfig();
        }

        public static void LoadDBMethodConfig()
        {
            ExeConfigurationFileMap exeFile = new ExeConfigurationFileMap();
            string strTemp = ""; // Application.ExecutablePath;
            int iIndex = strTemp.LastIndexOf('\\');
            string strExeName = strTemp.Substring(iIndex + 1, strTemp.Length - iIndex - 1);

            exeFile.ExeConfigFilename = AppDomain.CurrentDomain.BaseDirectory + strDBMethodConfigPath;


            Configuration cfg = ConfigurationManager.OpenMappedExeConfiguration(exeFile, ConfigurationUserLevel.None);

            #region Server Paramater
            inDBCount = cfg.ConnectionStrings.ConnectionStrings.Count;
            if (inDBCount > 0)
            {
                DBConnect Db = new DBConnect();
                string DBName = "";
                for (int i = 0; i < inDBCount; i++)
                {
                    DBName = cfg.ConnectionStrings.ConnectionStrings[i].Name.Trim();
                    Db.ConnectionString = cfg.ConnectionStrings.ConnectionStrings[i].ConnectionString.Trim();
                    Db.ProviderName = cfg.ConnectionStrings.ConnectionStrings[i].ProviderName.Trim();


                    strDBConnString.Add(DBName, Db);
                }
            }

            GYRO = cfg.AppSettings.Settings["GYRO"].Value;
            #endregion
        }
    }
}
