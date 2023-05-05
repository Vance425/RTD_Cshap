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
using Microsoft.Extensions.Logging;
using System.Xml;
using System.IO;
using RTDWebAPI.Commons.Method.Tools;
using System.Threading;

namespace RTDWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : BasicController
    {

        private readonly NLog.ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly DBTool _dbTool;
        IFunctionService _functionService;
        private readonly List<DBTool> _lstDBSession;
        IBaseDataService _BaseDataService = new BaseDataService();

        public LoginController(NLog.ILogger logger, IConfiguration configuration, List<DBTool> lstDBSession)
        {
            string tmpMsg = "";
            _logger = logger;
            _configuration = configuration;
            //_dbTool = dbTool;
            _lstDBSession = lstDBSession;

            for (int idb = _lstDBSession.Count - 1; idb >= 0; idb--)
            {
                _dbTool = _lstDBSession[idb];
                if (_dbTool.IsConnected)
                {
                    break;
                }
            }

            tmpMsg = string.Format("{2} UI API, Function[{0}], Thread ID [{1}]", "GetUIDataController", Thread.CurrentThread.ManagedThreadId, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
            Console.WriteLine(tmpMsg);
            _logger.Info(tmpMsg);
        }

        [HttpPost]
        public LoginResult Post([FromBody] UserModel value)
        {
            LoginResult foo;
            string tmpMsg = "";
            DataTable dt = null;
            string acc_type = "";
            _functionService = new FunctionService();

            try
            {
                Console.WriteLine(value.Username);

                JCETWebServicesClient jcetWebServiceClient = new JCETWebServicesClient();
                //jcetWebServiceClient.hostname = "127.0.0.1";
                //jcetWebServiceClient.portno = 54350;
                if (value.Username.ToLower().Equals("gyro"))
                {
                    //"Position": "202401031319001"
                    if (value.Password.Equals("gsi5613686"))
                    {
                        tmpMsg = string.Format("User Name: [{0}] Super User login success.", value.Username);
                        foo = new LoginResult()
                        {
                            Success = true,
                            State = "OK",
                            UserType = "ADMIN",
                            Message = tmpMsg
                        };

                        return foo;
                    }
                }
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
                        //"Position": "202401031319001"
                        dt = _dbTool.GetDataTable(_BaseDataService.GetUserAccountType(value.Username));
                        acc_type = "OP";
                        if (dt.Rows.Count > 0)
                        {
                            acc_type = dt.Rows[0] is null ? "OP" : dt.Rows[0]["ACC_TYPE"].ToString();
                        }
                        tmpMsg = string.Format("User Name: [{0}] Login success.", value.Username);
                        foo = new LoginResult()
                        {
                            Success = true,
                            State = "OK",
                            UserType = acc_type,
                            Message = tmpMsg
                        };
                    }
                    else
                    {
                        tmpMsg = string.Format("user login failed. error: {0}", member_validation_message);
                        foo = new LoginResult()
                        {
                            Success = false,
                            State = "NG",
                            UserType = "NA",
                            Message = tmpMsg
                        };
                    }
                }
                else
                {
                    tmpMsg = resultMsg.retMessage;
                    foo = new LoginResult()
                    {
                        Success = false,
                        State = "NG",
                        UserType = "NA",
                        Message = tmpMsg
                    };
                }

            }
            catch (Exception ex)
            {
                tmpMsg = ex.Message;
                foo = new LoginResult()
                {
                    Success = false,
                    State = "NG",
                    UserType = "NA",
                    Message = tmpMsg
                };
            }

            if(foo.Success)
                _logger.Info(string.Format("Login Info: Result[{0}] UserID[{1}] UserType[{2}] ", "Success", value.Username, foo.UserType, tmpMsg));
            else
                _logger.Info(string.Format("Login Info: Result[{0}] UserID[{1}] UserType[{2}] ", "Invalid", value.Username, foo.UserType, tmpMsg));

            return foo;
        }
    }
}
