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
    public class GetCarrierInfo : BasicController
    {
        private readonly IFunctionService _functionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly DBTool _dbTool;

        public GetCarrierInfo(IConfiguration configuration, ILogger logger, IFunctionService functionService, DBTool dbTool)
        {
            _logger = logger;
            _configuration = configuration;
            _functionService = functionService;
            _dbTool = dbTool;
        }

        [HttpPost]
        public ApiResultCarrierInfo Get([FromBody] classCarrier value)
        {
            ApiResultCarrierInfo foo;
            string funcName = "GetCarrierInfo";
            string tmpMsg = "";
            int iExecuteMode = 1;
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                //// 查詢資料
                //_dbTool.SQLExec(_BaseDataService.UpdateTableRTDDefaultSet("ExecuteMode", value.CarrierId), out tmpMsg, true);

                if (tmpMsg.Equals(""))
                {
                    foo = new ApiResultCarrierInfo()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmpMsg
                    };
                }
                else
                {
                    foo = new ApiResultCarrierInfo()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new ApiResultCarrierInfo()
                {
                    Success = true,
                    State = "NG",
                    Message = ex.Message
                };
            }
            finally
            {
                //_logger.LogInformation(string.Format("Info :{0}", value.CarrierID));
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return foo;
        }
        public class classCarrier
        {
            public string CarrierId { get; set; }
        }
        public class ApiResultCarrierInfo
        {
            public bool Success { get; set; }
            public string State { get; set; }
            public string LotId { get; set; }
            public string ProcessCode { get; set; }
            public string ErrorCode { get; set; }
            public string Message { get; set; }
        }
    }
}
