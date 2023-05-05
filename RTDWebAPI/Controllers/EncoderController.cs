using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RTDWebAPI.Commons.Method.Encoder;
using RTDWebAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static RTDWebAPI.Commons.Method.TestService.classTestService;

namespace RTDWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EncoderController : BasicController
    {

        private readonly ILogger<EncoderController> _logger;

        public EncoderController(ILogger<EncoderController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public APIResult Post([FromBody] EncodedrData value)
        {
            APIResult foo;

            Console.WriteLine(value.oriString);
            if (value.encodeType == "MD5")
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "OK",
                    Message = string.Format("Encrypt Done. Encode String [{0}]", Encoder.ToMD5(value.oriString))
                };
            }
            else
            {
                foo = new APIResult()
                {
                    Success = false,
                    State = "NG",
                    ErrorCode = "0001",
                    Message = "加密失敗"
                };
            }

            _logger.LogInformation("This is Info log");
            _logger.LogWarning("This is Warning log");
            _logger.LogError("This is Error log");
            _logger.LogDebug("This is Debug log");
            _logger.LogCritical("This is Critical log");

            eventQueue.Enqueue(2);

            return foo;
        }
    }

    public class EncodedrData
    {
        public string oriString { get; set; }
        public string encodeType { get; set; }
    }
}
