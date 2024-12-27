using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
            DataTable dt2 = null;
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

            string _table = "";
            string _carrierId = "";
            string _lotId = "";
            string v_STAGE = "";
            string v_CUSTOMERNAME = "";
            string v_PARTID = "";
            string v_LOTTYPE = "";
            string v_AUTOMOTIVE = "";
            string v_STATE = "";
            string v_HOLDCODE = "";
            string v_TURNRATIO = "0";
            string v_EOTD = "";
            string v_HOLDREAS = "";
            string v_POTD = "";
            string v_WAFERLOT = "";
            string v_Force = "";

            int _total_qty = 0;
            int _quantity = 0;

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
                        Message = "Carrier Id is not exist.",
                        Stage = "",
                        Cust = "",
                        PartID = "",
                        LotType = "",
                        Automotive = "",
                        HoldCode = "",
                        TurnRatio = 0,
                        EOTD = "",
                        WaferLot = "",
                        HoldReas = "",
                        POTD = ""
                    };
                }
                else
                {
                    _total_qty = 0;
                    _quantity = 0;
                    _lotId = dt.Rows[0]["lot_id"].ToString();

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

                    try
                    {
                        if (v_CUSTOMERNAME.Equals(""))
                        {
                            sql = _BaseDataService.SelectTableLotInfoByLotid(_lotId);
                            dt2 = _dbTool.GetDataTable(sql);

                            if (dt2.Rows.Count > 0)
                            {
                                v_CUSTOMERNAME = dt2.Rows[0]["CUSTOMERNAME"].ToString().Equals("") ? "" : dt2.Rows[0]["CUSTOMERNAME"].ToString();
                                v_PARTID = dt2.Rows[0]["PARTID"].ToString().Equals("") ? "" : dt2.Rows[0]["PARTID"].ToString();
                                v_LOTTYPE = dt2.Rows[0]["LOTTYPE"].ToString().Equals("") ? "" : dt2.Rows[0]["LOTTYPE"].ToString();
                            }
                        }

                        sql = _BaseDataService.QueryErackInfoByLotID(_configuration["eRackDisplayInfo:contained"], _lotId);
                        dtTemp = _dbTool.GetDataTable(sql);

                        if (dtTemp.Rows.Count > 0)
                        {
                            v_STAGE = dtTemp.Rows[0]["STAGE"].ToString().Equals("") ? "" : dtTemp.Rows[0]["STAGE"].ToString();
                            v_AUTOMOTIVE = dtTemp.Rows[0]["AUTOMOTIVE"].ToString().Equals("") ? "" : dtTemp.Rows[0]["AUTOMOTIVE"].ToString();
                            v_STATE = dtTemp.Rows[0]["STATE"].ToString().Equals("") ? "" : dtTemp.Rows[0]["STATE"].ToString();
                            v_HOLDCODE = dtTemp.Rows[0]["HOLDCODE"].ToString().Equals("") ? "" : dtTemp.Rows[0]["HOLDCODE"].ToString();
                            v_TURNRATIO = dtTemp.Rows[0]["TURNRATIO"].ToString().Equals("") ? "0" : dtTemp.Rows[0]["TURNRATIO"].ToString();
                            v_EOTD = dtTemp.Rows[0]["EOTD"].ToString().Equals("") ? "" : dtTemp.Rows[0]["EOTD"].ToString();
                            v_POTD = dtTemp.Rows[0]["POTD"].ToString().Equals("") ? "" : dtTemp.Rows[0]["POTD"].ToString();
                            v_HOLDREAS = dtTemp.Rows[0]["HoldReas"].ToString().Equals("") ? "" : dtTemp.Rows[0]["HoldReas"].ToString();
                            v_WAFERLOT = dtTemp.Rows[0]["waferlotid"].ToString().Equals("") ? "" : dtTemp.Rows[0]["waferlotid"].ToString();
                            v_Force = "false";
                        }
                    }
                    catch (Exception ex)
                    {
                        tmpMsg = string.Format("[AutoSentInfoUpdate: Column Issue. {0}]", ex.Message);
                        _logger.Debug(tmpMsg);
                    }
                }

                foo = new ApiResultQuantityInfo()
                {
                    State = "OK",
                    CarrierId = value.CarrierId,
                    LotId = dt.Rows[0]["lot_id"].ToString().Equals("") ? "" : dt.Rows[0]["lot_id"].ToString(),
                    Total = _total_qty,
                    Quantity = _quantity,
                    ErrorCode = "",
                    Message = "",
                    Stage = v_STAGE,
                    Cust = v_CUSTOMERNAME,
                    PartID = v_PARTID,
                    LotType = v_LOTTYPE,
                    Automotive = v_AUTOMOTIVE,
                    HoldCode = v_HOLDCODE,
                    TurnRatio = v_TURNRATIO.Equals("") ? 0 : float.Parse(v_TURNRATIO),
                    EOTD = v_EOTD,
                    WaferLot = v_WAFERLOT,
                    HoldReas = v_HOLDREAS,
                    POTD = v_POTD
                };
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
                    Message = ex.Message,
                    Stage = "",
                    Cust = "",
                    PartID = "",
                    LotType = "",
                    Automotive = "",
                    HoldCode = "",
                    TurnRatio = 0,
                    EOTD = "",
                    WaferLot = "",
                    HoldReas = "",
                    POTD = ""
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
            public string Stage { get; set; }
            public string Cust { get; set; }
            public string PartID { get; set; }
            public string LotType { get; set; }
            public string Automotive { get; set; }
            public string HoldCode { get; set; }
            public float TurnRatio { get; set; }
            public string EOTD { get; set; }
            public string WaferLot { get; set; }
            public string HoldReas { get; set; }
            public string POTD { get; set; }
        }
    }
}
