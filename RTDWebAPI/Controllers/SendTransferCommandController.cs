using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NLog;
using RTDWebAPI.Commons.Method.Database;
using RTDWebAPI.Interface;
using RTDWebAPI.Models;
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
    public class SendTransferCommandController : BasicController
    {
        private readonly IFunctionService _functionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly List<DBTool> _lstDBSession;
        private readonly DBTool _dbTool;

        public SendTransferCommandController(List<DBTool> lstDBSession, IConfiguration configuration, ILogger logger, IFunctionService functionService)
        {
            _dbTool = (DBTool)lstDBSession[0];
            _logger = logger;
            _configuration = configuration;
            _functionService = functionService;
        }

        [HttpPost]
        public APIResult Post([FromBody] TransferList value)
        {
            APIResult foo = new APIResult();
            string tmpMsg = "";
            bool bResult = false;


            try
            {
                //foo = _functionService.SentDispatchCommandtoMCS(_configuration, _logger);

                //List<string> tmpp = new List<string>();

                bResult = _functionService.SentTransferCommandtoToMCS(_dbTool, _configuration, _logger, value, out tmpMsg);
            }
            catch(Exception ex)
            {
            }

            if(tmpMsg.Equals(""))
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "OK",
                    Message = tmpMsg
                };
            }
            else
            {
                foo = new APIResult()
                {
                    Success = false,
                    State = "NG",
                    Message = tmpMsg
                };
            }

            _logger.Info(string.Format("Info:{0}",tmpMsg));
            _logger.Warn(string.Format("Warning:{0}", tmpMsg));
            _logger.Error(string.Format("Error:{0}", tmpMsg));
            _logger.Debug(string.Format("Debug:{0}", tmpMsg));

            //string sql = "select * from gyro_lot_carrier_associate";
            //DataSet ds = dbPool.GetDataSet(sql);

            return foo;
        }
    }
}
