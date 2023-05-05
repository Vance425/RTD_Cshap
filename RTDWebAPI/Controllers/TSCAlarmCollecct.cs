using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nancy.Json;
using NLog;
using RTDWebAPI.Commons.DataRelated.SQLSentence;
using RTDWebAPI.Commons.Method.Database;
using RTDWebAPI.Interface;
using RTDWebAPI.Models;
using RTDWebAPI.Service;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;

namespace RTDWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]

    public class TSCAlarmCollectController : BasicController
    {
        IBaseDataService BaseDataService;
        SqlSentences sqlSentences;
        //IConfiguration configuration;
        //public PortStateChangeController(IConfiguration _configuration)
        //{
        //    configuration = _configuration;
        //}

        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly DBTool _dbTool;
        private readonly ConcurrentQueue<EventQueue> _eventQueue;
        private readonly List<DBTool> _lstDBSession;

        public TSCAlarmCollectController(ILogger logger, IConfiguration configuration, List<DBTool> lstDBSession, ConcurrentQueue<EventQueue> eventQueue)
        {
            _logger = logger;
            _configuration = configuration;
            //_dbTool = (DBTool) lstDBSession[1];
            _eventQueue = eventQueue;
            _lstDBSession = lstDBSession;

            for (int idb = 0; idb < _lstDBSession.Count; idb++)
            {
                _dbTool = _lstDBSession[idb];
                if (_dbTool.IsConnected)
                {
                    break;
                }
            }
        }

        [HttpPost]
        public APIResult Post([FromBody] TSCAlarmCollect value)
        {
            APIResult foo;
            IBaseDataService _BaseDataService = new BaseDataService();
            IFunctionService _functionService = new FunctionService();

            string funcName = "TSCAlarmCollect";
            string tmpMsg = "";
            EventQueue _eventQ = new EventQueue();
            _eventQ.EventName = funcName;
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            string equipID = "";

            foo = new APIResult();
            try
            {
                var jsonStringName = new JavaScriptSerializer();
                var jsonStringResult = jsonStringName.Serialize(value);
                _logger.Info(string.Format("Function:{0}, Received:[{1}]", funcName, jsonStringResult));

                if (value.ALID.Equals(""))
                {
                    foo.Success = false;
                    foo.State = "NG";
                    foo.Message = "Input Alarm Info not correct.";
                    return foo;
                }

                //equipID = value


                _eventQ.EventObject = value;
                _eventQueue.Enqueue(_eventQ);

                _logger.Info(string.Format("Function:{0}, Responced:[{1}]", funcName, value.ALID));

                foo.Success = true;
                foo.State = "OK";
                foo.Message = String.Format("");
                _logger.Debug(foo.Message);
            }
            catch (Exception ex)
            {
                foo.Success = false;
                foo.State = "NG";
                foo.Message = String.Format("Unknow issue. [{0}] Exception: {1}", funcName, ex.Message);
                _logger.Debug(foo.Message);
            }
            finally
            {
                /*
                if (dt != null)
                {
                    dt.Clear(); dt.Dispose(); dt = null;
                }
                dr = null;
                */
            }

            return foo;
        }
    }
}
