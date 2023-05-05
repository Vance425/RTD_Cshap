using Microsoft.AspNetCore.Mvc;
using NLog;
using Microsoft.Extensions.Configuration;
using RTDWebAPI.Commons.DataRelated.SQLSentence;
using RTDWebAPI.Commons.Method.Database;
using RTDWebAPI.Commons.Method.WSClient;
using RTDWebAPI.Interface;
using RTDWebAPI.Models;
using RTDWebAPI.Service;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Collections;
using RTDWebAPI.Commons.Method.WebServiceClient;

namespace RTDWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WebServiceTest : BasicController
    {

        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly DBTool _dbTool;
        IFunctionService _functionService;
        public WebServiceTest(ILogger logger, IConfiguration configuration, DBTool dbTool)
        {
            _logger = logger;
            _configuration = configuration;
            _dbTool = dbTool;
        }

        [HttpPost]
        public string Post([FromBody] AvailableQualifiedTesterMachine value)
        {
            APIResult foo;
            string tmpMsg = "";
            string _url;
            string data = "";
            Hashtable ht = new Hashtable();
            _functionService = new FunctionService();
            string funcName = "AvailableQualifiedTesterMachine";

            Console.WriteLine(value.Username);

            try
            {
                JCETWebServicesClient jcetWebServiceClient = new JCETWebServicesClient();
                //jcetWebServiceClient.hostname = "127.0.0.1";
                //jcetWebServiceClient.portno = 54350;
                _url = "http://192.168.0.233:8080/GYROWebSrv.asmx";

                JCETWebServicesClient.ResultMsg resultMsg = new JCETWebServicesClient.ResultMsg();
                //resultMsg = jcetWebServiceClient.GetAvailableQualifiedTesterMachine(value.Mode, value.Username, value.Password, value.LotId);


                ht.Add("pEquipID", value.Username);
                ht.Add("pEquipStatus", value.Password);

                data = JCETWebServicesClient.SoapV1_1WebService(_url, "GYRO_UpdateEqpStatus", ht, "http://tempuri.org/");

                _logger.Info(string.Format("Info:{0}", data));

                
            }
            catch(Exception ex)
            {

            }
            try
            {
                WebServicesClient WebServiceClient = new WebServicesClient();
                //jcetWebServiceClient.hostname = "127.0.0.1";
                //jcetWebServiceClient.portno = 54350;
                WebServicesClient._url = "127.0.0.1";
                WebServicesClient.ResultMsg resultMsg = new WebServicesClient.ResultMsg();
                resultMsg = WebServicesClient.GYRO_UpdateEqpStatus(value.Mode, value.Username, value.Password, value.LotId);

                _logger.Info(string.Format("Info:{0}", tmpMsg));

                if (resultMsg.status)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(resultMsg.retMessage);

                    XmlNodeList xnlA = xmlDoc.GetElementsByTagName("string");

                    if (xnlA.Count > 0)
                    {
                        List<string> lstTester = new List<string>();
                        foreach (XmlNode xnA in xnlA)
                        {
                            if (!xnA.InnerText.Equals(""))
                            {
                                lstTester.Add(xnA.InnerText);
                            }
                        }

                        foo = new APIResult()
                        {
                            Success = true,
                            State = "OK",
                            Message = String.Join(",", lstTester.ToArray())
                        };
                    }
                    else
                    {
                        tmpMsg = "No Available Tester Machine.";

                        foo = new APIResult()
                        {
                            Success = false,
                            State = "NG",
                            Message = tmpMsg
                        };
                    }
                }
                else
                {
                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = resultMsg.retMessage
                    };
                }
            }
            catch (Exception ex)
            {
                tmpMsg = String.Format("Unknow issue. [{0}] Exception: {1}", funcName, ex.Message);
                foo = new APIResult()
                {
                    Success = false,
                    State = "NG",
                    Message = tmpMsg
                };
                _logger.Debug(foo.Message);
            }

            return data;
        }


    }
}
