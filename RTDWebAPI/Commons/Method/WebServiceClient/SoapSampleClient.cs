using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RTDWebAPI.Commons.Method.Tools;
using RTDWebAPI.Models;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Web;
using System.Web.Services.Description;
using System.Xml;
using System.Xml.Serialization;

namespace RTDWebAPI.Commons.Method.WebServiceClient
{

    /// <summary>
    /// SOAP輔助類
    /// </summary>
    public static class SoapSampleHelper
    {
        /// <summary>
        /// 消息格式
        /// </summary>
        private const String FORMAT_ENVELOPE = @"<?xml version='1.0' encoding='utf-8'?>
                <soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>
                  <soap:Body>
                    <{0} xmlns='{1}'>{2}</{0}>
                  </soap:Body>
                </soap:Envelope>";

        /// <summary>
        /// 參數格式
        /// </summary>
        private const String FORMAT_PARAMETER = "<{0}>{1}</{0}>";

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="soapAction">SOAP動作</param>
        /// <param name="soapParameters">參數集合</param>
        /// <returns>返回值</returns>
        public static String MakeEnvelope(String soapAction, params SoapSampleParameter[] soapSampleParameters)
        {
            String nameSpace, methodName;

            GetNameSpaceAndMethodName(soapAction, out nameSpace, out methodName);

            return String.Format(FORMAT_ENVELOPE, methodName, nameSpace, BuildSoapParameters(soapSampleParameters));
        }
        public partial class _TPQuery_CheckPROMISLoginRequestBody
        {

            [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue = false, Order = 0)]
            public string Username;

            [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue = false, Order = 1)]
            public string Password;

            public _TPQuery_CheckPROMISLoginRequestBody()
            {
            }

            public _TPQuery_CheckPROMISLoginRequestBody(string Username, string Password)
            {
                this.Username = Username;
                this.Password = Password;
            }
        }
        /// <summary>
        /// 創建SOAP參數內容
        /// </summary>
        /// <param name="soapSampleParameters">參數集合</param>
        /// <returns>SOAP參數內容</returns>
        public static String BuildSoapParameters(IEnumerable<SoapSampleParameter> soapSampleParameters)
        {
            var buffer = new StringBuilder();

            foreach (var soapParameter in soapSampleParameters)
            {
                var strContent = GetObjectContent(soapParameter.Value);
                //parseContent(strContent)
                buffer.AppendFormat(FORMAT_PARAMETER, soapParameter.Name, strContent);

            }

            return buffer.ToString();
        }
        public static string parseContent(string strContent)
        {
            var buffer = new StringBuilder();

            JObject jsonParams = JObject.Parse(strContent);
            foreach(var param in jsonParams)
            {
                buffer.AppendFormat(FORMAT_PARAMETER, param.Key, param.Value);
            }

            return buffer.ToString();
        }

        /// <summary>
        /// 獲取空間名
        /// </summary>
        /// <param name="soapAction">SOAP动作</param>
        /// <returns>名称空间</returns>
        public static String GetNameSpace(String soapAction)
        {
            String nameSpace, methodName;

            GetNameSpaceAndMethodName(soapAction, out nameSpace, out methodName);

            return nameSpace;
        }

        /// <summary>
        /// 獲取函式名稱
        /// </summary>
        /// <param name="soapAction">SOAP動作</param>
        /// <returns>函式名稱</returns>
        public static String GetMethodName(String soapAction)
        {
            String nameSpace, methodName;

            GetNameSpaceAndMethodName(soapAction, out nameSpace, out methodName);

            return methodName;
        }

        /// <summary>
        /// 獲取名稱空間和函式名稱
        /// </summary>
        /// <param name="soapAction">SOAP動作</param>
        /// <param name="nameSpace">名稱空間</param>
        /// <param name="methodName">函式名稱</param>
        public static void GetNameSpaceAndMethodName(String soapAction, out String nameSpace, out String methodName)
        {
            nameSpace = (methodName = String.Empty);

            var index = soapAction.LastIndexOf(Path.AltDirectorySeparatorChar);
            nameSpace = soapAction.Substring(0, index + 1);
            methodName = soapAction.Substring(index + 1, soapAction.Length - index - 1);
        }

        /// <summary>
        /// 獲取對象XML
        /// </summary>
        /// <param name="graph">圖</param>
        /// <returns>對象內容XML</returns>
        public static String GetObjectContent(Object graph)
        {
            using (var memoryStream = new MemoryStream())
            {
                var graphType = graph.GetType();
                var xmlSerializer = new XmlSerializer(graphType);

                // XML序列化
                xmlSerializer.Serialize(memoryStream, graph);

                // 獲取對象XML
                var strContent = Encoding.UTF8.GetString(memoryStream.ToArray());
                var xmlDocument = new XmlDocument();

                xmlDocument.LoadXml(strContent);

                // 返回對象內容XML
                var contentNode = xmlDocument.SelectSingleNode(graphType.Name);

                if (contentNode != null)
                    return contentNode.InnerXml;

                return graph.ToString();
            }
        }
    }

    /// <summary>
    /// SOAP參數
    /// </summary>
    public sealed class SoapSampleParameter
    {
        /// <summary>
        /// 構造函數
        /// </summary>
        /// <param name="name">名稱</param>
        /// <param name="value">值</param>
        public SoapSampleParameter(String name, Object value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// 名稱
        /// </summary>
        public String Name { get; private set; }

        /// <summary>
        /// 值
        /// </summary>
        public Object Value { get; private set; }
    }

    /// <summary>
    /// SOAP客戶端
    /// </summary>
    public sealed class SoapSampleClient
    {
        /// <summary>
        /// 構造函數
        /// </summary>
        /// <param name="uriString">請求地址</param>
        /// <param name="soapAction">SOAP動作</param>
        public SoapSampleClient(String uriString, String soapAction)
            : this(new Uri(uriString), soapAction) { }

        /// <summary>
        /// 構造函數
        /// </summary>
        /// <param name="uri">請求地址</param>
        /// <param name="soapAction">SOAP動作</param>
        public SoapSampleClient(Uri uri, String soapAction)
        {
            this.Uri = uri;
            this.SoapAction = soapAction;
            this.Arguments = new List<SoapSampleParameter>();
            this.Credentials = CredentialCache.DefaultNetworkCredentials;
        }

        /// <summary>
        /// 參數集合
        /// </summary>
        public IList<SoapSampleParameter> Arguments { get; private set; }

        /// <summary>
        /// 身份憑証
        /// </summary>
        public ICredentials Credentials { get; set; }

        /// <summary>
        /// 請求地址
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// SOAP動作
        /// </summary>
        public String SoapAction { get; set; }

        /// <summary>
        /// 獲取嚮應
        /// </summary>
        /// <returns>嚮應</returns>
        public WebResponse GetResponse()
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(this.Uri);
            webRequest.Headers.Add("SOAPAction", String.Format("\"{0}\"", this.SoapAction));
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            webRequest.Credentials = this.Credentials;

            // 寫入請求SOAP信息
            using (var requestStream = webRequest.GetRequestStream())
            {
                using (var textWriter = new StreamWriter(requestStream))
                {
                    var envelope = SoapSampleHelper.MakeEnvelope(this.SoapAction, this.Arguments.ToArray());

                    if (!String.IsNullOrEmpty(envelope))
                        textWriter.Write(envelope);
                }
            }

            // 獲取SOAP請求返回
            return webRequest.GetResponse();
        }

        /// <summary>
        /// 獲取返回結果
        /// </summary>
        /// <returns>返回值</returns>
        public Object GetResult()
        {
            // 獲取嚮應
            var webResponse = this.GetResponse();
            var xmlReader = XmlTextReader.Create(webResponse.GetResponseStream());
            var xmlDocument = new XmlDocument();

            // 加載嚮應XML
            xmlDocument.Load(xmlReader);

            var nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);
            nsmgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");

            var bodyNode = xmlDocument.SelectSingleNode("soap:Envelope/soap:Body", nsmgr);

            if (bodyNode.FirstChild.HasChildNodes)
                return bodyNode.FirstChild.FirstChild.InnerXml;

            return null;
        }
    }

    public sealed class WebServicesClient
    {
        public static String _url { get; set; }
        public static String hostname { get; set; }
        public static int portno { get; set; }

        public ResultMsg GYRO_UpdateEqpStatus(string username, string pwd)
        {
            ResultMsg retMsg = new ResultMsg();
            string MethodCode = "GYRO_UpdateEqpStatus";
            String soapAction = string.Format("http://tempuri.org/{0}", MethodCode);
            if (_url.Equals(""))
            {
                string tmpUrl = "http://{0}:{1}/WebService.asmx";
                _url = string.Format(tmpUrl, hostname, portno);
            }

            try
            {
                LogonModel loginBody = new LogonModel();
                loginBody.Username = username;
                loginBody.Password = pwd;
                string soapValue = JsonConvert.SerializeObject(loginBody);

                Hashtable ht = new Hashtable();
                ht.Add("pEquipID", username);
                ht.Add("pEquipStatus", pwd);
                var data = WebServicesClient.SoapV1_2WebService(_url, MethodCode, ht, "http://tempuri.org/");

                retMsg.status = true;
                retMsg.retMessage = HttpUtility.HtmlDecode(data.ToString());
            }
            catch(Exception ex)
            {
                retMsg.status = false;
                retMsg.retMessage = ex.Message;

            }

            return retMsg;
        }

        public ResultMsg GetGyroLotInfo(string useMethod, string username, string pwd, string lotid)
        {
            ResultMsg retMsg = new ResultMsg();
            string MethodCode = "GetGyroLotInfo";
            String soapAction = string.Format("http://tempuri.org/{0}", MethodCode);
            if (_url.Equals(""))
            {
                string tmpUrl = "http://{0}:{1}/WebService.asmx";
                _url = string.Format(tmpUrl, hostname, portno);
            }

            try
            {
                ILogger logger = LogManager.GetCurrentClassLogger();
                //XmlUtil xmlUtil = new XmlUtil();
                //AvailableTesterMachine availableTesterMachine = new AvailableTesterMachine();
                //availableTesterMachine.Username = username;
                //availableTesterMachine.Password = pwd;
                //availableTesterMachine.LotId = lotid;

                //LogonModel loginBody = new LogonModel();
                //loginBody.Username = username;
                //loginBody.Password = pwd;
                //string soapValue = JsonConvert.SerializeObject(loginBody);

                Hashtable ht = new Hashtable();
                ht.Add("Username", username);
                ht.Add("Password", pwd);
                ht.Add("LotId", lotid);
                ht.Add("Mode", useMethod.Equals("") ? "Soap12" : useMethod);

                logger.Debug(string.Format("Debug: GetAvailableQualifiedTesterMachine Lot[{0}] / Method[{1}]", lotid, useMethod.ToLower()));
                var data = "";
                switch(useMethod.ToLower())
                {
                    case "soap11":
                        data = WebServicesClient.SoapV1_1WebService(_url, MethodCode, ht, "http://tempuri.org/");
                        break;
                    case "get":
                        data = WebServicesClient.WebServiceGet(_url, MethodCode, ht);
                        break;
                    case "post":
                        data = WebServicesClient.PostWebService(_url, MethodCode, ht);
                        break;
                    case "soap12":
                    default:
                        data = WebServicesClient.SoapV1_2WebService(_url, MethodCode, ht, "http://tempuri.org/");
                        break;
                }

                logger.Debug(string.Format("Debug: {0}", data.ToString()));
                retMsg.status = true;
                retMsg.retMessage = HttpUtility.HtmlDecode(data.ToString());
            }
            catch (Exception ex)
            {
                retMsg.status = false;
                retMsg.retMessage = ex.Message;
            }

            return retMsg;
        }

        public class ResultMsg
        {
            public bool status { get; set; }
            public string retMessage { get; set; }
            public string remark { get; set; }
        }

        public static string SoapV1_2WebService(String URL, String MethodName, Hashtable Pars, string XmlNs)
        {
            ILogger logger = LogManager.GetCurrentClassLogger();
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URL);
            request.Method = "POST";
            request.ContentType = "application/soap+xml; charset=utf-8";

            // 凭证
            request.Credentials = CredentialCache.DefaultCredentials;
            //超时时间
            request.Timeout = 120000;
            byte[] data = HashtableToSoap12(Pars, XmlNs, MethodName);
            logger.Debug(string.Format("Debug: {0}", System.Text.Encoding.UTF8.GetString(data)));
            request.ContentLength = data.Length;
            Stream writer = request.GetRequestStream();
            writer.Write(data, 0, data.Length);
            writer.Close();
            var response = request.GetResponse();
            XmlDocument doc = new XmlDocument();
            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            String retXml = sr.ReadToEnd();
            sr.Close();
            doc.LoadXml(retXml);
            XmlNamespaceManager mgr = new XmlNamespaceManager(doc.NameTable);
            mgr.AddNamespace("soap12", "http://www.w3.org/2003/05/soap-envelope");
            String xmlStr = doc.SelectSingleNode("//soap12:Body/*/*", mgr).InnerXml;

            string xmlstr2 = doc.InnerXml;
            return xmlStr;
        }

        private static byte[] HashtableToSoap12(Hashtable ht, String XmlNs, String MethodName)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<soap12:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\"></soap12:Envelope>");
            XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.InsertBefore(decl, doc.DocumentElement);
            XmlElement soapBody = doc.CreateElement("soap12", "Body", "http://www.w3.org/2003/05/soap-envelope");

            XmlElement soapMethod = doc.CreateElement(MethodName);
            soapMethod.SetAttribute("xmlns", XmlNs);
            foreach (string k in ht.Keys)
            {

                XmlElement soapPar = doc.CreateElement(k);
                soapPar.InnerXml = ObjectToSoapXml(ht[k]);
                soapMethod.AppendChild(soapPar);
            }
            soapBody.AppendChild(soapMethod);
            doc.DocumentElement.AppendChild(soapBody);
            return Encoding.UTF8.GetBytes(doc.OuterXml);
        }
        private static string ObjectToSoapXml(object o)
        {
            XmlSerializer mySerializer = new XmlSerializer(o.GetType());
            MemoryStream ms = new MemoryStream();
            mySerializer.Serialize(ms, o);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(Encoding.UTF8.GetString(ms.ToArray()));
            if (doc.DocumentElement != null)
            {
                return doc.DocumentElement.InnerXml;
            }
            else
            {
                return o.ToString();
            }
        }
        public static string SoapV1_1WebService(String URL, String MethodName, Hashtable Pars, string XmlNs)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URL);
            request.Method = "POST";
            request.ContentType = "text/xml; charset=utf-8";
            request.Headers.Add("SOAPAction", "\"" + XmlNs + (XmlNs.EndsWith("/") ? "" : "/") + MethodName + "\"");
            // 憑證
            request.Credentials = CredentialCache.DefaultCredentials;
            //超時時間
            request.Timeout = 120000;
            byte[] data = HashtableToSoap(Pars, XmlNs, MethodName);
            request.ContentLength = data.Length;
            Stream writer = request.GetRequestStream();
            writer.Write(data, 0, data.Length);
            writer.Close();
            var response = request.GetResponse();
            XmlDocument doc = new XmlDocument();
            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            String retXml = sr.ReadToEnd();
            sr.Close();
            doc.LoadXml(retXml);
            XmlNamespaceManager mgr = new XmlNamespaceManager(doc.NameTable);
            mgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            String xmlStr = doc.SelectSingleNode("//soap:Body/*/*", mgr).InnerXml;

            return xmlStr;
        }
        private static byte[] HashtableToSoap(Hashtable ht, String XmlNs, String MethodName)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"></soap:Envelope>");
            XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.InsertBefore(decl, doc.DocumentElement);
            XmlElement soapBody = doc.CreateElement("soap", "Body", "http://schemas.xmlsoap.org/soap/envelope/");

            XmlElement soapMethod = doc.CreateElement(MethodName);
            soapMethod.SetAttribute("xmlns", XmlNs);
            foreach (string k in ht.Keys)
            {

                XmlElement soapPar = doc.CreateElement(k);
                soapPar.InnerXml = ObjectToSoapXml(ht[k]);
                soapMethod.AppendChild(soapPar);
            }
            soapBody.AppendChild(soapMethod);
            doc.DocumentElement.AppendChild(soapBody);
            return Encoding.UTF8.GetBytes(doc.OuterXml);
        }
        /// <summary>
        /// 需要WebService支持Get調用
        /// </summary>
        public static string WebServiceGet(String URL, String MethodName, Hashtable Pars)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URL + "/" + MethodName + "?" + HashtableToPostData(Pars));
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            // 憑證
            request.Credentials = CredentialCache.DefaultCredentials;
            //超時時間
            request.Timeout = 120000;
            var response = request.GetResponse();
            var stream = response.GetResponseStream();
            StreamReader sr = new StreamReader(stream, Encoding.UTF8);
            String retXml = sr.ReadToEnd();
            sr.Close();
            return retXml;
        }
        private static String HashtableToPostData(Hashtable ht)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string k in ht.Keys)
            {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }
                sb.Append(HttpUtility.UrlEncode(k) + "=" + HttpUtility.UrlEncode(ht[k].ToString()));
            }
            return sb.ToString();
        }
        /// <summary>
        /// 需要WebService支持Post調用
        /// </summary>
        public static string PostWebService(String URL, String MethodName, Hashtable ht)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URL + "/" + MethodName);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            // 憑證
            request.Credentials = CredentialCache.DefaultCredentials;
            //超時時間
            request.Timeout = 120000;
            var PostStr = HashtableToPostData(ht);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(PostStr);
            request.ContentLength = data.Length;
            Stream writer = request.GetRequestStream();
            writer.Write(data, 0, data.Length);
            writer.Close();
            var response = request.GetResponse();
            var stream = response.GetResponseStream();
            StreamReader sr = new StreamReader(stream, Encoding.UTF8);
            String retXml = sr.ReadToEnd();
            sr.Close();
            return retXml;
        }
    }
}
