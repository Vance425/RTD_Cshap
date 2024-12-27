/*Tools.cs
 * Model: Commons.Method.Tools
 * *Realtime Assistive Equipment Production Vehicle Dispatching System
  *** Commons Tools
  *** 
  **/
using RTDDAC;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Commons.Method.Tools
{
    public class Tools
    {
        public class HttpWebService
        {

            
        }
        /// <summary> 
        /// 獲取時間戳記
        /// </summary> 
        /// <returns></returns> 
        //public static string GetTimeStampByDay()
        //{
        //    DateTime.Now
        //    TimeSpan ts = DateTime.Now - new DateTime(now.year, now);
        //    return Convert.ToInt64(ts.TotalSeconds).ToString();
        //}
        /// <summary> 
        /// 獲取時間戳記
        /// </summary> 
        /// <returns></returns> 
        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }
        /// <summary> 
        /// 獲取Unit ID 唯一識別碼 (U02022081500000001) 18碼
        /// 千萬/每日
        /// </summary> 
        /// <returns></returns> 
        public static string GetUnitID(DBTool _dbTool)
        {
            //20221118 改為GID使用, 開頭由U >> G
            string commandId = "G{0}{1}";
            string strDateTime = DateTime.Now.ToString("yyyyMMddHHmm");
            long unikey = OracleSequence.Nextvalue(_dbTool, "UID_STREAMCODE");
            string Seq = unikey.ToString().PadLeft(7, '0');
            commandId = string.Format(commandId, strDateTime, Seq);
            return commandId.Trim();
        }
        /// <summary> 
        /// 獲取指令識別碼 (C0202208151800001) 17碼
        /// 10萬/每小時 >> 240萬/每日
        /// </summary> 
        /// <returns></returns> 
        public static string GetCommandID(DBTool _dbTool)
        {
            string commandId = "{0}{1}";
            string strDateTime = DateTime.Now.ToString("yyyyMMddHHmm");
            long unikey = OracleSequence.Nextvalue(_dbTool, "command_streamCode");
            string Seq = unikey.ToString().PadLeft(5,'0');
            commandId = string.Format(commandId, strDateTime, Seq);
            return commandId.Trim(); 
        }

        public double TimerTool(string unit, string lastDateTime)
        {
            double dbTime = 0;
            DateTime curDT = DateTime.Now;
            try
            {
                DateTime tmpDT = Convert.ToDateTime(lastDateTime);
                TimeSpan timeSpan = new TimeSpan(curDT.Ticks - tmpDT.Ticks);

                switch (unit.ToLower())
                {
                    case "day":
                        dbTime = timeSpan.TotalDays;
                        break;
                    case "hours":
                        dbTime = timeSpan.TotalHours;
                        break;
                    case "minutes":
                        dbTime = timeSpan.TotalMinutes;
                        break;
                    case "seconds":
                        dbTime = timeSpan.TotalSeconds;
                        break;
                    case "milliseconds":
                        dbTime = timeSpan.TotalMilliseconds;
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            { }
            return dbTime;
        }
        public double TimerTool(string unit, string startDateTime, string lastDateTime)
        {
            double dbTime = 0;
            DateTime StartDT;
            DateTime LastDT;
            try
            {
                StartDT = Convert.ToDateTime(startDateTime);
                LastDT = Convert.ToDateTime(lastDateTime);
                TimeSpan timeSpan = new TimeSpan(LastDT.Ticks - StartDT.Ticks);

                switch (unit.ToLower())
                {
                    case "day":
                        dbTime = timeSpan.TotalDays;
                        break;
                    case "hours":
                        dbTime = timeSpan.TotalHours;
                        break;
                    case "minutes":
                        dbTime = timeSpan.TotalMinutes;
                        break;
                    case "seconds":
                        dbTime = timeSpan.TotalSeconds;
                        break;
                    case "milliseconds":
                        dbTime = timeSpan.TotalMilliseconds;
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            { }
            return dbTime;
        }

        /// <summary> 
        /// 獲取時間戳記
        /// </summary> 
        /// <returns></returns> 
        public static string GetRTDSecurityKey()
        {
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            string _key = string.Format("{0}{1}", Convert.ToInt64(ts.Ticks).ToString(), ts.GetHashCode().ToString());
            return _key;
        }
    }
    public class RTDSecurity
    {
        /// <summary> 
        /// Security Key
        /// </summary> 
        /// <returns></returns> 
        private static string Securitykey = "default";
        public static string SecurityKey
        {
            get { return Securitykey; }
            set { Securitykey = value; }
        }

        /// <summary> 
        /// 獲取RTD Security Key
        /// </summary> 
        /// <returns></returns> 
        public static string GenerateSecurityKey()
        {
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            string _key = string.Format("{0}{1}", Convert.ToInt64(ts.Ticks).ToString(), ts.GetHashCode().ToString());
            return _key;
        }
        /// <summary> 
        /// Register Security Key
        /// </summary> 
        /// <returns></returns> 
        public static void RegisterSecurityKey(string Key)
        {
            SecurityKey = Key;
        }
        public static void RegisterSecurityKey(DBTool _dbTool, string Key)
        {
            string _sql = "";
            string tmpMsg = "";
            DataTable dtTemp;
            SecurityKey = Key;
            _sql = GetRTDSecKey();
            dtTemp = _dbTool.GetDataTable(_sql);

            if(dtTemp.Rows.Count <= 0)
            {
                ///Insert
                _sql = InsertRTDSecKey(Key, "RTD");
                _dbTool.SQLExec(_sql, out tmpMsg, true);
            }
            else
            {
                ///Update
                _sql = UpdateRTDSecKey(Key, "RTD");
                _dbTool.SQLExec(_sql, out tmpMsg, true);
            }
        }
        public static string GetRTDSecKey()
        {
            string _retuls = "select * from rtd_default_set where parameter = 'RTDSecKey'";
            return _retuls;
        }
        public static string InsertRTDSecKey(string _key, string _updateBy)
        {
            string _result = "";
            string _defaultStr = "insert into rtd_default_set (parameter, paramtype, paramvalue, modifyby, lastmodify_dt, description ) values ('RTDSecKey', 'Key', '{0}', '{1}', sysdate, '')";
            _result = string.Format(_defaultStr, _key, _updateBy);
            return _result;
        }
        public static string UpdateRTDSecKey(string _key, string _updateBy)
        {
            string _result = "";
            string _defaultStr = "update rtd_default_set set paramvalue='{0}', modifyby='{1}', lastmodify_dt=sysdate where parameter='RTDSecKey' and paramtype='Key'";
            _result = string.Format(_defaultStr, _key, _updateBy);
            return _result;
        }
        /// <summary> 
        /// Register Security Key
        /// </summary> 
        /// <returns></returns> 
        public static string ShowSecurityKey()
        {
            return SecurityKey;
        }
        public static string ShowSecurityKey(DBTool _dbTool)
        {
            string _SecurityKey = "";
            string _sql = "";
            string tmpMsg = "";
            DataTable dtTemp;
            _sql = GetRTDSecKey();
            dtTemp = _dbTool.GetDataTable(_sql);
            if(dtTemp.Rows.Count > 0)
            {
                _SecurityKey = dtTemp.Rows[0]["paramvalue"].ToString();
            }

            return _SecurityKey;
        }
    }
    public static class XmlUtil
    {
        public static string ToXML(string str)
        {
            StringReader Reader = new StringReader(str);
            XmlDocument xml = new XmlDocument();
            xml.Load(Reader);
            return xml.InnerText.ToString();

        }
        /// <summary>
        /// 将一个对象序列化为XML字符串
        /// </summary>
        /// <param name="o">要序列化的对象</param>
        /// <param name="encoding">编码方式</param>
        /// <returns>序列化产生的XML字符串</returns>
        public static string XmlSerialize(object o, Encoding encoding)
        {
            if (o == null)
            {
                throw new ArgumentNullException("o");
            }

            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            using (MemoryStream stream = new MemoryStream())
            {
                XmlSerializer serializer = new XmlSerializer(o.GetType());

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.NewLineChars = "\r\n";
                settings.Encoding = encoding;
                settings.IndentChars = "  ";
                settings.OmitXmlDeclaration = true;

                using (XmlWriter writer = XmlWriter.Create(stream, settings))
                {
                    //Create our own namespaces for the output
                    XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                    //Add an empty namespace and empty value
                    ns.Add("", "");
                    serializer.Serialize(writer, o, ns);
                    writer.Close();
                }

                stream.Position = 0;
                using (StreamReader reader = new StreamReader(stream, encoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// 从XML字符串中反序列化对象
        /// </summary>
        /// <typeparam name="T">结果对象类型</typeparam>
        /// <param name="s">包含对象的XML字符串</param>
        /// <param name="encoding">编码方式</param>
        /// <returns>反序列化得到的对象</returns>
        public static T XmlDeserialize<T>(string s, Encoding encoding)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException("s");
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            XmlSerializer mySerializer = new XmlSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(encoding.GetBytes(s)))
            {
                using (StreamReader sr = new StreamReader(ms, encoding))
                {
                    return (T)mySerializer.Deserialize(sr);
                }
            }
        }

        /// <summary>
        /// 将一个对象按XML序列化的方式写入到一个文件
        /// </summary>
        /// <param name="o">要序列化的对象</param>
        /// <param name="path">保存文件路径</param>
        /// <param name="encoding">编码方式</param>
        public static void XmlSerializeToFile(object o, string path, Encoding encoding)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (o == null)
            {
                throw new ArgumentNullException("o");
            }

            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            using (FileStream file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                XmlSerializer serializer = new XmlSerializer(o.GetType());

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.NewLineChars = "\r\n";
                settings.Encoding = encoding;
                settings.IndentChars = "    ";

                using (XmlWriter writer = XmlWriter.Create(file, settings))
                {
                    serializer.Serialize(writer, o);
                    writer.Close();
                }
            }
        }

        /// <summary>
        /// 读入一个文件，并按XML的方式反序列化对象。
        /// </summary>
        /// <typeparam name="T">结果对象类型</typeparam>
        /// <param name="path">文件路径</param>
        /// <param name="encoding">编码方式</param>
        /// <returns>反序列化得到的对象</returns>
        public static T XmlDeserializeFromFile<T>(string path, Encoding encoding)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            string xml = File.ReadAllText(path, encoding);
            return XmlDeserialize<T>(xml, encoding);
        }
    }
    public static class OracleSequence
    {
        public static bool CreateSequence(DBTool _dbTool, string _SequenceName, int _minvalue, int _maxvalue)
        {
            string tmpMsg = ""; bool _bResult = false;
            string tmpSQL = "";
            DataTable dt = null;
            DataRow[] dr = null;

            try
            {
                tmpSQL = string.Format("SELECT count(*) FROM all_sequences WHERE sequence_name = '{0}'", _SequenceName.ToUpper());
                dt = _dbTool.GetDataTable(tmpSQL);

                if(int.Parse(dt.Rows[0][0].ToString()) <= 0)
                { 
                    tmpSQL = String.Format("create sequence {0} minvalue {1} maxvalue {2} start with 1 increment by 1 cache 10", _SequenceName.ToUpper(), _minvalue, _maxvalue);
                    _dbTool.SQLExec(tmpSQL, out tmpMsg, true);
                }

                _bResult = true;
            }
            catch (Exception ex)
            { _bResult = false; }
            return _bResult;
        }
        public static bool SequenceReset(DBTool _dbTool, string _SequenceName)
        {
            string tmpMsg = ""; bool _bResult = false;
            long currval = 0; long increment = 0;
            DataTable dt = null;
            DataRow[] dr = null;
            string tmpSQL = "";

            try
            {
                /*
                 * select carrier_streamCode.currval from dual;
                   alter sequence carrier_streamCode increment by -9; --(-28-1)
                   alter sequence carrier_streamCode minvalue 1;
                 */
                List<int> seq = _dbTool.GetSeqBySeqName(_SequenceName.ToUpper(), 1);
                currval = seq[0];
                tmpSQL = String.Format("alter sequence {0} increment by {1}", _SequenceName.ToUpper(), - 1 * (currval - 2));
                //_dbTool.SQLExec(tmpSQL, out tmpMsg, true);
                tmpSQL = string.Format("SELECT {0}.NEXTVAL AS NEXTVAL FROM DUAL", _SequenceName.ToUpper());
                //_dbTool.SQLExec(tmpSQL, out tmpMsg, true);
                tmpSQL = String.Format("alter sequence {0} increment by 1 minvalue 1", _SequenceName.ToUpper());
                //_dbTool.SQLExec(tmpSQL, out tmpMsg, true);
                tmpSQL = String.Format("alter sequence {0} restart start with 1", _SequenceName.ToUpper());
                _dbTool.SQLExec(tmpSQL, out tmpMsg, true);
                _bResult = true;
            }
            catch (Exception ex)
            { _bResult = false; }
            return _bResult;
        }
        public static long Nextvalue(DBTool _dbTool, string _SequenceName)
        {
            long nextval = 0;
            DataTable dt = null;
            DataRow[] dr = null;

            try
            {
                List<int> seq = _dbTool.GetSeqBySeqName(_SequenceName, 1);
                nextval = seq[0];
            }
            catch (Exception ex)
            { }
            return nextval;
        }
        public static bool BackOne(DBTool _dbTool, string _SequenceName)
        {
            string tmpMsg = ""; bool _bResult = false;
            string tmpSQL = "";

            try
            {
                tmpSQL = string.Format("alter sequence {0} increment by -1", _SequenceName.ToUpper());
                _dbTool.SQLExec(tmpSQL, out tmpMsg, true);
                tmpSQL = string.Format("select {0}.nextval from dual", _SequenceName.ToUpper());
                _dbTool.SQLExec(tmpSQL, out tmpMsg, true);
                tmpSQL = string.Format("alter sequence {0} increment by 1", _SequenceName.ToLower());
                _dbTool.SQLExec(tmpSQL, out tmpMsg, true);
                _bResult = true;
            }
            catch (Exception ex)
            { _bResult = false; }
            return _bResult;
        }
        public static bool ExistsSequance(DBTool _dbTool, string _SequenceName)
        {
            DataTable dt = null;

            string tmpMsg = ""; bool _bResult = false;
            string tmpSQL = string.Format("select * from user_sequences where sequence_name like '%{0}%'", _SequenceName.ToUpper());

            try
            {
                dt = _dbTool.GetDataTable(tmpSQL);
                if (dt.Rows.Count > 0)
                    _bResult = true;
                else
                    _bResult = false;
            }
            catch (Exception ex)
            { _bResult = false; }

            return _bResult;
        }
    }
    public class DebugSwitch
    {
        public static string GetDebugKey(string _key)
        {
            string _retuls = "select * from rtd_default_set where parameter = 'DebugSW'";
            if(_key.Equals(""))
                _retuls = "select * from rtd_default_set where parameter = 'DebugSW'";
            else
                _retuls = string.Format("select * from rtd_default_set where parameter = 'DebugSW' and paramtype = '{0}'", _key);

            return _retuls;
        }
        public static string InsertDebugKey(string _key, string _updateBy)
        {
            string _result = "";
            if (!_key.Equals(""))
            {
                string _defaultStr = "insert into rtd_default_set (parameter, paramtype, paramvalue, modifyby, lastmodify_dt, description ) values ('DebugSW', 'databaseAccess', '{0}', '{1}', sysdate, '')";
                _result = string.Format(_defaultStr, _key, _updateBy.Equals("") ? "RTD" : _updateBy);
            }
            return _result;
        }
        public static string UpdateDebugKey(string _key, string _state, string _updateBy)
        {
            string _result = "";

            if (!_key.Equals(""))
            {
                string _defaultStr = "update rtd_default_set set paramvalue='{0}', modifyby='{1}', lastmodify_dt=sysdate where parameter='DebugSW' and paramtype='{2}'";
                _result = string.Format(_defaultStr, _state, _updateBy.Equals("") ? "RTD" : _updateBy, _key);
            }
            return _result;
        }
        /// <summary> 
        /// Register Security Key
        /// </summary> 
        /// <returns></returns> 
        public static string ShowDebugCurrentStateKey(DBTool _dbTool, string _key)
        {
            string _SecurityKey = "";
            string _sql = "";
            string tmpMsg = "";
            DataTable dtTemp;
            _sql = GetDebugKey(_key);
            dtTemp = _dbTool.GetDataTable(_sql);
            if (dtTemp.Rows.Count > 0)
            {
                _SecurityKey = dtTemp.Rows[0]["paramvalue"].ToString();
            }

            return _SecurityKey;
        }

    }
}
