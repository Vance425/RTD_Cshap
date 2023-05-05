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
    public class SendCommandController : BasicController
    {
        private readonly IFunctionService _functionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public SendCommandController(IConfiguration configuration, ILogger logger, IFunctionService functionService)
        {
            _logger = logger;
            _configuration = configuration;
            _functionService = functionService;
        }

        [HttpPost]
        public APIResult Post([FromBody] InfoUpdate value)
        {
            APIResult foo = new APIResult();
            string tmpMsg = "";

            try
            {
                //foo = _functionService.SentDispatchCommandtoMCS(_configuration, _logger);

                List<string> tmpp = new List<string>();
                //foo = _functionService.SentCommandtoMCS(_configuration, _logger, tmpp);
                //tmpp.Add(value.CarrierID);
                //tmpp.Add(value.LotID);
                //tmpp.Add(value.Stage);
                //tmpp.Add(value.Cust);
               
                tmpp.Add(value.LotID);
                tmpp.Add(value.Stage);
                tmpp.Add("");
                tmpp.Add("");
                tmpp.Add(value.CarrierID);
                tmpp.Add(value.Cust);
                tmpp.Add(value.PartID);
                tmpp.Add(value.LotType);
                tmpp.Add(value.Automotive);
                tmpp.Add(value.State);
                tmpp.Add(value.HoldCode);
                tmpp.Add(value.TurnRatio.ToString());
                tmpp.Add(value.EOTD);
                tmpp.Add(value.HoldReas);
                tmpp.Add(value.POTD);
                tmpp.Add(value.WaferLot);

                foo = _functionService.SentCommandtoMCSByModel(_configuration, _logger, "InfoUpdate", tmpp);
            }
            catch(Exception ex)
            {
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

    public class CommandContent
    {
        public string CommandID { get; set; }
        public string CommandPkg { get; set; }
        public string Result { get; set; }
    }

    class Product
    {
        public string Name { get; set; }
        public double Price { get; set; }
        public string Category { get; set; }
    }
}
