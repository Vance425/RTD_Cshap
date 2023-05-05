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

namespace RTDWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CheckAvailableQualifiedTesterMachine : BasicController
    {

        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly DBTool _dbTool;
        IFunctionService _functionService;
        public CheckAvailableQualifiedTesterMachine(ILogger logger, IConfiguration configuration, List<DBTool> lstDBSession)
        {
            _logger = logger;
            _configuration = configuration;
            _dbTool = (DBTool)lstDBSession[1];
        }

        [HttpPost]
        public APIResult Post([FromBody] AvailableQualifiedTesterMachine value)
        {
            APIResult foo;
            string tmpMsg = "";
            _functionService = new FunctionService();
            string funcName = "AvailableQualifiedTesterMachine";

            Console.WriteLine(value.Username);

            try
            {
                JCETWebServicesClient jcetWebServiceClient = new JCETWebServicesClient();
                //jcetWebServiceClient.hostname = "127.0.0.1";
                //jcetWebServiceClient.portno = 54350;
                jcetWebServiceClient._url = _configuration["WebService:url"];
                JCETWebServicesClient.ResultMsg resultMsg = new JCETWebServicesClient.ResultMsg();
                resultMsg = jcetWebServiceClient.GetAvailableQualifiedTesterMachine(false, value.Mode, value.Username, value.Password, value.LotId);

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
                            if(!xnA.InnerText.Equals(""))
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
            catch(Exception ex)
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

            return foo;
        }
    }
}
