using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
    public class ReleaseCarrierController : BasicController
    {
        private readonly IFunctionService _functionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly DBTool _dbTool;
        private readonly ConcurrentQueue<EventQueue> _eventQueue;
        private readonly Dictionary<string, object> _uiDataCatch;
        private readonly List<DBTool> _lstDBSession;

        public ReleaseCarrierController(List<DBTool> lstDBSession, IConfiguration configuration, ILogger logger, IFunctionService functionService, ConcurrentQueue<EventQueue> eventQueue, Dictionary<string, object> uiDataCatch)
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

        [HttpPost]
        public APIResult Post([FromBody] TransferList value)
        {
            APIResult foo = new();
            IBaseDataService _BaseDataService = new BaseDataService();
            EventQueue _eventQ = new EventQueue();
            string funcName = "ReleaseCarrier";
            string tmpMsg = "";

            _eventQ.EventName = funcName;
            string CarrierId = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";

            try
            {
                CarrierId = value.CarrierID;
                if (CarrierId.Equals(""))
                {
                    foo.Success = false;
                    foo.State = "NG";
                    foo.Message = "Carrier Id can not be empty.";
                    return foo;
                }

                // 查詢Carrier 資料
                sql = string.Format(_BaseDataService.SelectTableCarrierTransferByCarrier(CarrierId));
                dt = _dbTool.GetDataTable(sql);
                dr = dt.Select();

                if (dt.Rows.Count > 0)
                {
                    tmpMsg = "";

                    // 更新狀態資料
                    if (_dbTool.SQLExec(_BaseDataService.UpdateTableCarrierTransferByCarrier(CarrierId, "Normal"), out tmpMsg, true))
                    {
                        //Do Nothing
                        foo.Success = true;
                        foo.State = "OK";
                        foo.Message = tmpMsg;
                    }
                    else
                    {
                        //Do Nothing
                        string tmp2Msg = String.Format("Update failed. [Exception] {0}", tmpMsg);
                        foo.Success = false;
                        foo.State = "NG";
                        foo.Message = tmp2Msg;
                    }
                }
                else
                {
                    //Do Nothing
                    tmpMsg = String.Format("Can not find the Carrier Id [{0}]", CarrierId);
                    foo.Success = false;
                    foo.State = "NG";
                    foo.Message = tmpMsg;
                }

                if (foo.State == "OK")
                {
                    _eventQ.EventObject = value;
                    _eventQueue.Enqueue(_eventQ);
                }
            }
            catch(Exception ex)
            {
                foo.Success = false;
                foo.State = "NG";
                foo.Message = String.Format("Unknow issue. [{0}] Exception: {1}", funcName, ex.Message);
                _logger.Debug(foo.Message);
            }
            finally
            {
                dt.Clear(); dt.Dispose(); 
                dt = null; dr = null;
            }

            return foo;
        }
    }
}
