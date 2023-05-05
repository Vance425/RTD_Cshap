using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NLog;
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
    public class SyncEquipmentStatusController : BasicController
    {
        private readonly IFunctionService _functionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public SyncEquipmentStatusController(IConfiguration configuration, ILogger logger, IFunctionService functionService)
        {
            _logger = logger;
            _configuration = configuration;
            _functionService = functionService;
        }

        [HttpPost]
        public APIResult Post([FromBody] EquipmentStatusSync value)
        {
            APIResult foo = new APIResult();
            string tmpMsg = "";
            string _tmpFuncName = "SyncEquipmentStatus";

            try
            {
                //foo = _functionService.SentDispatchCommandtoMCS(_configuration, _logger);

                List<string> tmpp = new List<string>();
                //foo = _functionService.SentCommandtoMCS(_configuration, _logger, tmpp);
                tmpp.Add(value.PortID);

                foo = _functionService.SentCommandtoMCSByModel(_configuration, _logger, "EquipmentStatusSync", tmpp);
            }
            catch(Exception ex)
            {
                tmpMsg = ex.Message;
                _logger.Debug(string.Format("[{0}] Debug:{0}", _tmpFuncName, tmpMsg));
            }

            return foo;
        }
    }
}
