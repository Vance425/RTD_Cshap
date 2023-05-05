using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
    public class MoveCarrierController : BasicController
    {
        private readonly IFunctionService _functionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly DBTool _dbTool;
        private readonly ConcurrentQueue<EventQueue> _eventQueue;
        private readonly List<DBTool> _lstDBSession;

        public MoveCarrierController(List<DBTool> lstDBSession, IConfiguration configuration, ILogger logger, IFunctionService functionService, ConcurrentQueue<EventQueue> eventQueue)
        {
            _logger = logger;
            _configuration = configuration;
            _functionService = functionService;
            //_dbTool = dbTool;
            _eventQueue = eventQueue;
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
            string funcName = "MoveCarrier";
            string tmpMsg = "";

            _eventQ.EventName = funcName;
            string LotId = "";
            string CarrierId = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            int iQty = 0;

            try
            {
                CarrierId = value.CarrierID;
                if (CarrierId.Equals(""))
                {
                    foo.Success = false;
                    foo.State = "NG";
                    foo.Message = "Transfer command must provide carrier id.";
                    return foo;
                }

                //系統取出lot id 及 Quantity
                sql = string.Format(_BaseDataService.SelectTableCarrierAssociateByCarrierID(CarrierId));
                dt = _dbTool.GetDataTable(sql);
                if (dt.Rows.Count > 0)
                {
                    LotId = dt.Rows[0]["Lot_ID"].ToString().Trim();
                    value.LotID = LotId;
                    iQty = dt.Rows[0]["Quantity"].ToString().Equals("") ? 0 : int.Parse(dt.Rows[0]["Quantity"].ToString().Trim()); 
                    value.Quantity = iQty;
                }

                // 查詢Carrier 當前位址
                sql = string.Format(_BaseDataService.SelectTableCarrierTransferByCarrier(CarrierId));
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    tmpMsg = "";
                    string tmpSource = _functionService.GetLocatePort(dt.Rows[0]["Locate"].ToString(), dt.Rows[0]["PortNo"].ToString().Equals("") ? 1 : int.Parse(dt.Rows[0]["PortNo"].ToString()), "");

                    value.Source = tmpSource.Equals(value.Source) ? value.Source : tmpSource;
                    value.CarrierType = dt.Rows[0]["command_type"].ToString().Equals("Null") ? "" : dt.Rows[0]["command_type"].ToString();
                }
                else
                {
                    //Do Nothing
                    tmpMsg = "The Carrier has not on e-Rack!";
                    foo.Success = false;
                    foo.State = "NG";
                    foo.Message = tmpMsg;

                    return foo;
                }

                string strLocate = "";
                string strPort = "";
                string strRows = "0";
                string strCols = "0";
                if (!value.Source.Equals(""))
                {
                    if (value.Source.Contains("_LP"))
                    {
                        strLocate = value.Source.Split("_LP")[0].ToString();
                        strPort = value.Source.Split("_LP")[1].ToString();

                        sql = string.Format(_BaseDataService.QueryCarrierByCarrierID(value.CarrierID));
                        dt = _dbTool.GetDataTable(sql);
                        //strRows = dt.Rows[0]["rack_rows"].ToString();
                        //strCols = dt.Rows[0]["rack_cols"].ToString();
                        //rack_rows, rack_cols
                        sql = string.Format(_BaseDataService.QueryRackByGroupID(strLocate));
                        dt = _dbTool.GetDataTable(sql);

                        if (dt.Rows.Count > 0)
                        {
                            if (dt.Rows[0]["MAC"].ToString().Equals("STOCK"))
                            {
                                value.Source = strLocate;
                                //value.Source = "";
                                //value.Source = string.Format("{0}{1}-{2}", strLocate, strRows.PadLeft(2, '0'), strCols.PadLeft(2, '0'));
                            }
                        }
                    }
                }

                if (!value.Dest.Equals(""))
                {

                    if (value.Dest.Contains("_LP"))
                    {
                        strLocate = value.Dest.Split("_LP")[0].ToString();
                        strPort = value.Dest.Split("_LP")[1].ToString();

                        sql = string.Format(_BaseDataService.QueryRackByGroupID(strLocate));
                        dt = _dbTool.GetDataTable(sql);

                        if (dt.Rows.Count > 0)
                        {
                            if (dt.Rows[0]["MAC"].ToString().Equals("STOCK")) {
                                value.Dest = strLocate;
                            }
                            else 
                            {

                                value.Dest = string.Format("{0}_LP{1}", strLocate, strPort.PadLeft(2,'0'));
                            }
                        }
                    }

                    value.CommandType = "MANUAL-DIRECT";

                    //Do Nothing
                    foo.Success = true;
                    foo.State = "OK";
                    foo.Message = tmpMsg;
                }
                else
                {
                    //Do Nothing
                    tmpMsg = "Destination must be specified!";
                    foo.Success = false;
                    foo.State = "NG";
                    foo.Message = tmpMsg;

                    return foo;
                }

                if (foo.State == "OK")
                {
                    _eventQ.EventObject = value;
                    _eventQueue.Enqueue(_eventQ);
                }
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
                dt.Clear(); dt.Dispose();
                dt = null; dr = null;
            }

            return foo;
        }
    }
}
