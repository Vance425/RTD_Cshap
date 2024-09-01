using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nancy.Json;
using Newtonsoft.Json;
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

    public class CommandStatusUpdateController : BasicController
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly DBTool _dbTool;
        private readonly ConcurrentQueue<EventQueue> _eventQueue;
        private readonly List<DBTool> _lstDBSession;

        public CommandStatusUpdateController(ILogger logger, IConfiguration configuration, List<DBTool> lstDBSession, ConcurrentQueue<EventQueue> eventQueue)
        {
            _logger = logger;
            _configuration = configuration;
            //_dbTool = (DBTool)lstDBSession[1];
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
        public APIResult Post([FromBody] CommandStatusUpdate value)
        {
            APIResult foo;
            IBaseDataService _BaseDataService = new BaseDataService();

            string funcName = "CommandStatusUpdate";
            string tmpMsg = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            bool bExecSql = false;
            EventQueue _eventQ = new EventQueue();
            _eventQ.EventName = funcName;

            foo = new APIResult();

            int FailedNum = 0; //AddByBird@20230421_for跳出迴圈

            try
            {
                var jsonStringName = new JavaScriptSerializer();
                var jsonStringResult = jsonStringName.Serialize(value);
                _logger.Info(string.Format("Function:{0}, Received:[{1}]", funcName, jsonStringResult));

                _eventQ.EventObject = value;
                _eventQueue.Enqueue(_eventQ);

                /*
                while (true)
                {
                    try
                    {
                        sql = _BaseDataService.SelectTableWorkInProcessSchByCmdId(value.CommandID);
                        dt = _dbTool.GetDataTable(sql);

                        if (dt.Rows.Count > 0)
                        {
                            if (!dt.Rows[0]["cmd_type"].ToString().Equals("Pre-Transfer"))
                            {
                                bExecSql = _dbTool.SQLExec(_BaseDataService.UpdateTableWorkInProcessSchByCmdId(value.Status, value.LastStateTime, value.CommandID.Trim()), out tmpMsg, true);

                                if (bExecSql)
                                    break;
                            }
                        }
                        else
                            break;
                    }
                    catch (Exception ex)
                    {
                        //tmpMsg = String.Format("UpdateTableWorkInProcessSchByCmdId fail. {0}", ex.Message);
                        tmpMsg = String.Format("UpdateTableWorkInProcessSchByCmdId fail. {0}", ex.ToString()); //ModifyByBird@20230421_秀出更多錯誤資訊
                        _logger.Debug(tmpMsg);
                        FailedNum++; //AddByBird@20230421_跳出迴圈
                    }

                    //AddByBird@20230421_跳出迴圈
                    if (FailedNum >=3)
                    {
                        tmpMsg = String.Format("Execute UpdateTableWorkInProcessSchByCmdId Failed (Retry 3 Times). Received:[{0}]", jsonStringResult);
                        _logger.Error(tmpMsg);
                        break; //AddByBird@20230421_跳出迴圈
                    }
                }

                if (!bExecSql)
                {
                    foo.Success = false;
                    foo.State = "NG";
                    foo.Message = "Command ID is not exist.";
                    return foo;
                }
                else
                {
                    foo.Success = true;
                    foo.State = "OK";
                    foo.Message = "";
                    
                    if (foo.State == "OK")
                    {
                        _eventQ.EventObject = value;
                        _eventQueue.Enqueue(_eventQ);
                    }
                }
                */
                
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

            tmpMsg = String.Format("[CommandStatusUpdate] Response Done. {0}, state is {1}", value.CommandID.Trim(), foo.State);
            _logger.Debug(tmpMsg);

            return foo;
        }
    }
}
