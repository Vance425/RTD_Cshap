using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
    public class CheckDatabaseConnection : BasicController
    {
        private readonly IFunctionService _functionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly DBTool _dbTool;

        public CheckDatabaseConnection(IConfiguration configuration, ILogger logger, IFunctionService functionService, List<DBTool> lstDBSession)
        {
            _logger = logger;
            _configuration = configuration;
            _functionService = functionService;
            _dbTool = (DBTool)lstDBSession[1];
        }

        [HttpGet]
        public string Get()
        {
            string funcName = "CheckDatabaseConnection";
            string tmpMsg = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                if (_dbTool is null)
                {
                    tmpMsg = "Database Connection is null";
                }
                else
                {
                    if(_dbTool.dbPool.CheckConnet(out tmpMsg))
                    {
                        string tmp2Msg = "";
                        _dbTool.DisConnectDB(out tmp2Msg);


                        if (!_dbTool.IsConnected)
                        {
                            _dbTool.ConnectDB(out tmp2Msg);
                        }
                        else
                        {
                            tmpMsg = "Database is Connected.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return tmpMsg;
        }
    }
}
