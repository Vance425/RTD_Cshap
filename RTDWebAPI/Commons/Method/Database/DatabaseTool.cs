using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Threading;
using NLog;
using NLog.Targets;
using NLog.Config;

namespace RTDWebAPI.Commons.Method.Database
{
    public class DBTool
    {
        #region Parameter
        /// <summary>
        /// SQL Server 服務器地址或名稱
        /// </summary>
        string strServerName = "";

        string strPortNum = "";
        /// <summary>
        /// SQL Server DB名稱
        /// </summary>
        string strDBName = "";

        /// <summary>
        /// Oracle TNS 名稱
        /// </summary>
        string strTNSName = "";

        /// <summary>
        /// DB帳號
        /// </summary>
        string strLoginID = "";

        /// <summary>
        /// DB 密碼
        /// </summary>
        string strLoginPWD = "";

        bool bAutoDisConn = false;

        /// <summary>
        /// 資料庫類型 (Oracle/Sql Serve/DB2)
        /// </summary>
        DataBaseType dbType;
        private IDBParameters dbParameters;
        /// <summary>
        /// 連接字串
        /// </summary>
        string strDBConnString;
        string strDBProvider = "";

        const string OracleConnStringChar = "Data Source={0};User ID={1};password={2}";
        const string OracleProvider = "Oracle.ManagedDataAccess.Client";
        const string SqlConnStringChar = "Data Source={0};Initial Catalog={1};Persist Security Info=True;User ID={2};password={3};";
        const string SqlProvider = "System.Data.SqlClient";
        
        const string DB2ConnStringChar = "DATABASE={2}; UserID={3};pwd={4}; Server={0}:{1};";
        const string DB2Provider = "IBM.Data.DB2";
        
        /// <summary>
        /// TimeOut時間, 預設30秒
        /// </summary>
        int iTimeOutTime = 30;


        public DBPool dbPool = null;

        public ILogger _dblogger { get; set; }

        #endregion

        #region 建構函式


        /// <summary>
        /// SQL Server連接參數
        /// </summary>
        /// <param name="strServerName">SQL Server服務器名稱</param>
        /// <param name="strDBName">資料庫名稱</param>
        /// <param name="strLoginID">帳號</param>
        /// <param name="strLoginPWD">密碼</param>
        public DBTool(string iServerName, string iDBName, string iLoginID, string iLoginPWD)
        {
            strServerName = iServerName;
            strDBName = iDBName;
            strLoginID = iLoginID;
            strLoginPWD = iLoginPWD;
            dbType = DataBaseType.SQLSERVER;
            dbParameters = new DBParameters.SqlServerParameter();

            strDBConnString = string.Format(SqlConnStringChar, iServerName, iDBName, iLoginID, iLoginPWD);
            strDBProvider = SqlProvider;
            if (dbPool == null)
                dbPool = new DBPool(strDBConnString, strDBProvider);
        }

        /// <summary>
        /// Oracle 連接參數
        /// </summary>
        /// <param name="iTNSName">TNS 名稱</param>
        /// <param name="iLoginID">帳號</param>
        /// <param name="iLoginPWD">密碼</param>
        public DBTool(string iTNSName, string iLoginID, string iLoginPWD)
        {
            strTNSName = iTNSName;
            strLoginID = iLoginID;
            strLoginPWD = iLoginPWD;
            dbType = DataBaseType.ORACLE;
            dbParameters = new DBParameters.OracleParameter();
            strDBConnString = string.Format(OracleConnStringChar, iTNSName, iLoginID, iLoginPWD);
            strDBProvider = OracleProvider;
            if (dbPool == null)
                dbPool = new DBPool(strDBConnString, strDBProvider);
        }

        /// <summary>
        /// DB2 連接參數
        /// </summary>
        /// <param name="iTNSName">TNS 名稱</param>
        /// <param name="iLoginID">帳號</param>
        /// <param name="iLoginPWD">密碼</param>
        public DBTool(string iServerName, string iPort, string iDBName, string iLoginID, string iLoginPWD)
        {
            strServerName = iServerName;
            strPortNum = iPort;
            strDBName = iDBName;
            strLoginID = iLoginID;
            strLoginPWD = iLoginPWD;
            dbType = DataBaseType.DB2;
            dbParameters = new DBParameters.DB2Parameter();
            strDBConnString = string.Format(DB2ConnStringChar, iServerName, iPort, iDBName, iLoginID, iLoginPWD);
            strDBProvider = DB2Provider;
            if (dbPool == null)
                dbPool = new DBPool(strDBConnString, strDBProvider);
        }
        /// <summary>
        /// 傳入連接字串,Provider
        /// </summary>
        /// <param name="iConnString"></param>
        /// <param name="iDBProvider"></param>
        public DBTool(string iConnString, string iDBProvider, string autoDisconn, out string msg)
        {
            strDBConnString = iConnString;
            strDBProvider = iDBProvider;
            if (iDBProvider.IndexOf("Oracle") > -1)
            {
                dbType = DataBaseType.ORACLE;
                dbParameters = new DBParameters.OracleParameter();
            }
            else if (iDBProvider.IndexOf("Sql") > -1)
            {
                dbType = DataBaseType.SQLSERVER;
                dbParameters = new DBParameters.SqlServerParameter();
            }
            else if (iDBProvider.IndexOf("DB2") > -1)
            {
                dbType = DataBaseType.DB2;
                dbParameters = new DBParameters.DB2Parameter();
            }
            else
            {
                throw new Exception("No Support DB Type");
            }

            if(!autoDisconn.Equals(""))
            {
                bAutoDisConn = autoDisconn.ToUpper().Equals("TRUE") ? true : false;
            }

            if (dbPool == null)
                dbPool = new DBPool(strDBConnString, strDBProvider);
            dbPool.CheckConnet(out msg);
        }


        #endregion

        #region 方法
        /// <summary>
        /// 設置資料庫連接狀態
        /// </summary>
        /// <param name="bIsConnect">資料庫連接狀(true表示連結中)</param>


        public bool ConnectDB(out string msg)
        {
            if (dbPool == null)
                dbPool = new DBPool(strDBConnString, strDBProvider);

            if (!dbPool.CheckConnet(out msg))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool DisConnectDB(out string msg)
        {
            if (!dbPool.DisConnet(out msg))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ReConnectDB(out string msg)
        {
            bool isConn = false;
            int iRetry = 0;

            while(true)
            {
                try
                {
                    lock(dbPool)
                    {
                        if (!dbPool.DisConnet(out msg))
                            isConn = false;
                        else
                            isConn = false;

                        if (!isConn)
                        {
                            if (ConnectDB(out msg))
                                isConn = true;
                        }
                    }
                }
                catch(Exception ex)
                {
                    isConn = false;
                    msg = ex.Message;
                }

                if(isConn)
                {
                    msg = "";
                    return true;
                }
                else
                {
                    if (iRetry <= 10)
                    {
                        iRetry++;
                        msg = "";
                    }
                    else
                    {
                        return false;
                    }
                }
                Thread.Sleep(3000);
            }
        }

        public string GetSysDateTime()
        {

            DataTable dtSDT = dbPool.GetDataTable(dbParameters.SysDateTimeString, "DBTIME");
            if (dtSDT == null)
            {
                return "";
            }
            else
            {
                return dtSDT.Rows[0]["DBTIME"].ToString();
            }
        }

        /// <summary>
        /// 將字串轉成日期格式
        /// </summary>
        /// <param name="strDate"></param>
        /// <returns></returns>
        public string TO_DATE(string strDate = "")
        {
            if (strDate == "")
                strDate = GetSysDateTime();
            strDate = string.Format(dbParameters.SysDateTime, strDate);
            return strDate;
        }

        /// <summary>
        /// 將字串前後加上單引號。EX : '{0}' 
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public string Quote(string inputString)
        {
            inputString = string.Format("{0}{1}", "", inputString);
            return string.Format(" '{0}' ", inputString.Trim().Replace("'", "''"));
        }

        /// <summary>
        /// 取SEQ
        /// </summary>
        /// <param name="SeqName"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public List<int> GetSeqBySeqName(string SeqName, int num = 1)
        {
            List<int> lsSeq = new List<int>();
            string sqlSelect = "SELECT LEVEL,{0}  AS NEXTVAL FROM DUAL CONNECT BY LEVEL<={1}";
            sqlSelect = string.Format(@sqlSelect, SeqName + ".NEXTVAL", num);
            DataTable dt = new DataTable();
            dt = dbPool.GetDataTable(sqlSelect, bAutoDisConn);
            foreach (DataRow drItem in dt.Rows)
            {
                lsSeq.Add(Convert.ToInt32(drItem["NEXTVAL"].ToString()));
            }
            return lsSeq;
        }

        #region　先不用
        //public DataSet GetDataSet(string sql)
        //{
        //    return dbPool.GetDataSet(sql);

        //}
        //public DataSet GetDataSet(string sql, string name)
        //{
        //    return dbPool.GetDataSet(sql, name);
        //}
        //public DataSet GetDataSetbySqls(List<string> sqlList)
        //{
        //    return dbPool.GetDataSetbySqls(sqlList);
        //}
        #endregion

        public DataTable GetDataTable(string sql)
        {
            DataTable dtTemp = null;
            dbPool.logger = _dblogger;
            if(sql.Trim().Equals(""))
            {
                dtTemp = new DataTable();
                return dtTemp;
            }

            return dbPool.GetDataTable(sql, bAutoDisConn);
        }
        public DataTable GetDataTable(string sql, bool autoDisconn)
        {
            dbPool.logger = _dblogger;
            return dbPool.GetDataTable(sql, autoDisconn);
        }
        public DataTable GetDataTable(string sql, string name)
        {
            dbPool.logger = _dblogger;
            return dbPool.GetDataTable(sql, name);
        }
        /// <summary>
        /// 開始交易(BeginTransaction)，完成後，手動呼叫commit,只要呼叫了一次BeginTransaction,後面的交易都要手動commit,或者指定keepTrans属性
        /// </summary>
        public void BeginTransaction()
        {
            dbPool.DoBeginTransaction();
        }
        public void Commit()
        {
            dbPool.DoCommitTransaction();
        }
        public void RollBack()
        {
            dbPool.DoRollbackTransaction();
        }

        #region　先不用
        ///// <summary>
        ///// 執行sql 語句,直接commit
        ///// </summary>
        ///// <param name="sql"></param>
        //public void SQLExec(string sql)
        //{
        //    dbPool.SQLExec(new string[] { sql }, true);
        //}

        /// <summary>
        /// 執行sql 語句,直接commit并关闭连接
        /// </summary>
        /// <param name="listSql"></param>
        //public void SQLExec(List<string> listSql)
        //{
        //    dbPool.SQLExec(listSql.ToArray(), true);
        //}


        /// <summary>
        /// 執行sql 語句    
        /// </summary>
        /// <param name="sql">sql字串 </param>
        /// <param name="commitClose">是否要作commit並close連線</param>
        //public void SQLExec(string sql, bool commitClose)
        //{
        //    dbPool.SQLExec(new string[] { sql }, commitClose);
        //}

        //public bool SQLExec(string sql, int chkCount)
        //{
        //    return dbPool.SQLExec(sql, chkCount);
        //}
        //public bool SQLExec(string sql, int chkCount, out string errorMsg)
        //{
        //    return dbPool.SQLExec(sql, chkCount, out errorMsg);
        //}

        /// <summary>
        /// 執行sql語法陣列
        /// </summary>
        /// <param name="sql">sql語法</param>
        /// <param name="commitClose">true表示要commit且关闭连接</param>
        //public void SQLExec(string[] sql, bool commitClose)
        //{
        //    dbPool.SQLExec(sql, commitClose);
        //}
        #endregion

        /// <summary>
        /// 執行sql語法陣列
        /// </summary>
        /// <param name="listSql"></param>
        /// <param name="commit">True：Commit 但不關閉連接</param>
        public bool SQLExec(List<string> listSql, out string errMsg, bool commit = true)
        {
            dbPool.logger = _dblogger;
            return dbPool.SQLExec(listSql, out errMsg, commit);
        }

        /// <summary>
        /// 執行sql 語句
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="errMsg"></param>
        /// <param name="commit">TRUE:Commit 但不關閉連接</param>
        /// <returns></returns>
        public bool SQLExec(string sql, out string errMsg, bool commit = true)
        {
            List<string> sqlList = new List<string>();
            bool bExec = false;
            int execTime = 0;
            string tmpMsg;
            while (true)
            {
                try
                {
                    if (sql.Trim().Equals(""))
                    {
                        errMsg = "sql sentence can not emty.";
                        return bExec;
                    }
                    sqlList.Add(sql);

                    if (execTime > 0)
                    {
                        tmpMsg = string.Format("retry do SQLExec logic, retry time: [{0}][{1}]", execTime, sql);
                        _dblogger.Error(tmpMsg);
                    }

                    bExec = SQLExec(sqlList, out errMsg, commit);
                }
                catch (Exception ex)
                {
                    tmpMsg = string.Format("[{0}][{1}][{2}][{3}]", "DBAccess", "SQLExec", ex.Message, sql);
                    if(_dblogger is not null)
                        _dblogger.Error(tmpMsg);

                    tmpMsg = "";
                    if (ex.Message.IndexOf("ORA-03150") > 0 || ex.Message.IndexOf("ORA-02063") > 0 || ex.Message.IndexOf("ORA-12614") > 0
                        || ex.Message.IndexOf("Object reference not set to an instance of an object") > 0
                        || ex.Message.IndexOf("OracleException") > 0)
                    {

                        //ReConnectDB(out tmpMsg);
                        //if (!tmpMsg.Equals(""))
                        //    tmpMsg = string.Format("[, ReConnection:{0}]", tmpMsg);
                        tmpMsg = string.Format("do SQLExec happened some issue. [{0}]", ex.Message);
                        _dblogger.Error(tmpMsg);

                        DisConnectDB(out tmpMsg);
                        if (!tmpMsg.Equals(""))
                            _dblogger.Error(string.Format("auto dispose fail [{0}]", tmpMsg));
                        else
                            _dblogger.Error(string.Format("auto dispose"));

                        tmpMsg = string.Format("Database access problem. SQLExec fail. [{0}]");
                    }
                    else
                    {
                        tmpMsg = string.Format("SQLExec fail.");
                    }

                    if(!tmpMsg.Equals(""))
                        _dblogger.Error(tmpMsg);

                    String dbMsg = "";

                    if (dbMsg.Equals(""))
                        tmpMsg = string.Format(tmpMsg, "DB disconnected");
                    else
                        tmpMsg = string.Format(tmpMsg, dbMsg);

                    errMsg = String.Format("{0} {1}] sql:{2}", tmpMsg, ex.Message, sql);
                }

                if (errMsg.Equals(""))
                    break;

                if (execTime > 3)
                    break;
                else
                    Thread.Sleep(300);

                errMsg = "";
                execTime++;
            }

            return bExec;
        }
        public bool SQL2Exec(string sql, out string errMsg, bool commit = true)
        {
            List<string> sqlList = new List<string>();
            sqlList.Add(sql);
            return SQLExec(sqlList, out errMsg, commit);

        }

        public DbParameter CreateDbParameter(string name, DbType dbType, ParameterDirection direction, object value = null, int size = 100)
        {
            return dbPool.CreateDbParameter(name, dbType, direction, value, size);
        }

        public bool ExecutePorcedure(string procName, List<DbParameter> coll, out string msg, bool commit = true)
        {
            return dbPool.ExecutePorcedure(procName, coll, out msg, commit);
        }

        public bool ExecutePorcedure(string procName, List<DbParameter> coll, ref DataSet ds, out string msg, bool commit = true)
        {
            return dbPool.ExecutePorcedure(procName, coll, ref ds, out msg, commit);
        }

        #region　先不用
        /// <summary>
        /// 由DataSet資料更新DB，一次commit
        /// </summary>
        /// <param name="ds">要更新的DataSet,內部DataTable的名稱要與實際Table名稱相同</param>
        /// <returns></returns>
        //public void UpdateDataSet(DataSet ds)
        //{
        //    dbPool.UpdateDataSet(ds, true);
        //}
        /// <summary>
        /// 由DataSet資料更新DB，自行决定commit
        /// </summary>
        /// <param name="ds">要更新的DataSet,內部DataTable的名稱要與實際Table名稱相同</param>
        /// <returns></returns>
        //public void UpdateDataSet(DataSet ds, bool commitClose)
        //{
        //    dbPool.UpdateDataSet(ds, commitClose);
        //}
        #endregion

        #endregion

        #region 属性


        /// <summary>
        /// 資料庫狀態
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (dbPool != null)
                    return dbPool.IsConnected;
                else return false;
            }
        }

        public int TimeOut
        {
            get { return iTimeOutTime; }
            set { iTimeOutTime = value; }
        }
        /// <summary>
        /// 資料庫類型
        /// </summary>
        /// 
        public DataBaseType DBType
        {
            get { return dbType; }
        }

        /// <summary>
        /// 連接資料庫字串
        /// </summary>
        public string DBConnString
        {
            get { return strDBConnString; }
        }
        public string DBProvider
        {
            get { return strDBProvider; }
        }


        public IDBParameters DBParameters
        {
            get { return dbParameters; }
        }
        /// <summary>
        /// SQL Server 服務器地址或名稱
        /// </summary>
        public string ServerName
        {
            get { return strServerName; }
        }

        /// <summary>
        /// DB2 服務器端口
        /// </summary>
        public string PortNum
        {
            get { return strPortNum; }
        }
        /// <summary>
        /// SQL Server DB名稱
        /// </summary>
        public string DBName
        {
            get { return strDBName; }
        }
        /// <summary>
        /// Oracle TNS名稱
        /// </summary>
        public string TNSName
        {
            get { return strTNSName; }
        }

        /// <summary>
        /// DB帳號
        /// </summary>
        public string LoginID
        {
            get { return strLoginID; }
        }

        /// <summary>
        /// DB密碼
        /// </summary>
        public string LoginPWD
        {
            get { return strLoginPWD; }
        }
        #endregion

    }
}
