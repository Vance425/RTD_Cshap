using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
using System.Threading;
using System.Threading.Tasks;

namespace RTDWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetQuantityByCarrier : BasicController
    {
        private readonly IFunctionService _functionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly DBTool _dbTool2;
        private readonly List<DBTool> _lstDBSession;

        public GetQuantityByCarrier(IConfiguration configuration, ILogger logger, IFunctionService functionService, List<DBTool> lstDBSession)
        {
            _logger = logger;
            _configuration = configuration;
            _functionService = functionService;
            _lstDBSession = lstDBSession;

            for (int idb = 0; idb < _lstDBSession.Count; idb++)
            {
                _dbTool2 = _lstDBSession[idb];
                if (_dbTool2.IsConnected)
                {
                    break;
                }
            }
        }

        [HttpPost]
        public ApiResultQuantityInfo Get([FromBody] clsCarrier value)
        {
            ApiResultQuantityInfo foo;
            string funcName = "GetQuantityByCarrier";
            string tmpMsg = "";
            int iExecuteMode = 1;
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();
            int iSplitMode = 0;

            bool _bTest = false;
            bool _DBConnect = true;
            int _retrytime = 0;
            bool _retry = false;
            string tmpDataSource = "";
            string tmpConnectString = "";
            string tmpDatabase = "";
            string tmpAutoDisconn = "";
            DBTool _dbTool;

            try
            {
                tmpDataSource = string.Format("{0}:{1}/{2}", _configuration["DBconnect:Oracle:ip"], _configuration["DBconnect:Oracle:port"], _configuration["DBconnect:Oracle:Name"]);
                tmpConnectString = string.Format(_configuration["DBconnect:Oracle:connectionString"], tmpDataSource, _configuration["DBconnect:Oracle:user"], _configuration["DBconnect:Oracle:pwd"]);
                tmpDatabase = _configuration["DBConnect:Oracle:providerName"];
                tmpAutoDisconn = _configuration["DBConnect:Oracle:autoDisconnect"];
                _dbTool = _dbTool2;

                if (_bTest)
                {
                    _dbTool.DisConnectDB(out tmpMsg);
                    _logger.Error(tmpMsg);
                }

                while (_DBConnect)
                {
                    try
                    {
                        _retrytime++;

                        sql = string.Format(_BaseDataService.SelectRTDDefaultSet("SPLITMODE"));
                        dtTemp = _dbTool.GetDataTable(sql);
                        _DBConnect = false;
                        break;
                    }
                    catch (Exception ex)
                    {
                        tmpMsg = "";
                        _dbTool.DisConnectDB(out tmpMsg);
                        _retry = true;
                    }

                    if (_retry)
                    {
                        try
                        {
                            tmpMsg = "";
                            _dbTool = new DBTool(tmpConnectString, tmpDatabase, tmpAutoDisconn, out tmpMsg);
                            _dbTool._dblogger = _logger;

                            if (_retrytime > 3)
                            {
                                _DBConnect = false;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Thread.Sleep(300);
                        }
                    }
                }

                //sql = string.Format(_BaseDataService.SelectRTDDefaultSet("SPLITMODE"));
                //dtTemp = _dbTool.GetDataTable(sql);
                if (dtTemp.Rows.Count > 0)
                {
                    iSplitMode = dtTemp.Rows[0]["paramvalue"].ToString().Equals("0") ? 0 : int.Parse(dtTemp.Rows[0]["paramvalue"].ToString());
                }
                else
                    iSplitMode = 0;

                //// 查詢資料
                switch (iSplitMode)
                {
                    case 1:
                        sql = _BaseDataService.QueryQuantityByCarrier(value.CarrierId);
                        break;
                    case 2:
                        sql = _BaseDataService.QueryQuantity2ByCarrier(value.CarrierId);
                        break;
                    case 0:
                    default:
                        sql = _BaseDataService.QueryQuantityByCarrier(value.CarrierId);
                        break;
                }
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count <= 0)
                {
                    foo = new ApiResultQuantityInfo()
                    {
                        State = "NG",
                        CarrierId = value.CarrierId,
                        LotId = "",
                        Total = 0,
                        Quantity = 0,
                        ErrorCode = "",
                        Message = "Carrier Id is not exist."
                    };
                }
                else
                {
                    int _total_qty = 0;
                    int _quantity = 0;
                    switch (iSplitMode)
                    {
                        case 1:
                            _total_qty = int.Parse(dt.Rows[0]["quantity"].ToString());
                            _quantity = int.Parse(dt.Rows[0]["quantity"].ToString());
                            if (!dt.Rows[0]["total_qty"].ToString().Equals(dt.Rows[0]["quantity"].ToString()))
                            {
                                sql = _BaseDataService.UpdateLotinfoTotalQty(dt.Rows[0]["lot_id"].ToString(), _quantity);
                                _dbTool.SQLExec(sql, out tmpMsg, true);
                            }
                            break;
                        case 2:
                        case 0:
                        default:
                            _total_qty = int.Parse(dt.Rows[0]["total_qty"].ToString());
                            _quantity = int.Parse(dt.Rows[0]["quantity"].ToString());
                            break;
                    }

                    foo = new ApiResultQuantityInfo()
                    {
                        State = "OK",
                        CarrierId = value.CarrierId,
                        LotId = dt.Rows[0]["lot_id"].ToString(),
                        Total = _total_qty,
                        Quantity = _quantity
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new ApiResultQuantityInfo()
                {
                    State = "NG",
                    CarrierId = value.CarrierId,
                    LotId = "",
                    Total = 0,
                    Quantity = 0,
                    ErrorCode = "",
                    Message = ex.Message
                };
            }
            finally
            {
                //_logger.LogInformation(string.Format("Info :{0}", value.CarrierID));
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return foo;
        }
        public class clsCarrier
        {
            public string CarrierId { get; set; }
        }
        public class ApiResultQuantityInfo
        {
            public string State { get; set; }
            public string CarrierId { get; set; }
            public string LotId { get; set; }
            public int Total { get; set; }
            public int Quantity { get; set; }
            public string ErrorCode { get; set; }
            public string Message { get; set; }
        }
    }
}
