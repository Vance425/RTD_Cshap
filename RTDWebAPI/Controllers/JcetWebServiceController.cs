using Microsoft.AspNetCore.Mvc;
using NLog;
using Microsoft.Extensions.Configuration;
using RTDWebAPI.Commons.DataRelated.SQLSentence;
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
using System.IO;
using RTDWebAPI.Commons.Method.Tools;
using static RTDWebAPI.Commons.Method.WSClient.JCETWebServicesClient;
using RTDDAC;

namespace RTDWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JcetWebServiceController : BasicController
    {

        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly DBTool _dbTool;
        IFunctionService _functionService;
        private readonly List<DBTool> _lstDBSession;

        public JcetWebServiceController(ILogger logger, IConfiguration configuration, List<DBTool> lstDBSession)
        {
            _logger = logger;
            _configuration = configuration;
            //_dbTool = dbTool; 
            _lstDBSession = lstDBSession;

            for (int idb = 0; idb < _lstDBSession.Count; idb++)
            {
                _dbTool = _lstDBSession[idb];
                if (_dbTool.IsConnected)
                {
                    break;
                }
            }
        }

        [HttpPost ("_TPQuery_CheckPROMISLogin")]
        public APIResult _TPQuery_CheckPROMISLogin([FromBody] UserModel value)
        {
            APIResult foo;
            string tmpMsg = "";
            _functionService = new FunctionService();

            try
            {
                Console.WriteLine(value.Username);

                JCETWebServicesClient jcetWebServiceClient = new JCETWebServicesClient();
                //jcetWebServiceClient.hostname = "127.0.0.1";
                //jcetWebServiceClient.portno = 54350;
                jcetWebServiceClient._url = _configuration["WebService:url"];
                JCETWebServicesClient.ResultMsg resultMsg = new JCETWebServicesClient.ResultMsg();
                resultMsg = jcetWebServiceClient.UserLogin(value.Username, value.Password);
                string result3 = resultMsg.retMessage;
                //<?xml version="1.0" encoding="utf - 8"?><Beans><Status Value="FAILURE" /><ErrMsg Value="SECURITY. % UAF - W - LOGFAIL, user authorization failure, privileges removed." /></Beans>';
                //string test = "<body><head>test header</head></body>";

                if (resultMsg.status)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(result3);
                    XmlNode xn = xmlDoc.SelectSingleNode("Beans");

                    XmlNodeList xnlA = xn.ChildNodes;
                    String member_valodation = "";
                    String member_validation_message = "";
                    foreach (XmlNode xnA in xnlA)
                    {
                        Console.WriteLine(xnA.Name);
                        if ((xnA.Name) == "Status")
                        {
                            XmlElement xeB = (XmlElement)xnA;
                            if ((xeB.GetAttribute("Value")) == "SUCCESS")
                            {
                                member_valodation = "OK";
                            }
                            else
                            {
                                member_valodation = "NG";
                            }

                        }
                        if ((xnA.Name) == "ErrMsg")
                        {
                            XmlElement xeB = (XmlElement)xnA;
                            member_validation_message = xeB.GetAttribute("Value");
                        }

                        Console.WriteLine(member_valodation);
                    }
                    if (member_valodation == "OK")
                    {
                        tmpMsg = string.Format("User Name: [{0}] Login success.", value.Username);
                        foo = new APIResult()
                        {
                            Success = true,
                            State = "OK",
                            Message = tmpMsg
                        };
                    }
                    else
                    {
                        tmpMsg = string.Format("user login failed. error: {0}", member_validation_message);
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
                    tmpMsg = resultMsg.retMessage;
                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }

            }
            catch (Exception ex)
            {
                tmpMsg = ex.Message;
                foo = new APIResult()
                {
                    Success = false,
                    State = "NG",
                    Message = tmpMsg
                };
            }

            _logger.Info(string.Format("Info:{0}", tmpMsg));

            return foo;
        }

        [HttpPost ("AvailableQualifiedTesterMachine")]
        public APIResult AvailableQualifiedTesterMachine([FromBody] AvailableQualifiedTesterMachine value)
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

                _logger.Debug(string.Format("Info:{0}", tmpMsg));

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

            return foo;
        }
        [HttpPost("_TPQuery_CurrentLotState")]
        public APIResult _TPQuery_CurrentLotState([FromBody] classLotState value)
        {
            APIResult foo;
            string tmpMsg = "";
            string member_currentState = "";
            string funcName = "_TPQuery_CurrentLotState";
            _functionService = new FunctionService();

            try
            {
                Console.WriteLine(value.lotid);

                JCETWebServicesClient jcetWebServiceClient = new JCETWebServicesClient();
                //jcetWebServiceClient.hostname = "127.0.0.1";
                //jcetWebServiceClient.portno = 54350;
                jcetWebServiceClient._url = _configuration["WebService:url"];
                JCETWebServicesClient.ResultMsg resultMsg = new JCETWebServicesClient.ResultMsg();
                resultMsg = jcetWebServiceClient.CurrentLotState(value.userMethod, value.username, value.pwd, value.lotid);
                string result3 = resultMsg.retMessage;
                //<?xml version="1.0" encoding="utf - 8"?><Beans><Status Value="FAILURE" /><ErrMsg Value="SECURITY. % UAF - W - LOGFAIL, user authorization failure, privileges removed." /></Beans>';
                //string test = "<body><head>test header</head></body>";

                if (resultMsg.status)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(result3);
                    XmlNode xn = xmlDoc.SelectSingleNode("Beans");

                    XmlNodeList xnlA = xn.ChildNodes;
                    String member_valodation = "";
                    String member_validation_message = "";
                    foreach (XmlNode xnA in xnlA)
                    {
                        Console.WriteLine(xnA.Name);
                        if ((xnA.Name) == "Status")
                        {
                            XmlElement xeB = (XmlElement)xnA;
                            if ((xeB.GetAttribute("Value")) == "SUCCESS")
                            {
                                member_valodation = "OK";
                                continue;
                            }
                            else
                            {
                                member_valodation = "NG";
                            }
                        }
                        if (member_valodation.Equals("OK"))
                        {
                            if ((xnA.Name) == "Msg")
                            {
                                XmlElement xeB = (XmlElement)xnA;
                                member_currentState = xeB.GetAttribute("Value").Equals("") ? "" : xeB.GetAttribute("Value");
                            }
                            break;
                        }
                        if ((xnA.Name) == "ErrMsg")
                        {
                            XmlElement xeB = (XmlElement)xnA;
                            member_validation_message = xeB.GetAttribute("Value");
                        }

                        Console.WriteLine(member_valodation);
                    }
                    if (member_valodation == "OK")
                    {
                        tmpMsg = string.Format("Lot Id [{0}]: current state is [{1}]", value.lotid, member_currentState.Equals("D") ? "WAIT" : "HOLD");
                        foo = new APIResult()
                        {
                            Success = true,
                            State = "OK",
                            Message = tmpMsg
                        };
                    }
                    else
                    {
                        tmpMsg = string.Format("The Method [{0}] run failed.", funcName);
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
                    tmpMsg = resultMsg.retMessage;
                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }

            }
            catch (Exception ex)
            {
                tmpMsg = ex.Message;
                foo = new APIResult()
                {
                    Success = false,
                    State = "NG",
                    Message = tmpMsg
                };
            }

            _logger.Info(string.Format("Info:{0}",tmpMsg));

            return foo;
        }
        public class classLotState
        {
            public string username { get; set; }
            public string pwd { get; set; }
            public string lotid { get; set; }
            public string userMethod { get; set; }
        }
        [HttpPost("_TPQuery_CustomizeEvent")]
        public APIResult _TPQuery_CustomizeEvent([FromBody] classCustomizeEvent value)
        {

            //public ResultMsg CustomizeEvent(string _func, string useMethod, bool isProduct, string equip, string username, string pwd, string lotid, string _args)
            APIResult foo;
            string tmpMsg = "";
            string member_currentState = "";
            string funcName = "_TPQuery_CustomizeEvent";
            _functionService = new FunctionService();

            try
            {
                Console.WriteLine(value.lotid);

                JCETWebServicesClient jcetWebServiceClient = new JCETWebServicesClient();
                //jcetWebServiceClient.hostname = "127.0.0.1";
                //jcetWebServiceClient.portno = 54350;
                jcetWebServiceClient._url = _configuration["WebService:url"];
                jcetWebServiceClient._urlUAT = _configuration["WebService:urlUAT"];
                JCETWebServicesClient.ResultMsg resultMsg = new JCETWebServicesClient.ResultMsg();
                resultMsg = jcetWebServiceClient.CustomizeEvent(value.function, value.userMethod, value.isProduct, value.equipid, value.username, value.pwd, value.lotid, value.args);
                string result3 = resultMsg.retMessage;
                //<?xml version="1.0" encoding="utf - 8"?><Beans><Status Value="FAILURE" /><ErrMsg Value="SECURITY. % UAF - W - LOGFAIL, user authorization failure, privileges removed." /></Beans>';
                //string test = "<body><head>test header</head></body>";

                if (resultMsg.status)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    String member_valodation = "";
                    String member_validation_message = "";

                    try { 
                        xmlDoc.LoadXml(result3);
                        XmlNode xn = xmlDoc.SelectSingleNode("Beans");
                        XmlNodeList xnlA = xn.ChildNodes;
                        foreach (XmlNode xnA in xnlA)
                        {
                            Console.WriteLine(xnA.Name);
                            member_valodation = "";
                            member_validation_message = "";

                            if ((xnA.Name) == "Status")
                            {
                                XmlElement xeB = (XmlElement)xnA;
                                if ((xeB.GetAttribute("Value")) == "SUCCESS")
                                {
                                    member_valodation = "OK";
                                    continue;
                                }
                                else
                                {
                                    member_valodation = "NG";
                                }
                            }
                            if (member_valodation.Equals("OK"))
                            {
                                if ((xnA.Name) == "Msg")
                                {
                                    XmlElement xeB = (XmlElement)xnA;
                                    member_currentState = xeB.GetAttribute("Value").Equals("") ? "" : xeB.GetAttribute("Value");
                                }
                                break;
                            }
                            if ((xnA.Name) == "ErrMsg")
                            {
                                XmlElement xeB = (XmlElement)xnA;
                                member_validation_message = xeB.GetAttribute("Value");
                            }

                            Console.WriteLine(member_valodation);
                        }
                    }
                    catch (Exception ex) {
                        tmpMsg = string.Format("get result issue. [{0}]", result3);
                    }

                    if (member_valodation == "OK")
                    {
                        tmpMsg = string.Format("Lot Id [{0}]: current state is [{1}]", value.lotid, member_currentState.Equals("D") ? "WAIT" : "HOLD");
                        foo = new APIResult()
                        {
                            Success = true,
                            State = "OK",
                            Message = tmpMsg
                        };
                    }
                    else
                    {
                        if (tmpMsg.Equals(""))
                        {
                            tmpMsg = string.Format("The Method [{0}] run failed.", funcName);
                        }

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
                    tmpMsg = resultMsg.retMessage;
                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }

            }
            catch (Exception ex)
            {
                tmpMsg = ex.Message;
                foo = new APIResult()
                {
                    Success = false,
                    State = "NG",
                    Message = tmpMsg
                };
            }

            _logger.Info(string.Format("Info:{0}", tmpMsg));

            return foo;
        }
        ////public ResultMsg CustomizeEvent(string _func, string useMethod, bool isProduct, string equip, string username, string pwd, string lotid, string _args)
        public class classCustomizeEvent
        {
            public string function { get; set; }
            public bool isProduct { get; set; }
            public string username { get; set; }
            public string pwd { get; set; }
            public string lotid { get; set; }
            public string equipid { get; set; }
            public string userMethod { get; set; }
            public string args { get; set; }
        }
    }
}
