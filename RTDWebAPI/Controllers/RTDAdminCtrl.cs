using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nancy.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RTDDAC;
using RTDWebAPI.Interface;
using RTDWebAPI.Models;
using RTDWebAPI.Service;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace RTDWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RTDAdminCtrl : BasicController
    {
        private readonly IFunctionService _functionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly DBTool _dbTool;

        public RTDAdminCtrl(IConfiguration configuration, ILogger logger, IFunctionService functionService, DBTool dbTool)
        {
            _logger = logger;
            _configuration = configuration;
            _functionService = functionService;
            _dbTool = dbTool;
        }

        [HttpPost("ForceDeleteOrder")]
        public Boolean ForceDeleteOrder([FromBody] classWorkinprocessSchCmd value)
        {
            APIResult foo = new();
            string funcName = "ForceDeleteOrder";
            string tmpMsg = "";
            string tmp2Msg = "";
            string strResult = "";
            bool bResult = false;
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            string tableOrder = "";
            string _keyOfEnv = "";
            string _keyRTDEnv = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                var jsonStringName = new JavaScriptSerializer();
                var jsonStringResult = jsonStringName.Serialize(value);
                _logger.Info(string.Format("Function:[{0}], WorkinProcess:{1}", funcName, jsonStringResult));

                _keyRTDEnv = _configuration["RTDEnvironment:type"];
                _keyOfEnv = string.Format("RTDEnvironment:commandsTable:{0}", _configuration["RTDEnvironment:type"]);
                //"RTDEnvironment:commandsTable:PROD"
                tableOrder = _configuration[_keyOfEnv] is null ? "workinprocess_sch" : _configuration[_keyOfEnv];

                if (_dbTool.SQLExec(_BaseDataService.DeleteWorkInProcessSchByCmdId(value.CommandID, tableOrder), out tmpMsg, true))
                {
                    //Do Nothing
                    foo.Success = true;
                    foo.State = "OK";
                    foo.Message = tmpMsg;
                }
                else
                {
                    //Do Nothing
                    tmp2Msg = String.Format("WorkinProcess delete fail. [{0}] [Exception] {1}", value.UserID, tmpMsg);
                    foo.Success = false;
                    foo.State = "NG";
                    foo.Message = tmp2Msg;
                }

                return true;
            }
            catch (Exception ex)
            {
                bResult = false;
            }
            finally
            {
                //Do Nothing
            }

            return bResult;
        }
        public class classWorkinprocessSchCmd
        {
            public string CommandID { get; set; }
            public string UserID { get; set; }
        }
    }
}
