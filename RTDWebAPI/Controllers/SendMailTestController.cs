using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RTDWebAPI.Commons.Method.Database;
using RTDWebAPI.Commons.Method.Mail;
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
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace RTDWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SendMailTestController : BasicController
    {
        private readonly IFunctionService _functionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly DBTool _dbTool;
        private readonly ConcurrentQueue<EventQueue> _eventQueue;
        private readonly Dictionary<string, object> _uiDataCatch;
        private readonly List<DBTool> _lstDBSession;

        public SendMailTestController(List<DBTool> lstDBSession, IConfiguration configuration, ILogger logger, IFunctionService functionService, ConcurrentQueue<EventQueue> eventQueue, Dictionary<string, object> uiDataCatch)
        {
            _dbTool = (DBTool)lstDBSession[0];
            _logger = logger;
            _configuration = configuration;
            _functionService = functionService;
            _eventQueue = eventQueue;
            _uiDataCatch = uiDataCatch;
            _lstDBSession = lstDBSession;

            for (int idb = _lstDBSession.Count - 1; idb >= 0; idb--)
            {
                _dbTool = _lstDBSession[idb];
                if (_dbTool.IsConnected)
                {
                    break;
                }
            }
        }

        [HttpPost("SendMailTest")]
        public APIResult SendMailTest([FromBody] classMailMessage value)
        {
            APIResult foo;
            string funcName = "SendMailTest";
            string tmpMsg = "";
            string tmpSmsMsg = "";
            string strResult = "";
            bool bResult = false;
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();
            MailMessage tmpMailMsg = new MailMessage();
            tmpMailMsg.To.Add(_configuration["MailSetting:AlarmMail"]);

            try
            {
                tmpSmsMsg = string.Format("LOT {0} NEED TO SETUP NOW. EQUIP DOWN FROM RTS.", "Test");

                /* 上面3個參數分別是發件人地址（可以隨便寫），發件人姓名，編碼*/
                tmpMailMsg.Subject = value.Subject.Equals("") ? "Device Setup Alert" : value.Subject;//郵件標題
                tmpMailMsg.SubjectEncoding = Encoding.UTF8;//郵件標題編碼
                tmpMailMsg.Body = value.Body.Equals("") ? string.Format("LOT {0} ({1}) NEED TO SETUP NOW. EQUIP DOWN FROM RTS.", "Test", "MachinePort") : value.Body; //郵件內容
                tmpMailMsg.BodyEncoding = Encoding.UTF8;//郵件內容編碼 
                tmpMailMsg.IsBodyHtml = true;//是否是HTML郵件 
                tmpMailMsg.From = new MailAddress(_configuration["MailSetting:AlarmMail"]);
            }
            catch (Exception ex)
            {
                tmpMsg = String.Format("MailMessage failed. [Exception]: {0}", ex.Message);
                _logger.Debug(tmpMsg);
            }

            ///寄送Mail
            try
            {
                MailController MailCtrl = new MailController();
                MailCtrl.Config = _configuration;
                MailCtrl.Logger = _logger;
                MailCtrl.DB = _dbTool;
                MailCtrl.MailMsg = tmpMailMsg;

                tmpMsg = MailCtrl.SendMail();

                foo = new APIResult()
                {
                    Success = true,
                    State = "OK",
                    Message = tmpMsg
                };

                tmpMsg = string.Format("SendMail: {0}, [{1}]", tmpMailMsg.Subject, tmpMailMsg.Body);
                _logger.Info(tmpMsg);
            }
            catch (Exception ex)
            {
                tmpMsg = String.Format("SendMail failed. [Exception]: {0}", ex.Message);
                _logger.Debug(tmpMsg);

                foo = new APIResult()
                {
                    Success = false,
                    State = "NG",
                    Message = tmpMsg
                };
            }

            return foo;
        }
        /* 上面3個參數分別是發件人地址（可以隨便寫），發件人姓名，編碼
        tmpMailMsg.Subject = "Device Setup Alert";//郵件標題
                                tmpMailMsg.SubjectEncoding = Encoding.UTF8;//郵件標題編碼
                                tmpMailMsg.Body = string.Format("LOT {0} ({1}) NEED TO SETUP NOW. EQUIP DOWN FROM RTS.", lotid, tmpPartId); //郵件內容
        tmpMailMsg.BodyEncoding = Encoding.UTF8;//郵件內容編碼 
                                tmpMailMsg.IsBodyHtml = true;//是否是HTML郵件 
                                bAlarm = true;
            */
        public class classMailMessage
        {
            public string Subject { get; set; }
            public string SubjectEncoding { get; set; }
            public string Body { get; set; }
            public string BodyEncoding { get; set; }
            public string IsBodyHtml { get; set; }
        }
    }
}
