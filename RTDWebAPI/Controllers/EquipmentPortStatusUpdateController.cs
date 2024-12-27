using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nancy.Json;
using NLog;
using RTDDAC;
using RTDWebAPI.Commons.DataRelated.SQLSentence;
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

    public class EquipmentPortStatusUpdateController : BasicController
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
        private readonly DBTool _dbTool2;
        private readonly ConcurrentQueue<EventQueue> _eventQueue;
        private readonly List<DBTool> _lstDBSession;

        public EquipmentPortStatusUpdateController(ILogger logger, IConfiguration configuration, List<DBTool> lstDBSession, ConcurrentQueue<EventQueue> eventQueue)
        {
            _logger = logger;
            _configuration = configuration;
            //_dbTool = (DBTool)lstDBSession[1];
            _eventQueue = eventQueue;
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
        public APIResult Post([FromBody] AEIPortInfo value)
        {
            APIResult foo = new();
            IBaseDataService _BaseDataService = new BaseDataService();
            string funcName = "EquipmentPortStatusUpdate";
            string tmpMsg = "";

            EventQueue _eventQ = new EventQueue();
            _eventQ.EventName = funcName;
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string _lastLot = "";
            Boolean _isDisabled = false;

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
                var jsonStringName = new JavaScriptSerializer();
                var jsonStringResult = jsonStringName.Serialize(value);
                _logger.Debug(string.Format("Function:{0}, Received:[{1}]", funcName, jsonStringResult));

                tmpDataSource = string.Format("{0}:{1}/{2}", _configuration["DBconnect:Oracle:ip"], _configuration["DBconnect:Oracle:port"], _configuration["DBconnect:Oracle:Name"]);
                tmpConnectString = string.Format(_configuration["DBconnect:Oracle:connectionString"], tmpDataSource, _configuration["DBconnect:Oracle:user"], _configuration["DBconnect:Oracle:pwd"]);
                tmpDatabase = _configuration["DBConnect:Oracle:providerName"];
                tmpAutoDisconn = _configuration["DBConnect:Oracle:autoDisconnect"];
                _dbTool = _dbTool2;

                if (value.PortID.Equals(""))
                {
                    foo.Success = false;
                    foo.State = "NG";
                    foo.Message = "Port ID not correct.";
                    return foo;
                }


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

                        sql = string.Format(_BaseDataService.SelectTableEQP_Port_SetByPortId(value.PortID));
                        dt = _dbTool.GetDataTable(sql);
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

                /// 查詢資料
                //sql = string.Format(_BaseDataService.SelectTableEQP_Port_SetByPortId(value.PortID));
                //dt = _dbTool.GetDataTable(sql);
                if (dt.Rows.Count > 0)
                {
                    _isDisabled = dt.Rows[0]["DISABLE"].ToString().Equals("1") ? true : false;
                }

                string tCondition = string.Format("Port_Seq = {0}", value.PortID);
                dr = dt.Select("");

                if (dr.Length <= 0)
                {
                    foo.Success = false;
                    foo.State = "NG";
                    foo.Message = "PortID is not exist.";
                    //return foo;
                    goto leave;
                }
                else
                {
                    if (_isDisabled)
                    {
                        //Port is disabled. Do not update port state.
                        foo.Success = false;
                        foo.State = "OK";
                        foo.Message = "";
                        //return foo;
                        goto leave;
                    }
                    else
                    {
                        if (dr[0]["Port_State"].ToString().Equals(value.PortTransferState))
                        {
                            foo.Success = false;
                            foo.State = "NG";
                            foo.Message = "Port stae no change.";
                        }
                        else
                        {
                            string EquipID = dr[0]["EQUIPID"].ToString();
                            string PortSeq = dr[0]["Port_Seq"].ToString();
                            string PortState = value.PortTransferState.ToString();
                            _dbTool.SQLExec(_BaseDataService.UpdateTableEQP_Port_Set(EquipID, PortSeq, PortState), out tmpMsg, true);

                            //20230413V1.0 Added by Vance
                            if (PortState.Equals("1"))
                            {
                                sql = string.Format(_BaseDataService.QueryLastLotFromEqpPort(EquipID, PortSeq));
                                dtTemp = _dbTool.GetDataTable(sql);
                                if (dtTemp.Rows.Count > 0)
                                {
                                    _lastLot = dtTemp.Rows[0]["lastLot"].ToString();

                                    _dbTool.SQLExec(_BaseDataService.UpdateLastLotIDtoEQPPortSet(EquipID, PortSeq, _lastLot), out tmpMsg, true);
                                }
                            }

                            int haveMetalRing = 0;
                            if (!value.CarrierID.Equals(""))
                            {
                                //更新Carrier 位置
                                sql = string.Format(_BaseDataService.GetCarrierByLocate(EquipID, int.Parse(PortSeq)));
                                dtTemp = _dbTool.GetDataTable(sql);

                                if (dtTemp.Rows.Count > 0)
                                {
                                    /** 最新的Carrier: value.CarrierID, 舊的Carrier: dtTemp.Rows[0]["carrier_id"].ToString()*/
                                    if (dtTemp.Rows[0]["carrier_id"].ToString().Equals(value.CarrierID))
                                    {
                                        //不更新
                                    }
                                    else
                                    {
                                        CarrierLocationUpdate oCarrierLoc = new CarrierLocationUpdate();
                                        oCarrierLoc.CarrierID = value.CarrierID;
                                        oCarrierLoc.TransferState = PortState;
                                        oCarrierLoc.Location = value.PortID;
                                        oCarrierLoc.LocationType = "EQP";

                                        //清除舊的Carrier Locate
                                        sql = String.Format(_BaseDataService.CarrierLocateReset(oCarrierLoc, haveMetalRing));
                                        _dbTool.SQLExec(sql, out tmpMsg, true);

                                        //更新新的Carrier Locate
                                        sql = String.Format(_BaseDataService.UpdateTableCarrierTransfer(oCarrierLoc, haveMetalRing));
                                        _dbTool.SQLExec(sql, out tmpMsg, true);

                                    }
                                }
                                else
                                {
                                    CarrierLocationUpdate oCarrierLoc = new CarrierLocationUpdate();
                                    oCarrierLoc.CarrierID = value.CarrierID;
                                    oCarrierLoc.TransferState = PortState;
                                    oCarrierLoc.Location = value.PortID;
                                    oCarrierLoc.LocationType = "EQP";

                                    //更新新的Carrier Locate
                                    sql = String.Format(_BaseDataService.UpdateTableCarrierTransfer(oCarrierLoc, haveMetalRing));
                                    _dbTool.SQLExec(sql, out tmpMsg, true);

                                }

                            }
                            else
                            {
                                CarrierLocationUpdate oCarrierLoc = new CarrierLocationUpdate();
                                //更新Carrier 位置
                                sql = string.Format(_BaseDataService.GetCarrierByLocate(EquipID, int.Parse(PortSeq)));
                                dtTemp = _dbTool.GetDataTable(sql);

                                if (dtTemp.Rows.Count > 0)
                                {
                                    oCarrierLoc = new CarrierLocationUpdate();
                                    oCarrierLoc.CarrierID = value.CarrierID;
                                    oCarrierLoc.TransferState = PortState;
                                    oCarrierLoc.Location = value.PortID;
                                    oCarrierLoc.LocationType = "EQP";

                                    if (!PortState.Equals("3") && !PortState.Equals("5"))
                                    {
                                        //清除舊的Carrier Locate
                                        sql = String.Format(_BaseDataService.CarrierLocateReset(oCarrierLoc, haveMetalRing));
                                        _dbTool.SQLExec(sql, out tmpMsg, true);
                                    }
                                    else
                                    {
                                        if(value.CarrierID.Equals(""))
                                        {
                                            if (!value.LotID.Equals(""))
                                            {
                                                ///Carrier is empty
                                                ///lotid not empty, can use lotid to get last carrierid.
                                                sql = string.Format(_BaseDataService.SelectTableCarrierAssociate3ByLotid(value.LotID));
                                                dtTemp = _dbTool.GetDataTable(sql);

                                                if (dtTemp.Rows.Count > 0)
                                                {
                                                    oCarrierLoc = new CarrierLocationUpdate();
                                                    oCarrierLoc.CarrierID = dtTemp.Rows[0]["carrier_id"].ToString();
                                                    oCarrierLoc.TransferState = PortState;
                                                    oCarrierLoc.Location = value.PortID;
                                                    oCarrierLoc.LocationType = "EQP";

                                                    //清除舊的Carrier Locate
                                                    sql = String.Format(_BaseDataService.CarrierLocateReset(oCarrierLoc, haveMetalRing));
                                                    _dbTool.SQLExec(sql, out tmpMsg, true);

                                                    //更新新的Carrier Locate
                                                    sql = String.Format(_BaseDataService.UpdateTableCarrierTransfer(oCarrierLoc, haveMetalRing));
                                                    _dbTool.SQLExec(sql, out tmpMsg, true);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (!value.LotID.Equals(""))
                                    {
                                        ///Carrier is empty
                                        ///lotid not empty, can use lotid to get last carrierid.
                                        sql = string.Format(_BaseDataService.SelectTableCarrierAssociate3ByLotid(value.LotID));
                                        dtTemp = _dbTool.GetDataTable(sql);

                                        if (dtTemp.Rows.Count > 0)
                                        {
                                            oCarrierLoc = new CarrierLocationUpdate();
                                            oCarrierLoc.CarrierID = dtTemp.Rows[0]["carrier_id"].ToString();
                                            oCarrierLoc.TransferState = PortState;
                                            oCarrierLoc.Location = value.PortID;
                                            oCarrierLoc.LocationType = "EQP";

                                            //清除舊的Carrier Locate
                                            sql = String.Format(_BaseDataService.CarrierLocateReset(oCarrierLoc, haveMetalRing));
                                            _dbTool.SQLExec(sql, out tmpMsg, true);

                                            //更新新的Carrier Locate
                                            sql = String.Format(_BaseDataService.UpdateTableCarrierTransfer(oCarrierLoc, haveMetalRing));
                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                        }
                                    }
                                }
                            }

                            foo.Success = true;
                            foo.State = "OK";
                            foo.Message = "Port ID has be changed.";

                        }
                    }
                }

                if (foo.State == "OK")
                {
                    _eventQ.EventObject = value;
                    _eventQueue.Enqueue(_eventQ);
                }
                else
                {
                    //Do Nothing
                }
                //_eventQ.EventObject = value;
                //_eventQueue.Enqueue(_eventQ);

                foo.Success = true;
                foo.State = "OK";
                foo.Message = String.Format("");
                _logger.Debug(foo.Message);

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