using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nancy.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RTDWebAPI.Commons.Method.Database;
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
    public class DeleteWorkinProcessController : BasicController
    {
        private readonly IFunctionService _functionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly DBTool _dbTool;
        private readonly ConcurrentQueue<EventQueue> _eventQueue;
        private readonly List<DBTool> _lstDBSession;

        public DeleteWorkinProcessController(List<DBTool> lstDBSession, IConfiguration configuration, ILogger logger, IFunctionService functionService, ConcurrentQueue<EventQueue> eventQueue)
        {
            _logger = logger;
            _configuration = configuration;
            _functionService = functionService;
            //_dbTool = (DBTool)lstDBSession[0];
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
        public APIResult Post([FromBody] KeyWorkInProcessSch value)
        {
            APIResult foo = new();
            IBaseDataService _BaseDataService = new BaseDataService();
            EventQueue _eventQ = new EventQueue();
            string funcName = "DeleteWorkinProcessController";
            string tmpMsg = "";

            _eventQ.EventName = funcName;
            string CommandId = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            string lotid = "";

            string tableOrder = "";
            string _keyOfEnv = "";
            string _keyRTDEnv = "";

            try
            {
                var jsonStringName = new JavaScriptSerializer();
                var jsonStringResult = jsonStringName.Serialize(value);
                _logger.Info(string.Format("Function:[{0}], WorkinProcess:{1}", funcName, jsonStringResult));

                _keyRTDEnv = _configuration["RTDEnvironment:type"];
                _keyOfEnv = string.Format("RTDEnvironment:commandsTable:{0}", _configuration["RTDEnvironment:type"]);
                //"RTDEnvironment:commandsTable:PROD"
                tableOrder = _configuration[_keyOfEnv] is null ? "workinprocess_sch" : _configuration[_keyOfEnv];

                CommandId = value.CommandID;
                if (CommandId.Equals(""))
                {
                    foo.Success = false;
                    foo.State = "NG";
                    foo.Message = "Command ID can not be empty.";
                    return foo;
                }

                /*
                if (true)
                {
                    tmpMsg = "";
                    _dbTool.ReConnectDB(out tmpMsg);
                    if (!tmpMsg.Equals(""))
                        _logger.Debug(string.Format("DBTool Re-Connection Failed. [{0}]", tmpMsg));
                }
                */
                // 查詢Lot資料
                sql = string.Format(_BaseDataService.SelectTableWorkInProcessSchByCmdId(CommandId, tableOrder));
                dt = _dbTool.GetDataTable(sql);
                dr = dt.Select();

                if (dt.Rows.Count > 0)
                {
                    tmpMsg = "";
                    lotid = !dt.Rows[0]["lotid"].ToString().Equals("") ? dt.Rows[0]["lotid"].ToString().Trim() : "" ;

                    APIResult apiResult = new APIResult();
                    if (dt.Rows[0]["cmd_current_state"].Equals("Init"))
                    { //Cancel
                        apiResult = _functionService.SentAbortOrCancelCommandtoMCS(_configuration, _logger, 1, CommandId);
                        if(apiResult.Success)
                        {
                            
                        }
                        else
                        {

                        }
                    }
                    else if (dt.Rows[0]["cmd_current_state"].Equals("Running"))
                    { //Abort
                        apiResult = _functionService.SentAbortOrCancelCommandtoMCS(_configuration, _logger, 2, CommandId);
                        if (apiResult.Success)
                        {
                            
                        }
                        else
                        {

                        }
                    }
                    else if (dt.Rows[0]["cmd_current_state"].Equals("Failed"))
                    { //Reset lot_info RTD_STATE to READY
                        if (!lotid.Equals(""))
                        {
                            sql = _BaseDataService.UpdateTableLotInfoToReadyByLotid(lotid);
                            _dbTool.SQLExec(sql, out tmpMsg, true);
                        }
                        tmpMsg = "Command run failed.";
                        apiResult.Success = true;
                        apiResult.State = "NG";
                        apiResult.Message = tmpMsg;
                    }
                    else
                    { //Pass of Success/ Failed/ Others
                        if (!lotid.Equals(""))
                        {
                            sql = _BaseDataService.UpdateTableLotInfoToReadyByLotid(lotid);
                            _dbTool.SQLExec(sql, out tmpMsg, true);
                        }
                        tmpMsg = "";
                        apiResult.Success = true;
                        apiResult.State = "OK";
                        apiResult.Message = tmpMsg;
                    }

                    if (apiResult.Success)
                    {
                        // 更新狀態資料
                        string CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        if (_dbTool.SQLExec(_BaseDataService.UpdateTableWorkInProcessSchByCmdId("DELETE", CurrentTime, CommandId, tableOrder), out tmpMsg, true))
                        {
                            if (_dbTool.SQLExec(_BaseDataService.UpdateTableWorkInProcessSchHisByCmdId(CommandId), out tmpMsg, true))
                            {
                                foreach (DataRow row in dt.Rows)
                                {
                                    if (row["CARRIERID"].ToString().Equals("*") || row["CARRIERID"].ToString().Equals(""))
                                    { }
                                    else
                                    {
                                        if (_dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(row["CARRIERID"].ToString(), false), out tmpMsg, true))
                                        { }
                                    }
                                }

                                if (_dbTool.SQLExec(_BaseDataService.DeleteWorkInProcessSchByCmdId(CommandId, tableOrder), out tmpMsg, true))
                                {
                                    //Do Nothing
                                    foo.Success = true;
                                    foo.State = "OK";
                                    foo.Message = tmpMsg;
                                }
                                else
                                {
                                    //Do Nothing
                                    string tmp2Msg = String.Format("WorkinProcess delete fail. [Exception] {0}", tmpMsg);
                                    foo.Success = false;
                                    foo.State = "NG";
                                    foo.Message = tmp2Msg;
                                }
                            }

                        }
                        else
                        {
                            //Do Nothing
                            string tmp2Msg = String.Format("WorkinProcess update fail. [Exception] {0}", tmpMsg);
                            foo.Success = false;
                            foo.State = "NG";
                            foo.Message = tmp2Msg;

                            _dbTool.ReConnectDB(out tmpMsg);
                        }
                    }
                    else
                    {
                        foo = apiResult;
                    }
                }
                else
                {
                    tmpMsg = "Data Not found.";
                    foo.Success = false;
                    foo.State = "NG";
                    foo.Message = tmpMsg;
                }

                if (foo.State == "OK")
                {
                    _eventQ.EventObject = value;
                    _eventQueue.Enqueue(_eventQ);
                }
                else
                {
                    _logger.Debug(foo.Message);
                }
            }
            catch(Exception ex)
            {
                foo.Success = false;
                foo.State = "NG";
                foo.Message = String.Format("Unknow issue. [{0}] Exception: {1}", funcName, ex.Message);
                _logger.Debug(foo.Message);
            }

            return foo;
        }
        public class KeyWorkInProcessSch
        {
            public string CommandID { get; set; }
            public string Username { get; set; }
        }
    }
}
