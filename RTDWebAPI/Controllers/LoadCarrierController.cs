using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RTDWebAPI.Models;
using System;

namespace RTDWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoadCarrierController : BasicController
    {

        private readonly ILogger<LoadCarrierController> _logger;

        public LoadCarrierController(ILogger<LoadCarrierController> logger)
        {
            _logger = logger;
        }
        public APIResult Post([FromBody] EventContent value)
        {
            APIResult foo;
            string tmpMsg = "";

            Console.WriteLine(value.EventName);

            tmpMsg = String.Format("{0} OK", value.EventName);
            //if (value.UserID == "VVVL")
            //{
            //    tmpMsg = "部門: IT, 名字: VVVL";
            //    foo = new APIResult()
            //    {
            //        Success = true,
            //        State = "OK",
            //        Message = tmpMsg
            //    };
            //}
            //else
            //{
            //    tmpMsg = "無法發現到指定的 ID";
            //    foo = new APIResult()
            //    {
            //        Success = false,
            //        State = "NG",
            //        Message = tmpMsg
            //    };
            //}
            foo = new APIResult()
            {
                Success = true,
                State = "OK",
                Message = tmpMsg
            };

            eventQueue.Enqueue(1);

            _logger.LogInformation(string.Format("Info:{0}", tmpMsg));
            logger.Info(string.Format("Info: {0}", tmpMsg));

            return foo;
        }
    }
    public class EventContent
    {
        public string EventName { get; set; }
        public string EventType { get; set; }
        public string ErackID { get; set; }
        public string SlotNo { get; set; }
        public string CarrierID { get; set; }
        public string CarrierType { get; set; }
    }
}
