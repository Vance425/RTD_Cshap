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
using System.Threading;

namespace RTDWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]

    public class CarrierLocationUpdateController : BasicController
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly DBTool _dbTool2;
        private readonly ConcurrentQueue<EventQueue> _eventQueue;
        private readonly IFunctionService _functionService;
        private readonly List<DBTool> _lstDBSession;

        public CarrierLocationUpdateController(ILogger logger, IConfiguration configuration, List<DBTool> lstDBSession, ConcurrentQueue<EventQueue> eventQueue, IFunctionService functionService)
        {
            _logger = logger;
            _configuration = configuration;
            //_dbTool = (DBTool)lstDBSession[1];
            _eventQueue = eventQueue;
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
        public APIResult Post([FromBody] CarrierLocationUpdate value)
        {
            APIResult foo;
            IBaseDataService _BaseDataService = new BaseDataService();

            string funcName = "CarrierLocationUpdate";
            string tmpMsg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataTable dtTemp2 = null;
            DataRow[] dr = null;
            string sql = "";
            string _table = "";
            string _lotid = "";
            EventQueue _eventQ = new EventQueue();
            _eventQ.EventName = funcName;
            string _custdevice = "";
            bool _dbConnect = false;

            bool _bTest = false;
            bool _DBConnect = true;
            int _retrytime = 0;
            bool _retry = false;
            string tmpDataSource = "";
            string tmpConnectString = "";
            string tmpDatabase = "";
            string tmpAutoDisconn = "";
            DBTool _dbTool;

            foo = new APIResult();
            try
            {
                var jsonStringName = new JavaScriptSerializer();
                var jsonStringResult = jsonStringName.Serialize(value);
                _logger.Debug(string.Format("Function:{0}, Received:[{1}]", funcName, jsonStringResult));

                //_eventQ.EventObject = value;
                //_eventQueue.Enqueue(_eventQ);

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

                        dt = _dbTool.GetDataTable(_BaseDataService.SelectTableCarrierTransferByCarrier(value.CarrierID.Trim()));
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
                                _logger.Error(string.Format("Event[{0}] retry fail. [{1}]", funcName, tmpMsg));
                                break;
                            }
                        }
                        catch (Exception ex)
                        { Thread.Sleep(300); }
                    }
                }

                //// 查詢資料
                //dt = _dbTool.GetDataTable(_BaseDataService.SelectTableCarrierTransferByCarrier(value.CarrierID.Trim()));
                dr = dt.Select();
                if (dr.Length <= 0)
                {
                    foo.Success = false;
                    foo.State = "NG";
                    foo.Message = "Carrier is not exist.";
                    //return foo;
                    goto leave;
                }
                else
                {
                    /*
                        1.	Load Carrier
                        2.	Unload Carrier
                    */
                    dr = dt.Select();
                    
                    if (!value.CarrierID.Equals(""))
                    {
                        string strLocate = "";
                        string strPort = "0";
                        if (value.Location.Contains("_LP"))
                        {
                            strLocate = value.Location.Split("_LP")[0].ToString();
                            strPort = value.Location.Split("_LP")[1].ToString();
                        }
                        else
                        {
                            strLocate = value.Location;
                            strPort = "1";
                        }
                        string lstMetalRing = _configuration["CarrierTypeSet:MetalRing"];
                        int haveMetalRing = 0;
                        if (lstMetalRing.Contains(strLocate))
                            haveMetalRing = 1;
                        else
                            haveMetalRing = 0;

                        sql = String.Format(_BaseDataService.CarrierLocateReset(value, haveMetalRing));
                        _dbTool.SQLExec(sql, out tmpMsg, true);

                        sql = String.Format(_BaseDataService.UpdateTableCarrierTransfer(value, haveMetalRing));
                        _dbTool.SQLExec(sql, out tmpMsg, true);

                        sql = _BaseDataService.QueryLotInfoByCarrier(value.CarrierID);
                        dtTemp = _dbTool.GetDataTable(sql);
                        if(dtTemp.Rows.Count > 0)
                        {
                            _lotid = dtTemp.Rows[0]["lot_id"].ToString();
                            _custdevice = dtTemp.Rows[0]["custdevice"].ToString();

                            if (_functionService.TriggerCarrierInfoUpdate(_dbTool, _configuration, _logger, dtTemp.Rows[0]["lot_id"].ToString()))
                            { }

                            //20240219 delay unload time to last
                            /*
                            if (dtTemp.Rows[0]["isLock"].ToString().Equals("1"))
                            {
                                sql = String.Format(_BaseDataService.UnLockLotInfoWhenReady(dtTemp.Rows[0]["lot_id"].ToString()));
                                _dbTool.SQLExec(sql, out tmpMsg, true);
                            }
                            */

                            string lotAge = "";
                            dtTemp2 = _dbTool.GetDataTable(_BaseDataService.QueryDataByLotid(_functionService.GetExtenalTables(_configuration, "SyncExtenalData", "AdsInfo"), dtTemp.Rows[0]["lot_id"].ToString()));
                            if (dtTemp2.Rows.Count > 0)
                            {
                                try { 
                                    lotAge = dtTemp2.Rows[0]["lot_Age"].ToString();

                                    sql = _BaseDataService.UpdateLotAgeByLotID(dtTemp.Rows[0]["lot_id"].ToString(), lotAge);
                                    _dbTool.SQLExec(sql, out tmpMsg, true);
                                }
                                catch (Exception ex) { }

                                try { 
                                    if(!dtTemp2.Rows[0]["recipe"].ToString().Equals(_custdevice))
                                    {
                                        sql = _BaseDataService.UpdateCustDeviceByLotID(dtTemp.Rows[0]["lot_id"].ToString(), dtTemp2.Rows[0]["recipe"].ToString());
                                        _dbTool.SQLExec(sql, out tmpMsg, true);
                                    }
                                }
                                catch(Exception ex) { }
                            }

                            /*
                            try {
                                //Sync potd 
                                _table = _configuration["eRackDisplayInfo:Table"];

                                if (value.LocationType.Equals("ERACK"))
                                {
                                    sql = string.Format(_BaseDataService.QueryeRackDisplayBylotid(_table, _lotid));
                                    dt = _dbTool.GetDataTable(sql);

                                    if (dt.Rows.Count > 0)
                                    {
                                        string _potd = "";
                                        _potd = dt.Rows[0]["eotd"].ToString();

                                        if(!_potd.Equals(dtTemp.Rows[0]["lot_id"].ToString()))
                                        {
                                            sql = _BaseDataService.UpdateEotdToLotInfo(_lotid, _potd);
                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                        }
                                    }
                                }
                            }
                            catch(Exception ex) { }
                            */

                            try {
#if DEBUG
                                _table = _configuration["SyncExtenalData:AdsInfo:Table:Debug"];
#else
_table = _configuration["SyncExtenalData:AdsInfo:Table:Prod"];
#endif

                                //ERACK, MR, STOCKER
                                if (value.LocationType.Equals("ERACK") || value.LocationType.Equals("MR") || value.LocationType.Equals("STOCKER"))
                                {

                                    sql = string.Format(_BaseDataService.QueryQtimeOfOnlineCarrier(_table, _lotid));
                                    dt = _dbTool.GetDataTable(sql);

                                    if (dt.Rows.Count > 0)
                                    {
                                        float iqtime1 = 0;
                                        float iqtime = 0;
                                        string pkgfullname1 = "";
                                        string pkgfullname = "";
                                        string stage1 = "";
                                        string stage = "";
                                        string lotRecipe = "";
                                        string adsRecipe = "";

                                        iqtime1 = float.Parse(dt.Rows[0]["qtime1"].ToString());
                                        iqtime = float.Parse(dt.Rows[0]["qtime"].ToString());
                                        pkgfullname1 = dt.Rows[0]["pkgfullname1"].ToString();
                                        pkgfullname = dt.Rows[0]["pkgfullname"].ToString();
                                        stage1 = dt.Rows[0]["stage1"].ToString();
                                        stage = dt.Rows[0]["stage"].ToString();
                                        lotRecipe = dt.Rows[0]["recipe2"].ToString();
                                        adsRecipe = dt.Rows[0]["recipe"].ToString();

                                        if (!iqtime.Equals(iqtime1))
                                        {
                                            if (iqtime < iqtime1)
                                            {
                                                sql = _BaseDataService.UpdateQtimeToLotInfo(_lotid, 0, "");
                                                _dbTool.SQLExec(sql, out tmpMsg, true);
                                            }
                                            else
                                            {
                                                sql = _BaseDataService.UpdateQtimeToLotInfo(_lotid, iqtime, "");
                                                _dbTool.SQLExec(sql, out tmpMsg, true);
                                            }
                                        }

                                        if (!pkgfullname.Equals(pkgfullname1))
                                        {
                                            sql = _BaseDataService.UpdateStageToLotInfo(_lotid, "", pkgfullname);
                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                        }

                                        if (!stage.Equals(stage1))
                                        {
                                            sql = _BaseDataService.UpdateStageToLotInfo(_lotid, stage, "");
                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                        }

                                        if (value.LocationType.Equals("ERACK") || value.LocationType.Equals("STOCKER"))
                                        {
                                            sql = _BaseDataService.EQPListReset(_lotid);
                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                        }

                                        if (!lotRecipe.Equals(adsRecipe))
                                        {
                                            if (!adsRecipe.Equals(""))
                                            {
                                                sql = _BaseDataService.UpdateRecipeToLotInfo(_lotid, adsRecipe);
                                                _dbTool.SQLExec(sql, out tmpMsg, true);
                                            }
                                        }
                                    }
                                }
                            }
                            catch(Exception ex) {
                                sql = _BaseDataService.UpdateQtimeToLotInfo(_lotid, 0, "");
                                _dbTool.SQLExec(sql, out tmpMsg, true);
                            }

                            //20240219 move to here
                            if (dtTemp.Rows[0]["isLock"].ToString().Equals("1"))
                            {
                                sql = String.Format(_BaseDataService.UnLockLotInfoWhenReady(dtTemp.Rows[0]["lot_id"].ToString()));
                                _dbTool.SQLExec(sql, out tmpMsg, true);
                            }
                        }

                        foo.Success = true;
                        foo.State = "OK";
                        foo.Message = "";
                        //foo.LotId = dr[0]["lot_id"] is not null ? dr[0]["lot_id"].ToString() : "";
                        //foo.ProcessCode = dr[0]["stage"] is not null ? dr[0]["stage"].ToString() : "";
                    }
                    else
                    {
                        foo.Success = false;
                        foo.State = "NG";
                        foo.Message = "State not correct.";
                        //return foo;
                        goto leave;
                    }

                    if (foo.State == "OK")
                    {
                        _eventQ.EventObject = value;
                        _eventQueue.Enqueue(_eventQ);
                    }
                } 

                foo.Success = true;
                foo.State = "OK";
                foo.Message = "";

            leave:
                if (_retry)
                {
                    tmpMsg = "";
                    _dbTool.DisConnectDB(out tmpMsg);
                }
            }
            catch (Exception ex)
            {
                foo.Success = false;
                foo.State = "NG";
                foo.Message = String.Format("Unknow issue. [{0}] Exception: {1}", funcName, ex.Message);
                _logger.Debug(foo.Message);
                //return foo;
            }

            return foo;
        }
    }
}
