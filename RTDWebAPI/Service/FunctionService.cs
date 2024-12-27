using Microsoft.Extensions.Configuration;
using Nancy.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RTDDAC;
using RTDWebAPI.APP;
using RTDWebAPI.Commons.Method.Mail;
using RTDWebAPI.Commons.Method.Tools;
using RTDWebAPI.Commons.Method.WSClient;
using RTDWebAPI.Interface;
using RTDWebAPI.Models;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.ServiceModel;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace RTDWebAPI.Service
{
    public class FunctionService : IFunctionService
    {
        public IBaseDataService _BaseDataService = new BaseDataService();

        public bool AutoCheckEquipmentStatus(DBTool _dbTool, ConcurrentQueue<EventQueue> _evtQueue)
        {

            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            string tmpMsg = "";
            //_evtQueue = new ConcurrentQueue<EventQueue>();
            EventQueue oEventQ = new EventQueue();
            oEventQ.EventName = "AutoCheckEquipmentStatus";

            bool bResult = false;
            try
            {
                NormalTransferModel normalTransfer = new NormalTransferModel();
                //20230413V1.1 Added by Vance
                sql = string.Format(_BaseDataService.SelectTableLotInfoOfReady());
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    DataTable dt2 = null;
                    List<string> lstEqp = new List<string>();
                    foreach (DataRow dr2 in dt.Rows)
                    {
                        normalTransfer = new NormalTransferModel();
                        sql = string.Format(_BaseDataService.SelectTableCarrierType(dr2["lotID"].ToString()));
                        dt2 = _dbTool.GetDataTable(sql);

                        if (dt2.Rows.Count <= 0)
                        {
                            //不在貨架上面, Lock 先排除
                            sql = _BaseDataService.LockLotInfoWhenReady(dr2["lotID"].ToString());
                            _dbTool.SQLExec(sql, out tmpMsg, true);
                            continue;
                        }
                        normalTransfer.LotID = dr2["lotID"].ToString();
                        normalTransfer.CarrierID = dt2.Rows[0]["Carrier_ID"].ToString();

                        if (!normalTransfer.CarrierID.Equals(""))
                        {
                            //己派送的, Lock
                            oEventQ.EventObject = normalTransfer;
                            _evtQueue.Enqueue(oEventQ);

                            sql = _BaseDataService.LockLotInfoWhenReady(dr2["lotID"].ToString());
                            _dbTool.SQLExec(sql, out tmpMsg, true);
                            return true;
                        }
                    }
                }
                else
                {
                    sql = _BaseDataService.UnLockAllLotInfoWhenReadyandLock();
                    _dbTool.SQLExec(sql, out tmpMsg, true);
                }
            }
            catch (Exception ex)
            {
                tmpMsg = String.Format("[Exception][{0}]: {1}", oEventQ.EventName, ex.Message);
            }
            finally
            {
                if(dt != null)
                    dt.Dispose();
            }
            dt = null;
            dr = null;

            return bResult;
        }
        public bool AbnormalyEquipmentStatus(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, bool DebugMode, ConcurrentQueue<EventQueue> _evtQueue, out List<NormalTransferModel> _lstNormalTransfer)
        {

            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            EventQueue oEventQ = new EventQueue();
            oEventQ.EventName = "AbnormalyEquipmentStatus";
            _lstNormalTransfer = new List<NormalTransferModel>();

            bool bResult = false;

            string tableOrder = "";
            string _keyOfEnv = "";
            string _keyRTDEnv = "";

            try
            {
                _keyRTDEnv = _configuration["RTDEnvironment:type"];
                _keyOfEnv = string.Format("RTDEnvironment:commandsTable:{0}", _keyRTDEnv);
                //"RTDEnvironment:commandsTable:PROD"
                tableOrder = _configuration[_keyOfEnv] is null ? "workinprocess_sch" : _configuration[_keyOfEnv];

                NormalTransferModel normalTransfer = new NormalTransferModel();
                sql = string.Format(_BaseDataService.SelectEqpStatusWaittoUnload());
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    DataTable dt2 = null;

                    foreach (DataRow dr2 in dt.Rows)
                    {
                        oEventQ = new EventQueue();
                        oEventQ.EventName = "AbnormalyEquipmentStatus";

                        normalTransfer = new NormalTransferModel();

                        sql = string.Format(_BaseDataService.SelectTableWorkInProcessSchByEquipPort(dr2["EQUIPID"].ToString(), dr2["PORT_ID"].ToString(), tableOrder));
                        dt2 = _dbTool.GetDataTable(sql);

                        if (dt2.Rows.Count > 0)
                        {
                            if (dt2.Rows[0]["SOURCE"].ToString().Equals("*"))
                                continue;
                            else
                            {
                                DataTable dt3;
                                sql = string.Format(_BaseDataService.SelectTableWorkInProcessSchByPortId(dr2["PORT_ID"].ToString(), tableOrder));
                                dt3 = _dbTool.GetDataTable(sql);
                                if (dt3.Rows.Count > 0)
                                    continue;
                                dt3 = null;
                            }
                        }


                        normalTransfer.EquipmentID = dr2["EQUIPID"].ToString();
                        normalTransfer.PortModel = dr2["PORT_MODEL"].ToString();

                        dt2 = null;
                        sql = string.Format(_BaseDataService.QueryCarrierByLocate(dr2["EQUIPID"].ToString(), _configuration["eRackDisplayInfo:Table"]));
                        dt2 = _dbTool.GetDataTable(sql);

                        if (dt2.Rows.Count > 0)
                        {
                            normalTransfer.CarrierID = dt2.Rows[0]["Carrier_ID"].ToString().Equals("") ? "*" : dt2.Rows[0]["Carrier_ID"].ToString();
                            normalTransfer.LotID = dt2.Rows[0]["lot_id"].ToString().Equals("") ? "*" : dt2.Rows[0]["lot_id"].ToString();
                        }
                        else
                        {
                            normalTransfer.CarrierID = "";
                            normalTransfer.LotID = "";
                        }

                        if (!normalTransfer.EquipmentID.Equals(""))
                        {
                            if (DebugMode)
                            {
                                _logger.Debug(string.Format("[EqpStatusWaittoUnload] {0} / {1} / {2}", normalTransfer.EquipmentID, normalTransfer.CarrierID, normalTransfer.LotID));
                            }

                            _lstNormalTransfer.Add(normalTransfer);
                            oEventQ.EventObject = normalTransfer;
                            _evtQueue.Enqueue(oEventQ);
                        }
                    }
                }

                sql = string.Format(_BaseDataService.SelectEqpStatusIsDownOutPortWaittoUnload());
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    DataTable dt2 = null;

                    foreach (DataRow dr2 in dt.Rows)
                    {
                        oEventQ = new EventQueue();
                        oEventQ.EventName = "AbnormalyEquipmentStatus";

                        normalTransfer = new NormalTransferModel();

                        //sql = string.Format(_BaseDataService.SelectTableWorkInProcessSchByEquip(dr2["EQUIPID"].ToString()));
                        sql = string.Format(_BaseDataService.SelectTableWorkInProcessSchByEquipPort(dr2["EQUIPID"].ToString(), dr2["PORT_ID"].ToString(), tableOrder));
                        dt2 = _dbTool.GetDataTable(sql);

                        if (dt2.Rows.Count > 0)
                        {
                            if (dt2.Rows[0]["SOURCE"].ToString().Equals("*"))
                                continue;
                            else
                            {
                                DataTable dt3;
                                sql = string.Format(_BaseDataService.SelectTableWorkInProcessSchByPortId(dr2["PORT_ID"].ToString(), tableOrder));
                                dt3 = _dbTool.GetDataTable(sql);
                                if (dt3.Rows.Count > 0)
                                    continue;
                                dt3 = null;
                            }
                        }


                        normalTransfer.EquipmentID = dr2["EQUIPID"].ToString();
                        normalTransfer.PortModel = dr2["PORT_MODEL"].ToString();

                        dt2 = null;
                        sql = string.Format(_BaseDataService.QueryCarrierByLocate(dr2["EQUIPID"].ToString(), _configuration["eRackDisplayInfo:Table"]));
                        dt2 = _dbTool.GetDataTable(sql);

                        if (dt2.Rows.Count > 0)
                        {
                            normalTransfer.CarrierID = dt2.Rows[0]["Carrier_ID"].ToString().Equals("") ? "*" : dt2.Rows[0]["Carrier_ID"].ToString();
                            normalTransfer.LotID = dt2.Rows[0]["lot_id"].ToString().Equals("") ? "*" : dt2.Rows[0]["lot_id"].ToString();
                        }
                        else
                        {
                            normalTransfer.CarrierID = "";
                            normalTransfer.LotID = "";
                        }

                        if (!normalTransfer.EquipmentID.Equals(""))
                        {
                            if (DebugMode)
                            {
                                _logger.Debug(string.Format("[EqpStatusIsDownOutPortWaittoUnload] {0} / {1} / {2}", normalTransfer.EquipmentID, normalTransfer.CarrierID, normalTransfer.LotID));
                            }
                            _lstNormalTransfer.Add(normalTransfer);
                            oEventQ.EventObject = normalTransfer;
                            _evtQueue.Enqueue(oEventQ);
                        }
                    }
                }

                //Ready to Load時, 除model 為IO機台外, 其它的 IN的狀態不可為 0 (out of service)
                sql = string.Format(_BaseDataService.SelectEqpStatusReadytoload());
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    DataTable dt2 = null;

                    foreach (DataRow dr2 in dt.Rows)
                    {

                        oEventQ = new EventQueue();
                        oEventQ.EventName = "AbnormalyEquipmentStatus";


                        normalTransfer = new NormalTransferModel();

                        sql = string.Format(_BaseDataService.SelectTableWorkInProcessSchByEquipPort(dr2["EQUIPID"].ToString(), dr2["PORT_ID"].ToString(), tableOrder));
                        dt2 = _dbTool.GetDataTable(sql);

                        if (dt2.Rows.Count > 0)
                        {
                            if (dt2.Rows[0]["DEST"].ToString().Equals(dr2["PORT_ID"].ToString()))
                                continue;
                            else
                            {
                                //DataTable dt3;
                                //sql = string.Format(_BaseDataService.SelectTableWorkInProcessSchByPortId(dr2["PORT_ID"].ToString()));
                                //dt3 = _dbTool.GetDataTable(sql);
                                //if (dt3.Rows.Count > 0)
                                //    continue;
                                //dt3 = null;
                            }
                        }

                        if (dr2["PORT_MODEL"].ToString().Equals(""))
                            continue;

                        normalTransfer.EquipmentID = dr2["EQUIPID"].ToString();
                        normalTransfer.PortModel = dr2["PORT_MODEL"].ToString();

                        dt2 = null;
                        sql = string.Format(_BaseDataService.QueryCarrierByLocateType("ERACK", dr2["EQUIPID"].ToString(), "semi_int.actl_ewlberack_vw@semi_int"));
                        dt2 = _dbTool.GetDataTable(sql);

                        if (dt2.Rows.Count > 0)
                        {
                            normalTransfer.CarrierID = dt2.Rows[0]["Carrier_ID"].ToString().Equals("") ? "*" : dt2.Rows[0]["Carrier_ID"].ToString();
                            normalTransfer.LotID = dt2.Rows[0]["lot_id"].ToString().Equals("") ? "*" : dt2.Rows[0]["lot_id"].ToString();
                        }
                        else
                        {
                            normalTransfer.CarrierID = "";
                            normalTransfer.LotID = "";
                        }

                        if (!normalTransfer.EquipmentID.Equals(""))
                        {
                            if (DebugMode)
                            {
                                _logger.Debug(string.Format("[EqpStatusReadytoload] {0} / {1} / {2}", normalTransfer.EquipmentID, normalTransfer.CarrierID, normalTransfer.LotID));
                            }

                            _lstNormalTransfer.Add(normalTransfer);
                            //_lstNormalTransfer.AddRange(normalTransfer);
                            oEventQ.EventObject = normalTransfer;
                            _evtQueue.Enqueue(oEventQ);
                        }
                    }
                }
            }
            catch (Exception ex)
            { }
            finally
            {
                if (dt != null)
                    dt.Dispose(); 
            }
            dt = null;
            dr = null;

            return bResult;
        }
        public bool CheckLotInfo(DBTool _dbTool, IConfiguration _configuration, ILogger _logger)
        {
            DataTable dt = null;
            DataTable dt2 = null;
            DataTable dtTemp = null;
            DataTable dtTemp2 = null;
            DataRow[] dr = null;
            string sql = "";
            string tmpMsg = "";

            bool bResult = false;
            try
            {
                sql = string.Format(_BaseDataService.SelectTableCheckLotInfo(GetExtenalTables(_configuration, "SyncExtenalData", "AdsInfo")));
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    string sql2 = "";
                    string sqlMsg = "";
                    tmpMsg = "Sync Ads Data for LotInfo: LotID='{0}', RTD State from [{1}] to [{2}]";
                    string _tmpOriState = "";
                    string _tmpNewState = "";
                    string _lotID = "";

                    foreach (DataRow dr2 in dt.Rows)
                    {
                        sql2 = "";
                        sqlMsg = "";

                        try {
                            _lotID = dr2["lotid"] is null ? "--lotID--" : dr2["lotid"].ToString();
                            _tmpOriState = dr2["OriState"] is null ? "--Ori--" : dr2["OriState"].ToString();
                            _tmpNewState = dr2["State"] is null ? "--New--" : dr2["State"].ToString();
                        } catch (Exception ex) { }

                        if (dr2["State"].ToString().Equals("New"))
                        {
                            if (dr2["OriState"].ToString().Equals("INIT"))
                            { }
                            else if (dr2["OriState"].ToString().Equals("NONE"))
                            {
                                //增加
                                sql2 = string.Format(_BaseDataService.InsertTableLotInfo(GetExtenalTables(_configuration, "SyncExtenalData", "AdsInfo"), dr2["lotid"].ToString()));
                                _dbTool.SQLExec(sql2, out sqlMsg, true);

                                if (TriggerCarrierInfoUpdate(_dbTool, _configuration, _logger, dr2["lotid"].ToString()))
                                {
                                    //Send InfoUpdate
                                    _logger.Info(string.Format(tmpMsg, _lotID, _tmpOriState, _tmpNewState));
                                }
                            }
                            else if (!dr2["OriState"].ToString().Equals("INIT"))
                            {
                                //歸零
                                sql2 = string.Format(_BaseDataService.UpdateTableLotInfoReset(dr2["lotid"].ToString()));
                                _dbTool.SQLExec(sql2, out sqlMsg, true);

                                _logger.Info(string.Format(tmpMsg, _lotID, _tmpOriState, "Reset"));
                            }
                            else
                            {
                                //增加
                                sql2 = string.Format(_BaseDataService.InsertTableLotInfo(GetExtenalTables(_configuration, "SyncExtenalData", "AdsInfo"), dr2["lotid"].ToString()));
                                _dbTool.SQLExec(sql2, out sqlMsg, true);

                                if (TriggerCarrierInfoUpdate(_dbTool, _configuration, _logger, dr2["lotid"].ToString()))
                                {
                                    //Send InfoUpdate
                                    _logger.Info(string.Format(tmpMsg, _lotID, _tmpOriState, _tmpNewState));
                                }
                            }
                        }
                        else if (dr2["State"].ToString().Equals("Remove"))
                        {
                            //if (!dr2["OriState"].ToString().Equals("INIT"))
                            //{
                            //    //歸零
                            //    sql2 = string.Format(_BaseDataService.UpdateTableLotInfoReset(dr2["lotid"].ToString()));
                            //    _dbTool.SQLExec(sql2, out sqlMsg, true);
                            //}
                            //else 
                            if (!dr2["OriState"].ToString().Equals("COMPLETED"))
                            {
                                //Update State to DELETED
                                sql2 = string.Format(_BaseDataService.UpdateTableLotInfoState(dr2["lotid"].ToString(), "DELETED"));
                                _dbTool.SQLExec(sql2, out sqlMsg, true);

                                _logger.Info(string.Format(tmpMsg, _lotID, _tmpOriState, "Remove"));
                            }
                        }
                        else if (dr2["State"].ToString().Equals("DELETED"))
                        {
                            if (dr2["OriState"].ToString().Equals("INIT"))
                            {
                                //歸零
                                sql2 = string.Format(_BaseDataService.UpdateTableLotInfoReset(dr2["lotid"].ToString()));
                                _dbTool.SQLExec(sql2, out sqlMsg, true);

                                _logger.Info(string.Format(tmpMsg, _lotID, _tmpOriState, "Reset"));
                            }
                            else if (dr2["OriState"].ToString().Equals("DELETED"))
                            {
                                if (!dr2["lastmodify_dt"].ToString().Equals(""))
                                {
                                    sql2 = string.Format(_BaseDataService.QueryDataByLotid(GetExtenalTables(_configuration, "SyncExtenalData", "AdsInfo"), dr2["lotid"].ToString()));
                                    dtTemp = _dbTool.GetDataTable(sql2);

                                    if(dtTemp.Rows.Count > 0)
                                        _dbTool.SQLExec(_BaseDataService.SyncNextStageOfLot(GetExtenalTables(_configuration, "SyncExtenalData", "AdsInfo"), dr2["lotid"].ToString()), out sqlMsg, true);

                                    _logger.Info(string.Format(tmpMsg, _lotID, _tmpOriState, "Remove"));
                                }
                            }
                            else
                            { }
                        }
                        else if (dr2["State"].ToString().Equals("INIT"))
                        {
                            if (!dr2["lastmodify_dt"].ToString().Equals(""))
                            {
                                if (TimerTool("day", dr2["lastmodify_dt"].ToString()) > 1)
                                    _dbTool.SQLExec(_BaseDataService.UpdateTableLastModifyByLot(dr2["lotid"].ToString()), out sqlMsg, true);
                            }
                        }
                        else if (dr2["State"].ToString().Equals("NEXT"))
                        {
                            if (!dr2["lastmodify_dt"].ToString().Equals(""))
                            {
                                sql2 = string.Format(_BaseDataService.QueryDataByLotid("lot_info", dr2["lotid"].ToString()));
                                dtTemp = _dbTool.GetDataTable(sql2);

                                if(dtTemp.Rows.Count > 0)
                                {
                                    if(int.Parse(dtTemp.Rows[0]["priority"].ToString()) > 70)
                                    {
                                        _dbTool.SQLExec(_BaseDataService.SyncNextStageOfLotNoPriority(GetExtenalTables(_configuration, "SyncExtenalData", "AdsInfo"), dr2["lotid"].ToString()), out sqlMsg, true);
                                        _logger.Info(string.Format("Special priority: [{0}][{1}][{2}][{3}]", _lotID, dtTemp.Rows[0]["priority"].ToString(), _tmpOriState, _tmpNewState));
                                    }
                                    else
                                    {
                                        _dbTool.SQLExec(_BaseDataService.SyncNextStageOfLot(GetExtenalTables(_configuration, "SyncExtenalData", "AdsInfo"), dr2["lotid"].ToString()), out sqlMsg, true);    
                                    }

                                    if (TriggerCarrierInfoUpdate(_dbTool, _configuration, _logger, dr2["lotid"].ToString()))
                                    {
                                        //Send InfoUpdate
                                        _logger.Info(string.Format(tmpMsg, _lotID, _tmpOriState, _tmpNewState));
                                    }
                                }
                            }
                        }
                        else if (dr2["State"].ToString().Equals("WAIT"))
                        {
                            if (!dr2["lotid"].ToString().Equals(""))
                            {
                                sql = _BaseDataService.SelectTableLotInfoByLotid(dr2["lotid"].ToString());
                                dtTemp = _dbTool.GetDataTable(sql);

                                if(dtTemp.Rows.Count > 0)
                                {
                                    foreach(DataRow drT in dtTemp.Rows)
                                    {
                                        if(drT["state"].ToString().Equals("HOLD"))
                                        {
                                            sql = _BaseDataService.QueryDataByLotid(GetExtenalTables(_configuration, "SyncExtenalData", "AdsInfo"), dr2["lotid"].ToString());
                                            dtTemp2 = _dbTool.GetDataTable(sql);
                                            if(dtTemp2.Rows.Count > 0)
                                            {
                                                if(dtTemp2.Rows[0]["state"].ToString().Equals("WAIT"))
                                                {
                                                    sql2 = string.Format(_BaseDataService.UpdateLotinfoState(dr2["lotid"].ToString(), "WAIT"));
                                                    _dbTool.SQLExec(sql2, out sqlMsg, true);
                                                }
                                            }
                                        }
                                    }
                                }
                                //if (TriggerCarrierInfoUpdate(_dbTool, _configuration, _logger, dr2["lotid"].ToString()))
                                //{
                                //Send InfoUpdate
                                //}
                            }
                        }
                        else if (dr2["State"].ToString().Equals("Nothing"))
                        {
                            //Do Nothing
                        }
                    }

                    bResult = true;
                }

                dt = null;
                sql = string.Format(_BaseDataService.SelectTableCheckLotInfoNoData(GetExtenalTables(_configuration, "SyncExtenalData", "AdsInfo")));
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr2 in dt.Rows)
                    {
                        sql = string.Format(_BaseDataService.SelectTableLotInfoByLotid(dr2["lotid"].ToString()));
                        dtTemp = _dbTool.GetDataTable(sql);

                        if (dtTemp.Rows.Count > 0)
                        {
                            if (TimerTool("day", dtTemp.Rows[0]["lastmodify_dt"].ToString()) <= 1)
                                continue;
                        }

                        string sql2 = "";
                        string sqlMsg = "";
                        if (dr2["State"].ToString().Equals("Remove"))
                        {
                            if (!dr2["OriState"].ToString().Equals("COMPLETED"))
                            {
                                //Update State to DELETED
                                sql2 = string.Format(_BaseDataService.UpdateTableLotInfoState(dr2["lotid"].ToString(), "DELETED"));
                                _dbTool.SQLExec(sql2, out sqlMsg, true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                bResult = false;
                throw;
            }
            finally
            {
                if (dt != null)
                    dt.Dispose();
                if (dt2 != null)
                    dt2.Dispose();
                if (dtTemp != null)
                    dtTemp.Dispose();
            }
            dt = null;
            dt2 = null;
            dtTemp = null;
            dr = null;

            return bResult;
        }
        public bool SyncEquipmentData(DBTool _dbTool, IConfiguration _configuration, ILogger _logger)
        {
            DataTable dt = null;
            DataTable dt2 = null;
            DataRow[] dr = null;
            string sql = "";
            string tmpMsg = "";
            string _lastModifyDt = "";
            string _machineState = "";
            string _currStatus = "";
            string _downState = "";
            string _eqpid = "";

            bool bResult = false;
            try
            {
                //有新增Equipment, 直接同步
                sql = string.Format(_BaseDataService.InsertTableEqpStatus());
                _dbTool.SQLExec(sql, out tmpMsg, true);

                //自動同步Equipment Model, Port Number (先建立對照表, 依對照表同步)


                //自動檢查最新的Equipment Status, 如太長時間未更新, 觸發Equipment status sync
                sql = _BaseDataService.QueryEqpStatusNotSame(GetExtenalTables(_configuration, "SyncExtenalData", "RTSEQSTATE"));
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow drTemp in dt.Rows)
                    {
                        try
                        {
                            _eqpid = drTemp["equipid"].ToString();
                            _machineState = drTemp["machine_state_rts"].ToString();
                            _currStatus = drTemp["curr_status_rts"].ToString();
                            _downState = drTemp["down_state_rts"].ToString();
                            _lastModifyDt = drTemp["lastModify_dt"].ToString();

                            if (TimerTool("minutes", _lastModifyDt) > 10)
                            {
                                sql = _BaseDataService.UpdateEquipMachineStatus(_machineState, _currStatus, _downState, _eqpid);
                                _logger.Info(string.Format("Sync EQP State [{0}]", sql));
                                _dbTool.SQLExec(sql, out tmpMsg, true);

                                if(!tmpMsg.Equals(""))
                                    _logger.Debug(tmpMsg);
                            }
                        }
                        catch (Exception ex)
                        {
                            tmpMsg = String.Format("[Exception] {0}", ex.Message);
                            _logger.Debug(tmpMsg);
                        }
                    }
                }

                bResult = true;

            }
            catch (Exception ex)
            {
                bResult = false;
                throw;
            }

            return bResult;
        }
        public bool CheckLotCarrierAssociate(DBTool _dbTool, ILogger _logger)
        {

            DataTable dt = null;
            DataTable dt2 = null;
            DataRow[] dr = null;
            string sql = "";
            string tmpMsg = "";

            bool bResult = false;

            try
            {

                //Carrier Status is Init
                sql = string.Format(_BaseDataService.SelectTableLotInfo());
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    string sql2 = "";
                    string sqlMsg = "";

                    foreach (DataRow dr2 in dt.Rows)
                    {
                        sql2 = _BaseDataService.SelectTableCarrierAssociateByLotid(dr2["lotid"].ToString());
                        dt2 = _dbTool.GetDataTable(sql2);

                        if (dt2.Rows.Count > 0)
                        {
                            //表示此Lot有可用的Carrier 
                            string CarrierID = dt2.Rows[0]["carrier_id"].ToString();

                            //SelectTableCarrierAssociate 
                            //先有lot才會有關聯
                            //if (TimerTool("minutes", dr2["lastmodify_dt"].ToString()) > 3) { }
                            try
                            {
                                if (dr2["carrier_asso"].ToString().Equals("N"))
                                {
                                    if (dt2 is not null)
                                    {
                                        if (int.Parse(dt2.Rows[0]["ENABLE"].ToString()).Equals(1))
                                        {
                                            sql2 = string.Format(_BaseDataService.UpdateTableLotInfoSetCarrierAssociateByLotid(dr2["lotid"].ToString()));
                                            _dbTool.SQLExec(sql2, out sqlMsg, true);

                                            tmpMsg = string.Format("Bind Success. Lotid is [{0}]", dr2["lotid"].ToString());
                                            _logger.Debug(tmpMsg);
                                        }
                                    }
                                    else
                                    {
                                        tmpMsg = string.Format("carrier_asso is N, dt2 is null");
                                        _logger.Debug(tmpMsg);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                tmpMsg = string.Format("Bind Got Exception: {0}", ex.Message);
                                _logger.Debug(tmpMsg);
                            }
                        }
                        else
                        { //表示此Lot沒有可用的Carrier
                            if (dr2["carrier_asso"].ToString().Equals("Y"))
                            {
                                try
                                {
                                    if (dt2 is not null)
                                    {
                                        if (dt2.Rows.Count <= 0)
                                        {
                                            sql2 = string.Format(_BaseDataService.UpdateTableLotInfoSetCarrierAssociate2ByLotid(dr2["lotid"].ToString()));
                                            _dbTool.SQLExec(sql2, out sqlMsg, true);

                                            tmpMsg = string.Format("Unbind Success. Lotid is [{0}]", dr2["lotid"].ToString());
                                            _logger.Debug(tmpMsg);
                                        }
                                    }
                                    else
                                    {
                                        tmpMsg = string.Format("carrier_asso is Y, dt2 is null");
                                        _logger.Debug(tmpMsg);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    tmpMsg = string.Format("Unbind Got Exception: {0}", ex.Message);
                                    _logger.Debug(tmpMsg);
                                }
                            }
                        }
                    }
                }


                //當Carrier Transfer與Carrier Associate不同步時, 補齊Carrier Transfer！
                sql = string.Format(_BaseDataService.SelectCarrierAssociateIsTrue());
                dt = _dbTool.GetDataTable(sql);
                if (dt.Rows.Count > 0)
                {
                    tmpMsg = "";
                    foreach (DataRow dr3 in dt.Rows)
                    {
                        dt2 = _dbTool.GetDataTable(_BaseDataService.SelectTableCarrierAssociate2ByLotid(dr3["lotid"].ToString()));
                        dr = dt2.Select("Enable is not null");
                        if (dr.Length <= 0)
                        {
                            _dbTool.SQLExec(_BaseDataService.InsertInfoCarrierTransfer(dt2.Rows[0]["carrier_id"].ToString()), out tmpMsg, true);

                        }

                    }
                }

                bResult = true;
            }
            catch (Exception ex)
            {
                bResult = false;
            }
            finally
            {
                if (dt != null)
                    dt.Dispose();
                if (dt2 != null)
                    dt2.Dispose();
            }
            dt = null;
            dt2 = null;
            dr = null;

            return bResult;
        }
        public bool CheckLotEquipmentAssociate(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, ConcurrentQueue<EventQueue> _evtQueue)
        {

            DataTable dt = null;
            DataTable dtTemp = null;
            DataTable dtCopy = null;
            string sql = "";
            string tmpMsg = "";
            bool bResult = false;
            bool bReflush = false;
            //ConcurrentQueue<EventQueue>  evtQueue = new ConcurrentQueue<EventQueue>();
            bool byStage = true;
            DateTime _startTime;
            DateTime _stopTime;
            TimeSpan _tSpan;
            int _schSeqMode = 0;
            bool _removeLot = false;

            try
            {
                if(byStage)
                    sql = string.Format(_BaseDataService.SchSeqReflush(byStage));
                else
                    sql = string.Format(_BaseDataService.SchSeqReflush(byStage));

                dt = _dbTool.GetDataTable(sql);
                /***當Customer , Stage 為群組, 當超過1組以上, 則需要重新整理Sch Seq */
                if (dt.Rows.Count > 1)
                {
                    bReflush = true;
                }
               else
                {
                    bReflush = false;
                }
                /*
                if (dt.Rows.Count > 0)
                {
                    if (dt.Rows[0]["allCount"] is not null)
                    {
                        if (int.Parse(dt.Rows[0]["allCount"].ToString()) >= 1)
                            bReflush = true;
                    }
                    else
                    {
                        bReflush = true;
                    }
                }
                */


                if (!bReflush)
                {
                    sql = _BaseDataService.ReflushWhenSeqZeroStateWait();
                    dt = _dbTool.GetDataTable(sql);
                    if (dt.Rows.Count > 0)
                    {
                        bReflush = true;
                    }
                }

                sql = string.Format(_BaseDataService.ReflushProcessLotInfo(byStage));
                dt = _dbTool.GetDataTable(sql);
                int iSchSeq = 0;
                string sCustomer = "";
                string sLastStage = "";
                int isLock = 0;
                string tmpTable;
                bool _change = false;
                DataRow[] drTemp;

                _schSeqMode = _configuration["RTDEnvironment:ScheduleSeqMode"] is null ? 0 : int.Parse(_configuration["RTDEnvironment:ScheduleSeqMode"].ToString());
                _removeLot = _configuration["SyncExtenalData:AdsInfo:RemoveWhenNotExistADS"] is null ? false : _configuration["SyncExtenalData:AdsInfo:RemoveWhenNotExistADS"].ToString().Equals("True") ? true : false;

                if (dt.Rows.Count > 0)
                {
                    _startTime = DateTime.Now;
                    foreach (DataRow dr2 in dt.Rows)
                    {
                        try {

                            //Sync ads lot
#if DEBUG
                            tmpTable = _configuration["SyncExtenalData:AdsInfo:Table:Debug"];
#else
                        tmpTable = _configuration["SyncExtenalData:AdsInfo:Table:Prod"];
#endif

                            sql = string.Format(_BaseDataService.GetDataFromTableByLot(tmpTable, "lotid", dr2["lotid"].ToString()));
                            dtTemp = _dbTool.GetDataTable(sql);

                            if (dtTemp.Rows.Count > 0)
                            {
                                try
                                {
                                    dr2["adslot"] = dtTemp.Rows[0]["lotid"].ToString().Equals("") ? "Null" : dtTemp.Rows[0]["lotid"].ToString();
                                    dr2["stageage"] = dtTemp.Rows[0]["stageage"].ToString();
                                    dr2["qtime2"] = dtTemp.Rows[0]["qtime"].ToString();
                                }
                                catch (Exception ex) { }
                            }
                            else
                            {
                                dr2["adslot"] = "Null";
                                
                                if (_removeLot)
                                {
                                    _dbTool.SQLExec(_BaseDataService.RemoveLotFromLotInfo(dr2["lotid"].ToString()), out tmpMsg, true);
                                    _logger.Info(string.Format("Remove lot [{0}] from lot_info, cause the lotid not exist ads table. ", dr2["lotid"].ToString()));
                                }
                            }

                            //Sync TurnRatio

#if DEBUG
                            tmpTable = "ads_info";
#else
                        tmpTable = _configuration["eRackDisplayInfo:Table"];
#endif

                            sql = string.Format(_BaseDataService.GetDataFromTableByLot(tmpTable, "lotid", dr2["lotid"].ToString()));
                            dtTemp = _dbTool.GetDataTable(sql);

                            if (dtTemp.Rows.Count > 0)
                            {
                                if (!dr2["turnratio2"].ToString().Equals(dtTemp.Rows[0]["turnratio"].ToString()))
                                {
#if DEBUG
                                    dr2["turnratio3"] = dtTemp.Rows[0]["turnratio"].ToString().Equals("") ? dr2["turnratio2"].ToString() : dtTemp.Rows[0]["turnratio"].ToString().Equals("0") ? dr2["turnratio2"].ToString() : dtTemp.Rows[0]["turnratio"].ToString();
#else
                    dr2["turnratio3"] = dtTemp.Rows[0]["turnratio"].ToString().Equals("") ? "0" : dtTemp.Rows[0]["turnratio"].ToString();
#endif
                                    _dbTool.SQLExec(_BaseDataService.UpdateTurnRatioToLotInfo(dr2["lotid"].ToString(), dtTemp.Rows[0]["turnratio"].ToString()), out tmpMsg, true);
                                    _change = true;
                                }

                                if (!dr2["enddate"].ToString().Equals(dtTemp.Rows[0]["eotd"].ToString()))
                                {
                                    if (!dtTemp.Rows[0]["eotd"].ToString().Equals("")){
                                        dr2["eotd"] = dtTemp.Rows[0]["eotd"].ToString();
                                        _dbTool.SQLExec(_BaseDataService.UpdateEotdToLotInfo(dr2["lotid"].ToString(), dtTemp.Rows[0]["eotd"].ToString()), out tmpMsg, true);
                                        _change = true;
                                    }
                                }
                            }
                        }
                        catch(Exception ex) { }
                    }

                    drTemp = dt.Select("adslot<>'Null'");
                    if (drTemp.Length > 0)
                    {
                        DataView dv = drTemp.CopyToDataTable().DefaultView;
                        switch (_schSeqMode) {
                            case 0:
                                dv.Sort = "stage, rtd_state, priority desc , turnratio3 desc";
                                dtCopy = dv.ToTable();
                                break;
                            case 1:
                                dv.Sort = "priority desc , turnratio3 desc";
                                dtCopy = dv.ToTable();
                                break;
                            default:
                                dv.Sort = "stage, rtd_state, priority desc , lot_age desc";
                                dtCopy = dv.ToTable();
                                break;
                        }
                    }

                    if (_change)
                    {
                        _stopTime = DateTime.Now;
                        _tSpan = _stopTime.Subtract(_startTime);
                        _logger.Info(string.Format("Reflush Schedule Sequence Timespan: [{0}], Start:[{1}] - End:[{2}]", _tSpan.Seconds, _startTime, _stopTime));
                    }
                }

                if (dtCopy is not null)
                {
                    if (dtCopy.Rows.Count > 0)
                    {
                        string sql2 = "";
                        string sqlMsg = "";
                        sCustomer = dt.Rows[0]["CustomerName"].ToString();
                        sLastStage = dt.Rows[0]["Stage"].ToString();

                        if (GetLockState(_dbTool))
                            return bResult;

                        foreach (DataRow dr2 in dtCopy.Rows)
                        {
                            string CarrierID = GetCarrierByLotID(_dbTool, dr2["lotid"].ToString());

                            EventQueue _evtQ = new EventQueue();
                            _evtQ.EventName = "LotEquipmentAssociateUpdate";
                            NormalTransferModel _transferModel = new NormalTransferModel();

                            if (dr2["Rtd_State"].ToString().Equals("INIT"))
                            {
                                sql2 = string.Format(_BaseDataService.UpdateLotInfoSchSeqByLotid(dr2["lotid"].ToString(), 0));
                                _dbTool.SQLExec(sql2, out sqlMsg, true);
                                continue;
                            }

                            if (!dr2["Stage"].ToString().Equals(""))
                            {

                                if (bReflush)
                                {
                                    switch (_schSeqMode)
                                    {
                                        case 2:

                                            #region 以Stage 排列
                                            //以Stage 排列
                                            if (dr2["Stage"].ToString().Equals(sLastStage))
                                                iSchSeq += 1;
                                            else
                                            {
                                                //換站點 Stage
                                                iSchSeq = 1;
                                                sLastStage = dr2["Stage"].ToString();
                                            }
                                            _dbTool.SQLExec(_BaseDataService.ResetSchseqByModel(iSchSeq.ToString(), string.Format("{0},{1}", _schSeqMode.ToString(), dr2["Stage"].ToString())), out sqlMsg, true);
                                            #endregion
                                            break;

                                        case 1:

                                            //JCET eWLB 客制 by Turnratio
                                            iSchSeq += 1;
                                            _dbTool.SQLExec(_BaseDataService.ResetSchseqByModel(iSchSeq.ToString(), string.Format("{0}", _schSeqMode.ToString())), out sqlMsg, true);
                                            break;
                                        case 0:
                                        default:

                                            #region 加入Customer 排列
                                            //加入Customer 排列
                                            if (dr2["CustomerName"].ToString().Equals(sCustomer))
                                            {
                                                if (dr2["Stage"].ToString().Equals(sLastStage))
                                                    iSchSeq += 1;
                                                else
                                                {
                                                    //換站點 Stage
                                                    iSchSeq = 1;
                                                    sLastStage = dr2["Stage"].ToString();
                                                }
                                            }
                                            else
                                            {
                                                //換客戶
                                                iSchSeq = 1;
                                                sCustomer = dr2["CustomerName"].ToString();
                                                sLastStage = dr2["Stage"].ToString();
                                            }
                                            _dbTool.SQLExec(_BaseDataService.ResetSchseqByModel(iSchSeq.ToString(), string.Format("{0},{1},{2}", _schSeqMode.ToString(), dr2["Stage"].ToString(), dr2["CustomerName"].ToString())), out sqlMsg, true);
                                            #endregion
                                            break;
                                    }

                                    #region 20241106 marked this code
                                    //if (byStage)
                                    //{
                                    //    //以Stage 排列
                                    //    if (dr2["Stage"].ToString().Equals(sLastStage))
                                    //        iSchSeq += 1;
                                    //    else
                                    //    {
                                    //        //換站點 Stage
                                    //        iSchSeq = 1;
                                    //        sLastStage = dr2["Stage"].ToString();
                                    //    }
                                    //}
                                    //else
                                    //{
                                    //    //加入Customer 排列
                                    //    if (dr2["CustomerName"].ToString().Equals(sCustomer))
                                    //    {
                                    //        if (dr2["Stage"].ToString().Equals(sLastStage))
                                    //            iSchSeq += 1;
                                    //        else
                                    //        {
                                    //            //換站點 Stage
                                    //            iSchSeq = 1;
                                    //            sLastStage = dr2["Stage"].ToString();
                                    //        }
                                    //    }
                                    //    else
                                    //    {
                                    //        //換客戶
                                    //        iSchSeq = 1;
                                    //        sCustomer = dr2["CustomerName"].ToString();
                                    //        sLastStage = dr2["Stage"].ToString();
                                    //    }
                                    //}
                                    #endregion

                                    //update lot_info sch_seq
                                    sql2 = string.Format(_BaseDataService.UpdateLotInfoSchSeqByLotid(dr2["lotid"].ToString(), iSchSeq));
                                    _dbTool.SQLExec(sql2, out sqlMsg, true);

                                    string tmp = "";
                                    if (!sqlMsg.Equals(""))
                                         tmp = sqlMsg;
                                }
                            }

                            bool bagain = false;
                            if (dr2["EQUIP_ASSO"].ToString().Equals("N"))
                            {
                                if (TimerTool("minutes", dr2["lastmodify_dt"].ToString()) <= 1)
                                { continue; }
                                else
                                    bagain = true;

                                if (bagain)
                                {
                                    //EQUIP_ASSO 為 No, 立即執行檢查
                                    _transferModel.CarrierID = CarrierID;
                                    _transferModel.LotID = dr2["lotid"].ToString();
                                    _evtQ.EventObject = _transferModel;
                                    _evtQueue.Enqueue(_evtQ);
                                }
                            }
                            else
                            {
                                //Equipment Assoicate 為 Yes, 每5分鐘再次檢查一次
                                if (!dr2["EQUIPLIST"].ToString().Equals(""))
                                {
                                    if (TimerTool("minutes", dr2["lastmodify_dt"].ToString()) <= 5)
                                    { continue; }
                                    else
                                        bagain = true;
                                }
                                else
                                {
                                    if (TimerTool("minutes", dr2["lastmodify_dt"].ToString()) <= 1)
                                    { continue; }
                                    else
                                        bagain = true;
                                }

                                if (bagain)
                                {
                                    _transferModel.CarrierID = CarrierID;
                                    _transferModel.LotID = dr2["lotid"].ToString();
                                    _evtQ.EventObject = _transferModel;
                                    _evtQueue.Enqueue(_evtQ);
                                }
                            }
                        }

                    }
                }

                bResult = true;
            }
            catch (Exception ex)
            {
                bResult = false;
            }
            finally
            {
                //if (dt != null)
                  //  dt.Dispose();
            }
            dt = null;

            return bResult;
        }
        public bool UpdateEquipmentAssociateToReady(DBTool _dbTool, ConcurrentQueue<EventQueue> _evtQueue)
        {

            DataTable dt = null;
            string sql = "";

            bool bResult = false;
            //_evtQueue = new ConcurrentQueue<EventQueue>();

            try
            {
                sql = string.Format(_BaseDataService.SelectTableProcessLotInfo());
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    DataTable dt2 = null;
                    DataRow dr3 = null;
                    string sql2 = "";
                    string sqlMsg = "";

                    foreach (DataRow dr2 in dt.Rows)
                    {
                        string CarrierID = GetCarrierByLotID(_dbTool, dr2["lotid"].ToString());

                        EventQueue _evtQ = new EventQueue();
                        _evtQ.EventName = "UpdateEquipmentAssociateToReady";
                        NormalTransferModel _transferModel = new NormalTransferModel();

                        if (dr2["CARRIER_ASSO"].ToString().Equals("Y") && dr2["EQUIP_ASSO"].ToString().Equals("Y") && !dr2["STATE"].ToString().Equals("READY"))
                        {
                            sql2 = string.Format(_BaseDataService.UpdateTableLotInfoToReadyByLotid(dr2["lotid"].ToString()));
                            _dbTool.SQLExec(sql2, out sqlMsg, true);
                        }
                    }

                }

                bResult = true;
            }
            catch (Exception ex)
            {
                bResult = false;
            }
            finally
            {
                if (dt != null)
                    dt.Dispose();
            }
            dt = null;

            return bResult;
        }
        /// <summary>
        /// MCS-Lite : Sent Command to MCS-Lite
        /// </summary>
        /// <param name="_configuration"></param>
        /// <param name="_logger"></param>
        /// <returns></returns>
        /// 
        public string GetCarrierByLotID(DBTool _dbTool, string lotid)
        {
            DataTable dt = null;
            string sql = "";
            string strResult = "";
            try
            {
                sql = string.Format(_BaseDataService.SelectTableCarrierAssociateByLotid(lotid));
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    DataRow dr = dt.Rows[0];
                    strResult = dr["carrier_id"].ToString();
                }
            }
            catch (Exception ex)
            {
                strResult = ex.Message;
            }
            finally
            {
                if (dt != null)
                    dt.Dispose(); 
            }
            dt = null;

            return strResult;
        }
        public string GetCarrierByPortId(DBTool _dbTool, string _portId)
        {
            DataTable dt = null;
            string sql = "";
            string strResult = "";
            string tmpLocate = "";
            string tmpPortNo = "";
            int iPortNo = 0;
            try
            {
                if (_portId.Contains("_"))
                {
                    tmpLocate = _portId.Split('_')[0];
                    tmpPortNo = _portId.Split('_')[1];
                    iPortNo = !tmpPortNo.Trim().Equals("") ? int.Parse(tmpPortNo.Replace("LP", "")) : 0;
                }

                if (iPortNo > 0)
                {
                    sql = string.Format(_BaseDataService.GetCarrierByLocate(tmpLocate, iPortNo));
                    dt = _dbTool.GetDataTable(sql);

                    if (dt.Rows.Count > 0)
                    {
                        DataRow dr = dt.Rows[0];
                        strResult = dr["carrier_id"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                strResult = ex.Message;
            }
            finally
            {
                if (dt != null)
                    dt.Dispose();
            }
            dt = null;

            return strResult;
        }
        public HttpClient GetHttpClient(IConfiguration _configuration, string _tarMcsSvr)
        {
            HttpClient client = new HttpClient();
            try
            {
                string cfgPath_ip = "";
                string cfgPath_port = "";
                string cfgPath_timeSpan = "";

                if (_tarMcsSvr.Equals(""))
                {
                    cfgPath_ip = string.Format("MCS:ip");
                    cfgPath_port = string.Format("MCS:port");
                    cfgPath_timeSpan = string.Format("MCS:timeSpan");
                }
                else
                {
                    cfgPath_ip = string.Format("MCS:{0}:ip", _tarMcsSvr.Trim());
                    cfgPath_port = string.Format("MCS:{0}:port", _tarMcsSvr.Trim());
                    cfgPath_timeSpan = string.Format("MCS:{0}:timeSpan", _tarMcsSvr.Trim());
                }

                //client.BaseAddress = new Uri(string.Format("http://{0}:{1}/", _configuration["MCS:ip"], _configuration["MCS:port"]));
                //client.Timeout = TimeSpan.Parse(_configuration["MCS:timeSpan"]);
                client.BaseAddress = new Uri(string.Format("http://{0}:{1}/", _configuration[cfgPath_ip], _configuration[cfgPath_port]));
                client.Timeout = TimeSpan.FromSeconds(double.Parse(_configuration[cfgPath_timeSpan]));
                client.DefaultRequestHeaders.Accept.Clear();
            }
            catch (Exception ex)
            {

            }
            return client;
        }
        public string SendDispatchCommand(IConfiguration _configuration, string postData)
        {
            string responseHttpMsg = "";
            //建立 HttpClient //No Execute
            HttpClient client = GetHttpClient(_configuration, "");
            // 指定 authorization header
            JObject oToken = JObject.Parse(GetAuthrizationTokenfromMCS(_configuration));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oToken["token"].ToString());

            // 將 data 轉為 json
            string json = JsonConvert.SerializeObject(postData);
            // 將轉為 string 的 json 依編碼並指定 content type 存為 httpcontent
            HttpContent contentPost = new StringContent(json, Encoding.UTF8, "application/json");
            // 發出 post 並取得結果
            HttpResponseMessage response = client.PostAsync("api/command", contentPost).GetAwaiter().GetResult();
            // 將回應結果內容取出並轉為 string 再透過 linqpad 輸出
            responseHttpMsg = response.Content.ReadAsStringAsync().GetAwaiter().GetResult().ToString();

            client.Dispose();
            response.Dispose();
            return responseHttpMsg;
        }
        public bool SentDispatchCommandtoMCS(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, List<string> ListCmds)
        {
            bool bResult = false;
            string tmpState = "";
            string tmpMsg = "";
            string exceptionCmdId = "";
            string issueLotid = "";
            string _lastCmdID = "";
            string tmpCarrierId = "";

            HttpClient client;
            HttpResponseMessage response;

            string tableOrder = "";
            string _keyOfEnv = "";
            string _keyRTDEnv = "";

            try
            {
                _keyRTDEnv = _configuration["RTDEnvironment:type"];
                _keyOfEnv = string.Format("RTDEnvironment:commandsTable:{0}", _keyRTDEnv);
                //"RTDEnvironment:commandsTable:PROD"
                tableOrder = _configuration[_keyOfEnv] is null ? "workinprocess_sch" : _configuration[_keyOfEnv];

                if (ListCmds.Count > 0)
                {
                    _logger.Trace(string.Format("[SentDispatchCommandtoMCS]:{0}", "IN"));

                    //Execute 3
                    client = GetHttpClient(_configuration, "");
                    // Add an Accept header for JSON format.
                    // 為JSON格式添加一個Accept表頭
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    //Add Token
                    //JObject oToken = JObject.Parse(GetAuthrizationTokenfromMCS(_configuration));
                    //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oToken["token"].ToString());
                    response = new HttpResponseMessage();

                    DataTable dt = null;
                    DataTable dt2 = null;
                    DataTable dtTemp = null;
                    DataRow[] dr = null;
                    DataRow[] dr2 = null;
                    string sql = "";
                    bool bRetry = false;
                    _lastCmdID = "";

                    foreach (string theCmdId in ListCmds)
                    {
                        string tmpCmdType = "";
                        exceptionCmdId = theCmdId;
                        if (!_lastCmdID.Equals(theCmdId))
                            _lastCmdID = theCmdId;
                        else
                            continue;

                        _logger.Trace(string.Format("[SentDispatchCommandtoMCS]: Command ID [{0}]", theCmdId));
                        //// 查詢資料
                        ///
                        dt = _dbTool.GetDataTable(_BaseDataService.SelectTableWorkInProcessSchByCmdId(theCmdId, tableOrder));

                        if (dt.Rows.Count <= 0)
                            continue;
                        //已有其它線程正在處理

                        if(dt.Rows[0]["REPLACE"].ToString().Equals("1"))
                        {
                            if (dt.Rows.Count <= 1)
                                continue;
                        }

                        tmpCmdType = dt.Rows[0]["CMD_TYPE"] is null ? "" : dt.Rows[0]["CMD_TYPE"].ToString();

                        DateTime curDT = DateTime.Now;
                        if (int.Parse(dt.Rows[0]["ISLOCK"].ToString()).Equals(1))
                        {
                            try
                            {
                                string createDateTime = dt.Rows[0]["CREATE_DT"].ToString();
                                string lastDateTime = dt.Rows[0]["LASTMODIFY_DT"].ToString();
                                tmpCarrierId = dt.Rows[0]["CARRIERID"].ToString();
                                bool bHaveSent = dt.Rows[0]["CMD_CURRENT_STATE"].ToString().Equals("Init") ? true : false;

                                if (bHaveSent)
                                    continue;  //已送至TSC, 不主動刪除。等待TSC回傳結果！或人員自行操作刪除

                                if (!lastDateTime.Equals(""))
                                {
                                    bRetry = false;
                                    curDT = DateTime.Now;
                                    DateTime createDT = Convert.ToDateTime(createDateTime);
                                    DateTime tmpDT = Convert.ToDateTime(lastDateTime);
                                    TimeSpan minuteSpan = new TimeSpan(tmpDT.Ticks - curDT.Ticks);
                                    TimeSpan totalSpan = new TimeSpan(createDT.Ticks - curDT.Ticks);
                                    if (Math.Abs(minuteSpan.TotalMinutes) < 2)
                                    {
                                        continue;
                                    }
                                    else if (Math.Abs(minuteSpan.TotalMinutes) >= 2)
                                    {
                                        bRetry = true;
                                    }
                                    else if (Math.Abs(totalSpan.TotalMinutes) > 10)
                                    {
                                        dr = dt.Select("CMD_CURRENT_STATE not in ('Init', 'Running', 'Success')");

                                        if (dr.Length > 0)
                                        {
                                            //嘗試發送, 大於10分鐘仍未送出, 刪除
                                            _dbTool.SQLExec(_BaseDataService.UpdateTableWorkInProcessSchHisByCmdId(theCmdId), out tmpMsg, true);
                                            _dbTool.SQLExec(_BaseDataService.DeleteWorkInProcessSchByCmdId(theCmdId, tableOrder), out tmpMsg, true);

                                            if (tmpCarrierId.Equals("*") || tmpCarrierId.Equals(""))
                                            { }
                                            else
                                            {
                                                if (_dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(tmpCarrierId, false), out tmpMsg, true))
                                                { }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //Nothing
                                    }

                                    if (bRetry)
                                    {
                                        dr = dt.Select("CMD_CURRENT_STATE in ('Failed', '')");
                                        if (dr.Length > 0)
                                        {
                                            _dbTool.SQLExec(_BaseDataService.UpdateUnlockWorkInProcessSchByCmdId(theCmdId, tableOrder), out tmpMsg, true);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            { }

                            continue;
                        }
                        else
                            _dbTool.SQLExec(_BaseDataService.UpdateLockWorkInProcessSchByCmdId(theCmdId, tableOrder), out tmpMsg, true);

                        dr = dt.Select("CMD_CURRENT_STATE=''");
                        if (dr.Length <= 0)
                        {
                            sql = _BaseDataService.UpdateTableWorkInProcessSchByCmdId(" ", curDT.ToString("yyyy-M-d hhmmss"), theCmdId, tableOrder);
                            try
                            {
                                curDT = DateTime.Now;
                                dr2 = dt.Select("CMD_CURRENT_STATE='Failed'");
                                if (dr2.Length <= 0)
                                    continue;
                            }
                            catch (Exception ex)
                            {
                                curDT = DateTime.Now;
                                _dbTool.SQLExec(sql, out tmpMsg, true);
                            }
                        }
                        //Pre-Transfer
                        string tmpPriority = dt.Rows[0]["PRIORITY"].ToString();
                        _logger.Debug(string.Format("[SendTransferCommand][{0}][{1}]", tmpCmdType, tmpPriority));

                        if (!tmpCarrierId.Equals(""))
                        {
                            int iPriority = 0;
                            sql = _BaseDataService.QueryLotInfoByCarrierID(tmpCarrierId);
                            dtTemp = _dbTool.GetDataTable(sql);

                            if (dtTemp.Rows.Count > 0)
                            {
                                iPriority = dtTemp.Rows[0]["PRIORITY"] is null ? 0 : int.Parse(dtTemp.Rows[0]["PRIORITY"].ToString());
                            }

                            if (iPriority >= 70)
                            {
                                tmpPriority = iPriority.ToString();
                            }
                            else
                            {
                                if (tmpCmdType.ToUpper().Equals("PRE-TRANSFER"))
                                {
                                    string _tmpCarrierID = dt.Rows[0]["CARRIERID"].ToString();

                                    dt2 = _dbTool.GetDataTable(_BaseDataService.GetDispatchingPriority(_tmpCarrierID));

                                    if (dt2.Rows.Count > 0)
                                    {
                                        tmpPriority = dt2.Rows[0]["PRIORITY"].ToString().Equals("") ? "20" : dt2.Rows[0]["PRIORITY"].ToString();
                                    }
                                    else
                                        tmpPriority = "20";
                                }
                                else
                                {
                                    tmpPriority = iPriority.ToString();
                                }
                            }
                        }
                        else
                        {
                            if (tmpCmdType.ToUpper().Equals("PRE-TRANSFER"))
                            {
                                string _tmpCarrierID = dt.Rows[0]["CARRIERID"].ToString();

                                dt2 = _dbTool.GetDataTable(_BaseDataService.GetDispatchingPriority(_tmpCarrierID));

                                if (dt2.Rows.Count > 0)
                                {
                                    tmpPriority = dt2.Rows[0]["PRIORITY"].ToString().Equals("") ? "20" : dt2.Rows[0]["PRIORITY"].ToString();
                                }
                                else
                                    tmpPriority = "20";
                            }
                        }

                        dtTemp = _dbTool.GetDataTable(_BaseDataService.QueryMaxPriorityByCmdID(theCmdId));
                        if(dtTemp.Rows.Count > 0)
                        {
                            tmpPriority = dtTemp.Rows[0]["PRIORITY"].ToString().Equals("") ? tmpPriority : dtTemp.Rows[0]["PRIORITY"].ToString();
                        }

                        _logger.Debug(string.Format("[SendTransferCommand][{0}][{1}]", tmpCmdType, tmpPriority));

                        string tmp00 = "{{0}, {1}, {2}, {3}}";
                        string tmp01 = string.Format("\"CommandID\": \"{0}\"", theCmdId);
                        string tmp02 = string.Format("\"Priority\": \"{0}\"", tmpCmdType.ToUpper().Equals("PRE-TRANSFER") ? "20" : dt.Rows[0]["PRIORITY"].ToString());
                        string tmp03 = string.Format("\"Replace\": \"{0}\"", dt.Rows[0]["REPLACE"].ToString());
                        string strTransferCmd = "\"CommandID\": \"" + theCmdId + "\",\"Priority\": " + tmpPriority + ",\"Replace\": " + dt.Rows[0]["REPLACE"].ToString() + "{0}";
                        string strTransCmd = "";
                        string tmpUid = "";
                        string tmpCarrierID = "";
                        //string tmpCmdType = "";
                        string tmpLoadCmdType = "";
                        string tmpLoadCarrierID = "";
                        string tmpDest = "";
                        foreach (DataRow tmDr in dt.Rows)
                        {
                            tmpDest = "";

                            tmpUid = tmDr["UUID"].ToString();
                            tmpCmdType = tmDr["CMD_TYPE"].ToString();
                            tmpDest = tmDr["DEST"].ToString();

                            if (tmpCmdType.ToUpper().Equals("LOAD"))
                                tmpLoadCmdType = tmpCmdType;

                            //if (!strTransCmd.Equals(""))
                            //{
                            //strTransCmd = strTransCmd + ",";
                            //}
                            tmpCarrierID = tmDr["CARRIERID"].ToString().Equals("*") ? "" : tmDr["CARRIERID"].ToString();

                            if (strTransCmd.Equals(""))
                            {
                                strTransCmd = strTransCmd + "{" +
                                       "\"CarrierID\": \"" + tmpCarrierID + "\", " +
                                       "\"Source\": \"" + tmDr["SOURCE"].ToString() + "\", " +
                                       "\"Dest\": \"" + tmDr["DEST"].ToString() + "\", " +
                                       "\"LotID\": \"" + tmDr["LotID"].ToString() + "\", " +
                                       "\"Quantity\":" + tmDr["Quantity"].ToString() + ", " +
                                       "\"Total\":" + tmDr["Total"].ToString() + ", " +
                                       "\"CarrierType\": \"" + tmDr["CARRIERTYPE"].ToString() + "\"} ";
                            }
                            else
                            {
                                strTransCmd = strTransCmd + ", {" +
                                            "\"CarrierID\": \"" + tmpCarrierID + "\", " +
                                            "\"Source\": \"" + tmDr["SOURCE"].ToString() + "\", " +
                                            "\"Dest\": \"" + tmDr["DEST"].ToString() + "\", " +
                                            "\"LotID\": \"" + tmDr["LotID"].ToString() + "\", " +
                                            "\"Quantity\":" + tmDr["Quantity"].ToString() + ", " +
                                            "\"Total\":" + tmDr["Total"].ToString() + ", " +
                                            "\"CarrierType\": \"" + tmDr["CARRIERTYPE"].ToString() + "\"} ";
                            }
                        }


                        string tmp2Cmd = string.Format(", \"Transfer\": [{0}]", strTransCmd);
                        Uri gizmoUri = null;
                        string strGizmo = "";

                        string tmp3Cmd = string.Format(strTransferCmd.ToString(), tmp2Cmd);
                        string tmp04 = "{" + tmp3Cmd + "}";

                        var gizmo = JObject.Parse(tmp04);

                        _logger.Trace(string.Format("[SendTransferCommand]:{0}", tmp04));
                        response = client.PostAsJsonAsync("api/SendTransferCommand", gizmo).Result;
                        //response = client.PostAsJsonAsync("api/SendTransferCommand", tmp04).Result;

                        if (response != null)
                        {
                            //不為response, 即表示total加1 || 改成MCS回報後才統計總數
                            if (response.IsSuccessStatusCode)
                            {
                                //需等待回覆後再記錄
                                _logger.Info(string.Format("Info: SendCommand [{0}][{1}] is OK. {2}", theCmdId, tmpDest, response.StatusCode));
                                _dbTool.SQLExec(_BaseDataService.UpdateTableWorkInProcessSchHisByCmdId(theCmdId), out tmpMsg, true);
                                _dbTool.SQLExec(_BaseDataService.UpdateTableWorkInProcessSchByCmdId("Initial", DateTime.Now.ToString("yyyy-M-d hh:mm:ss"), theCmdId, tableOrder), out tmpMsg, true);
                                //新增一筆Total Record
                                //cmd_Type
                                //if (tmpLoadCmdType.ToUpper().Equals("LOAD") && !tmpLoadCarrierID.Equals(""))
                                //{
                                //    _dbTool.SQLExec(_BaseDataService.InsertRTDStatisticalRecord(curDT.ToString("yyyy-MM-dd HH:mm:ss"), theCmdId, "T"), out tmpMsg, true);
                                //}
                                //EWLB全統計
                                _dbTool.SQLExec(_BaseDataService.InsertRTDStatisticalRecord(curDT.ToString("yyyy-MM-dd HH:mm:ss"), theCmdId, "T"), out tmpMsg, true);

                                foreach (DataRow tmDr in dt.Rows)
                                {

                                    string tmpSql = _BaseDataService.SelectTableCarrierAssociateByCarrierID(tmDr["CARRIERID"].ToString());
                                    dt2 = _dbTool.GetDataTable(tmpSql);
                                    if (dt2.Rows.Count > 0)
                                    {
                                        string tmpLotid = dt2.Rows[0]["lot_id"].ToString().Trim();
                                        _dbTool.SQLExec(_BaseDataService.UpdateTableLotInfoState(tmpLotid, "PROC"), out tmpMsg, true);
                                    }
                                }

                                bResult = true;
                                tmpState = "OK";
                                tmpMsg = "";
                            }
                            else
                            {
                                _logger.Info(string.Format("Info: SendCommand [{0}][{1}] Failed. {2}", theCmdId, tmpDest, response.RequestMessage));
                                //傳送失敗, 即計算為Failed
                                //新增一筆Total Record
                                //_dbTool.SQLExec(_BaseDataService.InsertRTDStatisticalRecord(curDT.ToString("yyyy-MM-dd HH:mm:ss"), theCmdId, "F"), out tmpMsg, true);

                                //_dbTool.SQLExec(_BaseDataService.UpdateTableWorkInProcessSchHisByUId(tmpUid), out tmpMsg, true);
                                _dbTool.SQLExec(_BaseDataService.UpdateTableWorkInProcessSchByCmdId("Failed", DateTime.Now.ToString("yyyy-M-d hh:mm:ss"), theCmdId, tableOrder), out tmpMsg, true);
                                _dbTool.SQLExec(_BaseDataService.UpdateUnlockWorkInProcessSchByCmdId(theCmdId, tableOrder), out tmpMsg, true);
                                //Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                                bResult = false;
                                tmpState = "NG";
                                tmpMsg = string.Format("State Code : {0}, Reason : {1}.", response.StatusCode, response.RequestMessage);
                                //_logger.Info(string.Format("Info: SendCommand Failed. {0}", tmpMsg));
                                _logger.Info(string.Format("SendCommand Failed Message: {0}", tmpMsg));

                                string[] _argvs = new string[] { theCmdId, "", "", "" };
                                if (CallRTDAlarm(_dbTool, 10100, _argvs))
                                {
                                    _logger.Info(string.Format("Info: SendCommand Failed. {0}", tmpMsg));
                                }
                            }
                        }
                        else
                        {
                            bResult = false;
                            tmpState = "NG";
                            tmpMsg = "應用程式呼叫 API 發生異常";
                        }

                        //if(tmpState.Equals("NG"))
                        //_dbTool.SQLExec(_BaseDataService.UpdateUnlockWorkInProcessSchByCmdId(theCmdId), out tmpMsg, true);


                    }

                    //Release Resource
                    client.Dispose();
                    response.Dispose();
                }
            }
            catch (Exception ex)
            {
                //_dbTool.SQLExec(_BaseDataService.UpdateUnlockWorkInProcessSchByCmdId(exceptionCmdId), out tmpMsg, true);
                //需要Alarm to Front UI
                tmpMsg = string.Format("Info: SendCommand Failed. Exception is {0}, ", ex.Message);
                _logger.Info(tmpMsg);
                tmpMsg = "";
                _dbTool.SQLExec(_BaseDataService.UpdateTableWorkInProcessSchByCmdId(" ", DateTime.Now.ToString("yyyy-M-d hh:mm:ss"), exceptionCmdId, tableOrder), out tmpMsg, true);
                if (!tmpMsg.Equals(""))
                    _logger.Info(tmpMsg);
                tmpMsg = "";
                _dbTool.SQLExec(_BaseDataService.UpdateUnlockWorkInProcessSchByCmdId(exceptionCmdId, tableOrder), out tmpMsg, true);
                if (!tmpMsg.Equals(""))
                    _logger.Info(tmpMsg);
                string[] _argvs = new string[] { exceptionCmdId, "", "", "" };
                if (CallRTDAlarm(_dbTool, 10101, _argvs))
                {
                    _logger.Info(string.Format("Info: SendCommand Failed. {0}", ex.Message));
                }
            }

            //_logger.Info(string.Format("Info: Result is {0}, Reason :", foo.State, foo.Message));

            return bResult;
        }
        public bool SentCommandtoToMCS(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, List<string> ListCmds)
        {
            bool bResult = false;
            string tmpState = "";
            string tmpMsg = "";
            NormalTransferModel transferCmds = new NormalTransferModel();
            TransferList theTransfer = new TransferList();
            //Http Object
            HttpClient client;
            HttpResponseMessage response;

            string tableOrder = "";
            string _keyOfEnv = "";
            string _keyRTDEnv = "";

            try
            {
                _keyRTDEnv = _configuration["RTDEnvironment:type"];
                _keyOfEnv = string.Format("RTDEnvironment:commandsTable:{0}", _keyRTDEnv);
                //"RTDEnvironment:commandsTable:PROD"
                tableOrder = _configuration[_keyOfEnv] is null ? "workinprocess_sch" : _configuration[_keyOfEnv];

                if (ListCmds.Count > 0)
                {
                    //No Execute
                    client = GetHttpClient(_configuration, "");
                    // Add an Accept header for JSON format.
                    // 為JSON格式添加一個Accept表頭
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    //JObject oToken = JObject.Parse(GetAuthrizationTokenfromMCS(_configuration));
                    //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oToken["token"].ToString());
                    response = new HttpResponseMessage();

                    DataTable dt = null;
                    DataRow[] dr = null;
                    string sql = "";
                    int iRec = 0;
                    foreach (string theUuid in ListCmds)
                    {
                        //// 查詢資料
                        dt = _dbTool.GetDataTable(_BaseDataService.SelectTableWorkInProcessSchByCmdId(theUuid, tableOrder));
                        dr = dt.Select("CMD_CURRENT_STATE='Initial'");
                        if (dr.Length <= 0)
                        {
                            _logger.Info(string.Format("no find the uuid [{0}]", theUuid));
                            continue;
                        }

                        //組織派送指令
                        if (transferCmds.CommandID.Equals(""))
                        {
                            transferCmds.CommandID = "";
                            transferCmds.CarrierID = "";
                            transferCmds.Priority = 0;
                            transferCmds.Replace = 0;
                        }

                        theTransfer = new TransferList();
                        theTransfer.Source = dr[iRec]["Source"].ToString();
                        theTransfer.Dest = dr[iRec]["Dest"].ToString();
                        theTransfer.LotID = dr[iRec]["LotID"].ToString();
                        theTransfer.Quantity = 0;
                        theTransfer.CarrierType = dr[iRec]["CarrierType"].ToString();

                        transferCmds.Transfer.Add(theTransfer);
                        iRec++;

                        transferCmds.Replace = iRec;
                    }

                    Uri gizmoUri = null;

                    response = client.PostAsJsonAsync("api/command", transferCmds).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        gizmoUri = response.Headers.Location;
                        bResult = true;
                        tmpState = "OK";
                        tmpMsg = "";
                    }
                    else
                    {
                        //Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                        bResult = false;
                        tmpState = "NG";
                        tmpMsg = string.Format("State Code : {0}, Reason : {1}.", response.StatusCode, response.ReasonPhrase);
                    }

                    //Release Resource
                    client.Dispose();
                    response.Dispose();
                }
            }
            catch (Exception ex)
            {

            }

            //_logger.Info(string.Format("Info: Result is {0}, Reason :", foo.State, foo.Message));

            return bResult;
        }
        public bool SentTransferCommandtoToMCS(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, TransferList ListCmds, out string _tmpMsg)
        {
            bool bResult = false;
            string tmpState = "";
            string tmpMsg = "";
            NormalTransferModel transferCmds = new NormalTransferModel();
            TransferList theTransfer = new TransferList();
            //Http Object
            HttpClient client;
            HttpResponseMessage response;
            _tmpMsg = "";

            try
            {
                if (!ListCmds.CarrierID.Equals(""))
                {
                    //No Execute
                    client = GetHttpClient(_configuration, "");
                    // Add an Accept header for JSON format.
                    // 為JSON格式添加一個Accept表頭
                    client.Timeout = TimeSpan.FromMinutes(1);
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    //JObject oToken = JObject.Parse(GetAuthrizationTokenfromMCS(_configuration));
                    //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oToken["token"].ToString());
                    response = new HttpResponseMessage();

                    DataTable dt = null;
                    DataRow[] dr = null;
                    string sql = "";
                    int iRec = 0;

                    //foreach (string theUuid in ListCmds)
                    if(ListCmds is not null)
                    {
                        //// 查詢資料
                        /*
                        dt = _dbTool.GetDataTable(_BaseDataService.SelectTableWorkInProcessSchByCmdId(""));
                        dr = dt.Select("CMD_CURRENT_STATE='Initial'");
                        if (dr.Length <= 0)
                        {
                            _logger.Info(string.Format("no find the uuid [{0}]", theUuid));
                            continue;
                        }*/

                        //組織派送指令
                        if (transferCmds is null)
                        {
                            transferCmds = new NormalTransferModel();
                            transferCmds.CommandID = Tools.GetCommandID(_dbTool);
                            transferCmds.CarrierID = ListCmds.CarrierID;
                            transferCmds.Priority = 0;
                            transferCmds.Replace = 0;
                            transferCmds.Transfer = new List<TransferList>();
                        }
                        else
                        {
                            transferCmds.CommandID = Tools.GetCommandID(_dbTool);
                            transferCmds.CarrierID = ListCmds.CarrierID;
                            transferCmds.Priority = 0;
                            transferCmds.Replace = 0;
                            transferCmds.Transfer = new List<TransferList>();
                        }

                        theTransfer = new TransferList();
                        theTransfer.Source = ListCmds.Source;
                        theTransfer.Dest = ListCmds.Dest;
                        theTransfer.LotID = ListCmds.LotID;
                        theTransfer.Quantity = ListCmds.Quantity;
                        theTransfer.CarrierType = ListCmds.CarrierType;

                        transferCmds.Transfer.Add(theTransfer);
                        iRec++;

                        transferCmds.Replace = iRec;
                    }

                    Uri gizmoUri = null;

                    response = client.PostAsJsonAsync("api/command", transferCmds).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        gizmoUri = response.Headers.Location;
                        bResult = true;
                        tmpState = "OK";
                        tmpMsg = "";
                    }
                    else
                    {
                        //Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                        bResult = false;
                        tmpState = "NG";
                        tmpMsg = string.Format("State Code : {0}, Reason : {1}.", response.StatusCode, response.ReasonPhrase);
                    }

                    //Release Resource
                    client.Dispose();
                    response.Dispose();
                }
            }
            catch (Exception ex)
            {
                _tmpMsg = ex.Message;
            }

            //_logger.Info(string.Format("Info: Result is {0}, Reason :", foo.State, foo.Message));

            return bResult;
        }
        public APIResult SentCommandtoMCS(IConfiguration _configuration, ILogger _logger, List<string> agrs)
        {
            APIResult foo;
            bool bResult = false;
            string tmpState = "";
            string tmpMsg = "";
            int iRemotecmd = int.Parse(agrs[0]);
            string remoteCmd = "";
            string extCmd = "";
            //agrs : remote cmd, command id, vehicleId
            HttpClient client;
            HttpResponseMessage response;

            try
            {
                //Execute 1 
                client = GetHttpClient(_configuration, "");
                // Add an Accept header for JSON format.
                // 為JSON格式添加一個Accept表頭
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                JObject oToken = JObject.Parse(GetAuthrizationTokenfromMCS(_configuration));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oToken["token"].ToString());


                response = client.GetAsync("api/command").Result;  // Blocking call（阻塞调用）! 

                //iRemotecmd: system: [1.pause, 2.resume], transfer: [3.abort, 4.cancel],  vihicle: [5. release, 6.charge, 7.assert, 8.sweep]
                switch (iRemotecmd)
                {
                    case 1:
                        remoteCmd = "pause";
                        extCmd = "";
                        break;
                    case 2:
                        remoteCmd = "resume";
                        extCmd = " ";
                        break;
                    case 3:
                        remoteCmd = "abort";
                        extCmd = string.Format(", CommandID: '{0}'", agrs[1]);
                        break;
                    case 4:
                        remoteCmd = "cancel";
                        extCmd = string.Format(", CommandID: '{0}'", agrs[1]);
                        break;
                    case 5:
                        remoteCmd = "release";
                        extCmd = string.Format(", parameter: { VehicleID: '{0}'}", agrs[1]);
                        break;
                    case 6:
                        remoteCmd = "charge";
                        extCmd = string.Format(", parameter: { VehicleID: '{0}'}", agrs[1]);
                        break;
                    case 7:
                        remoteCmd = "assert";
                        extCmd = string.Format(", parameter: { VehicleID: '{0}'}", agrs[1]);
                        break;
                    case 8:
                        remoteCmd = "sweep";
                        extCmd = string.Format(", parameter: { VehicleID: '{0}'}", agrs[1]);
                        break;
                    default:
                        remoteCmd = "cancel";
                        extCmd = string.Format(", CommandID: '{0}'", agrs[1]);
                        break;
                }

                // Create a new product
                // 创建一个新产品
                //var gizmo = new Product() { Name = "Gizmo", Price = 100, Category = "Widget" };
                Uri gizmoUri = null;
                string strGizmo = "";
                if (extCmd.Equals(""))
                {
                    strGizmo = "{ \"remote_cmd\": \"{0}\"}";

                    try
                    {
                        strGizmo = string.Format(strGizmo, remoteCmd.ToString());
                    }
                    catch (Exception ex)
                    { tmpMsg = ex.Message; }
                }
                else
                {
                    strGizmo = "{ remote_cmd: \"{0}\",  {1} }";

                    strGizmo = string.Format(strGizmo, remoteCmd);
                }

                strGizmo = "{ \"remote_cmd\": \"pause\" }";
                var gizmo = JObject.Parse(strGizmo);
                response = client.PostAsJsonAsync("api/command", gizmo).Result;

                if (response.IsSuccessStatusCode)
                {
                    gizmoUri = response.Headers.Location;
                    bResult = true;
                    tmpState = "OK";
                    tmpMsg = "";
                }
                else
                {
                    //Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                    bResult = false;
                    tmpState = "NG";
                    tmpMsg = string.Format("State Code : {0}, Reason : {1}.", response.StatusCode, response.ReasonPhrase);
                    _logger.Debug(tmpMsg);
                }

                //PostAsJsonAsync is an extension method defined in System.Net.Http.HttpClientExtensions.It is equivalent to the following:
                //PostAsJsonAsync是在System.Net.Http.HttpClientExtensions中定义的一个扩展方法。上述代码与以下代码等效：

                //var product = new Product() { Name = "Gizmo", Price = 100, Category = "Widget" };

                //// Create the JSON formatter.
                //// 创建JSON格式化器。
                //MediaTypeFormatter jsonFormatter = new JsonMediaTypeFormatter();

                //// Use the JSON formatter to create the content of the request body.
                //// 使用JSON格式化器创建请求体内容。
                //HttpContent content = new ObjectContent<Product>(product, jsonFormatter);

                //// Send the request.
                //// 发送请求。
                //var resp = client.PostAsync("api/products", content).Result;

                foo = new APIResult()
                {
                    Success = bResult,
                    State = tmpState,
                    Message = tmpMsg
                };

                client.Dispose();
                response.Dispose();
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = false,
                    State = "NG",
                    Message = ex.Message
                };
                _logger.Debug(string.Format("Exception: {0}", foo.Message));
            }

            _logger.Info(string.Format("Info: Result is {0}, Reason :", foo.State, foo.Message));

            return foo;
        }
        public APIResult SentCommandtoMCSByModel(IConfiguration _configuration, ILogger _logger, string _model, List<string> args)
        {
            APIResult foo;
            bool bResult = false;
            string tmpFuncName = "SentCommandtoMCSByModel";
            string tmpState = "";
            string tmpMsg = "";
            string remoteCmd = "None";

            HttpClient client;
            HttpResponseMessage response;

            JObject jRespData = new JObject();

            try
            {
                _logger.Info(string.Format("Run Function [{0}]: Model is {1}", tmpFuncName, _model));
                client = GetHttpClient(_configuration, "");
                // Add an Accept header for JSON format.
                // 為JSON格式添加一個Accept表頭
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                Uri gizmoUri = null;
                string strGizmo = "{ }";

                switch (_model.ToUpper())
                {
                    case "INFOUPDATE":
                        remoteCmd = string.Format("api/{0}", _model);
                        InfoUpdate tmpModel = new InfoUpdate();
                        
                        tmpModel.LotID = args[0];
                        tmpModel.Stage = args[1];
                        //tmpModel.machine = args[2];
                        //tmpModel.desc = args[3];
                        tmpModel.CarrierID = args[4];
                        tmpModel.Cust = args[5];
                        tmpModel.PartID = args[6];
                        tmpModel.LotType = args[7];
                        tmpModel.Automotive = args[8];
                        tmpModel.State = args[9];
                        tmpModel.HoldCode = args[10];
                        tmpModel.TurnRatio = float.Parse(args[11]);
                        tmpModel.EOTD = args[12];
                        tmpModel.HoldReas = args[13];
                        tmpModel.POTD = args[14];
                        tmpModel.WaferLot = args[15];
                        tmpModel.Force = args[16].Equals("") ? false : args[16].ToLower().Equals("true") ? true : false;

                        gizmoUri = null;
                        strGizmo = System.Text.Json.JsonSerializer.Serialize<InfoUpdate>(tmpModel);
                        break;
                    case "EQUIPMENTSTATUSSYNC":
                        remoteCmd = string.Format("api/{0}", _model);
                        EquipmentStatusSync tmpEqpSync = new EquipmentStatusSync();
                        tmpEqpSync.PortID = args[0];

                        gizmoUri = null;
                        strGizmo = System.Text.Json.JsonSerializer.Serialize<EquipmentStatusSync>(tmpEqpSync);
                        break;
                    case "MANUALCHECKIN":
                        remoteCmd = string.Format("api/{0}", _model);
                        ManualCheckIn tmpManualCheckin = new ManualCheckIn();

                        tmpManualCheckin.CarrierID = args[0];
                        tmpManualCheckin.LotID = args[1];
                        tmpManualCheckin.PortID = args[2];
                        tmpManualCheckin.Quantity = int.Parse(args[3]);
                        tmpManualCheckin.Total = int.Parse(args[4]);
                        tmpManualCheckin.UserID = args[5];
                        tmpManualCheckin.Pwd = args[6];

                        gizmoUri = null;
                        strGizmo = System.Text.Json.JsonSerializer.Serialize<ManualCheckIn>(tmpManualCheckin);

                        break;
                    case "GETDEVICEINFO":
                        remoteCmd = string.Format("api/{0}", _model);
                        DeviceInfo tmpDeviceInfo = new DeviceInfo();

                        tmpDeviceInfo.DeviceID = args[0];

                        gizmoUri = null;
                        strGizmo = System.Text.Json.JsonSerializer.Serialize<DeviceInfo>(tmpDeviceInfo);

                        break;
                    case "BATCH":
                        remoteCmd = string.Format("api/{0}", _model);
                        Batch tmpBatch = new Batch();

                        tmpBatch.EQID = args[0];
                        tmpBatch.CarrierID = args[1];
                        tmpBatch.TotalFoup = int.Parse(args[2]);

                        gizmoUri = null;
                        strGizmo = System.Text.Json.JsonSerializer.Serialize<Batch>(tmpBatch);
                        _logger.Info(string.Format("SendMCS: Func[{0}], Json[{1}]", _model, strGizmo));

                        break;
                    case "SWITCHMODE":
                        remoteCmd = string.Format("api/{0}", _model);
                        SwitchMode tmpSwMode = new SwitchMode();

                        tmpSwMode.EQID = args[0];
                        tmpSwMode.MODE = int.Parse(args[1]);

                        gizmoUri = null;
                        strGizmo = System.Text.Json.JsonSerializer.Serialize<SwitchMode>(tmpSwMode);
                        _logger.Info(string.Format("SendMCS: Func[{0}], Json[{1}]", _model, strGizmo));

                        break;
                    default:
                        remoteCmd = string.Format("api/{0}", _model);
                        break;
                }

                response = new HttpResponseMessage();

                var gizmo = JObject.Parse(strGizmo);
                response = client.PostAsJsonAsync(remoteCmd, gizmo).Result;

                var respContent = response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    gizmoUri = response.Headers.Location;
                    bResult = true;
                    tmpState = "OK";
                    //tmpMsg = string.Format("State Code : {0}, Reason : {1}.", response.StatusCode, response.ReasonPhrase);

                    if (_model.ToUpper().Equals("GETDEVICEINFO"))
                    {
                        string tmpResult = respContent.Result;
                        APIResponse tmpResponse = new APIResponse(tmpResult);
                        jRespData = tmpResponse.Data;
                    }
                    else
                    {
                        string tmpResult = respContent.Result;

                        _logger.Info(tmpResult);

                        APIResponse tmpResponse = new APIResponse(tmpResult);
                        tmpMsg = string.Format("Func[{0}][State : {1}, Message : {2}]", _model, tmpResponse.State, tmpResponse.Message);
                        _logger.Info(tmpMsg);

                        tmpMsg = "";
                        if(tmpResponse.State.Equals("NG"))
                        {
                            bResult = false;
                            tmpState = "NG";
                            tmpMsg = tmpResponse.Message;
                        }

                        jRespData = tmpResponse.Data;
                        if(tmpMsg.Equals(""))
                            tmpMsg = string.Format("State Code : {0}, Reason : {1}.", response.StatusCode, response.ReasonPhrase);
                    }
                }
                else
                {
                    //Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                    string tmpResult = respContent.Result;

                    bResult = false;
                    tmpState = "NG";
                    tmpMsg = string.Format("State Code : {0}[{1}], Reason : {2}.", tmpState, response.StatusCode, response.ReasonPhrase);

                    _logger.Info(tmpMsg);
                }

                if (!_model.ToUpper().Equals("INFOUPDATE"))
                    _logger.Info(tmpMsg);

                foo = new APIResult()
                {
                    Success = bResult,
                    State = tmpState,
                    Message = tmpMsg,
                    Data = jRespData
                };

                client.Dispose();
                response.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Info(string.Format("Exception [{0}]: ex.Message [{1}]", tmpFuncName, ex.Message));
                foo = new APIResult()
                {
                    Success = false,
                    State = "NG",
                    Message = ex.Message
                };
            }

            _logger.Info(string.Format("[{0}, {1}]: Result is {2}, Reason : [{3}]", tmpFuncName, _model, foo.State, foo.Message));

            return foo;
        }
        public APIResult SentAbortOrCancelCommandtoMCS(IConfiguration _configuration, ILogger _logger, int iRemotecmd, string _commandId)
        {
            APIResult foo;
            bool bResult = false;
            string tmpState = "";
            string tmpMsg = "";
            string remoteCmd = "cancel";
            HttpClient client;
            HttpResponseMessage response;


            try
            {
                client = GetHttpClient(_configuration, "");
                client.Timeout = TimeSpan.FromSeconds(60);
                // Add an Accept header for JSON format.
                // 為JSON格式添加一個Accept表頭
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                switch (iRemotecmd)
                {
                    case 1:
                        remoteCmd = "api/CancelTransferCommand";
                        break;
                    case 2:
                        remoteCmd = "api/AbortTransferCommand";
                        break;
                    default:
                        remoteCmd = "api/CancelTransferCommand";
                        break;
                }

                response = new HttpResponseMessage();

                Uri gizmoUri = null;
                string strGizmo = "{ \"CommandID\": \"" + _commandId + "\" }";
                //strGizmo = string.Format("{ \"CommandID\": \"{0}\" }", _commandId);

                var gizmo = JObject.Parse(strGizmo);
                response = client.PostAsJsonAsync(remoteCmd, gizmo).Result;

                if (response.IsSuccessStatusCode)
                {
                    gizmoUri = response.Headers.Location;
                    bResult = true;
                    tmpState = "OK";
                    tmpMsg = "";
                }
                else
                {
                    //Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                    bResult = false;
                    tmpState = "NG";
                    tmpMsg = string.Format("State Code : {0}, Reason : {1}.", response.StatusCode, response.ReasonPhrase);

                    _logger.Info(string.Format("Exception [{0}]: State is NG.  message: {1}", remoteCmd, tmpMsg));

                }

                foo = new APIResult()
                {
                    Success = bResult,
                    State = tmpState,
                    Message = tmpMsg
                };

                client.Dispose();
                response.Dispose();
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = false,
                    State = "NG",
                    Message = ex.Message
                };

                _logger.Info(string.Format("Exception [{0}]: exception message: {1}", remoteCmd, ex.Message));
            }

            _logger.Info(string.Format("Info: Result is {0}, Reason :", foo.State, foo.Message));

            return foo;
        }
        public string GetAuthrizationTokenfromMCS(IConfiguration _configuration)
        {
            string tokenS = "";
            bool bKey = false;

            try
            {
                string path = System.IO.Directory.GetCurrentDirectory() + "Authorization";
                FileInfo fi;
                TimeSpan ts;

                if (File.Exists(path))
                {
                    fi = new FileInfo(path);
                    TimeSpan ts1 = new TimeSpan(fi.LastAccessTimeUtc.Ticks);
                    TimeSpan ts2 = new TimeSpan(DateTime.Now.Ticks);
                    ts = ts1.Subtract(ts2).Duration();
                    if (ts.Days >= 7)
                    {
                        bKey = true;
                    }
                }
                else
                {
                    bKey = true;
                }

                if (bKey)
                {
                    HttpClient client = null;
                    // Add an Accept header for JSON format.
                    // 為JSON格式添加一個Accept表頭
                    Dictionary<string, string> dicParams = new Dictionary<string, string>();
                    string _url = string.Format("http://{0}:{1}/api/login", _configuration["MCS:ip"], _configuration["MCS:port"]);
                    dicParams.Add("username", "gyro");
                    dicParams.Add("password", "gsi5613686");

                    using (client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        HttpResponseMessage response = client.PostAsync(_url, new FormUrlEncodedContent(dicParams)).Result;
                        var token = response.Content.ReadAsStringAsync().Result;
                        tokenS = token.ToString();
                    }

                    File.WriteAllText(path, tokenS);
                }
                else
                {
                    tokenS = File.ReadAllText(path);
                }
            }
            catch (Exception ex)
            { }
            return tokenS;
        }
        public string GetLotIdbyCarrier(DBTool _dbTool, string _carrierId, out string errMsg)
        {
            DataTable dt = null;
            DataRow[] dr = null;
            string tmpLotId = "";
            errMsg = "";

            try
            {
                dt = _dbTool.GetDataTable(_BaseDataService.SelectTableCarrierAssociateByCarrierID(_carrierId));
                dr = dt.Select();
                if (dr.Length <= 0)
                {
                    errMsg = "Can not find this Carrier Id.";
                }
                else
                {
                    if (dr[0]["LOT_ID"].ToString().Equals(""))
                    {
                        errMsg = "The carrier have not create association with lot.";
                    }
                    else
                    { tmpLotId = dr[0]["LOT_ID"].ToString(); }
                }
            }
            catch (Exception ex)
            { }
            finally
            {
                if (dt != null)
                    dt.Dispose(); 
            }
            dt = null;
            dr = null;

            return tmpLotId;
        }
        public List<string> CheckAvailableQualifiedTesterMachine(DBTool _dbTool, IConfiguration _configuration, bool DebugMode, ILogger _logger, string _lotId)
        {
            string tmpMsg = "";
            List<string> TesterMachineList = new List<string>();

            string url = _configuration["WebService:url"];
            string username = _configuration["WebService:username"];
            string password = _configuration["WebService:password"];
            string webServiceMode = _configuration["WebService:Mode"];

            DataTable dt = null;
            string tmpSql = "";

            try
            {
                JCETWebServicesClient jcetWebServiceClient = new JCETWebServicesClient();
                //jcetWebServiceClient.hostname = "127.0.0.1";
                //jcetWebServiceClient.portno = 54350;
                jcetWebServiceClient._url = url;
                JCETWebServicesClient.ResultMsg resultMsg = new JCETWebServicesClient.ResultMsg();
                resultMsg = jcetWebServiceClient.GetAvailableQualifiedTesterMachine(DebugMode, webServiceMode, username, password, _lotId);

#if DEBUG
                //_logger.Info(string.Format("Info:{0}", tmpMsg));
#else
#endif

                if (resultMsg.status)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(resultMsg.retMessage);

                    XmlNodeList xnlA = xmlDoc.GetElementsByTagName("string");

                    if (xnlA.Count > 0)
                    {
                        //先取得Current Stage
                        dt = null;
                        string curStage = "";
                        string curEqpAsso = "";
                        tmpSql = _BaseDataService.SelectTableLotInfoByLotid(_lotId);
                        dt = _dbTool.GetDataTable(tmpSql);
                        if (dt.Rows.Count > 0)
                        {
                            curStage = dt.Rows[0]["STAGE"].ToString().Trim();
                            curEqpAsso = dt.Rows[0]["EQUIP_ASSO"].ToString().Trim();
                        }

                        foreach (XmlNode xnA in xnlA)
                        {
                            if (!xnA.InnerText.Equals(" "))
                            {
                                dt = null;
                                tmpSql = "";
                                tmpSql = _BaseDataService.SelectTableEQUIP_MATRIX(xnA.InnerText, curStage);
                                dt = _dbTool.GetDataTable(tmpSql);
                                if (dt.Rows.Count > 0)
                                {
                                    DataTable dtLot = _dbTool.GetDataTable(_BaseDataService.SelectTableLotInfoByLotid(_lotId));
                                    string tmpCustomer = "";
                                    if (dtLot.Rows.Count > 0)
                                        tmpCustomer = dtLot.Rows[0]["CustomerName"].ToString();

                                    DataTable dtMap = _dbTool.GetDataTable(_BaseDataService.SelectPrefmap(xnA.InnerText));
                                    if (dtMap.Rows.Count > 0)
                                    {
                                        if (dtMap.Rows[0]["CUSTOMER_PREF_1"].ToString().Contains(tmpCustomer))
                                        {
                                            TesterMachineList.Insert(0, xnA.InnerText);
                                        }
                                        else if (dtMap.Rows[0]["CUSTOMER_PREF_2"].ToString().Contains(tmpCustomer))
                                        {
                                            TesterMachineList.Add(xnA.InnerText);
                                        }
                                        else if (dtMap.Rows[0]["CUSTOMER_PREF_3"].ToString().Contains(tmpCustomer))
                                        {
                                            TesterMachineList.Add(xnA.InnerText);
                                        }
                                        else
                                            TesterMachineList.Add(xnA.InnerText);
                                    }
                                    else
                                        TesterMachineList.Add(dt.Rows[0]["EQPID"].ToString());
                                }
                                else
                                    continue;
                            }
                        }

                        if (curEqpAsso.Equals("Y"))
                        {
                            if (TesterMachineList.Count <= 0)
                            {
                                tmpSql = _BaseDataService.UpdateTableLotInfoReset(_lotId);
                                _dbTool.SQLExec(tmpSql, out tmpMsg, true);
                            }
                        }
                    }
                    else
                    {
                        tmpMsg = "No Available Tester Machine.";
                    }
                }
                else
                {
                    tmpMsg = "Get Available Qualified Tester Machine failed.";
                }
            }
            catch (Exception ex)
            {
                tmpMsg = string.Format("Unknow error. [Exception] {0}", ex.Message);
                _logger.Debug(ex.Message);
            }

            return TesterMachineList;
        }
        public List<string> CheckAvailableQualifiedTesterMachine(IConfiguration _configuration, ILogger _logger, string _username, string _password, string _lotId)
        {
            List<string> TesterMachine = new List<string>();
            string webserurl = _configuration["WebService:url"];
            Console.WriteLine("hahaha:----" + webserurl);
            //var binding = new BasicHttpBinding();
            ////根據WebService 的URL 構建終端點對象，參數是提供的WebService地址
            //var endpoint = new EndpointAddress(String.Format(@" {0} ", webserurl));
            ////創建調用接口的工廠，注意這裡泛型只能傳入接口泛型接口裡面的參數是WebService裡面定義的類名+Soap 
            //var factory = new ChannelFactory<WebServiceSoap>(binding, endpoint);
            ////從工廠獲取具體的調用實例
            //var callClient = factory.CreateChannel();
            //調用具體的方法，這裡是HelloWorldAsync 方法


            ////調用TestMethod方法，並傳遞參數
            //CheckAvailableQualifiedTesterMachine body = new CheckAvailableQualifiedTesterMachine(_username, _password);
            //Task<CheckAvailableQualifiedTesterMachine> testResponsePara = callClient.CheckAvailableQualifiedTesterMachine(new CheckAvailableQualifiedTesterMachine(body));
            ////獲取
            //string result3 = testResponsePara.Result.Body._TPQuery_CheckPROMISLoginResult;
            ////<?xml version="1.0" encoding="utf - 8"?><Beans><Status Value="FAILURE" /><ErrMsg Value="SECURITY. % UAF - W - LOGFAIL, user authorization failure, privileges removed." /></Beans>';
            ////string test = "<body><head>test header</head></body>";
            ////XmlDocument xmlDoc = new XmlDocument();
            ////xmlDoc.LoadXml(result3);
            ////XmlNode xn = xmlDoc.SelectSingleNode("Beans");


            //XmlNodeList xnlA = xn.ChildNodes;
            //String member_valodation = "";
            //String member_validation_message = "";
            //foreach (XmlNode xnA in xnlA)
            //{
            //    Console.WriteLine(xnA.Name);
            //    if ((xnA.Name) == "Status")
            //    {
            //        XmlElement xeB = (XmlElement)xnA;
            //        if ((xeB.GetAttribute("Value")) == "SUCCESS")
            //        {
            //            member_valodation = "OK";
            //        }
            //        else
            //        {
            //            member_valodation = "NG";
            //        }

            //    }
            //    if ((xnA.Name) == "ErrMsg")
            //    {
            //        XmlElement xeB = (XmlElement)xnA;
            //        member_validation_message = xeB.GetAttribute("Value");
            //    }

            //    Console.WriteLine(member_valodation);
            //}
            //if (member_valodation == "OK")
            //{
            //    //A claim is a statement about a subject by an issuer and
            //    //represent attributes of the subject that are useful in the context of authentication and authorization operations.
            //    if (objLoginModel.UserName == "admin")
            //    {
            //        objLoginModel.Role = "Admin";
            //    }
            //    else
            //    {
            //        objLoginModel.Role = "User";
            //    }
            //    var claims = new List<Claim>() {
            //            //new Claim(ClaimTypes.NameIdentifier,Convert.ToString(user.UserId)),
            //                new Claim("user_name",objLoginModel.UserName),
            //                new Claim(ClaimTypes.Role,objLoginModel.Role),
            //            //new Claim("FavoriteDrink","Tea")
            //            };
            //    //Initialize a new instance of the ClaimsIdentity with the claims and authentication scheme
            //    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            //    //Initialize a new instance of the ClaimsPrincipal with ClaimsIdentity
            //    var principal = new ClaimsPrincipal(identity);
            //    //SignInAsync is a Extension method for Sign in a principal for the specified scheme.
            //    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            //        principal, new AuthenticationProperties() { IsPersistent = objLoginModel.RememberLogin });

            //    return LocalRedirect(objLoginModel.ReturnUrl);
            //}
            //else
            //{
            //    ModelState.AddModelError("UserName", "Username Error");
            //    ModelState.AddModelError("Password", "Password error");
            //    return View(objLoginModel);
            //}

            //TesterMachine.Add("CTWT-01");
            //TesterMachine.Add("CTDS-06");
            return TesterMachine;
        }
        public bool BuildTransferCommands(DBTool _dbTool, IConfiguration configuration, ILogger _logger, bool DebugMode, EventQueue _oEventQ, Dictionary<string, string> _threadControll, List<string> _lstEquipment, out List<string> _arrayOfCmds, ConcurrentQueue<EventQueue> _eventQueue)
        {
            bool bResult = false;
            string tmpMsg = "";
            string tmpSmsMsg = "";
            string strEquip = "";
            string strPortModel = "";
            ArrayList tmpCmds = new();
            _arrayOfCmds = new List<string>();
            string lotid = "";
            string tmpSql = "";
            string tmpCustomerName = "";
            string tmpStage = "";
            string tmpPartId = "";
            List<string> lstPortIDs = new List<string>();

            string tableOrder = "";
            string _keyOfEnv = "";
            string _keyRTDEnv = "";

            try
            {
                _keyRTDEnv = configuration["RTDEnvironment:type"];
                _keyOfEnv = string.Format("RTDEnvironment:commandsTable:{0}", _keyRTDEnv);
                //"RTDEnvironment:commandsTable:PROD"
                tableOrder = configuration[_keyOfEnv] is null ? "workinprocess_sch" : configuration[_keyOfEnv];

                DataTable dt = null;
                DataTable dtTemp = null;
                DataRow[] dr = null;// strPortModel

                if (_oEventQ.EventName.Equals("CommandStatusUpdate") || _oEventQ.EventName.Equals("CarrierLocationUpdate"))
                {
                    //CarrierLocationUpdate
                    return bResult;
                }
                else
                    lotid = ((NormalTransferModel)_oEventQ.EventObject).LotID;

                tmpSql = _BaseDataService.SelectTableLotInfoByLotid(lotid);
                dt = _dbTool.GetDataTable(tmpSql);
                if (dt.Rows.Count > 0)
                {
                    tmpCustomerName = dt.Rows[0]["CustomerName"].ToString().Trim();
                    tmpStage = dt.Rows[0]["Stage"].ToString().Trim();
                    tmpPartId = dt.Rows[0]["PartID"].ToString().Trim();
                }

                //由符合的設備中, 找出一台可用的機台
                string sql = "";
                bool bHaveEQP = false;
                bool bKey = false;

                foreach (string Equip in _lstEquipment)
                {
                    bHaveEQP = false;
                    try
                    {
                        //再次檢查, 是否有限定特定Stage 可於部份機台上執行！
                        sql = _BaseDataService.SelectTableEQUIP_MATRIX(Equip, tmpStage);
                        dt = _dbTool.GetDataTable(tmpSql);
                        if (dt.Rows.Count <= 0)
                        {
                            tmpMsg = String.Format("Equipment[{0}] not in EQUIP MATRIX or Matrix is disabled.", Equip);
                            _logger.Debug(tmpMsg);

                            continue;
                        }

                        sql = string.Format(_BaseDataService.SelectTableEQPStatusInfoByEquipID(Equip));
                        dt = _dbTool.GetDataTable(sql);

                        if (dt.Rows.Count > 0)
                        {
                            lstPortIDs = new List<string>();
                            strEquip = "";
                            if (!strEquip.Equals(""))
                            { bHaveEQP = true; }

                            //檢查狀態 Current Status 需要為UP
                            if (dt.Rows[0]["CURR_STATUS"].ToString().Equals("UP"))
                            {
                                //可用
                                strEquip = dt.Rows[0]["EQUIPID"].ToString();
                            }
                            else if (dt.Rows[0]["CURR_STATUS"].ToString().Equals("PM"))
                            {

                            }
                            else if (dt.Rows[0]["CURR_STATUS"].ToString().Equals("DOWN"))
                            {
                                if (dt.Rows[0]["DOWN_STATE"].ToString().Equals("IDLE"))
                                {
                                    strEquip = dt.Rows[0]["EQUIPID"].ToString();
                                }
                                if (dt.Rows[0]["DOWN_STATE"].ToString().Equals("DOWN"))
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                //無法用, 直接確認下一台Equipment
                                continue;
                            }
                            //檢查Port Status是否可用
                            bKey = false;
                            int iState = 0;

                            foreach (DataRow drCurr in dt.Rows)
                            {
                                iState = int.Parse(drCurr["PORT_STATE"].ToString());

                                if (iState == 0 || iState == 1 || iState == 6 || iState == 9)
                                {
                                    bKey = false; break;
                                }
                                else
                                {
                                    bKey = true;
                                    string tmpCurrPort = drCurr["Port_Id"].ToString().Trim();

                                    //20230413V1.2 Modify by Vance
                                    sql = string.Format(_BaseDataService.SelectTableWorkInProcessSchByEquipPort(Equip, tmpCurrPort, tableOrder));
                                    dtTemp = _dbTool.GetDataTable(sql);

                                    if (dtTemp.Rows.Count > 0)
                                        bHaveEQP = true;

                                    lstPortIDs.Add(tmpCurrPort);

                                    break;
                                }
                            }
                        }

                        if (bHaveEQP)
                        {
                            //己經有指令的直接加入嘗試再次發送
                            if (dt is not null)
                            {
                                _arrayOfCmds.Add(dtTemp.Rows[0]["CMD_ID"].ToString());

                                dtTemp.Clear(); dtTemp.Dispose(); dtTemp = null;
                            }
                            continue;
                        }

                        if (bHaveEQP)
                        {
                            if (dtTemp is not null)
                            { dtTemp.Clear(); dtTemp.Dispose(); dtTemp = null; }
                            break;
                        }
                    }
                    catch(Exception ex)
                    {
                        tmpMsg = String.Format("BuildTransferCommands. [Exception 1]: {0}", ex.Message);
                        _logger.Debug(tmpMsg);
                    }

                    if (!bKey)
                    { continue; }
                    else
                    { break; }
                }
                //取出EquipModel5
                if (strEquip.Equals(""))
                { return bResult; }

                sql = string.Format(_BaseDataService.SelectTableEQP_STATUSByEquipId(strEquip));
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    strPortModel = dt.Rows[0]["Port_Model"].ToString();
                }
                else
                {
                    _arrayOfCmds = null;
                    return bResult;
                }

                lock (_threadControll)
                {
                    if (_threadControll.ContainsKey(strEquip))
                    {
                        if (ThreadLimitTraffice(_threadControll, strEquip, 3, "ss", ">"))
                        {
                            _threadControll[strEquip] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else
                        {
                            return bResult;
                        }
                    }
                    else
                    {
                        if (!_threadControll.ContainsKey(strEquip))
                            _threadControll.Add(strEquip, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        else
                            return bResult;
                    }
                }

                try
                {
                    /*
                    * 3. If next lot is different CUSTDEVICE
                    a. RTD to trigger pre-alert by Email / SMS when distributed for the last lot with same device.
                    b. RTD to call SP to change RTS status to CONV and trigger Email / SMS alert to inform
                    equipment already change to CONV.
                    c. RTS to bring down equipment and change equipment status from Promis.
                    d. RTD to insert SMS data into table RTD_SMS_TRIGGER_DATA@AMHS. SCS will trigger SMS
                    based on the table.  //RTD 将 SMS 数据插入表 RTD_SMS_TRIGGER_DATA@AMHS。 SCS 将触发 SMS
                    e. RTD to stop distribute for the next lot until user to clear alarm from RTD.
                    */
                    string resultCode = "";
                    if (VerifyCustomerDevice(_dbTool, _logger, strEquip, tmpCustomerName, lotid, out resultCode))
                    {
                        bool bAlarm = false;

                        //同一设备分发最后一批时，RTD 通过电子邮件/短信触发预警。
                        //呼叫 SP 将 RTS 状态更改为 CONV 并触发 Email/SMS 警报通知
                        //将 SMS 数据插入表 RTD_SMS_TRIGGER_DATA@AMHS。 SCS 将触发 SMS
                        tmpSmsMsg = "";
                        //NEXT LOT 83633110.2 NEED TO SETUP AFTER CURRENT LOT END.  //換客戶的最後一批Lot發送此訊息
                        //--"NEXT LOT {0} NEED TO SETUP AFTER CURRENT LOT END.", LotId
                        //LOT 83633110.2 NEED TO SETUP NOW. EQUIP DOWN FROM RTS.    //換客戶後第一批Lot發此訊息
                        //--"LOT {0} NEED TO SETUP NOW. EQUIP DOWN FROM RTS.", LotId
                        MailMessage tmpMailMsg = new MailMessage();
                        tmpMailMsg.To.Add(configuration["MailSetting:AlarmMail"]);
                        //msg.To.Add("b@b.com");可以發送給多人
                        //msg.CC.Add("c@c.com");
                        //msg.CC.Add("c@c.com");可以抄送副本給多人 
                        //這裡可以隨便填，不是很重要
                        tmpMailMsg.From = new MailAddress(configuration["MailSetting:username"], configuration["MailSetting:EntryBy"], Encoding.UTF8);

                        switch (resultCode)
                        {
                            case "1001":
                                //不同客戶後的第一批
                                tmpSmsMsg = string.Format("LOT {0} NEED TO SETUP NOW. EQUIP DOWN FROM RTS.", lotid);

                                /* 上面3個參數分別是發件人地址（可以隨便寫），發件人姓名，編碼*/
                                tmpMailMsg.Subject = "Device Setup Alert";//郵件標題
                                tmpMailMsg.SubjectEncoding = Encoding.UTF8;//郵件標題編碼
                                tmpMailMsg.Body = string.Format("LOT {0} ({1}) NEED TO SETUP NOW. EQUIP DOWN FROM RTS.", lotid, tmpPartId); //郵件內容
                                tmpMailMsg.BodyEncoding = Encoding.UTF8;//郵件內容編碼 
                                tmpMailMsg.IsBodyHtml = true;//是否是HTML郵件 
                                bAlarm = true;
                                break;
                            case "1002":
                                string tmpNextLot = GetLotIdbyCarrier(_dbTool, GetCarrierByPortId(_dbTool, tmpPartId), out tmpMsg);
                                string tmpNextPartId = "";
                                if (!tmpNextLot.Equals(""))
                                {
                                    tmpSql = _BaseDataService.SelectTableLotInfoByLotid(lotid);
                                    dt = _dbTool.GetDataTable(tmpSql);
                                    if (dt.Rows.Count > 0)
                                        tmpNextPartId = dt.Rows[0]["PartId"].ToString().Trim();
                                }
                                //同客戶的最後一批
                                tmpSmsMsg = string.Format("NEXT LOT {0} NEED TO SETUP AFTER CURRENT LOT END.", tmpNextLot);

                                /* 上面3個參數分別是發件人地址（可以隨便寫），發件人姓名，編碼*/
                                tmpMailMsg.Subject = "Setup Pre-Alert";//郵件標題
                                tmpMailMsg.SubjectEncoding = Encoding.UTF8;//郵件標題編碼
                                tmpMailMsg.Body = string.Format("NEXT LOT {0} ({1}) NEED TO SETUP AFTER CURRENT LOT {2} ({3}) END.", tmpNextLot, tmpNextPartId, lotid, tmpPartId); //郵件內容
                                tmpMailMsg.BodyEncoding = Encoding.UTF8;//郵件內容編碼 
                                tmpMailMsg.IsBodyHtml = true;//是否是HTML郵件 

                                bAlarm = true;
                                break;
                            default:
                                bAlarm = false;
                                break;
                        }

                        if (bAlarm)
                        {
                            ///發送SMS 
                            try
                            {
                                tmpMsg = "";
                                sql = string.Format(_BaseDataService.InsertSMSTriggerData(strEquip, tmpStage, tmpSmsMsg, "N", configuration["MailSetting:EntryBy"]));
                                tmpMsg = string.Format("Send SMS: SQLExec[{0}]", sql);
                                _logger.Info(tmpMsg);
                                _dbTool.SQLExec(sql, out tmpMsg, true);
                            }
                            catch (Exception ex)
                            {
                                tmpMsg = String.Format("Insert SMS trigger data failed. [Exception]: {0}", ex.Message);
                                _logger.Debug(tmpMsg);
                            }

                            ///寄送Mail
                            try
                            {
                                /*
                                MailController MailCtrl = new MailController();
                                MailCtrl.Config = configuration;
                                MailCtrl.Logger = _logger;
                                MailCtrl.DB = _dbTool;
                                MailCtrl.MailMsg = tmpMailMsg;

                                MailCtrl.SendMail();
                                */
                                tmpMsg = string.Format("SendMail: {0}, [{1}]", tmpMailMsg.Subject, tmpMailMsg.Body);
                                _logger.Info(tmpMsg);
                            }
                            catch (Exception ex)
                            {
                                tmpMsg = String.Format("SendMail failed. [Exception]: {0}", ex.Message);
                                _logger.Debug(tmpMsg);
                            }
                        }
                    }
                    else
                    { }
                }
                catch (Exception ex)
                {
                    tmpMsg = String.Format("BuildTransferCommands. [Exception 2]: {0}", ex.Message);
                    _logger.Debug(tmpMsg);
                }

                //依Equip port Model來產生搬運指令
                //bool CreateTransferCommandByPortModel(_dbTool, _Equip, PortModel, out _arrayOfCmds)
                if (CreateTransferCommandByPortModel(_dbTool, configuration, _logger, DebugMode, strEquip, strPortModel, _oEventQ, out _arrayOfCmds, _eventQueue))
                { }
                else
                { }
            }
            catch (Exception ex)
            {
                tmpMsg = String.Format("BuildTransferCommands. [Exception]: {0}", ex.Message);
                _logger.Debug(tmpMsg);
            }

            return bResult;
        }
        public bool CreateTransferCommandByPortModel(DBTool _dbTool, IConfiguration configuration, ILogger _logger, bool DebugMode, string _Equip, string _portModel, EventQueue _oEventQ, out List<string> _arrayOfCmds, ConcurrentQueue<EventQueue> _eventQueue)
        {
            bool result = false;
            string tmpMsg = "";
            _arrayOfCmds = new List<string>();
            ILogger logger = LogManager.GetCurrentClassLogger();
            bool _DebugMode = true;
            bool bStateChange = false;
            bool bStageIssue = false;

            JCETWebServicesClient jcetWebServiceClient = new JCETWebServicesClient();
            JCETWebServicesClient.ResultMsg resultMsg = new JCETWebServicesClient.ResultMsg();

            string tableOrder = "";
            string _keyOfEnv = "";
            string _keyRTDEnv = "";

            try
            {
                DataTable dtTemp = null;
                DataTable dtTemp2 = null;
                DataTable dtTemp3 = null;
                DataTable dtPortsInfo = null;
                DataTable dtCarrierInfo = null;
                DataTable dtAvaileCarrier = null;
                DataTable dtWorkgroupSet = null;
                DataTable dtLoadPortCarrier = null;
                DataTable dtWorkInProcessSch;
                DataRow[] drIn = null;
                DataRow[] drOut = null;
                DataRow[] drCarrierData = null;
                DataRow drCarrier = null;
                DataRow[] drPortState;
                DataTable dtRecipeSet;
                DataRow[] drRecipe;

                string sql = "";
                string lotID = "";
                string CarrierID = "";
                string UnloadCarrierID = "";
                string UnloadCarrierType = "";
                string MetalRingCarrier = "";
                int Quantity = 0;
                string vStage = "";
                string vStage2 = "";
                List<string> lstAvailableAoiMachine = null;
                string strAvailableAoiMachine = null;
                bool useFaileRack = false;
                string faileRack = "";
                string outeRack = "";
                string ineRack = "";
                string ineRackID = "";
                string _carrierID = "";
                bool isMeasureFail = false;
                bool isManualMode = false;
                bool _expired = false;
                bool _effective = false;
                string rtsMachineState = "";
                string rtsCurrentState = "";
                string rtsDownState = "";
                string rtdEqpCustDevice = "";

                bool bTurnOnQTime = false;
                float iLotQTime = 0;
                float iQTimeLow = 0;
                float iQTimeHigh = 0;
                float iQTime = 0;

                bool bCheckEquipLookupTable = false;
                bool bCheckRcpConstraint = false;

                bool bNearComplete = false;
                bool bCheckDevice = false;
                bool bQTimeIssue = false;

                int _priority = 30;
                int _QTimeMode = 1;
                string _adsTable = "";
                string _qTimeTable = "";
                bool _qTimemode_enable = false;
                string _qTimemode_type = "Product";
                bool _qTime_isProduct = false;
                string _qTime_url = "";
                string _qTime_urlUAT = "";
                string _qTime_username = "";
                string _qTime_pwd = "";

                bool _NotInlookupTab = false;

                bool _isFurnace = false;
                int _furnState = 0;
                string _dummycarrier = "";
                string _effectiveslot = "";
                string _maximumqty = "";

                string _eqpworkgroup = "";
                //_maximumqty, _effectiveslot
                string _equiprecipe = "";
                string _lotrecipe = "";
                string _equiprecipeGroup = "";
                string _lotrecipeGroup = "";

                bool _stageController = false;
                bool _cannotSame = false;

                bool _aoiMeasurementLogic = false;

                _keyRTDEnv = configuration["RTDEnvironment:type"];
                _keyOfEnv = string.Format("RTDEnvironment:commandsTable:{0}", _keyRTDEnv);
                //"RTDEnvironment:commandsTable:PROD"
                tableOrder = configuration[_keyOfEnv] is null ? "workinprocess_sch" : configuration[_keyOfEnv];

                _DebugMode = DebugMode;

                if (_DebugMode)
                {
                    _logger.Debug(string.Format("[CreateTransferCommandByPortModel][Start] {0} / {1} / {2}", _oEventQ.EventName, _Equip, lotID));
                }
                //防止同一機台不同線程執行
                dtTemp = _dbTool.GetDataTable(_BaseDataService.QueryEquipLockState(_Equip));
                if (dtTemp.Rows.Count <= 0)
                {
                    _dbTool.SQLExec(_BaseDataService.LockEquip(_Equip, true), out tmpMsg, true);
                }

                //先取得Equipment 的Ports Status
                sql = string.Format(_BaseDataService.SelectTableEQP_STATUSByEquipId(_Equip));
                dtPortsInfo = _dbTool.GetDataTable(sql);
                NormalTransferModel evtObject2 = null;

                try {
                    if (configuration["NearCompleted:Enable"].ToLower().Equals("true"))
                        bNearComplete = true;
                } catch(Exception ex) { }
                try
                {
                    if (configuration["CheckCustDevice:Enable"].ToLower().Equals("true"))
                        bCheckDevice = true;

                    _QTimeMode = configuration["QTimeMode:mode"] is null ? 1 : int.Parse(configuration["QTimeMode:mode"].ToString());
                    _qTimemode_enable = configuration["QTimeMode:Enable"] is null ? false : configuration["QTimeMode:Enable"].ToString().ToLower().Equals("true") ? true : false;
                    _qTimemode_type = configuration["QTimeMode:type"] is null ? "UAT" : configuration["QTimeMode:type"].ToString().ToLower().Equals("uat") ? "UAT" : "Product";

                    _qTime_url = configuration["WebService:url"].ToString();
                    _qTime_urlUAT = configuration["WebService:urlUAT"].ToString();
                    _qTime_username = configuration["WebService:username"].ToString();
                    _qTime_pwd = configuration["WebService:password"].ToString();

                    if (_qTimemode_type.ToLower().Equals("product"))
                        _qTime_isProduct = true;


#if DEBUG
                    _adsTable = configuration["SyncExtenalData:AdsInfo:Table:Debug"] is null ? "rtd_ewlb_ads_vw" : (configuration["SyncExtenalData:AdsInfo:Table:Debug"].ToString());
                    _qTimeTable = configuration["QTimeTable:Table:Debug"] is null ? "rtd_ewlb_qtime_vw" : configuration["QTimeTable:Table:Debug"].ToString();
#else
                    _adsTable = configuration["SyncExtenalData:AdsInfo:Table:Prod"] is null ? "rtd_ewlb_ads_vw" : (configuration["SyncExtenalData:AdsInfo:Table:Prod"].ToString());
                    _qTimeTable = configuration["QTimeTable:Table:Prod"] is null ? "rtd_ewlb_qtime_vw" : configuration["QTimeTable:Table:Prod"].ToString();
#endif

                }
                catch (Exception ex) { }


                //SelectTableEQPStatusInfoByEquipID
                //先取得Equipment 的Ports Status
                sql = string.Format(_BaseDataService.SelectTableEQPStatusInfoByEquipID(_Equip));
                dtTemp = _dbTool.GetDataTable(sql);
                try {
                    if (dtTemp.Rows.Count > 0)
                    {
                        _furnState = int.Parse(dtTemp.Rows[0]["fvcStatus"].ToString());
                    }
                } catch(Exception ex) { }

                //取RTS Equipment Status
                dtTemp = _dbTool.GetDataTable(_BaseDataService.GetRTSEquipStatus(GetExtenalTables(configuration, "SyncExtenalData", "RTSEQSTATE"), _Equip));
                if (dtTemp.Rows.Count > 0)
                {
                    //machine_state, curr_status, down_state
                    //rtsMachineState, rtsCurrentState, rtsDownState
                    rtsMachineState = dtTemp.Rows[0]["machine_state"].ToString();
                    rtsCurrentState = dtTemp.Rows[0]["curr_status"].ToString();
                    rtsDownState = dtTemp.Rows[0]["down_state"].ToString();
                }
                else
                {
                    if (_DebugMode)
                    {
                        _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}] GetRTSEquipStatus[{1}] _Out", _oEventQ.EventName, _Equip));
                    }
                    return false; //RTS找不到, 即不繼續產生指令
                }

                switch (_oEventQ.EventName)
                {
                    case "CarrierLocationUpdate":
                        //Do Event CarrierLocationUpdate
                        CarrierLocationUpdate evtObject = (CarrierLocationUpdate)_oEventQ.EventObject;
                        CarrierID = evtObject.CarrierID;
                        sql = _BaseDataService.QueryLotInfoByCarrierID(CarrierID);
                        dtTemp = _dbTool.GetDataTable(sql);
                        if (dtTemp.Rows.Count > 0)
                        {
                            lotID = dtTemp.Rows[0]["lot_id"].ToString();
                        }
                        break;
                    case "LotEquipmentAssociateUpdate":
                        //STATE=WAIT , EQUIP_ASSO=N >> Do this process.
                        evtObject2 = (NormalTransferModel)_oEventQ.EventObject;
                        CarrierID = evtObject2.CarrierID;
                        lotID = evtObject2.LotID;
                        break;
                    case "AutoCheckEquipmentStatus":
                        //STATE=WAIT , EQUIP_ASSO=N >> Do this process.
                        evtObject2 = (NormalTransferModel)_oEventQ.EventObject;
                        lotID = evtObject2.LotID;
                        CarrierID = evtObject2.CarrierID;
                        bStateChange = true;
                        break;
                    case "AbnormalyEquipmentStatus":
                        evtObject2 = (NormalTransferModel)_oEventQ.EventObject;
                        lotID = evtObject2.LotID;
                        if (_DebugMode)
                        {
                            _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}] {1} / {2}", _oEventQ.EventName, _Equip, lotID));
                        }
                        bStateChange = true;
                        break;
                    case "EquipmentPortStatusUpdate":
                        evtObject2 = (NormalTransferModel)_oEventQ.EventObject;
                        lotID = evtObject2.LotID;

                        if (evtObject2.CarrierID.Equals(""))
                        {
                            sql = string.Format(_BaseDataService.SelectTableCarrierAssociate3ByLotid(lotID));
                            dtTemp = _dbTool.GetDataTable(sql);

                            if(dtTemp.Rows.Count > 0)
                            {
                                UnloadCarrierID = dtTemp.Rows[0]["carrier_id"].ToString();
                            }
                        }
                        else
                        {
                            UnloadCarrierID = evtObject2.CarrierID;
                        }
                        
                        if (_DebugMode)
                        {
                            _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}] {1} / {2}", _oEventQ.EventName, _Equip, lotID));
                        }
                        bStateChange = true;
                        break;
                    default:
                        break;
                }

                //QTIME Logic: lot QTime < Low or QTime > High do not build load command.
                //Get Q-Time set for this Equipment
                try
                {
                    if (_QTimeMode.Equals("1"))
                    {
                        //取 QTiime Limit from
                        //by Equipment 
                        iQTimeLow = 0;
                        iQTimeHigh = 0;
                        sql = _BaseDataService.SelectWorkgroupSet(_Equip);
                        dtTemp = _dbTool.GetDataTable(sql);

                        if (dtTemp.Rows.Count > 0)
                        {
                            iQTimeLow = dtTemp.Rows[0]["QTIME_LOW"] is not null ? int.Parse(dtTemp.Rows[0]["QTIME_LOW"].ToString()) : 0;
                            iQTimeHigh = dtTemp.Rows[0]["QTIME_HIGH"] is not null ? int.Parse(dtTemp.Rows[0]["QTIME_HIGH"].ToString()) : 0;
                        }
                        else
                        {
                            iQTimeLow = 0;
                            iQTimeHigh = 0;
                        }
                    }

                    //_logger.Debug(string.Format("[Q-Time Logic][{0}][{1}] {2}/{3}", _Equip, iQTimeLow, iQTimeHigh));
                }
                catch (Exception ex)
                {
                    _logger.Debug(string.Format("[Q-Time Logic][{0}][Exception: {1}]", _Equip, ex.Message));
                }

                //FVC 
                try {
                    dtTemp = _dbTool.GetDataTable(_BaseDataService.SelectWorkgroupSet(_Equip));

                    if (dtTemp.Rows.Count > 0)
                    {
                        try { 
                            _isFurnace = dtTemp.Rows[0]["IsFurnace"] is not null ? dtTemp.Rows[0]["IsFurnace"].ToString().Equals("1") ? true : false : false;
                            _dummycarrier = dtTemp.Rows[0]["dummy_locate"]  is not null ? dtTemp.Rows[0]["dummy_locate"].ToString() : "";
                            //_maximumqty, _effectiveslot
                            _effectiveslot = dtTemp.Rows[0]["effectiveslot"] is not null ? dtTemp.Rows[0]["effectiveslot"].ToString() : "";
                        }
                        catch (Exception ex) { }
                    }
                }
                catch(Exception ex) { }

                //站點不當前站點不同, 不產生指令
                if (!lotID.Equals(""))
                {
                    //站點不當前站點不同, 不產生指令
                    vStage = "";
#if DEBUG
                    sql = _BaseDataService.CheckLotStage("lot_info", lotID);
#else
sql = _BaseDataService.CheckLotStage(configuration["CheckLotStage:Table"], lotID);
#endif
                    dtTemp = _dbTool.GetDataTable(sql);

                    if (dtTemp.Rows.Count > 0)
                    {
                        //Stabe2 ADS, Stage 1 RTD
                        vStage = dtTemp.Rows[0]["stage2"].ToString().Equals("") ? dtTemp.Rows[0]["stage1"].ToString() : dtTemp.Rows[0]["stage2"].ToString();
                        //
                        vStage2 = dtTemp.Rows[0]["stage1"].ToString().Equals("") ? dtTemp.Rows[0]["stage2"].ToString() : dtTemp.Rows[0]["stage1"].ToString();
                        if (!dtTemp.Rows[0]["stage1"].ToString().Equals(dtTemp.Rows[0]["stage2"].ToString()))
                        {
                            _logger.Debug(string.Format("Base Information: LotID= {0}, RTD_Stage={1}, MES_Stage={2}, RTD_State={3}, MES_State={4}", lotID, dtTemp.Rows[0]["stage1"].ToString(), dtTemp.Rows[0]["stage2"].ToString(), dtTemp.Rows[0]["state1"].ToString(), dtTemp.Rows[0]["state2"].ToString()));
                            if (configuration["AppSettings:Work"].ToUpper().Equals("EWLB"))
                            {
                                if (bStateChange)
                                {
                                    bStageIssue = true;
                                }
                                else
                                {
                                    //_logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}] lot [{1}] stage not correct. can not build command.", _oEventQ.EventName, lotID));
                                    _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}] lot [{1}] Check Stage Failed. _Out", _oEventQ.EventName, lotID));
                                    //if (configuration["AppSettings:Work"].ToUpper().Equals("EWLB"))
                                    return result;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (configuration["AppSettings:Work"].ToUpper().Equals("EWLB"))
                        {
                            //Unload lot. not need control.
                            _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}] The lot [{1}] have stage issue, does not belong to current stage.", _oEventQ.EventName, lotID));
                            //return result;
                            bStageIssue = true;
                        }
                    }

                    //取得Lot QTime from ads
                    if (_QTimeMode.Equals("1"))
                    {
                        try
                        {
                            //Upgrade program logic for QTime
                            //Position: 202310111000001
                            //取得Lot QTime from ads
                            iLotQTime = 0;

                            sql = _BaseDataService.GetQTimeLot(configuration["PreDispatchToErack:lotState:tableName"], lotID);
                            dtTemp = _dbTool.GetDataTable(sql);

                            if (dtTemp.Rows.Count > 0)
                            {
                                bTurnOnQTime = dtTemp.Rows[0]["QTime"].ToString().Equals("NA") ? false : true;
                                if (bTurnOnQTime)
                                    iLotQTime = float.Parse(dtTemp.Rows[0]["QTime"].ToString());
                                else
                                    iLotQTime = 0;
                            }
                            else
                            {
                                bTurnOnQTime = false;
                                iLotQTime = 0;
                            }

                            _logger.Debug(string.Format("[Q-Time Logic SW][{0}][{1}][{2}][{3}/{4}] Lot QTime: {5}", lotID, _Equip, bTurnOnQTime, iQTimeLow, iQTimeHigh, iLotQTime));
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug(string.Format("[Q-Time Logic][{0}][{1}] Get Ads Data Failed. {2}", lotID, _Equip, ex.Message));
                        }
                    }

                    try
                    {
                        if (configuration["AppSettings:Work"].ToUpper().Equals("EWLB"))
                        {

                            //CheckMeasurementAndThickness Logic
                            try
                            {
                                if (!vStage.Equals(""))
                                {
                                    sql = _BaseDataService.CheckMeasurementAndThickness(configuration["MeasurementThickness:Table"], lotID, vStage, "");
                                    dtTemp2 = _dbTool.GetDataTable(sql);
                                    _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}]", "CheckMeasurementAndThickness", sql, dtTemp2.Rows.Count.ToString()));

                                    if (dtTemp2.Rows.Count > 2)
                                    {
                                        //已失敗過2次, 不再執行
                                        return result;
                                    }
                                    else if (dtTemp2.Rows.Count >= 1)
                                    {
                                        //曾失敗過一次,再失敗就移至Fail Erack
                                        isMeasureFail = true;
                                    }
                                    else
                                    {
                                        isMeasureFail = false;
                                    }
                                }
                                else
                                {
                                    string _table = configuration["eRackDisplayInfo:contained"].ToString().Split(',')[1];
                                    sql = _BaseDataService.CheckLotStageHold(_table, lotID);
                                    dtTemp = _dbTool.GetDataTable(sql);

                                    if (dtTemp.Rows.Count > 0)
                                    {
                                        vStage = dtTemp.Rows[0]["stage2"].ToString().Equals("") ? vStage : dtTemp.Rows[0]["stage2"].ToString();
                                    }

                                    if (!vStage.Equals(""))
                                    {
                                        sql = _BaseDataService.CheckMeasurementAndThickness(configuration["MeasurementThickness:Table"], lotID, vStage, "");
                                        dtTemp2 = _dbTool.GetDataTable(sql);
                                        _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}] _", "CheckMeasurementAndThickness2", sql, dtTemp2.Rows.Count.ToString()));

                                        if (dtTemp2.Rows.Count > 2)
                                        {
                                            //已失敗過2次, 不再執行
                                            return result;
                                        }
                                        else if (dtTemp2.Rows.Count >= 1)
                                        {
                                            //曾失敗過一次,再失敗就移至Fail Erack
                                            isMeasureFail = true;
                                        }
                                        else
                                        {
                                            isMeasureFail = false;
                                        }
                                    }
                                    else
                                    {
                                        return result;
                                    }
                                }

                                _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}]", "MeasureFail", isMeasureFail));
                            }
                            catch (Exception ex)
                            {
                                _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}]", "Exception", ex.Message));
                            }
                        }

                        //AvailableAoiMachine Logic
                        try
                        {
                            if (_aoiMeasurementLogic)
                            {

                                strAvailableAoiMachine = "";
                                sql = _BaseDataService.QueryAvailbleAOIMachineByLotid(lotID, GetExtenalTables(configuration, "SyncExtenalData", "availabeAoi"));
                                dtTemp = _dbTool.GetDataTable(sql);
                                if (dtTemp.Rows.Count > 0)
                                {
                                    lstAvailableAoiMachine = new List<string>();
                                    strAvailableAoiMachine = dtTemp.Rows[0]["equip2"].ToString().Replace(" ", "");

                                    _NotInlookupTab = dtTemp.Rows[0]["equip"].ToString().Trim().Equals("Not in Lookup Table") ? true : false;

                                    if (strAvailableAoiMachine.Contains(','))
                                    {
                                        string[] tmpList = strAvailableAoiMachine.Split(',');
                                        if (tmpList.Length > 0)
                                        {
                                            foreach (string tmpRec in tmpList)
                                            {
                                                if (!tmpRec.Trim().Equals(""))
                                                    lstAvailableAoiMachine.Add(tmpRec);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!strAvailableAoiMachine.Trim().Equals(""))
                                            lstAvailableAoiMachine.Add(strAvailableAoiMachine);
                                    }

                                    if (lstAvailableAoiMachine.Count() > 0)
                                        _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}]", "AvailableAoiMachine", lstAvailableAoiMachine.Count(), lstAvailableAoiMachine.ToString()));
                                }
                            }
                        }
                        catch(Exception ex) { }
                        
                    } catch (Exception ex)
                    { }

                    if(_NotInlookupTab || lstAvailableAoiMachine is not null)
                    {
                        var bRet = lstAvailableAoiMachine.Exists(t => t == _Equip);
                        _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}][{3}][{4}][{5}] _", "AvailableAoiMachine", bRet, lstAvailableAoiMachine.ToString(), lotID, vStage, _Equip));
                        if (bRet)
                        {
                            //Do Nothing
                            sql = _BaseDataService.CheckMeasurementAndThickness(configuration["MeasurementThickness:Table"], lotID, vStage, _Equip);
                            dtTemp2 = _dbTool.GetDataTable(sql);

                            if (dtTemp2.Rows.Count > 0)
                            {
                                return result;
                            }
                        }
                        else
                        {
                            sql = _BaseDataService.CheckPortStateIsUnload(_Equip);
                            dtTemp3 = _dbTool.GetDataTable(sql);

                            if (dtTemp3.Rows.Count > 0)
                            {
                                //有unload, 繼續跑
                                //Keep Going
                            }
                            else
                            {
                                return result;
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            sql = _BaseDataService.CheckMeasurementAndThickness(configuration["MeasurementThickness:Table"], lotID, vStage, _Equip);
                            dtTemp2 = _dbTool.GetDataTable(sql);

                            if (dtTemp2.Rows.Count > 0)
                            {
                                _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}][{3}][{4}] _", "Filter0", lotID, vStage, _Equip, sql));

                                sql = _BaseDataService.CheckPortStateIsUnload(_Equip);
                                dtTemp3 = _dbTool.GetDataTable(sql);

                                if (dtTemp3.Rows.Count > 0)
                                {
                                    //有unload, 繼續跑
                                    //Keep Going
                                }
                                else
                                {
                                    return result;
                                }
                            }
                            else
                            {
                                _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}][{3}][{4}]", "Filter1", lotID, vStage, _Equip, sql));
                            }
                        } catch(Exception ex) { }
                    }
                }
                else
                {
                    if (configuration["AppSettings:Work"].ToUpper().Equals("EWLB"))
                    {
                        //CheckMeasurementAndThickness Logic
                        try
                        {
                            //屬於Measurement & Thickness lot, 一定得由Lot ID決定Equipment
                            sql = _BaseDataService.CheckMeasurementAndThickness(configuration["MeasurementThickness:Table"], lotID, vStage, _Equip);
                            dtTemp2 = _dbTool.GetDataTable(sql);

                            if (dtTemp2.Rows.Count > 0)
                            {
                                _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}][{3}][{4}] _", "Filter0", lotID, vStage, _Equip, sql));

                                sql = _BaseDataService.CheckPortStateIsUnload(_Equip);
                                dtTemp3 = _dbTool.GetDataTable(sql);

                                if (dtTemp3.Rows.Count > 0)
                                {
                                    //有unload, 繼續跑
                                    //Keep Going
                                }
                                else
                                {
                                    return result;
                                }
                            }
                            else
                            {
                                _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}][{3}][{4}]", "Filter1", lotID, vStage, _Equip, sql));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}]", "Exception", ex.Message));
                        }
                    }
                }

                if (dtPortsInfo.Rows.Count <= 0)
                { return result; }
                else
                {
                    try
                    {
                        rtdEqpCustDevice = dtPortsInfo.Rows[0]["CustDevice"] is null ? "" : dtPortsInfo.Rows[0]["CustDevice"].ToString().Trim();
                    }catch(Exception ex)
                    {
                        rtdEqpCustDevice = "";
                    }

                    isManualMode = dtPortsInfo.Rows[0]["manualmode"] is null ? false : dtPortsInfo.Rows[0]["manualmode"].ToString().Equals(1) ? true : false;
                    _expired = dtPortsInfo.Rows[0]["expired"] is null ? false : dtPortsInfo.Rows[0]["expired"].ToString().Equals(1) ? true : false;
                    _effective = dtPortsInfo.Rows[0]["effective"] is null ? false : dtPortsInfo.Rows[0]["effective"].ToString().Equals(1) ? true : false;
                    //_priority = dtPortsInfo.Rows[0]["prio"] is null ? 20 : int.Parse(dtPortsInfo.Rows[0]["prio"].ToString());

                    _logger.Debug(string.Format("[GetEquipInfo 2]: EQUIPID[{0}], {1}/{2}/{3}/{4}", _Equip, rtdEqpCustDevice, isManualMode, _expired, _effective));
                }

                //上機失敗皆回(in erack)
                NormalTransferModel normalTransfer = new NormalTransferModel();
                TransferList lstTransfer = new TransferList();
                normalTransfer.EquipmentID = _Equip;
                normalTransfer.PortModel = _portModel;
                normalTransfer.Transfer = new List<TransferList>();
                normalTransfer.LotID = lotID;
                int iCheckState = 0;
                bool bIsMatch = true;
                string EquipCustDevice = "";
                bool bCheckRecipe = false;
                Dictionary<string, string> _dicStageCtrl = new Dictionary<string, string>();

                string _portCarrierType = "";

                //input: 空的 normalTransfer, dtPortsInfo, 

                //====================================
                //Port Type:
                //0. Out of Service
                //1. Transfer Blocked
                //2. Near Completion
                //3. Ready to Unload
                //4. Empty (Ready to load)
                //5. Reject and Ready to unload
                //6. Port Alarm
                //9. Unknow
                //====================================

                int iReplace = 0;
                string sPortState = "";
                int iPortState = 0;
                bool portLock = false;
                string sPortID = "";
                string slastLotID = "";
                bool _OnlyUnload = false;
                string _CarrierTypebyPort = "";
                foreach (DataRow drRecord in dtPortsInfo.Rows)
                {
                    //dtWorkInProcessSch = _dbTool.GetDataTable(_BaseDataService.SelectTableWorkInProcessSchByPortId(drRecord["PORT_ID"].ToString()));
                    //if (dtWorkInProcessSch.Rows.Count > 0)
                    //    continue;
                    _dicStageCtrl = new Dictionary<string, string>();//reset dicStageCtrl for next port
                    lstTransfer = new TransferList();
                    dtAvaileCarrier = null;
                    dtLoadPortCarrier = null;
                    portLock = false;
                    sPortID = "";
                    _stageController = false;
                    _cannotSame = false;

                    //SelectTableEQP_Port_SetByPortId
                    try
                    {
                        sPortID = drRecord["port_id"].ToString();

                        dtTemp = _dbTool.GetDataTable(_BaseDataService.SelectTableEQP_Port_SetByPortId(sPortID));
                        if (dtTemp.Rows.Count > 0)
                        {
                            portLock = dtTemp.Rows[0]["isLock"].ToString().Equals("1") ? true : false;

                            if (portLock)
                            {
                                _logger.Debug(string.Format("[CheckPortLock][{0}] isLock:[{1}]", sPortID, portLock));
                                continue;
                            }
                        }

                        dtTemp = _dbTool.GetDataTable(_BaseDataService.GetCarrierTypeByPort(sPortID));
                        if (dtTemp.Rows.Count > 0)
                        {
                            _CarrierTypebyPort = dtTemp.Rows[0]["command_type"].ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug(string.Format("[CheckPortLock][{0}] {1} , Exception: {2}", sPortID, EquipCustDevice, ex.Message));
                    }

                    try
                    {
                        dtTemp = _dbTool.GetDataTable(_BaseDataService.GetEquipCustDevice(drRecord["EQUIPID"].ToString()));
                        if (dtTemp.Rows.Count > 0)
                        {
                            EquipCustDevice = dtTemp.Rows[0]["device"] is null ? "" : dtTemp.Rows[0]["device"].ToString();
                            _logger.Debug(string.Format("[GetEquipCustDevice]: {0}, [CustDevice]: {1} ", drRecord["EQUIPID"].ToString(), EquipCustDevice));

                            if (!rtdEqpCustDevice.Equals(EquipCustDevice))
                            {
                                try
                                {
                                    sql = _BaseDataService.UpdateCustDeviceByEquipID(drRecord["EQUIPID"].ToString(), EquipCustDevice);
                                    _dbTool.SQLExec(sql, out tmpMsg, true);
                                }
                                catch (Exception ex) { }
                            }
                        }
                        else
                        {
                            if (bCheckDevice)
                                continue;
                        }
                    }catch(Exception ex)
                    {
                        _logger.Debug(string.Format("[GetEquipCustDevice][{0}] {1} , Exception: {2}", drRecord["EQUIPID"].ToString(), EquipCustDevice, ex.Message));
                    }
                    //Select Workgroup Set
                    dtWorkgroupSet = _dbTool.GetDataTable(_BaseDataService.SelectWorkgroupSet(drRecord["EQUIPID"].ToString()));

                    if(dtWorkgroupSet.Rows.Count > 0)
                    {
                        //Get WorkgroupSet 設定
                        try
                        {
                            if (dtWorkgroupSet.Rows.Count >= 1)
                            {
                                //Get eRack Set: in/out and Fail e-Rack, Lookup Table, priority of stage
                                try
                                {
                                    _eqpworkgroup = dtWorkgroupSet.Rows[0]["workgroup"] is null ? "" : dtWorkgroupSet.Rows[0]["workgroup"].ToString();

                                    string cdtTemp = string.Format("STAGE='{0}'", vStage);
                                    DataRow[] drWorkgroup = dtWorkgroupSet.Select(cdtTemp);

                                    if (drWorkgroup.Length > 0)
                                    {
                                        useFaileRack = drWorkgroup[0]["USEFAILERACK"] is null ? false : drWorkgroup[0]["USEFAILERACK"].ToString().Equals("1") ? true : false;
                                        faileRack = drWorkgroup[0]["F_ERACK"] is null ? "" : drWorkgroup[0]["F_ERACK"].ToString();
                                        outeRack = drWorkgroup[0]["OUT_ERACK"] is null ? "" : drWorkgroup[0]["OUT_ERACK"].ToString();
                                        ineRack = drWorkgroup[0]["IN_ERACK"] is null ? "" : drWorkgroup[0]["IN_ERACK"].ToString();
                                        bCheckEquipLookupTable = drWorkgroup[0]["CHECKEQPLOOKUPTABLE"] is null ? false : drWorkgroup[0]["CHECKEQPLOOKUPTABLE"].ToString().Equals("1") ? true : false;
                                        //rcpconstraint
                                        bCheckRcpConstraint = drWorkgroup[0]["RCPCONSTRAINT"] is null ? false : drWorkgroup[0]["RCPCONSTRAINT"].ToString().Equals("1") ? true : false;
                                        _priority = drWorkgroup[0]["prio"] is null ? 20 : int.Parse(drWorkgroup[0]["prio"].ToString());
                                        //effectiveslot, maximumqty
                                        bCheckRecipe = drWorkgroup[0]["CHECKCUSTDEVICE"] is null ? false : drWorkgroup[0]["CHECKCUSTDEVICE"].ToString().Equals("1") ? true : false;
                                        _stageController = drWorkgroup[0]["stagecontroller"] is null ? false : drWorkgroup[0]["stagecontroller"].ToString().Equals("1") ? true : false;
                                        _cannotSame = drWorkgroup[0]["cannotsame"] is null ? false : drWorkgroup[0]["cannotsame"].ToString().Equals("1") ? true : false;
                                        _aoiMeasurementLogic = drWorkgroup[0]["aoimeasurement"] is null ? false : drWorkgroup[0]["aoimeasurement"].ToString().Equals("1") ? true : false;
                                    }
                                    else
                                    {
                                        if (!vStage.Equals(vStage2))
                                        {
                                            //Stage and Stage2 not same 
                                            cdtTemp = string.Format("STAGE='{0}'", vStage2);
                                            drWorkgroup = dtWorkgroupSet.Select(cdtTemp);

                                            if (drWorkgroup.Length > 0)
                                            {
                                                useFaileRack = drWorkgroup[0]["USEFAILERACK"] is null ? false : drWorkgroup[0]["USEFAILERACK"].ToString().Equals("1") ? true : false;
                                                faileRack = drWorkgroup[0]["F_ERACK"] is null ? "" : drWorkgroup[0]["F_ERACK"].ToString();
                                                outeRack = drWorkgroup[0]["OUT_ERACK"] is null ? "" : drWorkgroup[0]["OUT_ERACK"].ToString();
                                                ineRack = drWorkgroup[0]["IN_ERACK"] is null ? "" : drWorkgroup[0]["IN_ERACK"].ToString();
                                                bCheckEquipLookupTable = drWorkgroup[0]["CHECKEQPLOOKUPTABLE"] is null ? false : drWorkgroup[0]["CHECKEQPLOOKUPTABLE"].ToString().Equals("1") ? true : false;
                                                //rcpconstraint
                                                bCheckRcpConstraint = drWorkgroup[0]["RCPCONSTRAINT"] is null ? false : drWorkgroup[0]["RCPCONSTRAINT"].ToString().Equals("1") ? true : false;
                                                _priority = drWorkgroup[0]["prio"] is null ? 20 : int.Parse(drWorkgroup[0]["prio"].ToString());
                                                bCheckRecipe = drWorkgroup[0]["CHECKCUSTDEVICE"] is null ? false : drWorkgroup[0]["CHECKCUSTDEVICE"].ToString().Equals("1") ? true : false;
                                                _stageController = drWorkgroup[0]["stagecontroller"] is null ? false : drWorkgroup[0]["stagecontroller"].ToString().Equals("1") ? true : false;
                                                _cannotSame = drWorkgroup[0]["cannotsame"] is null ? false : drWorkgroup[0]["cannotsame"].ToString().Equals("1") ? true : false;
                                                _aoiMeasurementLogic = drWorkgroup[0]["aoimeasurement"] is null ? false : drWorkgroup[0]["aoimeasurement"].ToString().Equals("1") ? true : false;
                                            }
                                            else
                                            {
                                                cdtTemp = string.Format("STAGE='{0}'", "DEFAULT");
                                                drWorkgroup = dtWorkgroupSet.Select(cdtTemp);

                                                if (drWorkgroup.Length > 0)
                                                {
                                                    useFaileRack = drWorkgroup[0]["USEFAILERACK"] is null ? false : drWorkgroup[0]["USEFAILERACK"].ToString().Equals("1") ? true : false;
                                                    faileRack = drWorkgroup[0]["F_ERACK"] is null ? "" : drWorkgroup[0]["F_ERACK"].ToString();
                                                    outeRack = drWorkgroup[0]["OUT_ERACK"] is null ? "" : drWorkgroup[0]["OUT_ERACK"].ToString();
                                                    ineRack = drWorkgroup[0]["IN_ERACK"] is null ? "" : drWorkgroup[0]["IN_ERACK"].ToString();
                                                    bCheckEquipLookupTable = drWorkgroup[0]["CHECKEQPLOOKUPTABLE"] is null ? false : drWorkgroup[0]["CHECKEQPLOOKUPTABLE"].ToString().Equals("1") ? true : false;
                                                    //rcpconstraint
                                                    bCheckRcpConstraint = drWorkgroup[0]["RCPCONSTRAINT"] is null ? false : drWorkgroup[0]["RCPCONSTRAINT"].ToString().Equals("1") ? true : false;
                                                    _priority = drWorkgroup[0]["prio"] is null ? 20 : int.Parse(drWorkgroup[0]["prio"].ToString());
                                                    bCheckRecipe = drWorkgroup[0]["CHECKCUSTDEVICE"] is null ? false : drWorkgroup[0]["CHECKCUSTDEVICE"].ToString().Equals("1") ? true : false;
                                                    _stageController = drWorkgroup[0]["stagecontroller"] is null ? false : drWorkgroup[0]["stagecontroller"].ToString().Equals("1") ? true : false;
                                                    _cannotSame = drWorkgroup[0]["cannotsame"] is null ? false : drWorkgroup[0]["cannotsame"].ToString().Equals("1") ? true : false;
                                                    _aoiMeasurementLogic = drWorkgroup[0]["aoimeasurement"] is null ? false : drWorkgroup[0]["aoimeasurement"].ToString().Equals("1") ? true : false;
                                                }
                                                else
                                                {
                                                    useFaileRack = dtWorkgroupSet.Rows[0]["USEFAILERACK"] is null ? false : dtWorkgroupSet.Rows[0]["USEFAILERACK"].ToString().Equals("1") ? true : false;
                                                    faileRack = dtWorkgroupSet.Rows[0]["F_ERACK"] is null ? "" : dtWorkgroupSet.Rows[0]["F_ERACK"].ToString();
                                                    outeRack = dtWorkgroupSet.Rows[0]["OUT_ERACK"] is null ? "" : dtWorkgroupSet.Rows[0]["OUT_ERACK"].ToString();
                                                    ineRack = dtWorkgroupSet.Rows[0]["IN_ERACK"] is null ? "" : dtWorkgroupSet.Rows[0]["IN_ERACK"].ToString();
                                                    bCheckEquipLookupTable = dtWorkgroupSet.Rows[0]["CHECKEQPLOOKUPTABLE"] is null ? false : dtWorkgroupSet.Rows[0]["CHECKEQPLOOKUPTABLE"].ToString().Equals("1") ? true : false;
                                                    //rcpconstraint
                                                    bCheckRcpConstraint = dtWorkgroupSet.Rows[0]["RCPCONSTRAINT"] is null ? false : dtWorkgroupSet.Rows[0]["RCPCONSTRAINT"].ToString().Equals("1") ? true : false;
                                                    bCheckRecipe = dtWorkgroupSet.Rows[0]["CHECKCUSTDEVICE"] is null ? false : dtWorkgroupSet.Rows[0]["CHECKCUSTDEVICE"].ToString().Equals("1") ? true : false;
                                                    _priority = 20;
                                                    _stageController = dtWorkgroupSet.Rows[0]["stagecontroller"] is null ? false : dtWorkgroupSet.Rows[0]["stagecontroller"].ToString().Equals("1") ? true : false;
                                                    _cannotSame = dtWorkgroupSet.Rows[0]["cannotsame"] is null ? false : dtWorkgroupSet.Rows[0]["cannotsame"].ToString().Equals("1") ? true : false;
                                                    _aoiMeasurementLogic = dtWorkgroupSet.Rows[0]["aoimeasurement"] is null ? false : dtWorkgroupSet.Rows[0]["aoimeasurement"].ToString().Equals("1") ? true : false;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            cdtTemp = string.Format("STAGE='{0}'", "DEFAULT");
                                            drWorkgroup = dtWorkgroupSet.Select(cdtTemp);

                                            if (drWorkgroup.Length > 0)
                                            {
                                                useFaileRack = drWorkgroup[0]["USEFAILERACK"] is null ? false : drWorkgroup[0]["USEFAILERACK"].ToString().Equals("1") ? true : false;
                                                faileRack = drWorkgroup[0]["F_ERACK"] is null ? "" : drWorkgroup[0]["F_ERACK"].ToString();
                                                outeRack = drWorkgroup[0]["OUT_ERACK"] is null ? "" : drWorkgroup[0]["OUT_ERACK"].ToString();
                                                ineRack = drWorkgroup[0]["IN_ERACK"] is null ? "" : drWorkgroup[0]["IN_ERACK"].ToString();
                                                bCheckEquipLookupTable = drWorkgroup[0]["CHECKEQPLOOKUPTABLE"] is null ? false : drWorkgroup[0]["CHECKEQPLOOKUPTABLE"].ToString().Equals("1") ? true : false;
                                                //rcpconstraint
                                                bCheckRcpConstraint = drWorkgroup[0]["RCPCONSTRAINT"] is null ? false : drWorkgroup[0]["RCPCONSTRAINT"].ToString().Equals("1") ? true : false;
                                                _priority = drWorkgroup[0]["prio"] is null ? 20 : int.Parse(drWorkgroup[0]["prio"].ToString());
                                                bCheckRecipe = drWorkgroup[0]["CHECKCUSTDEVICE"] is null ? false : drWorkgroup[0]["CHECKCUSTDEVICE"].ToString().Equals("1") ? true : false;
                                                _stageController = drWorkgroup[0]["stagecontroller"] is null ? false : drWorkgroup[0]["stagecontroller"].ToString().Equals("1") ? true : false;
                                                _cannotSame = drWorkgroup[0]["cannotsame"] is null ? false : drWorkgroup[0]["cannotsame"].ToString().Equals("1") ? true : false;
                                                _aoiMeasurementLogic = drWorkgroup[0]["aoimeasurement"] is null ? false : drWorkgroup[0]["aoimeasurement"].ToString().Equals("1") ? true : false;
                                            }
                                            else
                                            {
                                                useFaileRack = dtWorkgroupSet.Rows[0]["USEFAILERACK"] is null ? false : dtWorkgroupSet.Rows[0]["USEFAILERACK"].ToString().Equals("1") ? true : false;
                                                faileRack = dtWorkgroupSet.Rows[0]["F_ERACK"] is null ? "" : dtWorkgroupSet.Rows[0]["F_ERACK"].ToString();
                                                outeRack = dtWorkgroupSet.Rows[0]["OUT_ERACK"] is null ? "" : dtWorkgroupSet.Rows[0]["OUT_ERACK"].ToString();
                                                ineRack = dtWorkgroupSet.Rows[0]["IN_ERACK"] is null ? "" : dtWorkgroupSet.Rows[0]["IN_ERACK"].ToString();
                                                bCheckEquipLookupTable = dtWorkgroupSet.Rows[0]["CHECKEQPLOOKUPTABLE"] is null ? false : dtWorkgroupSet.Rows[0]["CHECKEQPLOOKUPTABLE"].ToString().Equals("1") ? true : false;
                                                //rcpconstraint
                                                bCheckRcpConstraint = drWorkgroup[0]["RCPCONSTRAINT"] is null ? false : drWorkgroup[0]["RCPCONSTRAINT"].ToString().Equals("1") ? true : false;
                                                _priority = 30;
                                                bCheckRecipe = dtWorkgroupSet.Rows[0]["CHECKCUSTDEVICE"] is null ? false : dtWorkgroupSet.Rows[0]["CHECKCUSTDEVICE"].ToString().Equals("1") ? true : false;
                                                _stageController = dtWorkgroupSet.Rows[0]["stagecontroller"] is null ? false : dtWorkgroupSet.Rows[0]["stagecontroller"].ToString().Equals("1") ? true : false;
                                                _cannotSame = dtWorkgroupSet.Rows[0]["cannotsame"] is null ? false : dtWorkgroupSet.Rows[0]["cannotsame"].ToString().Equals("1") ? true : false;
                                                _aoiMeasurementLogic = dtWorkgroupSet.Rows[0]["aoimeasurement"] is null ? false : dtWorkgroupSet.Rows[0]["aoimeasurement"].ToString().Equals("1") ? true : false;
                                            }
                                        }
                                    }

                                    _logger.Debug(string.Format("[WorkgroupSet][{0}][{1}]{2}[{3}][{4}][{5}][{6}][{7}][{8}]", drRecord["EQUIPID"].ToString(), vStage, useFaileRack, faileRack, outeRack, ineRack, bCheckEquipLookupTable, _priority, bCheckRcpConstraint));
                                }
                                catch(Exception ex)
                                { }

                                try
                                {
                                    if (_QTimeMode.Equals(1))
                                    {
                                        //Get QTime limit.
                                        //Upgrade program logic for QTime
                                        //Position: 202310111000001
                                        if (!vStage.Equals(""))
                                        {
                                            try
                                            {
                                                _logger.Debug(string.Format("[Q-Time Logic][{0}][{1}]{2}[{3}][{4}/{5}]", drRecord["EQUIPID"].ToString(), vStage, bTurnOnQTime, iLotQTime, iQTimeLow, iQTimeHigh));

                                                string cdtTemp = string.Format("STAGE='{0}'", vStage);
                                                DataRow[] drWorkgroup = dtWorkgroupSet.Select(cdtTemp);

                                                if (drWorkgroup.Length > 0)
                                                {
                                                    iQTimeLow = drWorkgroup[0]["QTIME_LOW"] is null ? 0 : float.Parse(drWorkgroup[0]["QTIME_LOW"].ToString());
                                                    iQTimeHigh = drWorkgroup[0]["QTIME_HIGH"] is null ? 0 : float.Parse(drWorkgroup[0]["QTIME_HIGH"].ToString());
                                                }
                                                else
                                                {
                                                    cdtTemp = string.Format("STAGE='{0}'", "DEFAULT");
                                                    drWorkgroup = dtWorkgroupSet.Select(cdtTemp);

                                                    if (drWorkgroup.Length > 0)
                                                    {
                                                        iQTimeLow = drWorkgroup[0]["QTIME_LOW"] is null ? 0 : float.Parse(drWorkgroup[0]["QTIME_LOW"].ToString());
                                                        iQTimeHigh = drWorkgroup[0]["QTIME_HIGH"] is null ? 0 : float.Parse(drWorkgroup[0]["QTIME_HIGH"].ToString());
                                                    }
                                                    else
                                                    {
                                                        iQTimeLow = 0;
                                                        iQTimeHigh = 0;
                                                    }

                                                    _logger.Debug(string.Format("[Get Q-Time Value By Stage][{0}][{1}]{2}[{3}][{4}/{5}]", drRecord["EQUIPID"].ToString(), vStage, bTurnOnQTime, iLotQTime, iQTimeLow, iQTimeHigh));
                                                }
                                            }
                                            catch (Exception ex) { }
                                        }
                                        else
                                        {
                                            try
                                            {
                                                _logger.Debug(string.Format("[Q-Time Logic][{0}][{1}]{2}[{3}][{4}/{5}]", drRecord["EQUIPID"].ToString(), vStage, bTurnOnQTime, iLotQTime, iQTimeLow, iQTimeHigh));
                                            }
                                            catch (Exception ex) { }
                                        }

                                        if (drRecord["port_state"].ToString().Equals("4"))
                                        {
                                            try
                                            {
                                                sql = _BaseDataService.CheckCarrierTypeOfQTime(drRecord["EQUIPID"].ToString(), vStage, drRecord["carrierType_M"].ToString());
                                                dtTemp2 = _dbTool.GetDataTable(sql);

                                                if (dtTemp2.Rows.Count > 0)
                                                {
                                                    iQTimeLow = 0;
                                                    iQTimeHigh = 6;
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.Debug(string.Format("[Q-CheckCarrierTypeOfQTime][Exception][{0}][{1}][{2}][{3}][{4}]", drRecord["EQUIPID"].ToString(), vStage, drRecord["carrierType_M"].ToString(), lotID, ex.Message));
                                                //continue;
                                            }

                                            if (bTurnOnQTime)
                                            {
                                                if (iLotQTime < iQTimeLow)
                                                {
                                                    //lotID, _Equip, iLotQTime
                                                    _logger.Debug(string.Format("[Q-Time is not enough][{0}][{1}][{2}][{3}]", lotID, iLotQTime, _Equip, iQTimeLow));
                                                    continue;
                                                }

                                                if (iLotQTime > iQTimeHigh)
                                                {
                                                    _logger.Debug(string.Format("[Q-Time exceeds][{0}][{1}][{2}][{3}]", lotID, iLotQTime, _Equip, iQTimeHigh));
                                                    continue;
                                                }
                                            }
                                        }

                                    }
                                    else if (_QTimeMode.Equals(2))
                                    {
                                        try
                                        {
                                            iQTime = 0;
                                            iLotQTime = 0;
                                            iQTimeLow = 0;
                                            iQTimeHigh = 0;

                                            if (!lotID.Equals(""))
                                            {
                                                if (!lotID.Equals(slastLotID))
                                                {
                                                    sql = _BaseDataService.GetAvailableCarrierByLocateOrderbyQTime(_adsTable, _qTimeTable, drRecord["carrierType_M"].ToString(), lotID, "", true, true, _eqpworkgroup);
                                                    dtTemp2 = _dbTool.GetDataTable(sql);

                                                    if (dtTemp2.Rows.Count > 0)
                                                    {
                                                        try
                                                        {
                                                            iQTime = dtTemp2.Rows[0]["QTIME1"] is null ? 0 : float.Parse(dtTemp2.Rows[0]["QTIME1"].ToString());
                                                            iLotQTime = dtTemp2.Rows[0]["QTIME"] is null ? 0 : float.Parse(dtTemp2.Rows[0]["QTIME"].ToString());
                                                            iQTimeLow = dtTemp2.Rows[0]["minallowabletw"] is null ? 0 : float.Parse(dtTemp2.Rows[0]["minallowabletw"].ToString());
                                                            iQTimeHigh = dtTemp2.Rows[0]["maxallowabletw"] is null ? 0 : float.Parse(dtTemp2.Rows[0]["maxallowabletw"].ToString());

                                                            if(dtTemp2.Rows[0]["gonow"].ToString().Equals("Y"))
                                                            {
                                                                ///Q-Time 為Go Now, change lot priority 為80
                                                                _dbTool.SQLExec(_BaseDataService.UpdatePriorityByLotid(lotID, 80), out tmpMsg, true);
                                                            }
                                                        }
                                                        catch (Exception ex) { }

                                                        if (_qTimemode_enable)
                                                        {
                                                            if (iQTime < 0)
                                                            {
                                                                _logger.Debug(string.Format("[Q-Time is not enough][{0}][{1}][{2}][{3}][{4}][{5}]", _QTimeMode, lotID, iLotQTime, _Equip, sPortID, iQTimeLow));
                                                                continue;
                                                            }

                                                            if (iQTime > 1)
                                                            {
                                                                //Auto hold lot times for Qtime 
                                                                int iholttimes = 0;
                                                                sql = _BaseDataService.SelectTableLotInfoByLotid(lotID);
                                                                dtTemp3 = _dbTool.GetDataTable(sql);
                                                                if (dtTemp3.Rows.Count > 0)
                                                                {
                                                                    iholttimes = int.Parse(dtTemp3.Rows[0]["hold_times"].ToString());
                                                                }

                                                                if (iholttimes <= 0)
                                                                {
                                                                    _logger.Debug(string.Format("[Q-Time exceeds][{0}][{1}][{2}][{3}][{4}][{5}]", _QTimeMode, lotID, iLotQTime, _Equip, drRecord["PORT_ID"].ToString(), iQTimeHigh));

                                                                    try
                                                                    {
                                                                        _logger.Debug(string.Format("[Q-Time exceeds][{0}][{1}]", "Auto hold lot. ZZ, Q-Time failed.", lotID));

                                                                        jcetWebServiceClient = new JCETWebServicesClient();
                                                                        jcetWebServiceClient._url = _qTime_url;
                                                                        jcetWebServiceClient._urlUAT = _qTime_urlUAT;
                                                                        string _args = "Q-Time failed,ZZ";
                                                                        //public ResultMsg CustomizeEvent(string _func, string useMethod, bool isProduct, string equip, string username, string pwd, string lotid, string _args)
                                                                        resultMsg = new JCETWebServicesClient.ResultMsg();
                                                                        resultMsg = jcetWebServiceClient.CustomizeEvent("holdlot", "post", _qTime_isProduct, _Equip, _qTime_username, _qTime_pwd, lotID, _args);
                                                                        string result3 = resultMsg.retMessage;

                                                                        sql = _BaseDataService.UadateHoldTimes(lotID);
                                                                        _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                    }
                                                                    catch (Exception ex) { }
                                                                }
                                                                continue;
                                                            }

                                                            _logger.Debug(string.Format("[Q-Time pass][{0}][{1}][{2}][{3}][{4}]", lotID, iLotQTime, iQTimeHigh, iQTime, iQTimeLow));
                                                        }

                                                        slastLotID = lotID;
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.Debug(string.Format("[Q-Time Logic][Exception][{0}][{1}][{2}][{3}][{4}]", drRecord["EQUIPID"].ToString(), vStage, drRecord["carrierType_M"].ToString(), lotID, ex.Message));
                                        }
                                    }
                                    else
                                    {

                                    }
                                }
                                catch (Exception ex)
                                { }

                                try {
                                    if(bCheckRecipe)
                                    {
                                        dtRecipeSet = _dbTool.GetDataTable(_BaseDataService.QueryRecipeSetting(drRecord["EQUIPID"].ToString()));

                                        dtTemp = _dbTool.GetDataTable(_BaseDataService.QueryCurrentEquipRecipe(drRecord["EQUIPID"].ToString()));

                                        if(dtTemp.Rows.Count > 0)
                                        {
                                            _equiprecipe = "";

                                            foreach (DataRow dr in dtTemp.Rows)
                                            {
                                                drRecipe = dtRecipeSet.Select(string.Format("recipeID='{0}'", dr["recipe"].ToString()));

                                                if(drRecipe.Length >0)
                                                {
                                                    _equiprecipe = dr["recipe"].ToString();
                                                    _equiprecipeGroup = drRecipe[0]["recipe_group"].ToString();
                                                }

                                                if (!_equiprecipeGroup.Equals(""))
                                                    break;
                                            }
                                        }
                                    }
                                }catch(Exception ex)
                                { }
                            }
                            else
                            {
                                useFaileRack = dtWorkgroupSet.Rows[0]["USEFAILERACK"] is null ? false : dtWorkgroupSet.Rows[0]["USEFAILERACK"].ToString().Equals("1") ? true : false;
                                faileRack = dtWorkgroupSet.Rows[0]["F_ERACK"] is null ? "" : dtWorkgroupSet.Rows[0]["F_ERACK"].ToString();
                                outeRack = dtWorkgroupSet.Rows[0]["OUT_ERACK"] is null ? "" : dtWorkgroupSet.Rows[0]["OUT_ERACK"].ToString();
                                ineRack = dtWorkgroupSet.Rows[0]["IN_ERACK"] is null ? "" : dtWorkgroupSet.Rows[0]["IN_ERACK"].ToString();
                                bCheckEquipLookupTable = false;
                                bCheckRcpConstraint = false;
                                _priority = 50;
                                _stageController = dtWorkgroupSet.Rows[0]["stagecontroller"] is null ? false : dtWorkgroupSet.Rows[0]["stagecontroller"].ToString().Equals("1") ? true : false;
                                _cannotSame = dtWorkgroupSet.Rows[0]["cannotsame"] is null ? false : dtWorkgroupSet.Rows[0]["cannotsame"].ToString().Equals("1") ? true : false;
                            }

                        } catch (Exception ex)
                        { }
                    }

                    if (_DebugMode)
                    {
                        _logger.Debug(string.Format("[Port_Type] {0} / {1} / {2}", drRecord["EQUIPID"].ToString(), drRecord["Port_Type"].ToString(), drRecord["port_id"].ToString()));
                    }

                    try {
                        dtTemp = _dbTool.GetDataTable(_BaseDataService.QueryStageCtrlListByPortNo(sPortID));

                        if(dtTemp.Rows.Count > 0)
                        {
                            _dicStageCtrl = dtTemp.AsEnumerable().ToDictionary(p => Convert.ToString(p["stage"]), p => Convert.ToString(p["portid"]));
                        }
                    }
                    catch (Exception ex)
                    { }

                    _carrierID = "";
                    //Port_Type: in 找一個載具Carrier, Out 找目的地Dest 
                    switch (drRecord["Port_Type"])
                    {
                        case "IN":
                            bool bNoFind = true;
                            //0. wait to load 找到適合的Carrier, 併產生Load指令
                            //1. wait to unload 且沒有其它符合的Carrier 直接產生Unload指令
                            //2. wait to unload 而且有其它適用的Carrier (full), 產生Swap指令(Load + Unload)
                            //2.1. Unload, 如果out port的 carrier type 相同, 產生transfer 指令至out port
                            iPortState = GetPortStatus(dtPortsInfo, drRecord["port_id"].ToString(), out sPortState);
                            if (_DebugMode)
                            {
                                _logger.Debug(string.Format("[Port_Type] {0} / {1}", drRecord["EQUIPID"].ToString(), iPortState, drRecord["PORT_ID"].ToString()));
                            }

                            switch (iPortState)
                            {
                                case 1:
                                    //1. Transfer Blocked
                                    continue;
    
                                case 2:
                                case 3:
                                case 5:
                                    //2. Near Completion
                                    //3.Ready to Unload
                                    //5. Reject and Ready to unload
                                    if (iPortState == 2)
                                    {
                                        if (!bNearComplete)
                                            break;
                                    }

                                    try
                                    {
                                        bIsMatch = false;
                                        dtWorkInProcessSch = _dbTool.GetDataTable(_BaseDataService.QueryWorkInProcessSchByPortIdForUnload(drRecord["PORT_ID"].ToString(), tableOrder));
                                        if (dtWorkInProcessSch.Rows.Count > 0)
                                            continue;

                                        //取得當前Load Port上的Carrier
                                        UnloadCarrierID = "";
                                        dtLoadPortCarrier = _dbTool.GetDataTable(_BaseDataService.SelectLoadPortCarrierByEquipId(drRecord["EQUIPID"].ToString()));
                                        if (dtLoadPortCarrier.Rows.Count > 0)
                                        {
                                            drIn = dtLoadPortCarrier.Select("PORTNO = '" + drRecord["PORT_SEQ"].ToString() + "'");
                                            UnloadCarrierID = drIn is null ? "" : drIn.Length > 0 ? drIn[0]["CARRIER_ID"].ToString() : "";
                                        }

                                        if (iPortState.Equals(5))
                                        {
                                            ///還原原本的lot status
                                            if (drIn.Length > 0)
                                            {
                                                string tmpCarrier = ""; string tmplastLot = "";
                                                tmpCarrier = drIn is null ? "" : drIn.Length > 0 ? drIn[0]["CARRIER_ID"].ToString() : "";
                                                sql = _BaseDataService.SelectTableCarrierAssociateByCarrierID(tmpCarrier);
                                                tmplastLot = "";

                                            }
                                        }

                                        ///240110
                                        dtAvaileCarrier = GetAvailableCarrierByLocate(_dbTool, configuration, drRecord["CARRIER_TYPE"].ToString(), "", ineRack, true, _keyRTDEnv, _eqpworkgroup);
                                        //dtAvaileCarrier = GetAvailableCarrier(_dbTool, drRecord["CARRIER_TYPE"].ToString(), true);
                                        //AvaileCarrier is true

                                        int iQty = 0; int iQuantity = 0; int iTotalQty = 0;
                                        int iCountOfCarr = 0;
                                        bool isLastLot = false;
                                        bNoFind = true;

                                        if (dtAvaileCarrier.Rows.Count > 0)
                                        {
                                            bIsMatch = false;
                                            string tmpMessage = "";
                                            string _lotInCarrier = "";

                                            foreach (DataRow draCarrier in dtAvaileCarrier.Rows)
                                            {
                                                if (_DebugMode)
                                                {
                                                    _logger.Debug(string.Format("---locate is {0}", draCarrier["locate"].ToString()));
                                                    _logger.Debug(string.Format("---out erack is {0}", dtWorkgroupSet.Rows[0]["out_erack"].ToString()));
                                                }
                                                CarrierID = "";

                                                if (_oEventQ.EventName.Equals("AbnormalyEquipmentStatus"))
                                                {
                                                    //SelectTableEquipmentPortsInfoByEquipId    
                                                    sql = string.Format(_BaseDataService.SelectTableLotInfoByLotid(draCarrier["lot_id"].ToString()));
                                                    dtTemp2 = _dbTool.GetDataTable(sql);

                                                    //由機台來找lot時, Equipment 需為主要機台(第一台)
                                                    string[] arrEqp = dtTemp2.Rows[0]["equiplist"].ToString().Split(',');
                                                    bool isFirst = false;

                                                    foreach (string tmpEqp in arrEqp)
                                                    {
                                                        sql = string.Format(_BaseDataService.SelectTableEquipmentPortsInfoByEquipId(tmpEqp));
                                                        dtTemp = _dbTool.GetDataTable(sql);

                                                        if (dtTemp.Rows.Count > 0)
                                                        {
                                                            if (dtTemp.Rows[0]["manualmode"].ToString().Equals("1"))
                                                            {
                                                                continue;
                                                            }
                                                            else
                                                            {
                                                                if (drRecord["EQUIPID"].ToString().Equals(tmpEqp))
                                                                {
                                                                    isFirst = true;
                                                                    break;
                                                                }
                                                            }

                                                            if (isFirst)
                                                            {
                                                                continue;
                                                            }
                                                            else
                                                            {

                                                            }
                                                        }
                                                    }

                                                    if (!isFirst)
                                                        continue;
                                                    //sql = string.Format(_BaseDataService.QueryEquipListFirst(draCarrier["lot_id"].ToString(), drRecord["EQUIPID"].ToString()));
                                                    //dtTemp = _dbTool.GetDataTable(sql);

                                                    //if (dtTemp.Rows.Count <= 0)
                                                    //continue;
                                                }

                                                ///Q-Time Logic 240110
                                                try
                                                {
                                                    _lotInCarrier = draCarrier["lot_id"].ToString();
                                                    _carrierID = draCarrier["carrier_id"].ToString();

                                                    if (_QTimeMode.Equals(1))
                                                    {

                                                        //取得Lot QTime from ads
                                                        sql = _BaseDataService.GetQTimeLot(configuration["PreDispatchToErack:lotState:tableName"], _lotInCarrier);
                                                        dtTemp = _dbTool.GetDataTable(sql);

                                                        if (dtTemp.Rows.Count > 0)
                                                        {
                                                            bTurnOnQTime = dtTemp.Rows[0]["QTIME"].ToString().Equals("NA") ? false : true;
                                                            if (bTurnOnQTime)
                                                                iLotQTime = float.Parse(dtTemp.Rows[0]["QTIME"].ToString());
                                                            else
                                                                iLotQTime = 0;
                                                        }
                                                        else
                                                        {
                                                            bTurnOnQTime = false;
                                                            iLotQTime = 0;
                                                        }

                                                        if (bTurnOnQTime)
                                                        {
                                                            _logger.Debug(string.Format("[Q-Time Logic in draCarrier][{0}][{1}] Lot QTime: {2}", _lotInCarrier, drRecord["EquipID"].ToString(), iLotQTime));

                                                            if (iLotQTime < iQTimeLow)
                                                            {
                                                                //lotID, _Equip, iLotQTime
                                                                _logger.Debug(string.Format("[Q-Time Logic is not enough][{0}][{1}][{2}][{3}] _Out", _lotInCarrier, iLotQTime, _Equip, iLotQTime));
                                                                //continue;
                                                                goto ResetReserve;
                                                            }

                                                            if (iLotQTime > iQTimeHigh)
                                                            {
                                                                _logger.Debug(string.Format("[Q-Time Logic exceeds][{0}][{1}][{2}][{3}] _Out", _lotInCarrier, iLotQTime, _Equip, iQTimeHigh));
                                                                //continue;
                                                                goto ResetReserve;
                                                            }
                                                        }
                                                    }
                                                    else if (_QTimeMode.Equals(2))
                                                    {
                                                        try
                                                        {
                                                            iQTime = 0;
                                                            iLotQTime = 0;
                                                            iQTimeLow = 0;
                                                            iQTimeHigh = 0;

                                                            if (!_lotInCarrier.Equals(""))
                                                            {
                                                                try
                                                                {
                                                                    iQTime = draCarrier["QTIME1"] is null ? 0 : float.Parse(draCarrier["QTIME1"].ToString());
                                                                    iLotQTime = draCarrier["QTIME"] is null ? 0 : float.Parse(draCarrier["QTIME"].ToString());
                                                                    iQTimeLow = draCarrier["minallowabletw"] is null ? 0 : float.Parse(draCarrier["minallowabletw"].ToString());
                                                                    iQTimeHigh = draCarrier["maxallowabletw"] is null ? 0 : float.Parse(draCarrier["maxallowabletw"].ToString());

                                                                    if (dtTemp2.Rows[0]["gonow"].ToString().Equals("Y"))
                                                                    {
                                                                        ///Q-Time 為Go Now, change lot priority 為80
                                                                        _dbTool.SQLExec(_BaseDataService.UpdatePriorityByLotid(_lotInCarrier, 80), out tmpMsg, true);
                                                                    }
                                                                }
                                                                catch (Exception ex) { }

                                                                if (_qTimemode_enable)
                                                                {
                                                                    if (iQTime < 0)
                                                                    {
                                                                        _logger.Debug(string.Format("[Q-Time is not enough][{0}][{1}][{2}][{3}][{4}][{5}]", _QTimeMode, lotID, iLotQTime, _Equip, sPortID, iQTimeLow));
                                                                        goto ResetReserve;
                                                                    }

                                                                    if (iQTime > 1)
                                                                    {
                                                                        //Auto hold lot times for Qtime 
                                                                        int iholttimes = 0;
                                                                        sql = _BaseDataService.SelectTableLotInfoByLotid(lotID);
                                                                        dtTemp3 = _dbTool.GetDataTable(sql);
                                                                        if(dtTemp3.Rows.Count > 0)
                                                                        {
                                                                            iholttimes = int.Parse(dtTemp3.Rows[0]["hold_times"].ToString());
                                                                        }

                                                                        if (iholttimes <= 0)
                                                                        { 
                                                                            _logger.Debug(string.Format("[Q-Time exceeds][{0}][{1}][{2}][{3}][{4}][{5}]", _QTimeMode, lotID, iLotQTime, _Equip, sPortID, iQTimeHigh));

                                                                            try
                                                                            {
                                                                                _logger.Debug(string.Format("[Q-Time exceeds][{0}][{1}]", "Auto hold lot. ZZ, Q-Time failed.", lotID));

                                                                                jcetWebServiceClient = new JCETWebServicesClient();
                                                                                jcetWebServiceClient._url = _qTime_url;
                                                                                jcetWebServiceClient._urlUAT = _qTime_urlUAT;
                                                                                string _args = "Q-Time failed,ZZ";
                                                                                //public ResultMsg CustomizeEvent(string _func, string useMethod, bool isProduct, string equip, string username, string pwd, string lotid, string _args)
                                                                                resultMsg = new JCETWebServicesClient.ResultMsg();
                                                                                resultMsg = jcetWebServiceClient.CustomizeEvent("holdlot", "post", _qTime_isProduct, _Equip, _qTime_username, _qTime_pwd, lotID, _args);
                                                                                string result3 = resultMsg.retMessage;

                                                                                sql = _BaseDataService.UadateHoldTimes(lotID);
                                                                                _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                            }
                                                                            catch (Exception ex) { }
                                                                        }
                                                                        goto ResetReserve;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            _logger.Debug(string.Format("[Q-Time Logic][Exception][{0}][{1}][{2}][{3}][{4}]", drRecord["EQUIPID"].ToString(), vStage, drRecord["carrierType_M"].ToString(), lotID, ex.Message));
                                                        }
                                                    }
                                                    else
                                                    {

                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    _logger.Debug(string.Format("[Q-Time Logic in draCarrier][{0}][{1}] Exception: {2}", _lotInCarrier, drRecord["EquipID"].ToString(), ex.Message));
                                                }

                                                //站點不當前站點不同, 不取這批lot
                                                if (!draCarrier["lot_id"].ToString().Equals(""))
                                                {
                                                    sql = _BaseDataService.CheckLotStage(configuration["CheckLotStage:Table"], draCarrier["lot_id"].ToString());
                                                    dtTemp = _dbTool.GetDataTable(sql);

                                                    if (dtTemp.Rows.Count <= 0)
                                                        continue;
                                                    else
                                                    {
                                                        if (!dtTemp.Rows[0]["stage1"].ToString().Equals(dtTemp.Rows[0]["stage2"].ToString()))
                                                        {
                                                            _logger.Debug(string.Format("---LotID= {0}, RTD_Stage={1}, MES_Stage={2}, RTD_State={3}, MES_State={4}", draCarrier["lot_id"].ToString(), dtTemp.Rows[0]["stage1"].ToString(), dtTemp.Rows[0]["stage2"].ToString(), dtTemp.Rows[0]["state1"].ToString(), dtTemp.Rows[0]["state2"].ToString()));
                                                        }
                                                    }
                                                }

                                                if (_portModel.Equals("1I1OT2"))
                                                {
                                                    //1I1OT2 特定機台邏輯
                                                    //IN: Lotid 會對應一個放有Matal Ring 的Cassette, 需存在才進行派送
                                                    MetalRingCarrier = "";

                                                    sql = string.Format(_BaseDataService.CheckMetalRingCarrier(draCarrier["carrier_id"].ToString()));
                                                    dtTemp = _dbTool.GetDataTable(sql);
                                                    if (dtTemp.Rows.Count > 0)
                                                    {
                                                        MetalRingCarrier = dtTemp.Rows[0]["carrier_id"].ToString();
                                                        _logger.Debug(string.Format("---MetalRingCarrier is {0}", MetalRingCarrier));
                                                        //break;
                                                        CarrierID = draCarrier["carrier_id"].ToString();
                                                    }
                                                    else
                                                    {
                                                        if (_DebugMode)
                                                            _logger.Debug(string.Format("---Can Not Find Have MetalRing Cassette."));
                                                        //Can Not Find Have MetalRing Cassette.
                                                        continue;
                                                    }
                                                }

                                                //Check Equipment CustDevice / Lot CustDevice is same.
                                                if (!EquipCustDevice.Equals(""))
                                                {
                                                    string device = "";
                                                    sql = _BaseDataService.QueryLotInfoByCarrierID(draCarrier["carrier_id"].ToString());
                                                    dtTemp = _dbTool.GetDataTable(sql);
                                                    if(dtTemp.Rows.Count > 0)
                                                    {
                                                        device = dtTemp.Rows[0]["custdevice"].ToString();
                                                        if (!device.Equals(EquipCustDevice))
                                                        {
                                                            continue;
                                                        }
                                                        else
                                                        {
                                                            if (bCheckDevice)
                                                                continue;
                                                        }
                                                    }
                                                }
                                                

                                                iQty = 0; iTotalQty = 0; iQuantity = 0; iCountOfCarr = 0;

                                                //Check Workgroup Set 
                                                bNoFind = true;
                                                sql = _BaseDataService.QueryRackByGroupID(dtWorkgroupSet.Rows[0]["in_erack"].ToString());
                                                dtTemp = _dbTool.GetDataTable(sql);
                                                foreach (DataRow drRack in dtTemp.Rows)
                                                {
                                                    if (draCarrier["locate"].ToString().Equals(drRack["erackID"]))
                                                    {
                                                        bNoFind = false;
                                                        break;
                                                    }
                                                }

                                                if (bNoFind)
                                                {
                                                    continue;
                                                }

                                                //if (!draCarrier["locate"].ToString().Equals(dtWorkgroupSet.Rows[0]["in_erack"]))
                                                //    continue;

                                                iQuantity = draCarrier.Table.Columns.Contains("quantity") ? int.Parse(draCarrier["quantity"].ToString()) : 0;
                                                //檢查Cassette Qty總和, 是否與Lot Info Qty相同, 相同才可派送 (滿足相同lot要放在相同機台上的需求)
                                                string sqlSentence = _BaseDataService.CheckQtyforSameLotId(draCarrier["lot_id"].ToString(), drRecord["CARRIER_TYPE"].ToString());
                                                DataTable dtSameLot = new DataTable();
                                                dtSameLot = _dbTool.GetDataTable(sqlSentence);
                                                if (dtSameLot.Rows.Count > 0)
                                                {
                                                    ///iQty is sum same lot casette.
                                                    iQty = int.Parse(dtSameLot.Rows[0]["qty"].ToString());
                                                    ///iTotalQty is lot total quantity.
                                                    iTotalQty = int.Parse(dtSameLot.Rows[0]["total_qty"].ToString());
                                                    iCountOfCarr = int.Parse(dtSameLot.Rows[0]["NumOfCarr"].ToString());

                                                    if (iCountOfCarr > 1)
                                                    {
                                                        if (iQty == iTotalQty)
                                                        { //To Do...
                                                            isLastLot = true;
                                                            sql = string.Format(_BaseDataService.LockMachineByLot(draCarrier["lot_id"].ToString(), iQuantity, 0));
                                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                                        }
                                                        else
                                                        {
                                                            if (iQty <= iQuantity)
                                                            {
                                                                isLastLot = true;
                                                                sql = string.Format(_BaseDataService.LockMachineByLot(draCarrier["lot_id"].ToString(), iQuantity, 0));
                                                                _dbTool.SQLExec(sql, out tmpMsg, true);
                                                            }
                                                            else
                                                            {
                                                                isLastLot = false;
                                                                sql = string.Format(_BaseDataService.LockMachineByLot(draCarrier["lot_id"].ToString(), iQuantity, 1));
                                                                _dbTool.SQLExec(sql, out tmpMsg, true);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (iQty < iTotalQty)
                                                        {
                                                            int _lockMachine = 0;
                                                            int _compQty = 0;
                                                            //draCarrier["lot_id"].ToString()
                                                            sql = string.Format(_BaseDataService.SelectTableLotInfoByLotid(draCarrier["lot_id"].ToString()));
                                                            dtTemp = _dbTool.GetDataTable(sql);
                                                            _lockMachine = dtTemp.Rows[0]["lockmachine"].ToString().Equals("1") ? 1 : 0;
                                                            _compQty = dtTemp.Rows[0]["comp_qty"].ToString().Equals("0") ? 0 : int.Parse(dtTemp.Rows[0]["comp_qty"].ToString());

                                                            if (_lockMachine.Equals(0) && _compQty == 0)
                                                            {
                                                                isLastLot = false;
                                                                sql = string.Format(_BaseDataService.LockMachineByLot(draCarrier["lot_id"].ToString(), iQuantity, 1));
                                                                _dbTool.SQLExec(sql, out tmpMsg, true);
                                                            }
                                                            else
                                                            {
                                                                if (_compQty + iQuantity >= iTotalQty)
                                                                {
                                                                    isLastLot = true;
                                                                    sql = string.Format(_BaseDataService.LockMachineByLot(draCarrier["lot_id"].ToString(), _compQty + iQuantity, 0));
                                                                    _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                }
                                                                else
                                                                    isLastLot = false;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            isLastLot = true;
                                                            sql = string.Format(_BaseDataService.LockMachineByLot(draCarrier["lot_id"].ToString(), iQuantity, 0));
                                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                                        }
                                                    }
                                                }

                                                if (CheckIsAvailableLot(_dbTool, draCarrier["lot_id"].ToString(), drRecord["EquipID"].ToString()))
                                                {
                                                    CarrierID = draCarrier["Carrier_ID"].ToString();
                                                    bIsMatch = true;
                                                }
                                                else
                                                {
                                                    bIsMatch = false;
                                                    continue;
                                                }

                                                if (bIsMatch)
                                                {
                                                    _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, true), out tmpMessage, true);
                                                    break;
                                                }

                                            ResetReserve:
                                                tmpMessage = "";
                                                _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, false), out tmpMessage, true);
                                                CarrierID = "";
                                                _carrierID = "";
                                                _lotInCarrier = "";
                                                continue;
                                            }

                                            if (!tmpMessage.Equals(""))
                                            {
                                                _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, false), out tmpMessage, true);
                                                continue;
                                            }
                                        }

                                        if (dtAvaileCarrier.Rows.Count <= 0 || !bIsMatch || bNoFind || isManualMode)
                                        {
                                            CarrierID = drIn is null ? "" : drIn.Length > 0 ? drIn[0]["CARRIER_ID"].ToString() : "";

                                            lstTransfer = new TransferList();
                                            lstTransfer.CommandType = "UNLOAD";
                                            lstTransfer.Source = drRecord["PORT_ID"].ToString();
                                            lstTransfer.Dest = drRecord["IN_ERACK"].ToString();
                                            lstTransfer.CarrierID = UnloadCarrierID.Equals("") ? "*" : UnloadCarrierID;
                                            lstTransfer.Quantity = 0;
                                            lstTransfer.CarrierType = dtLoadPortCarrier.Rows.Count > 0 ? dtLoadPortCarrier.Rows[0]["command_type"].ToString() : _CarrierTypebyPort;
                                            break;
                                        }
                                        //AvaileCarrier is true
                                        if (bIsMatch)
                                        {
                                            drCarrierData = dtAvaileCarrier.Select("carrier_id = '" + CarrierID + "'");
                                            if (_DebugMode)
                                                _logger.Debug(string.Format("[drCarrierData][{0}] {1} / {2}", drRecord["PORT_ID"].ToString(), drCarrierData[0]["COMMAND_TYPE"].ToString(), CarrierID));

                                        }

                                        lstTransfer = new TransferList();
                                        lstTransfer.CommandType = "LOAD";
                                        lstTransfer.Source = "*";
                                        lstTransfer.Dest = drRecord["PORT_ID"].ToString();
                                        lstTransfer.CarrierID = CarrierID;
                                        lstTransfer.Quantity = int.Parse(drCarrierData.Length > 0 ? drCarrierData[0]["QUANTITY"].ToString() : "0");
                                        lstTransfer.CarrierType = drCarrierData.Length > 0 ? drCarrierData[0]["command_type"].ToString() : "";
                                        lstTransfer.Total = iTotalQty;
                                        lstTransfer.IsLastLot = isLastLot ? 1 : 0;
                                        //normalTransfer.Transfer.Add(lstTransfer);
                                        //iReplace++;

                                        CarrierID = dtLoadPortCarrier.Rows.Count > 0 ? dtLoadPortCarrier.Rows[0]["CARRIER_ID"].ToString() : "";

                                        if (_portModel.Equals("1I1OT2"))
                                            break;
                                        //檢查Out port 與 In port 的carrier type是否相同
                                        drOut = dtPortsInfo.Select("Port_Type='OUT'");
                                        if (drOut[0]["CARRIER_TYPE"].ToString().Equals(drRecord["CARRIER_TYPE"].ToString()))
                                        {
                                            string tmpPortState = "";
                                            CarrierID = drIn is null ? "" : drIn.Length > 0 ? drIn[0]["CARRIER_ID"].ToString() : "";
                                            //確認Out port是否處於Wait to Unload
                                            switch (GetPortStatus(dtPortsInfo, drOut[0]["port_id"].ToString(), out tmpPortState))
                                            {
                                                case 0:
                                                case 1:
                                                    break;
                                                case 2:
                                                case 3:
                                                case 4:
                                                    normalTransfer.Transfer.Add(lstTransfer);
                                                    iReplace++;

                                                    lstTransfer = new TransferList();
                                                    lstTransfer.CommandType = "TRANS";
                                                    lstTransfer.Source = drRecord["PORT_ID"].ToString();
                                                    lstTransfer.Dest = drOut[0]["PORT_ID"].ToString();
                                                    lstTransfer.CarrierID = CarrierID;
                                                    lstTransfer.Quantity = int.Parse(dtLoadPortCarrier.Rows.Count > 0 ? dtLoadPortCarrier.Rows[0]["QUANTITY"].ToString() : "0");
                                                    lstTransfer.CarrierType = dtLoadPortCarrier.Rows.Count > 0 ? dtLoadPortCarrier.Rows[0]["command_type"].ToString() : "";
                                                    //normalTransfer.Transfer.Add(lstTransfer);
                                                    break;
                                                default:
                                                    CarrierID = drIn[0]["CARRIER_ID"].ToString();

                                                    normalTransfer.Transfer.Add(lstTransfer);
                                                    iReplace++;

                                                    lstTransfer = new TransferList();
                                                    lstTransfer.CommandType = "UNLOAD";
                                                    lstTransfer.Source = drRecord["PORT_ID"].ToString();
                                                    lstTransfer.Dest = drRecord["IN_ERACK"].ToString();
                                                    lstTransfer.CarrierID = UnloadCarrierID.Equals("") ? "*" : UnloadCarrierID;
                                                    lstTransfer.Quantity = 0;
                                                    lstTransfer.CarrierType = dtLoadPortCarrier.Rows.Count > 0 ? dtLoadPortCarrier.Rows[0]["command_type"].ToString() : _CarrierTypebyPort;
                                                    break;
                                            }

                                        }
                                        else
                                        {
                                            //Input vs Output Carrier Type is diffrence. do unload
                                            CarrierID = drIn[0]["CARRIER_ID"].ToString();

                                            normalTransfer.Transfer.Add(lstTransfer);
                                            iReplace++;

                                            lstTransfer = new TransferList();
                                            lstTransfer.CommandType = "UNLOAD";
                                            lstTransfer.Source = drRecord["PORT_ID"].ToString();
                                            lstTransfer.Dest = drRecord["IN_ERACK"].ToString();
                                            lstTransfer.CarrierID = UnloadCarrierID.Equals("") ? "*" : UnloadCarrierID;
                                            lstTransfer.Quantity = 0;
                                            lstTransfer.CarrierType = dtLoadPortCarrier.Rows.Count > 0 ? dtLoadPortCarrier.Rows[0]["command_type"].ToString() : _CarrierTypebyPort;
                                            break;
                                        }
                                    }
                                    catch (Exception ex)
                                    { }

                                    break;
                                case 4:
                                    //4. Empty (Ready to load)
                                    try
                                    {
                                        if (bStageIssue)
                                        {
                                            sql = _BaseDataService.LockLotInfoWhenReady(lotID);
                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                            if (_DebugMode)
                                            {
                                                _logger.Debug(string.Format("[StageIssue] {0} / {1} Load command create faild.", drRecord["EQUIPID"].ToString(), lotID));
                                            }

                                            continue;
                                        }

                                        dtWorkInProcessSch = _dbTool.GetDataTable(_BaseDataService.SelectTableWorkInProcessSchByPortId(drRecord["PORT_ID"].ToString(), tableOrder));
                                        if (dtWorkInProcessSch.Rows.Count > 0)
                                            continue;

                                        bIsMatch = false;
                                        dtAvaileCarrier = GetAvailableCarrierByLocate(_dbTool, configuration, drRecord["CARRIER_TYPE"].ToString(), "", ineRack, true, _keyRTDEnv, _eqpworkgroup);
                                        //dtAvaileCarrier = GetAvailableCarrier(_dbTool, drRecord["CARRIER_TYPE"].ToString(), true);
                                        if (dtAvaileCarrier is null)
                                            continue;
                                        if (dtAvaileCarrier.Rows.Count <= 0)
                                            continue;

                                        int iQty = 0; int iQuantity = 0; int iTotalQty = 0;
                                        int iCountOfCarr = 0;
                                        bool isLastLot = false;

                                        if (dtAvaileCarrier.Rows.Count > 0)
                                        {
                                            bIsMatch = false;
                                            string tmpMessage = "";
                                            string _lotInCarrier = "";
                                            MetalRingCarrier = "";

                                            foreach (DataRow draCarrier in dtAvaileCarrier.Rows)
                                            {
                                                if (_DebugMode)
                                                {
                                                    _logger.Debug(string.Format("---locate is {0}", draCarrier["locate"].ToString()));
                                                    _logger.Debug(string.Format("---in erack is {0}", dtWorkgroupSet.Rows[0]["in_erack"].ToString()));
                                                }
                                                CarrierID = "";

                                                if (_oEventQ.EventName.Equals("AbnormalyEquipmentStatus"))
                                                {
                                                    //SelectTableEquipmentPortsInfoByEquipId    
                                                    sql = string.Format(_BaseDataService.SelectTableLotInfoByLotid(draCarrier["lot_id"].ToString()));
                                                    dtTemp2 = _dbTool.GetDataTable(sql);

                                                    //由機台來找lot時, Equipment 需為主要機台(第一台)
                                                    string[] arrEqp = dtTemp2.Rows[0]["equiplist"].ToString().Split(',');
                                                    bool isFirst = false;

                                                    foreach (string tmpEqp in arrEqp)
                                                    {
                                                        sql = string.Format(_BaseDataService.SelectTableEquipmentPortsInfoByEquipId(tmpEqp));
                                                        dtTemp = _dbTool.GetDataTable(sql);

                                                        if (dtTemp.Rows.Count > 0)
                                                        {
                                                            if (dtTemp.Rows[0]["manualmode"].ToString().Equals("1"))
                                                            {
                                                                continue;
                                                            }
                                                            else
                                                            {
                                                                if (drRecord["EQUIPID"].ToString().Equals(tmpEqp))
                                                                {
                                                                    isFirst = true;
                                                                    break;
                                                                }
                                                            }

                                                            if (isFirst)
                                                            {
                                                                continue;
                                                            }
                                                            else
                                                            { 
                                                                
                                                            }
                                                        }
                                                    }

                                                    if(!isFirst)
                                                        continue;
                                                    //sql = string.Format(_BaseDataService.QueryEquipListFirst(draCarrier["lot_id"].ToString(), drRecord["EQUIPID"].ToString()));
                                                    //dtTemp = _dbTool.GetDataTable(sql);

                                                    //if (dtTemp.Rows.Count <= 0)
                                                        //continue;
                                                }

                                                ///Q-Time Logic 240110
                                                try
                                                {
                                                    _lotInCarrier = draCarrier["lot_id"].ToString();
                                                    _carrierID = draCarrier["carrier_id"].ToString();

                                                    if (_QTimeMode.Equals(1))
                                                    {

                                                        //取得Lot QTime from ads
                                                        sql = _BaseDataService.GetQTimeLot(configuration["PreDispatchToErack:lotState:tableName"], _lotInCarrier);
                                                        dtTemp = _dbTool.GetDataTable(sql);

                                                        if (dtTemp.Rows.Count > 0)
                                                        {
                                                            bTurnOnQTime = dtTemp.Rows[0]["QTIME"].ToString().Equals("NA") ? false : true;
                                                            if (bTurnOnQTime)
                                                                iLotQTime = float.Parse(dtTemp.Rows[0]["QTIME"].ToString());
                                                            else
                                                                iLotQTime = 0;
                                                        }
                                                        else
                                                        {
                                                            bTurnOnQTime = false;
                                                            iLotQTime = 0;
                                                        }

                                                        if (bTurnOnQTime)
                                                        {
                                                            _logger.Debug(string.Format("[Q-Time Logic in draCarrier][{0}][{1}] Lot QTime: {2}", _lotInCarrier, drRecord["EquipID"].ToString(), iLotQTime));

                                                            if (iLotQTime < iQTimeLow)
                                                            {
                                                                //lotID, _Equip, iLotQTime
                                                                _logger.Debug(string.Format("[Q-Time Logic is not enough][{0}][{1}][{2}][{3}] _Out", _lotInCarrier, iLotQTime, _Equip, iLotQTime));
                                                                //continue;
                                                                goto ResetReserve;
                                                            }

                                                            if (iLotQTime > iQTimeHigh)
                                                            {
                                                                _logger.Debug(string.Format("[Q-Time Logic exceeds][{0}][{1}][{2}][{3}] _Out", _lotInCarrier, iLotQTime, _Equip, iQTimeHigh));
                                                                //continue;
                                                                goto ResetReserve;
                                                            }
                                                        }
                                                    }
                                                    else if (_QTimeMode.Equals(2))
                                                    {
                                                        try
                                                        {
                                                            iQTime = 0;
                                                            iLotQTime = 0;
                                                            iQTimeLow = 0;
                                                            iQTimeHigh = 0;

                                                            if (!_lotInCarrier.Equals(""))
                                                            {
                                                                try
                                                                {
                                                                    iQTime = draCarrier["QTIME1"] is null ? 0 : float.Parse(draCarrier["QTIME1"].ToString());
                                                                    iLotQTime = draCarrier["QTIME"] is null ? 0 : float.Parse(draCarrier["QTIME"].ToString());
                                                                    iQTimeLow = draCarrier["minallowabletw"] is null ? 0 : float.Parse(draCarrier["minallowabletw"].ToString());
                                                                    iQTimeHigh = draCarrier["maxallowabletw"] is null ? 0 : float.Parse(draCarrier["maxallowabletw"].ToString());

                                                                    if (dtTemp2.Rows[0]["gonow"].ToString().Equals("Y"))
                                                                    {
                                                                        ///Q-Time 為Go Now, change lot priority 為80
                                                                        _dbTool.SQLExec(_BaseDataService.UpdatePriorityByLotid(_lotInCarrier, 80), out tmpMsg, true);
                                                                    }
                                                                }
                                                                catch (Exception ex) { }

                                                                if (_qTimemode_enable)
                                                                {
                                                                    if (iQTime < 0)
                                                                    {
                                                                        _logger.Debug(string.Format("[Q-Time is not enough][{0}][{1}][{2}][{3}][{4}][{5}]", _QTimeMode, lotID, iLotQTime, _Equip, sPortID, iQTimeLow));
                                                                        goto ResetReserve;
                                                                    }

                                                                    if (iQTime > 1)
                                                                    {
                                                                        //Auto hold lot times for Qtime 
                                                                        int iholttimes = 0;
                                                                        sql = _BaseDataService.SelectTableLotInfoByLotid(lotID);
                                                                        dtTemp3 = _dbTool.GetDataTable(sql);
                                                                        if (dtTemp3.Rows.Count > 0)
                                                                        {
                                                                            iholttimes = int.Parse(dtTemp3.Rows[0]["hold_times"].ToString());
                                                                        }

                                                                        if (iholttimes <= 0)
                                                                        {
                                                                            _logger.Debug(string.Format("[Q-Time exceeds][{0}][{1}][{2}][{3}][{4}][{5}]", _QTimeMode, lotID, iLotQTime, _Equip, sPortID, iQTimeHigh));

                                                                            try
                                                                            {
                                                                                _logger.Debug(string.Format("[Q-Time exceeds][{0}][{1}]", "Auto hold lot. ZZ, Q-Time failed.", lotID));

                                                                                jcetWebServiceClient = new JCETWebServicesClient();
                                                                                jcetWebServiceClient._url = _qTime_url;
                                                                                jcetWebServiceClient._urlUAT = _qTime_urlUAT;
                                                                                string _args = "Q-Time failed,ZZ";
                                                                                //public ResultMsg CustomizeEvent(string _func, string useMethod, bool isProduct, string equip, string username, string pwd, string lotid, string _args)
                                                                                resultMsg = new JCETWebServicesClient.ResultMsg();
                                                                                resultMsg = jcetWebServiceClient.CustomizeEvent("holdlot", "post", _qTime_isProduct, _Equip, _qTime_username, _qTime_pwd, lotID, _args);
                                                                                string result3 = resultMsg.retMessage;

                                                                                sql = _BaseDataService.UadateHoldTimes(lotID);
                                                                                _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                            }
                                                                            catch (Exception ex) { }
                                                                        }
                                                                        goto ResetReserve;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            _logger.Debug(string.Format("[Q-Time Logic][Exception][{0}][{1}][{2}][{3}][{4}]", drRecord["EQUIPID"].ToString(), vStage, drRecord["carrierType_M"].ToString(), lotID, ex.Message));
                                                        }
                                                    }
                                                    else
                                                    {

                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    _logger.Debug(string.Format("[Q-Time Logic in draCarrier][{0}][{1}] Exception: {2}", _lotInCarrier, drRecord["EquipID"].ToString(), ex.Message));
                                                }


                                                if (_portModel.Equals("1I1OT2"))
                                                {
                                                    _logger.Debug(string.Format("---_portModel is {0} / {1}", _portModel, draCarrier["carrier_id"].ToString()));
                                                    //1I1OT2 特定機台邏輯
                                                    //IN: Lotid 會對應一個放有Matal Ring 的Cassette, 需存在才進行派送
                                                    if (MetalRingCarrier.Equals(""))
                                                    {
                                                        sql = string.Format(_BaseDataService.CheckMetalRingCarrier(draCarrier["carrier_id"].ToString()));
                                                        dtTemp = _dbTool.GetDataTable(sql);
                                                        if (dtTemp.Rows.Count > 0)
                                                        {
                                                            MetalRingCarrier = dtTemp.Rows[0]["carrier_id"].ToString();
                                                            _logger.Debug(string.Format("---MetalRingCarrier is {0}", MetalRingCarrier));
                                                            //break;
                                                            CarrierID = draCarrier["carrier_id"].ToString();
                                                        }
                                                        else
                                                        {
                                                            if (_DebugMode)
                                                                _logger.Debug(string.Format("---Can Not Find Have MetalRing Cassette."));
                                                            //Can Not Find Have MetalRing Cassette.
                                                            continue;
                                                        }
                                                    }
                                                    else
                                                        continue;
                                                }

                                                if (!EquipCustDevice.Equals(""))
                                                {
                                                    string device = "";
                                                    sql = _BaseDataService.QueryLotInfoByCarrierID(draCarrier["carrier_id"].ToString());
                                                    dtTemp = _dbTool.GetDataTable(sql);
                                                    if (dtTemp.Rows.Count > 0)
                                                    {
                                                        device = dtTemp.Rows[0]["custdevice"].ToString();
                                                        if (!device.Equals(EquipCustDevice))
                                                        {
                                                            continue;
                                                        }
                                                        else
                                                        {
                                                            if (bCheckDevice)
                                                                continue;
                                                        }
                                                    }
                                                }

                                                iQty = 0; iTotalQty = 0; iQuantity = 0; iCountOfCarr = 0;

                                                //Check Workgroup Set 
                                                bNoFind = true;
                                                sql = _BaseDataService.QueryRackByGroupID(dtWorkgroupSet.Rows[0]["in_erack"].ToString());
                                                dtTemp = _dbTool.GetDataTable(sql);
                                                foreach (DataRow drRack in dtTemp.Rows)
                                                {
                                                    if (draCarrier["locate"].ToString().Equals(drRack["erackID"]))
                                                    {
                                                        bNoFind = false;
                                                        break;
                                                    }
                                                }

                                                if (bNoFind)
                                                {
                                                    if (_DebugMode)
                                                        _logger.Debug(string.Format("---Can Not Find eRack By GroupID."));

                                                    continue;
                                                }

                                                //if (!draCarrier["locate"].ToString().Equals(dtWorkgroupSet.Rows[0]["in_erack"]))
                                                //    continue;

                                                iQuantity = draCarrier.Table.Columns.Contains("quantity") ? int.Parse(draCarrier["quantity"].ToString()) : 0;
                                                //檢查Cassette Qty總和, 是否與Lot Info Qty相同, 相同才可派送 (滿足相同lot要放在相同機台上的需求)
                                                string sqlSentence = _BaseDataService.CheckQtyforSameLotId(draCarrier["lot_id"].ToString(), drRecord["CARRIER_TYPE"].ToString());
                                                DataTable dtSameLot = new DataTable();
                                                dtSameLot = _dbTool.GetDataTable(sqlSentence);
                                                if (dtSameLot.Rows.Count > 0)
                                                {
                                                    iQty = int.Parse(dtSameLot.Rows[0]["qty"].ToString());
                                                    iTotalQty = int.Parse(dtSameLot.Rows[0]["total_qty"].ToString());
                                                    iCountOfCarr = int.Parse(dtSameLot.Rows[0]["NumOfCarr"].ToString());

                                                    if (iCountOfCarr > 1)
                                                    {
                                                        if (iQty == iTotalQty)
                                                        { //To Do...
                                                            isLastLot = true;
                                                            sql = string.Format(_BaseDataService.LockMachineByLot(draCarrier["lot_id"].ToString(), iQuantity, 0));
                                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                                            _logger.Debug(tmpMsg);
                                                            tmpMsg = String.Format("=======IO==2==Load Carrier, Total Qty is {0}. Qty is {1}", iTotalQty, iQty);
                                                            _logger.Debug(tmpMsg);
                                                        }
                                                        else
                                                        {
                                                            tmpMsg = String.Format("=======IO==1==Load Carrier, Total Qty is {0}. Qty is {1}", iTotalQty, iQty);
                                                            _logger.Debug(tmpMsg);

                                                            if (iQty <= iQuantity)
                                                            {
                                                                isLastLot = true;
                                                                sql = string.Format(_BaseDataService.LockMachineByLot(draCarrier["lot_id"].ToString(), iQuantity, 0));
                                                                _dbTool.SQLExec(sql, out tmpMsg, true);
                                                            }
                                                            else
                                                            {
                                                                isLastLot = false;
                                                                sql = string.Format(_BaseDataService.LockMachineByLot(draCarrier["lot_id"].ToString(), iQuantity, 1));
                                                                _dbTool.SQLExec(sql, out tmpMsg, true);
                                                            }

                                                            if (iQty < iTotalQty)
                                                                continue;   //不搬運, 由unload 去發送(相同lot 需要由同一個port 執行)
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (iQty < iTotalQty)
                                                        {
                                                            int _lockMachine = 0;
                                                            int _compQty = 0;
                                                            //draCarrier["lot_id"].ToString()
                                                            sql = string.Format(_BaseDataService.SelectTableLotInfoByLotid(draCarrier["lot_id"].ToString()));
                                                            dtTemp = _dbTool.GetDataTable(sql);
                                                            _lockMachine = dtTemp.Rows[0]["lockmachine"].ToString().Equals("1") ? 1 : 0;
                                                            _compQty = dtTemp.Rows[0]["comp_qty"].ToString().Equals("0") ? 0 : int.Parse(dtTemp.Rows[0]["comp_qty"].ToString());

                                                            if (_lockMachine.Equals(0) && _compQty == 0)
                                                            {
                                                                isLastLot = false;
                                                                sql = string.Format(_BaseDataService.LockMachineByLot(draCarrier["lot_id"].ToString(), iQuantity, 1));
                                                                _dbTool.SQLExec(sql, out tmpMsg, true);
                                                            }
                                                            else
                                                            {
                                                                if (_compQty + iQuantity >= iTotalQty)
                                                                {
                                                                    isLastLot = true;
                                                                    sql = string.Format(_BaseDataService.LockMachineByLot(draCarrier["lot_id"].ToString(), _compQty + iQuantity, 0));
                                                                    _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                }
                                                                else
                                                                    isLastLot = false;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            isLastLot = true;
                                                            sql = string.Format(_BaseDataService.LockMachineByLot(draCarrier["lot_id"].ToString(), iQuantity, 0));
                                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                                        }
                                                    }
                                                }

                                                if (CarrierID.Equals(""))
                                                {
                                                    if (CheckIsAvailableLot(_dbTool, draCarrier["lot_id"].ToString(), drRecord["EquipID"].ToString()))
                                                    {
                                                        CarrierID = draCarrier["Carrier_ID"].ToString();
                                                        bIsMatch = true;
                                                    }
                                                    else
                                                    {
                                                        MetalRingCarrier = "";
                                                        bIsMatch = false;
                                                        if (_DebugMode)
                                                            _logger.Debug(string.Format("[IsMatch][{0}] {1} / {2} / {3}", drRecord["PORT_ID"].ToString(), bIsMatch, draCarrier["lot_id"].ToString(), drRecord["EquipID"].ToString()));
                                                        continue;
                                                    }
                                                }
                                                else
                                                    bIsMatch = true;

                                                if (bIsMatch)
                                                {
                                                    _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, true), out tmpMessage, true);
                                                    drCarrierData = dtAvaileCarrier.Select("carrier_id = '" + CarrierID + "'");
                                                    if (_DebugMode)
                                                        _logger.Debug(string.Format("[drCarrierData][{0}] {1} / {2} / {3}", drRecord["PORT_ID"].ToString(), bIsMatch, drCarrierData[0]["COMMAND_TYPE"].ToString(), CarrierID));

                                                    break;
                                                }

                                                continue;

                                            ResetReserve:
                                                tmpMessage = "";
                                                _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, false), out tmpMessage, true);
                                                CarrierID = "";
                                                _carrierID = "";
                                                _lotInCarrier = "";
                                                continue;
                                            }

                                            if (!bIsMatch)
                                                break;

                                            if (!tmpMessage.Equals(""))
                                            {
                                                _logger.Debug(string.Format("[tmpMessage] {0} / {1}", drRecord["EQUIPID"].ToString(), tmpMessage));
                                                _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, false), out tmpMessage, true);
                                                continue;
                                            }
                                            drCarrierData = dtAvaileCarrier.Select("carrier_id = '" + CarrierID + "'");
                                            if (_DebugMode)
                                                _logger.Debug(string.Format("[drCarrierData][{0}] {1} / {2}", drRecord["PORT_ID"].ToString(), drCarrierData[0]["COMMAND_TYPE"].ToString(), CarrierID));

                                        }

                                        lstTransfer = new TransferList();
                                        lstTransfer.CommandType = "LOAD";
                                        lstTransfer.Source = "*";
                                        lstTransfer.Dest = drRecord["PORT_ID"].ToString();
                                        lstTransfer.CarrierID = CarrierID;
                                        lstTransfer.Quantity = int.Parse(drCarrierData.Length > 0 ? drCarrierData[0]["QUANTITY"].ToString() : "0");
                                        lstTransfer.CarrierType = drCarrierData.Length > 0 ? drCarrierData[0]["command_type"].ToString() : "";
                                        lstTransfer.Total = iTotalQty;
                                        lstTransfer.IsLastLot = isLastLot ? 1 : 0;
                                    }
                                    catch (Exception ex)
                                    { }
                                    break;
                                case 0:
                                default:
                                    continue;
                                    break;
                            }

                            break;
                        case "OUT":
                            //bool bNoFind = true;
                            //0. wait to load 找到適合的Carrier, 並產生Load指令
                            //1. wait to unload 且沒有其它符合的Carrier 直接產生Unload指令
                            //2. wait to unload 而且有其它適用的Carrier(empty), 產生Load + Unload指令
                            //2.1. Load, 如果與in port的 carrier type 相同, 就不產生Load

                            //In port 與 Out port 的carrier type是否相同
                            bIsMatch = false;
                            bool bPortTypeSame = false;
                            drIn = dtPortsInfo.Select("Port_Type='IN'");
                            if (_portModel.Equals("2I2OT1"))
                            {
                                bPortTypeSame = false;
                            }
                            else
                            {
                                if (drIn[0]["CARRIER_TYPE"].ToString().Equals(drRecord["CARRIER_TYPE"].ToString()))
                                    bPortTypeSame = true;
                            }
                            dtLoadPortCarrier = _dbTool.GetDataTable(_BaseDataService.SelectLoadPortCarrierByEquipId(drRecord["EQUIPID"].ToString()));
                            if (dtLoadPortCarrier is not null)
                                drOut = dtLoadPortCarrier.Select("PORTNO = '" + drRecord["PORT_SEQ"].ToString() + "'");

                            iPortState = GetPortStatus(dtPortsInfo, drRecord["port_id"].ToString(), out sPortState);
                            if (_DebugMode)
                            {
                                _logger.Debug(string.Format("[Port_Type] {0} / {1}", drRecord["EQUIPID"].ToString(), iPortState));
                            }
                            switch (iPortState)
                            {
                                case 1:
                                    //1. Transfer Blocked
                                    continue;
                                case 2:
                                case 3:
                                case 5:
                                    //2. Near Completion
                                    //3.Ready to Unload
                                    //5. Reject and Ready to unload
                                    if (iPortState == 2)
                                    {
                                        if (!bNearComplete)
                                            break;
                                    }
                                    else if (iPortState == 3)
                                    {
                                        try
                                        {
                                            if (!lotID.Equals(""))
                                            {
                                                sql = _BaseDataService.GetLastStageOutErack(lotID, _adsTable, _eqpworkgroup);
                                                dtTemp2 = _dbTool.GetDataTable(sql);

                                                if (dtTemp2.Rows.Count > 0)
                                                {
                                                    string _tmpRack = "";
                                                    _tmpRack = dtTemp2.Rows[0]["OUT_ERACK"] is null ? "" : dtTemp2.Rows[0]["OUT_ERACK"].ToString();

                                                    if (!_tmpRack.Equals(""))
                                                    {
                                                        outeRack = _tmpRack;
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        { }
                                    }

                                    try
                                    {
                                        dtWorkInProcessSch = _dbTool.GetDataTable(_BaseDataService.QueryWorkInProcessSchByPortIdForUnload(drRecord["PORT_ID"].ToString(), tableOrder));
                                        if (dtWorkInProcessSch.Rows.Count > 0)
                                            continue;

                                        //1I1OT2 己經取得可用的MetalRing Carrier x.R
                                        if (MetalRingCarrier.Equals(""))
                                        {
                                            dtAvaileCarrier = GetAvailableCarrier(_dbTool, drRecord["CARRIER_TYPE"].ToString(), false, _keyRTDEnv);
                                            //AvaileCarrier is true
                                            if (dtAvaileCarrier.Rows.Count > 0)
                                            {
                                                bIsMatch = false;
                                                string tmpMessage = "";
                                                foreach (DataRow draCarrier in dtAvaileCarrier.Rows)
                                                {
                                                    if (_DebugMode)
                                                    {
                                                        _logger.Debug(string.Format("---locate is {0}", draCarrier["locate"].ToString()));
                                                        _logger.Debug(string.Format("---out erack is {0}", dtWorkgroupSet.Rows[0]["out_erack"].ToString()));
                                                    }

                                                    //Check Workgroup Set 
                                                    bNoFind = true;
                                                    sql = _BaseDataService.QueryRackByGroupID(dtWorkgroupSet.Rows[0]["out_erack"].ToString());
                                                    dtTemp = _dbTool.GetDataTable(sql);
                                                    foreach (DataRow drRack in dtTemp.Rows)
                                                    {
                                                        if (draCarrier["locate"].ToString().Equals(drRack["erackID"]))
                                                        {
                                                            bNoFind = false;
                                                            break;
                                                        }
                                                    }

                                                    if (bNoFind)
                                                    {
                                                        continue;
                                                    }
                                                    //if (!draCarrier["locate"].ToString().Equals(dtWorkgroupSet.Rows[0]["out_erack"]))
                                                    //    continue;

                                                    CarrierID = "";

                                                    if (_DebugMode)
                                                    {
                                                        _logger.Debug(string.Format("---1.1-PortModel is {0}", _portModel));
                                                    }

                                                    if (_portModel.Equals("2I2OT1"))
                                                    {
                                                        CarrierID = draCarrier["Carrier_ID"].ToString();
                                                        bIsMatch = true;
                                                    }
                                                    else
                                                    {
                                                        if (CheckIsAvailableLot(_dbTool, draCarrier["lot_id"].ToString(), drRecord["EquipID"].ToString()))
                                                        {
                                                            CarrierID = draCarrier["Carrier_ID"].ToString();
                                                            bIsMatch = true;
                                                        }
                                                        else
                                                        {
                                                            bIsMatch = false;
                                                            continue;
                                                        }
                                                    }

                                                    if (bIsMatch)
                                                    {
                                                        _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, true), out tmpMessage, true);
                                                        break;
                                                    }
                                                    break;
                                                }

                                                if (!tmpMessage.Equals(""))
                                                {
                                                    _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, false), out tmpMessage, true);
                                                    continue;
                                                }
                                            }
                                            
                                            if (_DebugMode)
                                            {
                                                _logger.Debug(string.Format("---dtAvaileCarrier.Rows.Count is {0}", dtAvaileCarrier.Rows.Count));
                                                _logger.Debug(string.Format("---bIsMatch is {0}", bIsMatch));
                                            }

                                            if (dtAvaileCarrier.Rows.Count <= 0 || !bIsMatch || isManualMode)
                                            {
                                                CarrierID = drOut.Length > 0 ? drOut[0]["CARRIER_ID"].ToString() : "";

                                                if (_portModel.Equals("2I2OT1"))
                                                {
                                                    if (CarrierID.Equals(""))
                                                        continue;
                                                }

                                                string tmpMessage = "";
                                                _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, true), out tmpMessage, true);

                                                if (!tmpMessage.Equals(""))
                                                {
                                                    _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, false), out tmpMessage, true);
                                                    continue;
                                                }

                                                lstTransfer = new TransferList();
                                                lstTransfer.CommandType = "UNLOAD";
                                                lstTransfer.Source = drRecord["PORT_ID"].ToString();
                                                lstTransfer.Dest = drRecord["OUT_ERACK"].ToString();
                                                lstTransfer.CarrierID = UnloadCarrierID.Equals("") ? "*" : UnloadCarrierID;
                                                lstTransfer.Quantity = 0;
                                                lstTransfer.CarrierType = dtLoadPortCarrier.Rows.Count > 0 ? dtLoadPortCarrier.Rows[0]["command_type"].ToString() : _CarrierTypebyPort;
                                                break;
                                            }

                                        }
                                        else
                                        {
                                            CarrierID = MetalRingCarrier;

                                            sql = string.Format(_BaseDataService.SelectTableCarrierTransferByCarrier(MetalRingCarrier));
                                            dtTemp = _dbTool.GetDataTable(sql);

                                            if (dtTemp.Rows.Count > 0)
                                            {
                                                dtAvaileCarrier = dtTemp;
                                            }
                                        }

                                        if (_DebugMode)
                                        {
                                            _logger.Debug(string.Format("----PortModel is {0}", _portModel));
                                        }

                                        if (_portModel.Equals("2I2OT1"))
                                        {
                                            CarrierID = CarrierID.Equals("") ? dtAvaileCarrier.Rows[0]["Carrier_ID"].ToString() : CarrierID;

                                            if (_DebugMode)
                                            {
                                                _logger.Debug(string.Format("----CarrierID is {0}", CarrierID));
                                            }

                                            lstTransfer = new TransferList();
                                            lstTransfer.CommandType = "LOAD";
                                            lstTransfer.Source = "*";
                                            lstTransfer.Dest = drRecord["PORT_ID"].ToString();
                                            lstTransfer.CarrierID = CarrierID;
                                            lstTransfer.Quantity = dtLoadPortCarrier.Rows.Count > 0 ? int.Parse(dtLoadPortCarrier.Rows[0]["QUANTITY"].ToString()) : 0;
                                            lstTransfer.CarrierType = dtLoadPortCarrier.Rows.Count > 0 ? dtLoadPortCarrier.Rows[0]["command_type"].ToString() : "";

                                            normalTransfer.Transfer.Add(lstTransfer);
                                            iReplace++;

                                            if (_DebugMode)
                                            {
                                                _logger.Debug(string.Format("----Logic true "));
                                            }
                                        }
                                        else if (_portModel.Equals("1I1OT2"))
                                        {
                                            if (_DebugMode)
                                            {
                                                _logger.Debug(string.Format("----MetalRing Carrier is {0}", MetalRingCarrier));
                                            }

                                            lstTransfer = new TransferList();
                                            lstTransfer.CommandType = "LOAD";
                                            lstTransfer.Source = "*";
                                            lstTransfer.Dest = drRecord["PORT_ID"].ToString();
                                            lstTransfer.CarrierID = CarrierID;
                                            lstTransfer.Quantity = dtAvaileCarrier.Rows.Count > 0 ? int.Parse(dtAvaileCarrier.Rows[0]["QUANTITY"].ToString()) : 0;
                                            lstTransfer.CarrierType = dtAvaileCarrier.Rows.Count > 0 ? dtAvaileCarrier.Rows[0]["command_type"].ToString() : "";

                                            normalTransfer.Transfer.Add(lstTransfer);
                                            iReplace++;

                                            if (_DebugMode)
                                            {
                                                _logger.Debug(string.Format("----Logic true "));
                                            }
                                        }
                                        else
                                        {
                                            if (!bPortTypeSame)
                                            {
                                                CarrierID = CarrierID.Equals("") ? dtAvaileCarrier.Rows[0]["Carrier_ID"].ToString() : CarrierID;

                                                lstTransfer = new TransferList();
                                                lstTransfer.CommandType = "LOAD";
                                                lstTransfer.Source = "*";
                                                lstTransfer.Dest = drRecord["PORT_ID"].ToString();
                                                lstTransfer.CarrierID = CarrierID;
                                                lstTransfer.Quantity = dtLoadPortCarrier.Rows.Count > 0 ? int.Parse(dtLoadPortCarrier.Rows[0]["QUANTITY"].ToString()) : 0;
                                                lstTransfer.CarrierType = dtLoadPortCarrier.Rows.Count > 0 ? dtLoadPortCarrier.Rows[0]["command_type"].ToString() : "";

                                                normalTransfer.Transfer.Add(lstTransfer);
                                                iReplace++;
                                            }
                                        }

                                        if (_DebugMode)
                                        {
                                            _logger.Debug(string.Format("----unload "));
                                        }

                                        CarrierID = drOut.Length > 0 ? drOut[0]["CARRIER_ID"].ToString() : "";

                                        if (_DebugMode)
                                        {
                                            _logger.Debug(string.Format("----unload CarrierID is {0}", CarrierID));
                                        }

                                        lstTransfer = new TransferList();
                                        lstTransfer.CommandType = "UNLOAD";
                                        lstTransfer.Source = drRecord["PORT_ID"].ToString();
                                        lstTransfer.Dest = drRecord["OUT_ERACK"].ToString();
                                        lstTransfer.CarrierID = UnloadCarrierID.Equals("") ? "*" : UnloadCarrierID;
                                        lstTransfer.Quantity = 0;
                                        lstTransfer.CarrierType = dtLoadPortCarrier.Rows.Count > 0 ? dtLoadPortCarrier.Rows[0]["command_type"].ToString() : _CarrierTypebyPort;
                                        //lstTransfer.CommandType = "UNLOAD";
                                        //lstTransfer.Source = drRecord["PORT_ID"].ToString();
                                        //lstTransfer.Dest = drRecord["OUT_ERACK"].ToString();
                                        //lstTransfer.CarrierID = CarrierID.Equals("") ? "*" : CarrierID;
                                        //lstTransfer.Quantity = 0;
                                        //lstTransfer.CarrierType = dtLoadPortCarrier.Rows.Count > 0 ? dtLoadPortCarrier.Rows[0]["COMMAND_TYPE"].ToString() : "";

                                        if (_DebugMode)
                                        {
                                            _logger.Debug(string.Format("----Done "));
                                        }
                                    }
                                    catch (Exception ex)
                                    { }
                                    break;
                                case 4:
                                    //4. Empty (Ready to load)
                                    try
                                    {
                                        dtWorkInProcessSch = _dbTool.GetDataTable(_BaseDataService.SelectTableWorkInProcessSchByPortId(drRecord["PORT_ID"].ToString(), tableOrder));
                                        if (dtWorkInProcessSch.Rows.Count > 0)
                                            continue;

                                        //drPortState = dtPortsInfo.Select("Port_State in (1, 4)");
                                        //if (drPortState.Length <= 0)
                                        //    continue;

                                        if (MetalRingCarrier.Equals(""))
                                        {
                                            dtAvaileCarrier = GetAvailableCarrier(_dbTool, drRecord["CARRIER_TYPE"].ToString(), false, _keyRTDEnv);
                                            if (dtAvaileCarrier is null)
                                                continue;
                                            if (dtAvaileCarrier.Rows.Count <= 0)
                                                continue;

                                            if (dtAvaileCarrier.Rows.Count > 0)
                                            {
                                                bIsMatch = false;
                                                string tmpMessage = "";
                                                foreach (DataRow draCarrier in dtAvaileCarrier.Rows)
                                                {

                                                    //Check Workgroup Set 
                                                    bNoFind = true;
                                                    sql = _BaseDataService.QueryRackByGroupID(dtWorkgroupSet.Rows[0]["out_erack"].ToString());
                                                    dtTemp = _dbTool.GetDataTable(sql);
                                                    foreach (DataRow drRack in dtTemp.Rows)
                                                    {
                                                        if (draCarrier["locate"].ToString().Equals(drRack["erackID"]))
                                                        {
                                                            bNoFind = false;
                                                            break;
                                                        }
                                                    }

                                                    if (bNoFind)
                                                    {
                                                        continue;
                                                    }
                                                    //if (!draCarrier["locate"].ToString().Equals(dtWorkgroupSet.Rows[0]["out_erack"]))
                                                    //    continue;

                                                    CarrierID = "";
                                                    //if (CheckIsAvailableLot(_dbTool, draCarrier["lot_id"].ToString(), drRecord["EquipID"].ToString()))
                                                    if (true)
                                                    {   //output port 不用檢查lot id
                                                        CarrierID = draCarrier["Carrier_ID"].ToString();
                                                        bIsMatch = true;
                                                    }
                                                    else
                                                    {
                                                        bIsMatch = false;
                                                        continue;
                                                    }

                                                    if (bIsMatch)
                                                    {
                                                        _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, true), out tmpMessage, true);
                                                        break;
                                                    }
                                                }

                                                if (!bIsMatch)
                                                    break;

                                                if (!tmpMessage.Equals(""))
                                                {
                                                    _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, false), out tmpMessage, true);
                                                    continue;
                                                }
                                                drCarrierData = dtAvaileCarrier.Select("carrier_id = '" + CarrierID + "'");
                                            }
                                        }
                                        else
                                        {
                                            sql = string.Format(_BaseDataService.SelectTableCarrierTransferByCarrier(MetalRingCarrier));
                                            dtTemp = _dbTool.GetDataTable(sql);

                                            if(dtTemp.Rows.Count > 0)
                                            {
                                                drCarrierData = dtTemp.Select("carrier_id = '" + MetalRingCarrier + "'");
                                                CarrierID = MetalRingCarrier;
                                                if (_DebugMode)
                                                {
                                                    _logger.Debug(string.Format("[Port_Type] {0} / {1} / {2}", drRecord["EQUIPID"].ToString(), CarrierID, drRecord["PORT_ID"].ToString()));
                                                }
                                            }
                                        }

                                        lstTransfer = new TransferList();
                                        lstTransfer.CommandType = "LOAD";
                                        lstTransfer.Source = "*";
                                        lstTransfer.Dest = drRecord["PORT_ID"].ToString();
                                        lstTransfer.CarrierID = CarrierID;
                                        lstTransfer.Quantity = drCarrierData[0]["QUANTITY"].ToString().Equals("") ? 0 : int.Parse(drCarrierData[0]["QUANTITY"].ToString());
                                        lstTransfer.CarrierType = drCarrierData[0]["command_type"].ToString();
                                    }
                                    catch (Exception ex)
                                    { }
                                    break;
                                case 0:
                                default:
                                    break;
                            }
                            break;
                        case "IO":
                        default:
                            //0. wait to load 找到適合的Carrier, 併產生Load指令
                            //1. wait to unload 且沒有其它符合的Carrier 直接產生Unload指令
                            //2. wait to unload 而且有其它適用的Carrier, 產生Swap指令(Load + Unload)
                            try
                            {
                                bNoFind = true;
                                int iQty = 0; int iQuantity = 0; int iTotalQty = 0;
                                bool isLastLot = false;

                                iPortState = GetPortStatus(dtPortsInfo, drRecord["port_id"].ToString(), out sPortState);
                                if (_DebugMode)
                                {
                                    _logger.Debug(string.Format("[PortState] {0} / {1}", drRecord["EQUIPID"].ToString(), iPortState));
                                }
                                switch (iPortState)
                                {
                                    case 1:
                                        //1. Transfer Blocked
                                        continue;
                                    case 2:
                                    case 3:
                                    case 5:
                                        //2. Near Completion
                                        //3.Ready to Unload
                                        //5. Reject and Ready to unload

                                        if (iPortState == 2)
                                        {
                                            if (!bNearComplete)
                                                break;
                                        }
                                        else if (iPortState == 3 || iPortState == 5)
                                        {
                                            try
                                            {
                                                if (!lotID.Equals(""))
                                                {
                                                    sql = _BaseDataService.GetLastStageOutErack(lotID, _adsTable, _eqpworkgroup);
                                                    dtTemp2 = _dbTool.GetDataTable(sql);

                                                    if(dtTemp2.Rows.Count > 0)
                                                    {
                                                        string _tmpRack = "";
                                                        _tmpRack = dtTemp2.Rows[0]["OUT_ERACK"] is null ? "" : dtTemp2.Rows[0]["OUT_ERACK"].ToString();

                                                        if(!_tmpRack.Equals(""))
                                                        {
                                                            outeRack = _tmpRack;
                                                        }

                                                        tmpMsg = string.Format("[GetLastStageOutErack][{0}][{1}][{2}][{3}][{4}]", dtTemp2.Rows[0]["lotid"].ToString(), dtTemp2.Rows[0]["stage"].ToString(), dtTemp2.Rows[0]["customername"].ToString(), dtTemp2.Rows[0]["fromstage"].ToString(), outeRack);
                                                        _logger.Debug(tmpMsg);
                                                    }
                                                }
                                                else
                                                {
                                                    if (_isFurnace)
                                                    {
                                                        outeRack = _dummycarrier;
                                                    }
                                                }
                                            }
                                            catch(Exception ex)
                                            { }

                                            if (isManualMode)
                                                _OnlyUnload = true;
                                        }

                                        try
                                        {
                                            dtWorkInProcessSch = _dbTool.GetDataTable(_BaseDataService.QueryWorkInProcessSchByPortIdForUnload(drRecord["PORT_ID"].ToString(), tableOrder));
                                            if (dtWorkInProcessSch.Rows.Count > 0)
                                                continue;

                                            if (_isFurnace)
                                            {
                                                //20241017 furnace reject也要做unload
                                                if(_furnState.Equals(2))
                                                    continue;
                                            }

                                            //取得當前Load Port上的Carrier
                                            UnloadCarrierID = "";
                                            dtLoadPortCarrier = _dbTool.GetDataTable(_BaseDataService.SelectLoadPortCarrierByEquipId(drRecord["EQUIPID"].ToString()));
                                            if (dtLoadPortCarrier is not null)
                                            {
                                                if (dtLoadPortCarrier.Rows.Count > 0)
                                                {
                                                    drIn = dtLoadPortCarrier.Select("PORTNO = '" + drRecord["PORT_SEQ"].ToString() + "'");
                                                    UnloadCarrierID = drIn is null ? "" : drIn.Length > 0 ? drIn[0]["CARRIER_ID"].ToString() : "";
                                                }
                                            }

                                            if (_OnlyUnload)
                                                goto UnloadLogic;

                                            dtAvaileCarrier = GetAvailableCarrierByLocate(_dbTool, configuration, drRecord["CARRIER_TYPE"].ToString(), "", ineRack, true, _keyRTDEnv, _eqpworkgroup);

                                            //int iQty = 0; int iQuantity = 0; int iTotalQty = 0;
                                            iQty = 0; iQuantity = 0; iTotalQty = 0;
                                            int iCountOfCarr = 0;
                                            //bool isLastLot = false;
                                            isLastLot = false;

                                            //AvaileCarrier is true
                                            if (dtAvaileCarrier.Rows.Count > 0)
                                            {
                                                bIsMatch = false;
                                                string tmpMessage = "";
                                                string _lotInCarrier = "";
                                                string _schSeq = "";
                                                string _qTime = "";
                                                string _goNow = "";
                                                string _lotAge = "";
                                                string _layerPrio = "";
                                                string _stageofLot = "";

                                                foreach (DataRow draCarrier in dtAvaileCarrier.Rows)
                                                {
                                                    iQty = 0; iTotalQty = 0; iQuantity = 0;
                                                    _carrierID = "";
                                                    _schSeq = ""; _qTime = ""; _goNow = ""; _layerPrio = ""; _lotAge = ""; _stageofLot = "";

                                                    try {
                                                        _schSeq = draCarrier["sch_seq"].ToString();
                                                        _qTime = draCarrier["qtime"].ToString();
                                                        _goNow = draCarrier["gonow"].ToString();
                                                        _lotAge = draCarrier["lot_age"].ToString();
                                                        _layerPrio = draCarrier["wpriority"].ToString(); 
                                                        //_schSeq, _qTime, _goNow, _layerPrio, _lotAge
                                                    }
                                                    catch (Exception ex) { }

                                                    try {
                                                        _lotInCarrier = draCarrier["lot_id"].ToString();
                                                        _carrierID = draCarrier["carrier_id"].ToString();

                                                        CarrierID = "";
                                                        if (CheckIsAvailableLot(_dbTool, _lotInCarrier, drRecord["EQUIPID"].ToString()))
                                                        {

                                                            CarrierID = _carrierID;
                                                            bIsMatch = true;

                                                            try
                                                            {
                                                                string _equiplist = "";
                                                                //Show equip list of lotid
                                                                sql = _BaseDataService.QueryEquipmentByLot(_lotInCarrier);
                                                                dtTemp = _dbTool.GetDataTable(sql);

                                                                if (dtTemp.Rows.Count > 0)
                                                                {
                                                                    _equiplist = dtTemp.Rows[0]["equiplist"].ToString();
                                                                }

                                                                _logger.Debug(string.Format("[CheckIsAvailableLot] {0} / {1} / {2} / {3} / {4} / {5} / {6} / {7} / {8}", drRecord["EQUIPID"].ToString(), bIsMatch, _lotInCarrier, _schSeq, _qTime, _goNow, _layerPrio, _lotAge, _equiplist));
                                                            }
                                                            catch (Exception ex) { }

                                                            try
                                                            {
                                                                sql = _BaseDataService.QueryLotinfoQuantity(_lotInCarrier);
                                                                dtTemp = _dbTool.GetDataTable(sql);
                                                                if (dtTemp.Rows.Count > 0)
                                                                {
                                                                    _stageofLot = dtTemp.Rows[0]["STAGE"].ToString();
                                                                }

                                                                //GetSetofLookupTable
                                                                sql = _BaseDataService.GetSetofLookupTable(_eqpworkgroup, _stageofLot);
                                                                dtTemp = _dbTool.GetDataTable(sql);
                                                                if (dtTemp.Rows.Count > 0)
                                                                {
                                                                    bCheckEquipLookupTable = dtTemp.Rows[0]["CHECKEQPLOOKUPTABLE"] is null ? false : dtTemp.Rows[0]["CHECKEQPLOOKUPTABLE"].ToString().Equals("1") ? true : false;
                                                                    bCheckRcpConstraint = dtTemp.Rows[0]["RCPCONSTRAINT"] is null ? false : dtTemp.Rows[0]["RCPCONSTRAINT"].ToString().Equals("1") ? true : false;
                                                                    _stageController = dtTemp.Rows[0]["stagecontroller"] is null ? false : dtTemp.Rows[0]["stagecontroller"].ToString().Equals("1") ? true : false;
                                                                }
                                                            }
                                                            catch (Exception ex) { }

                                                            if (_stageController)
                                                            {
                                                                string tmpdic = "";

                                                                try
                                                                {
                                                                    foreach (var kvp in _dicStageCtrl)
                                                                    {
                                                                        if (tmpdic.Equals(""))
                                                                            tmpdic = string.Format("[key={0},value={1}]", kvp.Key, kvp.Value);
                                                                        else
                                                                            tmpdic = string.Format("{0}[key={1},value={2}]", tmpdic, kvp.Key, kvp.Value);
                                                                    }
                                                                }
                                                                catch (Exception ex) { }

                                                                if (_dicStageCtrl.Count > 0)
                                                                {
                                                                    if (_dicStageCtrl.ContainsKey(_stageofLot))
                                                                    {
                                                                        if (_dicStageCtrl.ContainsValue(drRecord["PORT_ID"].ToString()))
                                                                        {
                                                                            //存在, 上機

                                                                        }
                                                                        else
                                                                        {
                                                                            _logger.Debug(tmpdic);

                                                                            _logger.Debug(string.Format("[StageController] {0} / {1} / {2} / {3} / {4}", drRecord["EQUIPID"].ToString(), "The Port Not in dictionary of Stage Controller", _eqpworkgroup, _lotInCarrier, _stageofLot));
                                                                            //不存在
                                                                            bIsMatch = false;
                                                                            _lotInCarrier = "";
                                                                            CarrierID = "";
                                                                            _carrierID = "";
                                                                            goto ResetReserve;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        _logger.Debug(tmpdic);

                                                                        _logger.Debug(string.Format("[StageController] {0} / {1} / {2} / {3} / {4}", drRecord["EQUIPID"].ToString(), "The Stage Not in dictionary of Stage Controller", _eqpworkgroup, _lotInCarrier, _stageofLot));
                                                                        bIsMatch = false;
                                                                        _lotInCarrier = "";
                                                                        CarrierID = "";
                                                                        _carrierID = "";
                                                                        goto ResetReserve;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    _logger.Debug(tmpdic);

                                                                    _logger.Debug(string.Format("[StageController] {0} / {1} / {2} / {3} / {4}", drRecord["EQUIPID"].ToString(), "No Set Stage Controller", _eqpworkgroup, _lotInCarrier, _stageofLot));
                                                                    bIsMatch = false;
                                                                    _lotInCarrier = "";
                                                                    CarrierID = "";
                                                                    _carrierID = "";
                                                                    goto ResetReserve;
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            bIsMatch = false;
                                                            _lotInCarrier = "";
                                                            CarrierID = "";
                                                            goto ResetReserve;
                                                        }

                                                        if (_DebugMode)
                                                        {
                                                            _logger.Debug(string.Format("[CheckIsAvailableLot] {0} / {1} / {2} / {3} / {4} / {5} / {6} / {7}", drRecord["EQUIPID"].ToString(), bIsMatch, _lotInCarrier, _schSeq, _qTime, _goNow, _layerPrio, _lotAge));
                                                            //_logger.Debug(string.Format("[CheckIsAvailableLot] {0} / {1} / {2}", drRecord["EQUIPID"].ToString(), bIsMatch, _lotInCarrier));
                                                        }

                                                        if (bIsMatch)
                                                        {
                                                            _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, true), out tmpMessage, true);
                                                            //break;
                                                        }

                                                        //QTIME Logic: lot QTime < Low or QTime > High do not build load command.
                                                        //Upgrade program logic for QTime
                                                        //Position: 202310111000001
                                                        try
                                                        {
                                                            if(_QTimeMode.Equals(1))
                                                            {

                                                                //取得Lot QTime from ads
                                                                sql = _BaseDataService.GetQTimeLot(configuration["PreDispatchToErack:lotState:tableName"], _lotInCarrier);
                                                                dtTemp = _dbTool.GetDataTable(sql);

                                                                if (dtTemp.Rows.Count > 0)
                                                                {
                                                                    bTurnOnQTime = dtTemp.Rows[0]["QTIME"].ToString().Equals("NA") ? false : true;
                                                                    if (bTurnOnQTime)
                                                                        iLotQTime = float.Parse(dtTemp.Rows[0]["QTIME"].ToString());
                                                                    else
                                                                        iLotQTime = 0;
                                                                }
                                                                else
                                                                {
                                                                    bTurnOnQTime = false;
                                                                    iLotQTime = 0;
                                                                }

                                                                if (bTurnOnQTime)
                                                                {
                                                                    _logger.Debug(string.Format("[Q-Time Logic in draCarrier][{0}][{1}] Lot QTime: {2}", _lotInCarrier, drRecord["EquipID"].ToString(), iLotQTime));

                                                                    if (iLotQTime < iQTimeLow)
                                                                    {
                                                                        //lotID, _Equip, iLotQTime
                                                                        _logger.Debug(string.Format("[Q-Time Logic is not enough][{0}][{1}][{2}][{3}] _Out", _lotInCarrier, iLotQTime, _Equip, iLotQTime));
                                                                        //continue;
                                                                        goto ResetReserve;
                                                                    }

                                                                    if (iLotQTime > iQTimeHigh)
                                                                    {
                                                                        _logger.Debug(string.Format("[Q-Time Logic exceeds][{0}][{1}][{2}][{3}] _Out", _lotInCarrier, iLotQTime, _Equip, iQTimeHigh));
                                                                        //continue;
                                                                        goto ResetReserve;
                                                                    }

                                                                    _logger.Debug(string.Format("[Q-Time pass][{0}][{1}][{2}][{3}][{4}]", _lotInCarrier, iLotQTime, iQTimeHigh, iQTime, iQTimeLow));
                                                                }
                                                            } else if (_QTimeMode.Equals(2))
                                                            {
                                                                try
                                                                {
                                                                    iQTime = 0;
                                                                    iLotQTime = 0;
                                                                    iQTimeLow = 0;
                                                                    iQTimeHigh = 0;

                                                                    if (!_lotInCarrier.Equals(""))
                                                                    {
                                                                        try
                                                                        {
                                                                            iQTime = draCarrier["QTIME1"] is null ? 0 : float.Parse(draCarrier["QTIME1"].ToString());
                                                                            iLotQTime = draCarrier["QTIME"] is null ? 0 : float.Parse(draCarrier["QTIME"].ToString());
                                                                            iQTimeLow = draCarrier["minallowabletw"] is null ? 0 : float.Parse(draCarrier["minallowabletw"].ToString());
                                                                            iQTimeHigh = draCarrier["maxallowabletw"] is null ? 0 : float.Parse(draCarrier["maxallowabletw"].ToString());

                                                                            if (dtTemp2.Rows[0]["gonow"].ToString().Equals("Y"))
                                                                            {
                                                                                ///Q-Time 為Go Now, change lot priority 為80
                                                                                _dbTool.SQLExec(_BaseDataService.UpdatePriorityByLotid(_lotInCarrier, 80), out tmpMsg, true);
                                                                            }
                                                                        }
                                                                        catch (Exception ex) { }

                                                                        if (_qTimemode_enable)
                                                                        {
                                                                            if (iQTime < 0)
                                                                            {
                                                                                _logger.Debug(string.Format("[Q-Time is not enough][{0}][{1}][{2}][{3}][{4}][{5}]", _QTimeMode, _lotInCarrier, iLotQTime, _Equip, sPortID, iQTimeLow));
                                                                                goto ResetReserve;
                                                                            }

                                                                            if (iQTime > 1)
                                                                            {
                                                                                //Auto hold lot times for Qtime 
                                                                                int iholttimes = 0;
                                                                                sql = _BaseDataService.SelectTableLotInfoByLotid(lotID);
                                                                                dtTemp3 = _dbTool.GetDataTable(sql);
                                                                                if (dtTemp3.Rows.Count > 0)
                                                                                {
                                                                                    iholttimes = int.Parse(dtTemp3.Rows[0]["hold_times"].ToString());
                                                                                }

                                                                                if (iholttimes <= 0)
                                                                                {
                                                                                    _logger.Debug(string.Format("[Q-Time exceeds][{0}][{1}][{2}][{3}][{4}][{5}]", _QTimeMode, _lotInCarrier, iLotQTime, _Equip, sPortID, iQTimeHigh));

                                                                                    try
                                                                                    {
                                                                                        _logger.Debug(string.Format("[Q-Time exceeds][{0}][{1}]", "Auto hold lot. ZZ, Q-Time failed.", _lotInCarrier));

                                                                                        jcetWebServiceClient = new JCETWebServicesClient();
                                                                                        jcetWebServiceClient._url = _qTime_url;
                                                                                        jcetWebServiceClient._urlUAT = _qTime_urlUAT;
                                                                                        string _args = "Q-Time failed,ZZ";
                                                                                        //public ResultMsg CustomizeEvent(string _func, string useMethod, bool isProduct, string equip, string username, string pwd, string lotid, string _args)
                                                                                        resultMsg = new JCETWebServicesClient.ResultMsg();
                                                                                        resultMsg = jcetWebServiceClient.CustomizeEvent("holdlot", "post", _qTime_isProduct, _Equip, _qTime_username, _qTime_pwd, _lotInCarrier, _args);
                                                                                        string result3 = resultMsg.retMessage;

                                                                                        sql = _BaseDataService.UadateHoldTimes(lotID);
                                                                                        _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                                    }
                                                                                    catch (Exception ex) { }
                                                                                }
                                                                                goto ResetReserve;
                                                                            }

                                                                            _logger.Debug(string.Format("[Q-Time pass][{0}][{1}][{2}][{3}][{4}]", _lotInCarrier, iLotQTime, iQTime, iQTimeHigh, iQTimeLow));
                                                                        }
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    _logger.Debug(string.Format("[Q-Time Logic][Exception][{0}][{1}][{2}][{3}][{4}]", drRecord["EQUIPID"].ToString(), vStage, drRecord["carrierType_M"].ToString(), _lotInCarrier, ex.Message));
                                                                }
                                                            } else
                                                            {

                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            _logger.Debug(string.Format("[Q-Time Logic in draCarrier][{0}][{1}] Exception: {2}", _lotInCarrier, drRecord["EquipID"].ToString(), ex.Message));
                                                        }
                                                    }
                                                    catch(Exception ex) { }

                                                    //站點不當前站點不同, 不取這批lot
                                                    if (!_lotInCarrier.Equals(""))
                                                    {
                                                        if (configuration["AppSettings:Work"].ToUpper().Equals("EWLB"))
                                                        {
                                                            try {
                                                                sql = _BaseDataService.CheckLotStage(configuration["CheckLotStage:Table"], _lotInCarrier);
                                                                dtTemp = _dbTool.GetDataTable(sql);

                                                                if (dtTemp.Rows.Count <= 0)
                                                                {
                                                                    //continue;
                                                                    goto ResetReserve;
                                                                }
                                                                else
                                                                {
                                                                    if (!dtTemp.Rows[0]["stage1"].ToString().Equals(dtTemp.Rows[0]["stage2"].ToString()))
                                                                    {
                                                                        _logger.Debug(string.Format("Base LotInfor: LotID= {0}, RTD_Stage={1}, MES_Stage={2}, RTD_State={3}, MES_State={4}", _lotInCarrier, dtTemp.Rows[0]["stage1"].ToString(), dtTemp.Rows[0]["stage2"].ToString(), dtTemp.Rows[0]["state1"].ToString(), dtTemp.Rows[0]["state2"].ToString()));
                                                                    }
                                                                }
                                                            }
                                                            catch(Exception ex) { }
                                                            
                                                            try
                                                            {
                                                                //不可Load 至已執行過的機台
                                                                sql = _BaseDataService.CheckMeasurementAndThickness(configuration["MeasurementThickness:Table"], _lotInCarrier, dtTemp.Rows[0]["stage2"].ToString(), drRecord["EQUIPID"].ToString());
                                                                dtTemp2 = _dbTool.GetDataTable(sql);

                                                                if (dtTemp2.Rows.Count > 0)
                                                                {
                                                                    _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}][{3}][{4}] _Out", "IS THK", _lotInCarrier, dtTemp.Rows[0]["stage2"].ToString(), drRecord["EQUIPID"].ToString(), sql));
                                                                    //continue;
                                                                    goto ResetReserve;
                                                                }
                                                                else
                                                                {
                                                                    _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}][{3}][{4}]", "None THK", draCarrier["lot_id"].ToString(), dtTemp.Rows[0]["stage2"].ToString(), drRecord["EQUIPID"].ToString(), sql));
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}] _Out", "Exception", ex.Message));
                                                                //continue;
                                                                goto ResetReserve;
                                                            }

                                                            if(bCheckEquipLookupTable)
                                                            {
                                                                try 
                                                                {
                                                                    sql = _BaseDataService.CheckLookupTable(configuration["CheckEqpLookupTable:Table"], drRecord["EQUIPID"].ToString(), _lotInCarrier);
                                                                    dtTemp = _dbTool.GetDataTable(sql);

                                                                    if (dtTemp.Rows.Count <= 0)
                                                                    {
                                                                        _logger.Debug(string.Format("[{0}][Failed][{1}][{2}][{3}] _Out", "CheckEquipLookupTable", drRecord["EQUIPID"].ToString(), _lotInCarrier, dtTemp.Rows[0]["equip2"].ToString()));
                                                                        //continue;
                                                                        goto ResetReserve;
                                                                    }
                                                                    else
                                                                    {
                                                                        _logger.Debug(string.Format("[{0}][Success][{1}][{2}][{3}]", "CheckEquipLookupTable", drRecord["EQUIPID"].ToString(), _lotInCarrier, dtTemp.Rows[0]["equip2"].ToString()));
                                                                    }
                                                                }
                                                                catch(Exception ex)
                                                                {
                                                                    _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}] _Out", "Exception", "CheckEquipLookupTable", ex.Message));
                                                                    //continue;
                                                                    goto ResetReserve;
                                                                }
                                                            }

                                                            if (bCheckRcpConstraint)
                                                            {
                                                                try
                                                                {
                                                                    sql = _BaseDataService.CheckRcpConstraint(configuration["CheckEqpLookupTable:Table"], drRecord["EQUIPID"].ToString(), _lotInCarrier);
                                                                    dtTemp = _dbTool.GetDataTable(sql);

                                                                    if (dtTemp.Rows.Count > 0)
                                                                    {
                                                                        _logger.Debug(string.Format("[{0}][Failed][{1}][{2}][{3}] _Out", "CheckRcpConstraint", drRecord["EQUIPID"].ToString(), _lotInCarrier, dtTemp.Rows[0]["rcpconstraint_list"].ToString()));
                                                                        //continue;
                                                                        goto ResetReserve;
                                                                    }
                                                                    else
                                                                    {
                                                                        _logger.Debug(string.Format("[{0}][Success][{1}][{2}][{3}]", "CheckRcpConstraint", drRecord["EQUIPID"].ToString(), _lotInCarrier, "Success"));
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}] _Out", "Exception", "CheckRcpConstraint", ex.Message));
                                                                    //continue;
                                                                    goto ResetReserve;
                                                                }
                                                            }
                                                        }
                                                    }

                                                    //Check Workgroup Set 
                                                    bNoFind = true;
                                                    sql = _BaseDataService.QueryRackByGroupID(dtWorkgroupSet.Rows[0]["in_erack"].ToString());
                                                    dtTemp = _dbTool.GetDataTable(sql);
                                                    foreach (DataRow drRack in dtTemp.Rows)
                                                    {
                                                        if (draCarrier["locate"].ToString().Equals(drRack["erackID"].ToString()))
                                                        {
                                                            bNoFind = false;
                                                            break;
                                                        }
                                                    }

                                                    if (bNoFind)
                                                    {
                                                        //continue;
                                                        goto ResetReserve;
                                                    }

                                                    if (_DebugMode)
                                                    {
                                                        _logger.Debug(string.Format("[QueryRackByGroupID] {0} / {1} / {2}", drRecord["EQUIPID"].ToString(), draCarrier["locate"].ToString(), _lotInCarrier));
                                                    }

                                                    if (_QTimeMode.Equals(1))
                                                    {
                                                        try
                                                        {
                                                            iLotQTime = 0;

                                                            sql = _BaseDataService.GetQTimeLot(configuration["PreDispatchToErack:lotState:tableName"], _lotInCarrier);
                                                            dtTemp = _dbTool.GetDataTable(sql);

                                                            if (dtTemp.Rows.Count > 0)
                                                            {
                                                                try
                                                                {
                                                                    //iLotQTime = float.Parse(dtTemp.Rows[0]["QTime"].ToString());

                                                                    bTurnOnQTime = dtTemp.Rows[0]["QTIME"].ToString().Equals("NA") ? false : true;
                                                                    if (bTurnOnQTime)
                                                                        iLotQTime = float.Parse(dtTemp.Rows[0]["QTIME"].ToString());
                                                                    else
                                                                        iLotQTime = 0;
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    _logger.Debug(string.Format("[GetQTimeLot][Exception][{0}][{1}][{2}][{3}] _Out", _lotInCarrier, iLotQTime, _Equip, dtTemp.Rows[0]["QTime"].ToString()));
                                                                }
                                                            }
                                                            else
                                                            {
                                                                bTurnOnQTime = false;
                                                                iLotQTime = 0;
                                                            }

                                                            if (bTurnOnQTime)
                                                            {
                                                                try
                                                                {
                                                                    try
                                                                    {
                                                                        sql = _BaseDataService.CheckCarrierTypeOfQTime(drRecord["EQUIPID"].ToString(), vStage, draCarrier["carrier_type"].ToString());
                                                                        dtTemp2 = _dbTool.GetDataTable(sql);

                                                                        if (dtTemp2.Rows.Count > 0)
                                                                        {
                                                                            //"Position": "202312221900002"
                                                                            iQTimeLow = 0;
                                                                            iQTimeHigh = 6;
                                                                        }
                                                                        else
                                                                        {
                                                                            _logger.Debug(string.Format("[Q-CheckCarrierTypeOfQTime][{0}][{1}][{2}][{3}]", drRecord["EQUIPID"].ToString(), vStage, draCarrier["carrier_type"].ToString(), _lotInCarrier));
                                                                        }
                                                                    }
                                                                    catch (Exception ex)
                                                                    {
                                                                        _logger.Debug(string.Format("[Q-CheckCarrierTypeOfQTime][Exception][{0}][{1}][{2}][{3}][{4}]", drRecord["EQUIPID"].ToString(), vStage, draCarrier["carrier_type"].ToString(), _lotInCarrier, ex.Message));
                                                                        //continue;
                                                                    }

                                                                    if (iLotQTime < iQTimeLow)
                                                                    {
                                                                        //lotID, _Equip, iLotQTime
                                                                        _logger.Debug(string.Format("[Q-Time is not enough][{0}][{1}][{2}][{3}] _Out", _lotInCarrier, iLotQTime, _Equip, iQTimeLow));
                                                                        //continue;
                                                                        goto ResetReserve;
                                                                    }

                                                                    if (iLotQTime > iQTimeHigh)
                                                                    {
                                                                        _logger.Debug(string.Format("[Q-Time exceeds][{0}][{1}][{2}][{3}] _Out", _lotInCarrier, iLotQTime, _Equip, iQTimeHigh));
                                                                        //continue;
                                                                        goto ResetReserve;
                                                                    }

                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    //_logger.Debug(string.Format("[Q-CheckCarrierTypeOfQTime][Exception][{0}][{1}][{2}]", drRecord["EQUIPID"].ToString(), vStage, draCarrier["carrier_type"].ToString()));
                                                                    //continue;
                                                                }
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            _logger.Debug(string.Format("[Q-Time][Exception][{0}][{1}][{2}][{3}]", drRecord["EQUIPID"].ToString(), vStage, _lotInCarrier, ex.Message));
                                                            //continue;
                                                            goto ResetReserve;
                                                        }
                                                    }

                                                    try
                                                    {
                                                        //check lot recipe
                                                        if (bCheckRecipe)
                                                        {
                                                            _lotrecipe = "";

                                                            try
                                                            {
                                                                _lotrecipe = draCarrier["custdevice"].ToString();

                                                                dtRecipeSet = _dbTool.GetDataTable(_BaseDataService.QueryRecipeSetting(drRecord["EQUIPID"].ToString()));
                                                                drRecipe = dtRecipeSet.Select(string.Format("recipeID='{0}'", _lotrecipe));

                                                                if (drRecipe.Length > 0)
                                                                {
                                                                    _lotrecipeGroup = drRecipe[0]["recipe_group"].ToString();
                                                                }


                                                                if (!_equiprecipeGroup.Equals(_lotrecipeGroup))
                                                                    continue;
                                                            }
                                                            catch (Exception ex)
                                                            { }
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    { }
                                                    //if (!draCarrier["locate"].ToString().Equals(dtWorkgroupSet.Rows[0]["in_erack"]))
                                                    //    continue;

                                                    iQuantity = draCarrier.Table.Columns.Contains("quantity") ? int.Parse(draCarrier["quantity"].ToString()) : 0;
                                                    //檢查Cassette Qty總和, 是否與Lot Info Qty相同, 相同才可派送 (滿足相同lot要放在相同機台上的需求)
                                                    string sqlSentence = _BaseDataService.CheckQtyforSameLotId(draCarrier["lot_id"].ToString(), drRecord["CARRIER_TYPE"].ToString());
                                                    DataTable dtSameLot = new DataTable();
                                                    dtSameLot = _dbTool.GetDataTable(sqlSentence);
                                                    if (dtSameLot.Rows.Count > 0)
                                                    {
                                                        iQty = int.Parse(dtSameLot.Rows[0]["qty"].ToString());
                                                        iTotalQty = int.Parse(dtSameLot.Rows[0]["total_qty"].ToString());
                                                        iCountOfCarr = int.Parse(dtSameLot.Rows[0]["NumOfCarr"].ToString());

                                                        if (_DebugMode)
                                                        {
                                                            _logger.Debug(string.Format("[CheckQtyforSameLotId] {0} / {1} / {2} / {3}", drRecord["EQUIPID"].ToString(), iCountOfCarr, iQty, iTotalQty));
                                                        }

                                                        if (iCountOfCarr > 1)
                                                        {
                                                            if (iQty == iTotalQty)
                                                            { //To Do...
                                                                isLastLot = true;
                                                                sql = string.Format(_BaseDataService.LockMachineByLot(_lotInCarrier, iQuantity, 1));
                                                                _dbTool.SQLExec(sql, out tmpMsg, true);
                                                            }
                                                            else
                                                            {
                                                                if (iQty <= iQuantity)
                                                                {
                                                                    isLastLot = true;
                                                                    sql = string.Format(_BaseDataService.LockMachineByLot(_lotInCarrier, iQuantity, 0));
                                                                    _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                }
                                                                else
                                                                {
                                                                    isLastLot = false;
                                                                    sql = string.Format(_BaseDataService.LockMachineByLot(_lotInCarrier, iQuantity, 1));
                                                                    _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (iQty < iTotalQty)
                                                            {
                                                                int _lockMachine = 0;
                                                                int _compQty = 0;
                                                                //draCarrier["lot_id"].ToString()
                                                                sql = string.Format(_BaseDataService.SelectTableLotInfoByLotid(_lotInCarrier));
                                                                dtTemp = _dbTool.GetDataTable(sql);
                                                                _lockMachine = dtTemp.Rows[0]["lockmachine"].ToString().Equals("1") ? 1 : 0;
                                                                _compQty = dtTemp.Rows[0]["comp_qty"].ToString().Equals("0") ? 0 : int.Parse(dtTemp.Rows[0]["comp_qty"].ToString());

                                                                if (_lockMachine.Equals(0) && _compQty == 0)
                                                                {
                                                                    isLastLot = false;
                                                                    sql = string.Format(_BaseDataService.LockMachineByLot(_lotInCarrier, iQuantity, 1));
                                                                    _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                }
                                                                else
                                                                {
                                                                    if (_compQty + iQuantity >= iTotalQty)
                                                                    {
                                                                        isLastLot = true;
                                                                        sql = string.Format(_BaseDataService.LockMachineByLot(_lotInCarrier, _compQty + iQuantity, 0));
                                                                        _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                    }
                                                                    else
                                                                        isLastLot = false;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                isLastLot = true;
                                                                sql = string.Format(_BaseDataService.LockMachineByLot(_lotInCarrier, iQuantity, 0));
                                                                _dbTool.SQLExec(sql, out tmpMsg, true);
                                                            }
                                                        }
                                                    }

                                                    if (bIsMatch)
                                                    {
                                                        _logger.Debug(string.Format("[HasMatchLogic] {0} / {1} / {2}", drRecord["EQUIPID"].ToString(), _lotInCarrier, CarrierID));
                                                        //if (!CarrierID.Equals(_lotInCarrier))
                                                        //{
                                                          //  _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, false), out tmpMessage, true);
                                                            //CarrierID = "";
                                                        //}
                                                        //_dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, true), out tmpMessage, true);
                                                        break;
                                                    }

                                                ResetReserve:
                                                    tmpMessage = "";
                                                    if(!CarrierID.Equals(""))
                                                        _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, false), out tmpMessage, true);
                                                    CarrierID = "";
                                                    _carrierID = "";
                                                    _lotInCarrier = "";
                                                    bIsMatch = false;
                                                    //continue;
                                                }

                                                if (!tmpMessage.Equals(""))
                                                {
                                                    if (_DebugMode)
                                                    {
                                                        _logger.Debug(string.Format("[tmpMessage] {0} / {1} ", drRecord["EQUIPID"].ToString(), tmpMessage));
                                                    }
                                                    _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, false), out tmpMessage, true);
                                                    continue;
                                                }
                                            }

                                            if (_DebugMode)
                                            {
                                                _logger.Debug(string.Format("[Build Unload Command] {0} / {1} / {2}", drRecord["EQUIPID"].ToString(), bIsMatch, dtAvaileCarrier.Rows.Count));
                                            }

                                        UnloadLogic:

                                            if (dtAvaileCarrier is null || dtAvaileCarrier.Rows.Count <= 0 || bIsMatch || CarrierID.Equals("") || bNoFind || isManualMode)
                                            {
                                                if (_DebugMode)
                                                {
                                                    if(dtAvaileCarrier is not null)
                                                        _logger.Debug(string.Format("[dtAvaileCarrier] Have Availe Carrier {0}", dtAvaileCarrier.Rows.Count));
                                                }

                                                String tmp11 = "";
                                                //if(CarrierID.Equals(""))
                                                    //CarrierID = drIn is not null ? drIn.Length > 0 ? drIn[0]["CARRIER_ID"].ToString() : "" : CarrierID.Equals("") ? "*" : CarrierID;

                                                if (_DebugMode)
                                                {
                                                    _logger.Debug(string.Format("[dtAvaileCarrier] Availe Carrier ID [{0}]", CarrierID));
                                                }

                                                string tempDest = "";
                                                string tmpReject = configuration["Reject:ERACK"] is not null ? configuration["Reject:ERACK"] : "IN_ERACK";
                                                if (iPortState.Equals(5))
                                                {
                                                    if (isMeasureFail)
                                                        tempDest = faileRack;
                                                    else
                                                        tempDest = drRecord[tmpReject].ToString().Equals("") ? drRecord["IN_ERACK"].ToString() : drRecord[tmpReject].ToString();
                                                }
                                                else
                                                {
                                                    if (!outeRack.Equals(""))
                                                        tempDest = outeRack;
                                                    else
                                                        tempDest = drRecord["OUT_ERACK"].ToString();
                                                }

                                                lstTransfer = new TransferList();
                                                lstTransfer.CommandType = "UNLOAD";
                                                lstTransfer.Source = drRecord["PORT_ID"].ToString();
                                                lstTransfer.Dest = tempDest;
                                                lstTransfer.CarrierID = UnloadCarrierID.Equals("") ? "*" : UnloadCarrierID;
                                                if (!lstTransfer.CarrierID.Equals(""))
                                                {
                                                    lstTransfer.LotID = "";
                                                }
                                                if(UnloadCarrierID.Equals(""))
                                                {
                                                    _logger.Debug(string.Format("[UnloadCarrierType] PORT_ID [{0}]", drRecord["PORT_ID"].ToString()));
                                                    string tmpCarrier = GetCarrierByPortId(_dbTool, drRecord["PORT_ID"].ToString());
                                                    _logger.Debug(string.Format("[UnloadCarrierType] tmpSQL [{0}]", tmpCarrier));
                                                    sql = _BaseDataService.SelectTableCarrierAssociateByCarrierID(tmpCarrier);
                                                    _logger.Debug(string.Format("[UnloadCarrierType] SQL [{0}]", sql));
                                                    dtTemp2 = _dbTool.GetDataTable(sql);
                                                    if (dtTemp2.Rows.Count > 0)
                                                    {
                                                        UnloadCarrierType = dtTemp2.Rows[0]["carrier_type"].ToString();
                                                        _logger.Debug(string.Format("[UnloadCarrierType] Carrier ID [{0}] / {1}", UnloadCarrierType, sql));
                                                    }
                                                }
                                                else
                                                {
                                                    sql = _BaseDataService.SelectTableCarrierAssociateByCarrierID(UnloadCarrierID);
                                                    _logger.Debug(string.Format("[UnloadCarrierType] SQL [{0}]", sql));
                                                    dtTemp2 = _dbTool.GetDataTable(sql);
                                                    if (dtTemp2.Rows.Count > 0)
                                                    {
                                                        UnloadCarrierType = dtTemp2.Rows[0]["command_type"].ToString();
                                                        _logger.Debug(string.Format("[UnloadCarrierType] Carrier ID [{0}] / {1}", UnloadCarrierType, sql));
                                                    }
                                                }
                                                lstTransfer.Quantity = 0;
                                                //lstTransfer.CarrierType = dtLoadPortCarrier.Rows.Count > 0 ? dtLoadPortCarrier.Rows[0]["carrier_type"].ToString() : "";
                                                lstTransfer.CarrierType = UnloadCarrierType.Equals("") ? dtLoadPortCarrier.Rows.Count > 0 ? dtLoadPortCarrier.Rows[0]["command_type"].ToString() : _CarrierTypebyPort : UnloadCarrierType;

                                                if (_DebugMode)
                                                {
                                                    _logger.Debug(string.Format("[dtAvaileCarrier] Unload Carrier ID [{0}] / {1}", lstTransfer.CarrierID, normalTransfer.Transfer.Count));
                                                }

                                                if (dtAvaileCarrier is null || dtAvaileCarrier.Rows.Count <= 0 || isManualMode)
                                                {
                                                    break;
                                                }
                                                else
                                                {
                                                    normalTransfer.Transfer.Add(lstTransfer);
                                                    iReplace++;
                                                }
                                                //break;
                                            }
                                            //AvaileCarrier is true
                                            if (bIsMatch)
                                            {
                                                lstTransfer = new TransferList();
                                                drCarrierData = dtAvaileCarrier.Select("carrier_id = '" + CarrierID + "'");

                                                if (_DebugMode)
                                                {
                                                    _logger.Debug(string.Format("[dtAvaileCarrier] Load Carrier ID [{0}]", CarrierID));
                                                }
                                            }

                                            if (drCarrierData is not null)
                                            {
                                                if (_DebugMode)
                                                {
                                                    _logger.Debug(string.Format("[dtAvaileCarrier] Not Null"));
                                                }

                                                if (drCarrierData.Length > 0)
                                                {
                                                    lstTransfer.CommandType = "LOAD";
                                                    lstTransfer.Source = "*";
                                                    lstTransfer.Dest = drRecord["PORT_ID"].ToString();
                                                    lstTransfer.CarrierID = CarrierID;
                                                    iQty = drCarrierData == null ? 0 : int.Parse(drCarrierData[0]["QUANTITY"].ToString());
                                                    lstTransfer.Quantity = iQty;
                                                    lstTransfer.CarrierType = drCarrierData == null ? "" : drCarrierData[0]["command_type"].ToString();
                                                    lstTransfer.Total = iTotalQty;
                                                    lstTransfer.IsLastLot = isLastLot ? 1 : 0;


                                                    if (_DebugMode)
                                                    {
                                                        _logger.Debug(string.Format("[dtAvaileCarrier] LOAD Command [{0}] / {1}", CarrierID, normalTransfer.Transfer.Count));
                                                    }

                                                    if (iReplace < 1)
                                                    {
                                                        normalTransfer.Transfer.Add(lstTransfer);
                                                        iReplace++;
                                                    }
                                                    else
                                                        break;
                                                }
                                            }

                                            if (_DebugMode)
                                            {
                                                _logger.Debug(string.Format("[dtAvaileCarrier] LOAD iReplace [{0}] / {1}", lstTransfer.CarrierID, iReplace));
                                            }

                                            if (CarrierID.Equals(""))
                                                CarrierID = dtLoadPortCarrier.Rows[0]["CARRIER_ID"].ToString();

                                            //檢查Out port 與 In port 的carrier type是否相同

                                            drOut = dtPortsInfo.Select("Port_Type='IO'");
                                            if (drOut.Length > 0)
                                            {
                                                if (drOut[0]["CARRIER_TYPE"].ToString().Equals(drRecord["CARRIER_TYPE"].ToString()))
                                                {
                                                    string tmpPortState = "";
                                                    lstTransfer = new TransferList();
                                                    //確認Out port是否處於Wait to Unload
                                                    switch (GetPortStatus(dtPortsInfo, drOut[0]["port_id"].ToString(), out tmpPortState))
                                                    {
                                                        case 0:
                                                        case 1:
                                                        case 4:
                                                        case 2:
                                                            break;
                                                        case 3:
                                                        default:
                                                            CarrierID = drIn is not null ? drIn.Length > 0 ? drIn[0]["CARRIER_ID"].ToString() : "" : "";

                                                            break;
                                                            //20240814 
                                                            lstTransfer = new TransferList();
                                                            lstTransfer.CommandType = "UNLOAD";
                                                            lstTransfer.Source = drRecord["PORT_ID"].ToString();
                                                            lstTransfer.Dest = drRecord["OUT_ERACK"].ToString();
                                                            lstTransfer.CarrierID = UnloadCarrierID.Equals("") ? "*" : UnloadCarrierID;
                                                            lstTransfer.Quantity = 0;
                                                            lstTransfer.CarrierType = dtLoadPortCarrier.Rows.Count > 0 ? dtLoadPortCarrier.Rows[0]["command_type"].ToString() : _CarrierTypebyPort;
                                                            break;
                                                    }

                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        { }

                                        break;
                                    case 4:
                                        //4. Empty (Ready to load)
                                        try
                                        {
                                            if (bStageIssue)
                                            {
                                                sql = _BaseDataService.LockLotInfoWhenReady(lotID);
                                                _dbTool.SQLExec(sql, out tmpMsg, true);
                                                if (_DebugMode)
                                                {
                                                    _logger.Debug(string.Format("[StageIssue] {0} / {1} Load command create faild.", drRecord["EQUIPID"].ToString(), lotID));
                                                }

                                                continue;
                                            }

                                            dtWorkInProcessSch = _dbTool.GetDataTable(_BaseDataService.SelectTableWorkInProcessSchByLotId(lotID, tableOrder));
                                            if (dtWorkInProcessSch.Rows.Count > 0)
                                            {
                                                if (_DebugMode)
                                                {
                                                    _logger.Debug(string.Format("[WorkInProcessSch] {0} / {1} The lotid has already exist WorkInProcessSch.", drRecord["EQUIPID"].ToString(), lotID));
                                                }

                                                continue;
                                            }

                                            if (_isFurnace)
                                            {
                                                if (!_furnState.Equals(1))
                                                    continue;
                                            }

                                            if (_isFurnace)
                                            {
                                                List<string> _lstTemp = new();
                                                string _sector = "";

                                                //QueryLocateBySector

                                                dtTemp = _dbTool.GetDataTable(_BaseDataService.QueryLocateBySector(ineRack));
                                                if (dtTemp.Rows.Count > 0)
                                                {
                                                    int _start = 0;
                                                    int _Len = 0;
                                                    int _exist = 0;
                                                    string _secTemp = "";
                                                    foreach (DataRow drSector in dtTemp.Rows)
                                                    {
                                                        _sector = drSector["sector"].ToString();
                                                        _sector = _sector.Replace("\",\"", "\"#\"");
                                                        _sector = _sector.Replace("\"", "").Replace("{", "").Replace("}", "");

                                                        _start = _sector.IndexOf(ineRack) + ineRack.Length + 1;
                                                        Console.WriteLine(_start);
                                                        _Len = _sector.IndexOf("#", _start);

                                                        Console.WriteLine(_Len);

                                                        if (_Len > 0)
                                                            _secTemp = _sector.Substring(_start, _Len - _start);
                                                        else
                                                        {
                                                            _secTemp = _sector.Substring(_start);

                                                            if (_secTemp.IndexOf("#") > 0)
                                                            {
                                                                _secTemp = _secTemp.Substring(0, _secTemp.IndexOf("#"));
                                                            }
                                                        }

                                                        //_sector = _secTemp;
                                                        ineRackID = drSector["erackID"].ToString();
                                                        _sector = _effectiveslot;

                                                        _lstTemp.Add(string.Format("{0}:{1}", ineRackID, _sector));
                                                    }
                                                }

                                                if(_lstTemp.Count > 0)
                                                    dtAvaileCarrier = GetAvailableCarrierForFVC(_dbTool, configuration, drRecord["CARRIER_TYPE"].ToString(), _lstTemp, true, _keyRTDEnv);
                                            }
                                            else
                                                dtAvaileCarrier = GetAvailableCarrierByLocate(_dbTool, configuration, drRecord["CARRIER_TYPE"].ToString(), "", ineRack, true, _keyRTDEnv, _eqpworkgroup);

                                            if (dtAvaileCarrier is null)
                                                continue;
                                            if (dtAvaileCarrier.Rows.Count <= 0)
                                            {
                                                if (_DebugMode)
                                                {
                                                    _logger.Debug(string.Format("[Availe Carrier] {0} / {1} No Available Carrier.", drRecord["EQUIPID"].ToString(), lotID));
                                                }

                                                continue;
                                            }

                                            if (_DebugMode)
                                            {
                                                _logger.Debug(string.Format("[GetAvailableCarrier] {0} / {1}", drRecord["EQUIPID"].ToString(), dtAvaileCarrier.Rows.Count));
                                            }

                                            //int iQty = 0; int iQuantity = 0; int iTotalQty = 0;
                                            iQty = 0; iQuantity = 0; iTotalQty = 0;
                                            int iCountOfCarr = 0;
                                            //bool isLastLot = false;
                                            isLastLot = false;

                                            if (dtAvaileCarrier.Rows.Count > 0)
                                            {
                                                if (_DebugMode)
                                                {
                                                    _logger.Debug(string.Format("[GetAvailableCarrier IN] {0} / {1}", drRecord["EQUIPID"].ToString(), dtAvaileCarrier.Rows.Count));
                                                }

                                                bIsMatch = false;
                                                string tmpMessage = "";
                                                try
                                                {
                                                    string _lotInCarrier = "";
                                                    string _schSeq = "";
                                                    string _qTime = "";
                                                    string _goNow = "";
                                                    string _lotAge = "";
                                                    string _layerPrio = "";
                                                    string _stageofLot = "";

                                                    foreach (DataRow draCarrier in dtAvaileCarrier.Rows)
                                                    {
                                                        iQty = 0; iTotalQty = 0; iQuantity = 0;

                                                        _schSeq = ""; _qTime = ""; _goNow = ""; _layerPrio = ""; _lotAge = ""; _stageofLot = ""; 

                                                        try
                                                        {
                                                            _schSeq = draCarrier["sch_seq"].ToString();
                                                            _qTime = draCarrier["qtime"].ToString();
                                                            _goNow = draCarrier["gonow"].ToString();
                                                            _lotAge = draCarrier["lot_age"].ToString();
                                                            _layerPrio = draCarrier["wpriority"].ToString(); 
                                                            //_schSeq, _qTime, _goNow, _layerPrio, _lotAge
                                                        }
                                                        catch (Exception ex) { }

                                                        try {
                                                            _lotInCarrier = draCarrier["lot_id"].ToString();
                                                            _carrierID = draCarrier["carrier_id"].ToString();

                                                            CarrierID = "";

                                                            if(_lotInCarrier.Equals(""))
                                                            {
                                                                if (_isFurnace)
                                                                {
                                                                    dtTemp = _dbTool.GetDataTable(_BaseDataService.CheckCarrierNumberforVFC(ineRackID, _effectiveslot));

                                                                    if (dtTemp.Rows.Count <= 1)
                                                                    {
                                                                        CarrierID = _carrierID;
                                                                        goto FurnaceDummyLot;
                                                                    }
                                                                    else
                                                                        continue;
                                                                }
                                                            }

                                                            if (CheckIsAvailableLot(_dbTool, _lotInCarrier, drRecord["EQUIPID"].ToString()))
                                                            {
                                                                CarrierID = _carrierID;

                                                                if (_DebugMode)
                                                                {
                                                                    //_logger.Debug(string.Format("[CheckIsAvailableLot] {0} / {1} / {2} / {3} / {4} / {5} / {6} / {7}", drRecord["EQUIPID"].ToString(), bIsMatch, _lotInCarrier, _schSeq, _qTime, _goNow, _layerPrio, _lotAge));
                                                                    //_logger.Info(string.Format("[CheckIsAvailableLot] {0} / {1} / {2}", drRecord["EQUIPID"].ToString(), CarrierID, _lotInCarrier));
                                                                }

                                                                bIsMatch = true;

                                                                try
                                                                {
                                                                    string _equiplist = "";
                                                                    //Show equip list of lotid
                                                                    sql = _BaseDataService.QueryEquipmentByLot(_lotInCarrier);
                                                                    dtTemp = _dbTool.GetDataTable(sql);

                                                                    if (dtTemp.Rows.Count > 0)
                                                                    {
                                                                        
                                                                        _equiplist = dtTemp.Rows[0]["equiplist"].ToString();
                                                                    }

                                                                    _logger.Debug(string.Format("[CheckIsAvailableLot] {0} / {1} / {2} / {3} / {4} / {5} / {6} / {7} / {8} / {9}", drRecord["EQUIPID"].ToString(), _eqpworkgroup, bIsMatch, _lotInCarrier, _schSeq, _qTime, _goNow, _layerPrio, _lotAge, _equiplist));
                                                                }
                                                                catch (Exception ex) { }

                                                                try
                                                                {
                                                                    sql = _BaseDataService.QueryLotinfoQuantity(_lotInCarrier);
                                                                    dtTemp = _dbTool.GetDataTable(sql);
                                                                    if (dtTemp.Rows.Count > 0)
                                                                    {
                                                                        _stageofLot = dtTemp.Rows[0]["STAGE"].ToString();
                                                                    }

                                                                    //GetSetofLookupTable
                                                                    sql = _BaseDataService.GetSetofLookupTable(_eqpworkgroup, _stageofLot);
                                                                    dtTemp = _dbTool.GetDataTable(sql);
                                                                    if (dtTemp.Rows.Count > 0)
                                                                    {
                                                                        bCheckEquipLookupTable = dtTemp.Rows[0]["CHECKEQPLOOKUPTABLE"] is null ? false : dtTemp.Rows[0]["CHECKEQPLOOKUPTABLE"].ToString().Equals("1") ? true : false;
                                                                        bCheckRcpConstraint = dtTemp.Rows[0]["RCPCONSTRAINT"] is null ? false : dtTemp.Rows[0]["RCPCONSTRAINT"].ToString().Equals("1") ? true : false;
                                                                        _stageController = dtTemp.Rows[0]["stagecontroller"] is null ? false : dtTemp.Rows[0]["stagecontroller"].ToString().Equals("1") ? true : false;
                                                                        //20240821 Add, 不同lot, stage不同, 設定檔要重新取
                                                                    }
                                                                }
                                                                catch (Exception ex) { }

                                                                if (_stageController)
                                                                {
                                                                    string tmpdic = "";

                                                                    try
                                                                    {
                                                                        foreach (var kvp in _dicStageCtrl)
                                                                        {
                                                                            if (tmpdic.Equals(""))
                                                                                tmpdic = string.Format("[key={0},value={1}]", kvp.Key, kvp.Value);
                                                                            else
                                                                                tmpdic = string.Format("{0}[key={1},value={2}]", tmpdic, kvp.Key, kvp.Value);
                                                                        }
                                                                    }catch(Exception ex) { }

                                                                    if (_dicStageCtrl.Count > 0)
                                                                    {
                                                                        if (_dicStageCtrl.ContainsKey(_stageofLot))
                                                                        {
                                                                            if (_dicStageCtrl.ContainsValue(drRecord["PORT_ID"].ToString()))
                                                                            {
                                                                                //存在, 上機

                                                                            }
                                                                            else
                                                                            {
                                                                                _logger.Debug(tmpdic);

                                                                                _logger.Debug(string.Format("[StageController] {0} / {1} / {2} / {3} / {4}", drRecord["EQUIPID"].ToString(), "The Port Not in dictionary of Stage Controller", _eqpworkgroup, _lotInCarrier, _stageofLot));
                                                                                //不存在
                                                                                bIsMatch = false;
                                                                                _lotInCarrier = "";
                                                                                CarrierID = "";
                                                                                _carrierID = "";
                                                                                goto ResetReserve;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            _logger.Debug(tmpdic);

                                                                            _logger.Debug(string.Format("[StageController] {0} / {1} / {2} / {3} / {4}", drRecord["EQUIPID"].ToString(), "The Stage Not in dictionary of Stage Controller", _eqpworkgroup, _lotInCarrier, _stageofLot));
                                                                            bIsMatch = false;
                                                                            _lotInCarrier = "";
                                                                            CarrierID = "";
                                                                            _carrierID = "";
                                                                            goto ResetReserve;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        _logger.Debug(tmpdic);

                                                                        _logger.Debug(string.Format("[StageController] {0} / {1} / {2} / {3} / {4}", drRecord["EQUIPID"].ToString(), "No Set Stage Controller", _eqpworkgroup, _lotInCarrier, _stageofLot));
                                                                        bIsMatch = false;
                                                                        _lotInCarrier = "";
                                                                        CarrierID = "";
                                                                        _carrierID = "";
                                                                        goto ResetReserve;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                bIsMatch = false;
                                                                _lotInCarrier = "";
                                                                CarrierID = "";
                                                                _carrierID = "";
                                                                goto ResetReserve;
                                                            }

                                                            if (bIsMatch)
                                                            {
                                                                _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, true), out tmpMessage, true);
                                                                //break;
                                                            }

                                                        } catch(Exception ex) { }
                                                        //Check Workgroup Set 
                                                        bNoFind = true;
                                                        try
                                                        {
                                                            _logger.Debug(string.Format("[In eRack]: {0} / {1}", drRecord["EQUIPID"].ToString(), ineRack));
                                                            sql = _BaseDataService.QueryRackByGroupID(ineRack);

                                                            dtTemp = _dbTool.GetDataTable(sql);
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            if (_DebugMode)
                                                            {
                                                                _logger.Debug(string.Format("[drRack Exception: {0} / {1}]", drRecord["EQUIPID"].ToString(), ex.Message));
                                                            }
                                                        }

                                                        if (dtTemp.Rows.Count > 0)
                                                        {
                                                            bNoFind = true;
                                                            try
                                                            {
                                                                foreach (DataRow drRack in dtTemp.Rows)
                                                                {
                                                                    if (!bNoFind)
                                                                        break;

                                                                    try
                                                                    {
                                                                        if (draCarrier["locate"].ToString().Equals(drRack["erackID"].ToString()))
                                                                        {
                                                                            bNoFind = false;
                                                                            if (_DebugMode)
                                                                            {
                                                                                _logger.Debug(string.Format("[AvailableCarrier ErackID] {0} / {1} / {2} / {3} _Out", drRecord["EQUIPID"].ToString(), drRack["erackID"].ToString(), draCarrier["locate"].ToString(), _carrierID));
                                                                            }
                                                                            //break;
                                                                            continue;
                                                                        }
                                                                        else
                                                                        {
                                                                            if (_DebugMode)
                                                                            {
                                                                                _logger.Debug(string.Format("[No Find] {0} / {1} / {2} _Out", drRecord["EQUIPID"].ToString(), draCarrier["locate"].ToString(), drRack["erackID"].ToString()));
                                                                            }
                                                                            continue;
                                                                            //"Position": "202312221900002"
                                                                            //goto ResetReserve;
                                                                        }

                                                                        if (_DebugMode)
                                                                        {
                                                                            _logger.Debug(string.Format("[drRack] {0} / {1} / {2}", drRecord["EQUIPID"].ToString(), draCarrier["locate"].ToString(), draCarrier["lot_id"].ToString()));
                                                                        }
                                                                    }
                                                                    catch (Exception ex)
                                                                    {
                                                                        if (_DebugMode)
                                                                        {
                                                                            _logger.Debug(string.Format("[No Find] [Exception:{0}]", ex.Message));
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                if (_DebugMode)
                                                                {
                                                                    _logger.Debug(string.Format("[No Find] [Exception:{0}]", ex.Message));
                                                                }
                                                            }
                                                        }

                                                        if (configuration["AppSettings:Work"].ToUpper().Equals("EWLB"))
                                                        {
                                                            try
                                                            {
                                                                //屬於Measurement & Thickness lot, 一定得由Lot ID決定Equipment
                                                                sql = _BaseDataService.CheckMeasurementAndThickness(configuration["MeasurementThickness:Table"], _lotInCarrier, vStage, drRecord["EQUIPID"].ToString());
                                                                dtTemp2 = _dbTool.GetDataTable(sql);

                                                                if (dtTemp2.Rows.Count > 0)
                                                                {
                                                                    _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}][{3}][{4}] _Out", "State4.1", _lotInCarrier, vStage, drRecord["EQUIPID"].ToString(), sql));
                                                                    //continue;
                                                                    goto ResetReserve;
                                                                }
                                                                else
                                                                {
                                                                    //Pass
                                                                    //_logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}][{3}][{4}]", "State4.2", draCarrier["lot_id"].ToString(), vStage, drRecord["EQUIPID"].ToString(), sql));
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}]", "State4.Ex", ex.Message));
                                                            }

                                                            if (bCheckEquipLookupTable)
                                                            {
                                                                try
                                                                {
                                                                    sql = _BaseDataService.CheckLookupTable(configuration["CheckEqpLookupTable:Table"], drRecord["EQUIPID"].ToString(), _lotInCarrier);
                                                                    dtTemp = _dbTool.GetDataTable(sql);

                                                                    if (dtTemp.Rows.Count <= 0)
                                                                    {
                                                                        _logger.Debug(string.Format("[{0}][Failed][{1}][{2}] _Out", "CheckEquipLookupTable", drRecord["EQUIPID"].ToString(), _lotInCarrier));
                                                                        //continue;
                                                                        goto ResetReserve;
                                                                    }
                                                                    else
                                                                    {
                                                                        _logger.Debug(string.Format("[{0}][Success][{1}][{2}][{3}]", "CheckEquipLookupTable", drRecord["EQUIPID"].ToString(), _lotInCarrier, dtTemp.Rows[0]["equip2"].ToString()));
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}] _Out", "Exception", "CheckEquipLookupTable", ex.Message));
                                                                    //continue;
                                                                    goto ResetReserve;
                                                                }
                                                            }

                                                            if (bCheckRcpConstraint)
                                                            {
                                                                try
                                                                {
                                                                    sql = _BaseDataService.CheckRcpConstraint(configuration["CheckEqpLookupTable:Table"], drRecord["EQUIPID"].ToString(), _lotInCarrier);
                                                                    dtTemp = _dbTool.GetDataTable(sql);

                                                                    if (dtTemp.Rows.Count > 0)
                                                                    {
                                                                        _logger.Debug(string.Format("[{0}][Failed][{1}][{2}][{3}] _Out", "CheckRcpConstraint", drRecord["EQUIPID"].ToString(), _lotInCarrier, dtTemp.Rows[0]["rcpconstraint_list"].ToString()));
                                                                        //continue;
                                                                        goto ResetReserve;
                                                                    }
                                                                    else
                                                                    {
                                                                        _logger.Debug(string.Format("[{0}][Success][{1}][{2}][{3}]", "CheckRcpConstraint", drRecord["EQUIPID"].ToString(), _lotInCarrier, "Success"));
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    _logger.Debug(string.Format("[CreateTransferCommandByPortModel][{0}][{1}][{2}] _Out", "Exception", "CheckRcpConstraint", ex.Message));
                                                                    //continue;
                                                                    goto ResetReserve;
                                                                }
                                                            }
                                                        }

                                                        if (_DebugMode)
                                                        {
                                                            _logger.Debug(string.Format("[Check Workgroup Set ] {0} / {1} _", drRecord["EQUIPID"].ToString(), dtTemp.Rows.Count.ToString()));
                                                        }

                                                        if (bNoFind)
                                                        {
                                                            //continue;
                                                            goto ResetReserve;
                                                        }

                                                        try
                                                        {
                                                            if (_QTimeMode.Equals(1))
                                                            {
                                                                try
                                                                {
                                                                    iLotQTime = 0;

                                                                    sql = _BaseDataService.GetQTimeLot(configuration["PreDispatchToErack:lotState:tableName"], _lotInCarrier);
                                                                    dtTemp = _dbTool.GetDataTable(sql);

                                                                    try
                                                                    {
                                                                        //iLotQTime = float.Parse(dtTemp.Rows[0]["QTime"].ToString());

                                                                        bTurnOnQTime = dtTemp.Rows[0]["QTIME"].ToString().Equals("NA") ? false : true;
                                                                        if (bTurnOnQTime)
                                                                            iLotQTime = float.Parse(dtTemp.Rows[0]["QTIME"].ToString());
                                                                        else
                                                                            iLotQTime = 0;
                                                                    }
                                                                    catch (Exception ex)
                                                                    {
                                                                        _logger.Debug(string.Format("[GetQTimeLot][Exception][{0}][{1}][{2}][{3}] _Out", _lotInCarrier, iLotQTime, _Equip, dtTemp.Rows[0]["QTime"].ToString()));
                                                                    }

                                                                    if (bTurnOnQTime)
                                                                    {
                                                                        bQTimeIssue = false;

                                                                        //Q-Time Logic for an lot
                                                                        try
                                                                        {
                                                                            try
                                                                            {
                                                                                sql = _BaseDataService.CheckCarrierTypeOfQTime(drRecord["EQUIPID"].ToString(), vStage, draCarrier["carrier_type"].ToString());
                                                                                dtTemp2 = _dbTool.GetDataTable(sql);

                                                                                if (dtTemp2.Rows.Count > 0)
                                                                                {
                                                                                    //"Position": "202312221900002"
                                                                                    iQTimeLow = 0;
                                                                                    iQTimeHigh = 6;
                                                                                }
                                                                                else
                                                                                {
                                                                                    _logger.Debug(string.Format("[Q-CheckCarrierTypeOfQTime][{0}][{1}][{2}][{3}]", drRecord["EQUIPID"].ToString(), vStage, draCarrier["carrier_type"].ToString(), _lotInCarrier));
                                                                                }
                                                                            }
                                                                            catch (Exception ex)
                                                                            {
                                                                                _logger.Debug(string.Format("[Q-CheckCarrierTypeOfQTime][Exception][{0}][{1}][{2}][{3}][{4}]", drRecord["EQUIPID"].ToString(), vStage, draCarrier["carrier_type"].ToString(), _lotInCarrier, ex.Message));
                                                                                //continue;
                                                                            }


                                                                            if (iLotQTime < iQTimeLow)
                                                                            {
                                                                                //lotID, _Equip, iLotQTime
                                                                                _logger.Debug(string.Format("[Q-Time is not enough][{0}][{1}][{2}][{3}] _Out", _lotInCarrier, iLotQTime, _Equip, iQTimeLow));
                                                                                bQTimeIssue = true;
                                                                                //continue;
                                                                                goto ResetReserve;
                                                                            }

                                                                            if (iLotQTime > iQTimeHigh)
                                                                            {
                                                                                _logger.Debug(string.Format("[Q-Time exceeds][{0}][{1}][{2}][{3}] _Out", _lotInCarrier, iLotQTime, _Equip, iQTimeHigh));
                                                                                bQTimeIssue = true;
                                                                                //continue;
                                                                                goto ResetReserve;
                                                                            }

                                                                        }
                                                                        catch (Exception ex)
                                                                        {
                                                                            //_logger.Debug(string.Format("[Q-CheckCarrierTypeOfQTime][Exception][{0}][{1}][{2}]", drRecord["EQUIPID"].ToString(), vStage, draCarrier["carrier_type"].ToString()));
                                                                            //continue;
                                                                        }
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    _logger.Debug(string.Format("[Q-Time][Exception][{0}][{1}][{2}][{3}]", drRecord["EQUIPID"].ToString(), vStage, _lotInCarrier, ex.Message));
                                                                    //continue;
                                                                    goto ResetReserve;
                                                                }
                                                            }
                                                            else if (_QTimeMode.Equals(2))
                                                            {
                                                                try
                                                                {
                                                                    iQTime = 0;
                                                                    iLotQTime = 0;
                                                                    iQTimeLow = 0;
                                                                    iQTimeHigh = 0;

                                                                    if (!_lotInCarrier.Equals(""))
                                                                    {
                                                                        try
                                                                        {
                                                                            iQTime = draCarrier["QTIME1"] is null ? 0 : float.Parse(draCarrier["QTIME1"].ToString());
                                                                            iLotQTime = draCarrier["QTIME"] is null ? 0 : float.Parse(draCarrier["QTIME"].ToString());
                                                                            iQTimeLow = draCarrier["minallowabletw"] is null ? 0 : float.Parse(draCarrier["minallowabletw"].ToString());
                                                                            iQTimeHigh = draCarrier["maxallowabletw"] is null ? 0 : float.Parse(draCarrier["maxallowabletw"].ToString());


                                                                            if (dtTemp2.Rows[0]["gonow"].ToString().Equals("Y"))
                                                                            {
                                                                                ///Q-Time 為Go Now, change lot priority 為80
                                                                                _dbTool.SQLExec(_BaseDataService.UpdatePriorityByLotid(_lotInCarrier, 80), out tmpMsg, true);
                                                                            }
                                                                        }
                                                                        catch (Exception ex) { }

                                                                        if (_qTimemode_enable)
                                                                        {
                                                                            if (iQTime < 0)
                                                                            {
                                                                                _logger.Debug(string.Format("[Q-Time is not enough][{0}][{1}][{2}][{3}][{4}][{5}]", _QTimeMode, _lotInCarrier, iLotQTime, _Equip, sPortID, iQTimeLow));
                                                                                goto ResetReserve;
                                                                            }

                                                                            if (iQTime > 1)
                                                                            {
                                                                                //Auto hold lot times for Qtime 
                                                                                int iholttimes = 0;
                                                                                sql = _BaseDataService.SelectTableLotInfoByLotid(lotID);
                                                                                dtTemp3 = _dbTool.GetDataTable(sql);
                                                                                if (dtTemp3.Rows.Count > 0)
                                                                                {
                                                                                    iholttimes = int.Parse(dtTemp3.Rows[0]["hold_times"].ToString());
                                                                                }

                                                                                if (iholttimes <= 0)
                                                                                {
                                                                                    _logger.Debug(string.Format("[Q-Time exceeds][{0}][{1}][{2}][{3}][{4}][{5}]", _QTimeMode, _lotInCarrier, iLotQTime, _Equip, drRecord["PORT_ID"].ToString(), iQTimeHigh));

                                                                                    try
                                                                                    {
                                                                                        _logger.Debug(string.Format("[Q-Time exceeds][{0}][{1}]", "Auto hold lot. ZZ, Q-Time failed.", _lotInCarrier));

                                                                                        jcetWebServiceClient = new JCETWebServicesClient();
                                                                                        jcetWebServiceClient._url = _qTime_url;
                                                                                        jcetWebServiceClient._urlUAT = _qTime_urlUAT;
                                                                                        string _args = "Q-Time failed,ZZ";
                                                                                        //public ResultMsg CustomizeEvent(string _func, string useMethod, bool isProduct, string equip, string username, string pwd, string lotid, string _args)
                                                                                        resultMsg = new JCETWebServicesClient.ResultMsg();
                                                                                        resultMsg = jcetWebServiceClient.CustomizeEvent("holdlot", "post", _qTime_isProduct, _Equip, _qTime_username, _qTime_pwd, _lotInCarrier, _args);
                                                                                        string result3 = resultMsg.retMessage;

                                                                                        sql = _BaseDataService.UadateHoldTimes(lotID);
                                                                                        _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                                    }
                                                                                    catch (Exception ex) { }
                                                                                }
                                                                                goto ResetReserve;
                                                                            }

                                                                            _logger.Debug(string.Format("[Q-Time pass][{0}][{1}][{2}][{3}][{4}]", _lotInCarrier, iLotQTime, iQTime, iQTimeHigh, iQTimeLow));
                                                                        }
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    _logger.Debug(string.Format("[Q-Time Logic][Exception][{0}][{1}][{2}][{3}][{4}]", drRecord["EQUIPID"].ToString(), vStage, drRecord["carrierType_M"].ToString(), _lotInCarrier, ex.Message));
                                                                }
                                                            }
                                                            else
                                                            {

                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            _logger.Debug(string.Format("[Q-Time Logic in draCarrier][{0}][{1}] Exception: {2}", _lotInCarrier, drRecord["EquipID"].ToString(), ex.Message));
                                                        }

                                                        try
                                                        {
                                                            //check lot recipe
                                                            if (bCheckRecipe)
                                                            {
                                                                _lotrecipe = "";

                                                                try
                                                                {
                                                                    _lotrecipe = draCarrier["custdevice"].ToString();

                                                                    dtRecipeSet = _dbTool.GetDataTable(_BaseDataService.QueryRecipeSetting(drRecord["EQUIPID"].ToString()));
                                                                    drRecipe = dtRecipeSet.Select(string.Format("recipeID='{0}'", _lotrecipe));

                                                                    if (drRecipe.Length > 0)
                                                                    {
                                                                        _lotrecipeGroup = drRecipe[0]["recipe_group"].ToString();
                                                                    }


                                                                    if (!_equiprecipeGroup.Equals(_lotrecipeGroup))
                                                                        continue;
                                                                }
                                                                catch (Exception ex)
                                                                { }
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        { }


                                                        //if (!draCarrier["locate"].ToString().Equals(dtWorkgroupSet.Rows[0]["in_erack"]))
                                                        //    continue;

                                                        iQuantity = draCarrier.Table.Columns.Contains("quantity") ? int.Parse(draCarrier["quantity"].ToString()) : 0;
                                                        //檢查Cassette Qty總和, 是否與Lot Info Qty相同, 相同才可派送 (滿足相同lot要放在相同機台上的需求)
                                                        string sqlSentence = _BaseDataService.CheckQtyforSameLotId(_lotInCarrier, drRecord["CARRIER_TYPE"].ToString());
                                                        DataTable dtSameLot = new DataTable();
                                                        dtSameLot = _dbTool.GetDataTable(sqlSentence);

                                                        if (_DebugMode)
                                                        {
                                                            _logger.Debug(string.Format("[CheckQtyforSameLotId] {0} / {1} / {2}", drRecord["EQUIPID"].ToString(), _lotInCarrier, drRecord["CARRIER_TYPE"].ToString()));
                                                        }

                                                        if (dtSameLot.Rows.Count > 0)
                                                        {
                                                            iQty = int.Parse(dtSameLot.Rows[0]["qty"].ToString());
                                                            iTotalQty = int.Parse(dtSameLot.Rows[0]["total_qty"].ToString());
                                                            iCountOfCarr = int.Parse(dtSameLot.Rows[0]["NumOfCarr"].ToString());

                                                            if (iCountOfCarr > 1)
                                                            {
                                                                if (iQty == iTotalQty)
                                                                { //To Do...
                                                                    isLastLot = false;
                                                                    sql = string.Format(_BaseDataService.LockMachineByLot(_lotInCarrier, iQuantity, 1));
                                                                    _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                    _logger.Debug(tmpMsg);
                                                                    tmpMsg = String.Format("=======IO==2==Load Carrier [{0}], Total Qty is {1}. Qty is {2}", _lotInCarrier, iTotalQty, iQty);
                                                                    _logger.Debug(tmpMsg);
                                                                }
                                                                else
                                                                {
                                                                    tmpMsg = String.Format("=======IO==1==Load Carrier, Total Qty is {0}. Qty is {1}", iTotalQty, iQty);
                                                                    _logger.Debug(tmpMsg);

                                                                    if (iQty <= iQuantity)
                                                                    {
                                                                        isLastLot = true;
                                                                        sql = string.Format(_BaseDataService.LockMachineByLot(_lotInCarrier, iQuantity, 0));
                                                                        _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                    }
                                                                    else
                                                                        isLastLot = false;

                                                                    if (iQty < iTotalQty)
                                                                        goto ResetReserve;//不搬運, 由unload 去發送(相同lot 需要由同一個port 執行)
                                                                    //continue;   

                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (iQty < iTotalQty)
                                                                {
                                                                    int _lockMachine = 0;
                                                                    int _compQty = 0;
                                                                    //draCarrier["lot_id"].ToString()
                                                                    sql = string.Format(_BaseDataService.SelectTableLotInfoByLotid(_lotInCarrier));
                                                                    dtTemp = _dbTool.GetDataTable(sql);
                                                                    _lockMachine = dtTemp.Rows[0]["lockmachine"].ToString().Equals("1") ? 1 : 0;
                                                                    _compQty = dtTemp.Rows[0]["comp_qty"].ToString().Equals("0") ? 0 : int.Parse(dtTemp.Rows[0]["comp_qty"].ToString());

                                                                    if (_lockMachine.Equals(0) && _compQty == 0)
                                                                    {
                                                                        isLastLot = false;
                                                                        sql = string.Format(_BaseDataService.LockMachineByLot(_lotInCarrier, iQuantity, 1));
                                                                        _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                    }
                                                                    else
                                                                    {
                                                                        if (_compQty + iQuantity >= iTotalQty)
                                                                        {
                                                                            isLastLot = true;
                                                                            sql = string.Format(_BaseDataService.LockMachineByLot(_lotInCarrier, _compQty + iQuantity, 0));
                                                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                        }
                                                                        else
                                                                            isLastLot = false;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    isLastLot = true;
                                                                    sql = string.Format(_BaseDataService.LockMachineByLot(_lotInCarrier, iQuantity, 0));
                                                                    _dbTool.SQLExec(sql, out tmpMsg, true);
                                                                }
                                                            }
                                                        }

                                                        if (bIsMatch)
                                                        {
                                                            _logger.Debug(string.Format("[HasMatchLogic] {0} / {1} / {2}", drRecord["EQUIPID"].ToString(), _lotInCarrier, CarrierID));
                                                            //if (!CarrierID.Equals(_lotInCarrier))
                                                            //{
                                                                //_dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, false), out tmpMessage, true);
                                                                //CarrierID = "";
                                                            //}
                                                            //_dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, true), out tmpMessage, true);
                                                            break;
                                                        }

                                                        if(!CarrierID.Equals(""))
                                                            _logger.Debug(string.Format("[Debug] {0} / {1} / {2}", drRecord["EQUIPID"].ToString(), _lotInCarrier, CarrierID));

                                                    ResetReserve:
                                                        tmpMessage = "";
                                                        if(!CarrierID.Equals(""))
                                                            _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, false), out tmpMessage, true);
                                                        CarrierID = "";
                                                        _carrierID = "";
                                                        _lotInCarrier = "";
                                                        bIsMatch = false;
                                                        //continue;
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    if (_DebugMode)
                                                    {
                                                        _logger.Debug(string.Format("[Exception:] {0} / {1}", drRecord["EQUIPID"].ToString(), ex.Message));
                                                    }
                                                }

                                                if (bQTimeIssue)
                                                    break;

                                                if (!bIsMatch)
                                                    break;

                                                if (!tmpMessage.Equals(""))
                                                {
                                                    if (_DebugMode)
                                                    {
                                                        _logger.Debug(string.Format("[tmpMessage] {0} / {1} _Out", drRecord["EQUIPID"].ToString(), tmpMessage));
                                                    }

                                                    if (!CarrierID.Equals(""))
                                                        _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(CarrierID, false), out tmpMessage, true);
                                                    continue;
                                                }

                                                FurnaceDummyLot:

                                                drCarrierData = dtAvaileCarrier.Select("carrier_id = '" + CarrierID + "'");
                                            }

                                            lstTransfer = new TransferList();
                                            lstTransfer.CommandType = "LOAD";
                                            lstTransfer.Source = "*";
                                            lstTransfer.Dest = drRecord["PORT_ID"].ToString();
                                            lstTransfer.CarrierID = CarrierID;
                                            lstTransfer.Quantity = drCarrierData[0]["QUANTITY"].ToString().Equals("") ? 0 : int.Parse(drCarrierData[0]["QUANTITY"].ToString());
                                            lstTransfer.CarrierType = drCarrierData[0]["command_type"].ToString();
                                            lstTransfer.Total = iTotalQty;
                                            lstTransfer.IsLastLot = isLastLot ? 1 : 0;
                                        }
                                        catch (Exception ex)
                                        {
                                            if (_DebugMode)
                                            {
                                                _logger.Debug(string.Format("[Exception:  {0} / {1} ][{2}]", drRecord["EQUIPID"].ToString(), iPortState, ex.Message));
                                            }
                                        }
                                        break;
                                    case 0:
                                    default:
                                        continue;
                                        break;
                                }
                                break;
                            }
                            catch (Exception ex)
                            { }
                            break;
                    }

                    if (lstTransfer is not null)
                    {

                        //將搬送指令加入Transfer LIst
                        if (lstTransfer.CarrierID is not null)
                        {

                            normalTransfer.Transfer.Add(lstTransfer);
                            iReplace++;
                        }
                    }

                    if (normalTransfer.Transfer.Count > 0)
                    {
                        if (!_portModel.Equals("1I1OT2"))
                            break;
                    }
                }
                normalTransfer.Replace = iReplace > 0 ? iReplace - 1 : 0;

                ////////////////output normalTransfer

                //由Carrier Type找出符合的Carrier 


                //產生派送指令識別碼
                //U + 2022081502 + 00001
                //將所產生的指令加入WorkInProcess_Sch
                bool single = true;
                DataTable dtExist = new DataTable();
                int iTransferNo = 0;

                tmpMsg = "";
                if (normalTransfer.Transfer is not null)
                {
                    if (_DebugMode)
                    {
                        _logger.Debug(string.Format("[{0}]----normalTransfer.Transfer.Count is {1}", normalTransfer.EquipmentID, normalTransfer.Transfer.Count));
                    }

                    if (normalTransfer.Transfer.Count > 0)
                    {
                        string tmpCarrierid = "";
                        int eqp_priority = 20;
                        List<string> lstEquips = new List<string>();

                        if (normalTransfer.Transfer.Count > 1)
                        { single = false; }

                        if (!single)
                            normalTransfer.CommandID = Tools.GetCommandID(_dbTool);

                        SchemaWorkInProcessSch workinProcessSch = new SchemaWorkInProcessSch();
                        workinProcessSch.UUID = Tools.GetUnitID(_dbTool);
                        workinProcessSch.Cmd_Id = normalTransfer.CommandID;
                        workinProcessSch.Cmd_State = "";    //派送前為NULL
                        workinProcessSch.EquipId = normalTransfer.EquipmentID;
                        workinProcessSch.Cmd_Current_State = "";    //派送前為NULL

                        try
                        {
                            //"Position": "202310171805001"
                            dtTemp = _dbTool.GetDataTable(_BaseDataService.SelectWorkgroupSet(normalTransfer.EquipmentID));

                            if (dtTemp.Rows.Count > 0)
                            {
                                eqp_priority = dtTemp.Rows[0]["prio"] is null ? 20 : dtTemp.Rows[0]["prio"].ToString().Equals("0") ? 20 : int.Parse(dtTemp.Rows[0]["prio"].ToString());
                            }
                            else
                            {
                                eqp_priority = 30;
                            }

                            _logger.Debug(string.Format("Get Equip[{0}] Workgroup priority is [{1}].", normalTransfer.EquipmentID, eqp_priority));
                        }
                        catch (Exception ex)
                        {
                            eqp_priority = 30;
                            _logger.Debug(string.Format("Exception: Equip[{0}], Message:[{1}].", normalTransfer.EquipmentID, ex.Message));
                        }

                        //workinProcessSch.Priority = 10;             //預設優先權為10
                        workinProcessSch.Priority = _priority.Equals(0) ? eqp_priority : _priority;             //預設優先權為10

                        if (single)
                            workinProcessSch.Replace = 0;
                        else
                        {
                            if (_portModel.Equals("2I2OT1"))
                            {
                                //workinProcessSch.Replace = normalTransfer.Transfer.Count > 1 ? normalTransfer.Transfer.Count - 1 : 1;
                                workinProcessSch.Replace = 0;
                                single = true;
                            }
                            else
                            {
                                workinProcessSch.Replace = 0;
                                string tmpLoad = "";
                                string tmpUnload = "";
                                foreach (TransferList trans in normalTransfer.Transfer)
                                {
                                    if (trans.CommandType.Equals("LOAD"))
                                    {
                                        tmpLoad = trans.Dest;
                                    }
                                    else if (trans.CommandType.Equals("UNLOAD"))
                                    {
                                        tmpUnload = trans.Source;
                                    }

                                    if (tmpLoad.Equals(tmpUnload))
                                    {
                                        lstEquips.Add(tmpLoad);
                                        tmpLoad = "";
                                        tmpUnload = "";
                                    }
                                }
                            }
                        }

                        bool isLoad = false;
                        bool isSwap = false;
                        string _lastDest = "";
                        int idxTrans = 0;
                        string _tmplotid = "";
                        DateTime dtStart = DateTime.Now;
                        foreach (TransferList trans in normalTransfer.Transfer)
                        {
                            //檢查Dest是否已存在於WorkinprocessSch裡, 存在不能再送, 
                            dtExist = new DataTable();
                            tmpMsg = "";

                            try {
                                tmpMsg = string.Format("NormalTransfer Command Type:[{0}], Command Source: {1}, Dest: {2}, CarrierID: {3}", trans.CommandType, trans.Source, trans.Dest, trans.CarrierID);
                                logger.Debug(tmpMsg);
                            }
                            catch(Exception ex) { }

                            if (trans.CommandType.Equals("LOAD"))
                            {
                                isLoad = true;

                                if (lstEquips.Exists(e => e.EndsWith(trans.Dest)))
                                {
                                    workinProcessSch.Replace = 1;
                                    isSwap = true;
                                }

                                _lastDest = trans.Dest;

                                dtExist = _dbTool.GetDataTable(_BaseDataService.SelectTableWorkInProcessSchByPortId(trans.Dest, tableOrder));
                                if (dtExist.Rows.Count > 0)
                                {
                                    logger.Debug(string.Format("Duplicate Command Type:[{0}], Command Source: {1}, Dest: {2}", trans.CommandType, trans.Source, trans.Dest));
                                    continue;   //目的地已有Carrier, 跳過
                                }
                                tmpCarrierid = trans.CarrierID.Equals("") ? "*" : trans.CarrierID;

                                //check carrier
                                dtExist = _dbTool.GetDataTable(_BaseDataService.SelectTableWorkInProcessSchByCarrier(tmpCarrierid, tableOrder));
                                if (dtExist.Rows.Count > 0)
                                {
                                    logger.Debug(string.Format("Duplicate Carrier [{0}], command [{1}] been auto cancel. _", tmpCarrierid, normalTransfer.CommandID));
                                    if (dtExist.Rows[0]["cmd_state"].ToString().Equals("Init"))
                                    {
                                        //UpdateTableReserveCarrier
                                        _dbTool.SQLExec(_BaseDataService.UpdateTableReserveCarrier(tmpCarrierid, true), out tmpMsg, true);
                                        continue;   //目的地已有Carrier, 跳過
                                    }
                                    if (dtExist.Rows[0]["cmd_state"].ToString().Equals("Initial"))
                                    {
                                        //DeleteWorkInProcessSchByCmdId
                                        APIResult apiResult = new APIResult();

                                        apiResult = SentAbortOrCancelCommandtoMCS(configuration, _logger, 1, dtExist.Rows[0]["cmd_id"].ToString());
                                        if (apiResult.Success)
                                        {
                                            _logger.Debug(string.Format("[Do Cancel][Success][{0}][{1}]", "Duplicate Carrier", apiResult.ErrorCode));
                                        }
                                        else
                                        {
                                            _logger.Debug(string.Format("[Do Cancel][Failed][{0}][{1}]", "Duplicate Carrier", apiResult.ErrorCode));
                                        }

                                        Thread.Sleep(3000);
                                        _dbTool.SQLExec(_BaseDataService.DeleteWorkInProcessSchByCmdId(dtExist.Rows[0]["cmd_id"].ToString(), tableOrder), out tmpMsg, true);

                                        continue;
                                    }
                                    if (dtExist.Rows[0]["cmd_state"].ToString().Equals("Running"))
                                    {
                                        //The Carrier state is running. cann't create new commands
                                        continue;   //目的地已有Carrier, 跳過
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                            if (trans.CommandType.Equals("UNLOAD"))
                            {
                                string _unloadCmdType = "";
                                isLoad = false;

                                tmpCarrierid = trans.CarrierID.Equals("") ? "*" : trans.CarrierID;

                                if (lstEquips.Exists(e => e.EndsWith(trans.Source)))
                                {
                                    workinProcessSch.Replace = 1;
                                    isSwap = true;
                                }

                                iTransferNo++;
                                tmpCarrierid = trans.CarrierID.Equals("") ? "*" : trans.CarrierID;
                                trans.LotID = normalTransfer.LotID;

                                /** 如果Command Type is Unload, Carrier ID is *, 暫停3秒, 防止重複產生Unload 指令*/
                                if (tmpCarrierid.Equals("*"))
                                    System.Threading.Thread.Sleep(3000);

                                dtExist = _dbTool.GetDataTable(_BaseDataService.QueryWorkInProcessSchByPortIdForUnload(trans.Source, tableOrder));
                                if (dtExist.Rows.Count > 0)
                                {
                                    if (iTransferNo > 1)
                                        continue;
                                    ////同一次迴騞裡, 不允許產生二筆command 

                                    logger.Debug(string.Format("Duplicate Command Type:[{0}], Command Source: {1}, Dest: {2} _Out", trans.CommandType, trans.Source, trans.Dest));
                                    if (dtExist.Rows[0]["cmd_state"].ToString().Equals("Init"))
                                    {
                                        continue;   //目的地已有Carrier, 跳過
                                    }
                                    if (dtExist.Rows[0]["cmd_state"].ToString().Equals("Initial"))
                                    {
                                        continue;   //目的地已有Carrier, 跳過
                                        //DeleteWorkInProcessSchByCmdId
                                        APIResult apiResult = new APIResult();

                                        apiResult = SentAbortOrCancelCommandtoMCS(configuration, _logger, 1, dtExist.Rows[0]["cmd_id"].ToString());
                                        if (apiResult.Success)
                                        {
                                            _logger.Debug(string.Format("[Do Cancel][Success][{0}][{1}]", "Duplicate Source", apiResult.ErrorCode));
                                        }
                                        else
                                        {
                                            _logger.Debug(string.Format("[Do Cancel][Failed][{0}][{1}]", "Duplicate Source", apiResult.ErrorCode));
                                        }

                                        Thread.Sleep(3000);
                                        _dbTool.SQLExec(_BaseDataService.DeleteWorkInProcessSchByCmdId(dtExist.Rows[0]["cmd_id"].ToString(), tableOrder), out tmpMsg, true);
                                    }
                                    if (dtExist.Rows[0]["cmd_state"].ToString().Equals("Running"))
                                    {
                                        //The Carrier state is running. cann't create new commands
                                        continue;   //已有Command在執行, 直接跳過
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                            
                            if(tmpCarrierid.Equals("*"))
                            {
                                //Pre-Transfer
                                if (trans.CommandType.ToUpper().Equals("PRE-TRANSFER"))
                                {
                                    workinProcessSch.Priority = 30;
                                }
                            }
                            else
                            {
                                int iPriority = 0;
                                sql = _BaseDataService.QueryLotInfoByCarrierID(tmpCarrierid);
                                dtTemp = _dbTool.GetDataTable(sql);

                                if (dtTemp.Rows.Count > 0)
                                {
                                    iPriority = dtTemp.Rows[0]["PRIORITY"] is null ? 0 : int.Parse(dtTemp.Rows[0]["PRIORITY"].ToString());
                                    tmpMsg = string.Format("the carrier id [{0}] current priority is [{1}]", tmpCarrierid, iPriority);
                                    _logger.Debug(tmpMsg);
                                }

                                if (iPriority >= 70)
                                {
                                    workinProcessSch.Priority = iPriority;
                                }
                                else
                                {
                                    if (trans.CommandType.ToUpper().Equals("PRE-TRANSFER"))
                                    {
                                        if (iPriority > 20)
                                        {
                                            workinProcessSch.Priority = iPriority;
                                        }
                                        else
                                        {
                                            workinProcessSch.Priority = 30;
                                        }
                                    }
                                    else
                                    {
                                        workinProcessSch.Priority = eqp_priority;
                                    }
                                }
                            }

                            if (single)
                                workinProcessSch.Cmd_Id = Tools.GetCommandID(_dbTool);
                            else
                            {

                                workinProcessSch.Cmd_Type = trans.CommandType.Equals("") ? "TRANS" : trans.CommandType;

                                if (_portModel.Equals("2I2OT1"))
                                {
                                    if (isLoad)
                                    {
                                        if (isSwap)
                                            workinProcessSch.Cmd_Id = "";
                                    }
                                    else
                                    {
                                        if (isSwap)
                                        {
                                            //workinProcessSch.Cmd_Id = "";
                                            workinProcessSch.UUID = Tools.GetUnitID(_dbTool);
                                        }
                                        //workinProcessSch.Cmd_Id = Tools.GetCommandID(_dbTool);
                                    }
                                }
                                else
                                {
                                    if (_DebugMode)
                                    {
                                        _logger.Debug(string.Format("----check commands type {0} ", trans.CommandType));
                                    }
                                    if (isSwap)
                                    {

                                        //20240606 DS swap command loadport 1/loadport 2 command id diff
                                        if (trans.CommandType.Equals("LOAD"))
                                        {
                                            if (idxTrans <= 0)
                                            {
                                                workinProcessSch.Cmd_Id = "";
                                            }
                                            if (idxTrans > 1)
                                            {
                                                workinProcessSch.Cmd_Id = "";
                                            }
                                            idxTrans++;
                                        }
                                        workinProcessSch.UUID = "";
                                        if (trans.CommandType.Equals("UNLOAD"))
                                            idxTrans++;
                                    }
                                    else
                                    {
                                        workinProcessSch.Cmd_Id = "";
                                        workinProcessSch.UUID = "";
                                    }
                                }
                            }

                            tmpMsg = "";
                            //workinProcessSch.UUID = Tools.GetUnitID(_dbTool);  //變更為GID使用, 可查詢同一批派出的指令
                            workinProcessSch.Cmd_Type = trans.CommandType.Equals("") ? "TRANS" : trans.CommandType;
                            workinProcessSch.CarrierId = tmpCarrierid;
                            workinProcessSch.CarrierType = trans.CarrierType.Equals("") ? "" : trans.CarrierType;
                            workinProcessSch.Source = trans.Source.Equals("*") ? "*" : trans.Source;
                            workinProcessSch.Dest = trans.Dest.Equals("") ? "*" : trans.Dest;
                            workinProcessSch.Quantity = trans.Quantity;
                            workinProcessSch.Total = trans.Total;
                            workinProcessSch.IsLastLot = trans.IsLastLot;
                            workinProcessSch.Back = "*";

                            DataTable dtInfo = new DataTable { };
                            if (trans.CarrierID is not null)
                            {
                                if (!trans.CarrierID.Equals("*") || trans.CarrierID.Trim().Equals(""))
                                    dtInfo = _dbTool.GetDataTable(_BaseDataService.QueryLotInfoByCarrierID(trans.CarrierID));
                                else
                                {
                                    if (!trans.LotID.Trim().Equals(""))
                                    {
                                        dtTemp = _dbTool.GetDataTable(_BaseDataService.SelectTableCarrierAssociate3ByLotid(trans.LotID));
                                        if (dtTemp.Rows.Count > 0)
                                        {
                                            workinProcessSch.CarrierId = dtTemp.Rows[0]["carrier_id"].ToString();
                                            dtInfo = _dbTool.GetDataTable(_BaseDataService.QueryLotInfoByCarrierID(workinProcessSch.CarrierId));
                                        }

                                        sql = _BaseDataService.QueryCommandsTypeByCarrierID(workinProcessSch.CarrierId);
                                        dtTemp = _dbTool.GetDataTable(sql);

                                        if (dtTemp.Rows.Count > 0)
                                        {
                                            if (!workinProcessSch.CarrierType.Equals(dtTemp.Rows[0]["command_type"].ToString()))
                                                workinProcessSch.CarrierType = dtTemp.Rows[0]["command_type"].ToString();
                                        }
                                    }
                                }
                            }

                            if (dtInfo is not null)
                            {
                                if (dtInfo.Rows.Count > 0)
                                {
                                    workinProcessSch.LotID = dtInfo.Rows.Count <= 0 ? " " : dtInfo.Rows[0]["lotid"].ToString();
                                    workinProcessSch.Customer = dtInfo.Rows.Count <= 0 ? " " : dtInfo.Rows[0]["customername"].ToString();

                                    if (int.Parse(dtInfo.Rows[0]["priority"].ToString()) > 70)
                                    {
                                        workinProcessSch.Priority = int.Parse(dtInfo.Rows[0]["priority"].ToString());
                                    }
                                }
                            }
                            else
                            {
                                workinProcessSch.LotID = "";
                                workinProcessSch.Customer = "";
                            }

                            if (_DebugMode)
                            {
                                _logger.Debug(string.Format("Insert Logic: "));
                            }

                            if (workinProcessSch.Cmd_Id.Equals(""))
                                workinProcessSch.Cmd_Id = Tools.GetCommandID(_dbTool);

                            if (workinProcessSch.UUID.Equals(""))
                                workinProcessSch.UUID = Tools.GetUnitID(_dbTool);

                            sql = _BaseDataService.InsertTableWorkinprocess_Sch(workinProcessSch, tableOrder);

                            if (_DebugMode)
                            {
                                _logger.Debug(string.Format("----do insert sql [{0}] ", sql));
                            }

                            if (_dbTool.SQLExec(sql, out tmpMsg, true))
                            {
                                if (tmpMsg.Equals(""))
                                {
                                    sql = _BaseDataService.UpdateTableWorkInProcessSchHisByCmdId(workinProcessSch.Cmd_Id);
                                    _dbTool.SQLExec(sql, out tmpMsg, true);

                                    string _portID = "";
                                    if (workinProcessSch.Cmd_Type.Equals("LOAD"))
                                        _portID = workinProcessSch.Dest;
                                    if (workinProcessSch.Cmd_Type.Equals("UNLOAD"))
                                        _portID = workinProcessSch.Source;

                                    if (!_portID.Equals(""))
                                        _dbTool.SQLExec(_BaseDataService.LockEquipPortByPortId(_portID, true), out tmpMsg, true);

                                    if (tmpMsg.Equals(""))
                                    {
                                        _logger.Debug(string.Format("----Port [{0}] has been Lock.", _portID));
                                    }
                                    else
                                        _logger.Debug(string.Format("----Port [{0}] lock faile. Message:[{1}]", _portID, tmpMsg));
                                }
                            }

                            if (!tmpMsg.Equals(""))
                            {
                                _logger.Debug(string.Format("--Error. Insert Workinprocess_Sch error. [command ID {0}, command Type {1}, source {2}, error:{3}].", workinProcessSch.Cmd_Id, workinProcessSch.Cmd_Type, workinProcessSch.Source, tmpMsg));
                            }

                            normalTransfer.CommandID = workinProcessSch.Cmd_Id;

                            if (!isSwap)
                            {
                                workinProcessSch.Cmd_Id = "";
                                workinProcessSch.UUID = "";
                            }
                        }

                        _arrayOfCmds.Add(normalTransfer.CommandID);
                    }

                }
                ////加入_arrayOfCmds
                //if (normalTransfer.Transfer.Count <= 0)
                //    OracleSequence.BackOne(_dbTool, "command_streamCode");
                //else 
                //    _arrayOfCmds.Add(normalTransfer.CommandID);

                ///Release Equipment
                _dbTool.SQLExec(_BaseDataService.LockEquip(_Equip, false), out tmpMsg, true);
            }
            catch (Exception ex)
            {
                //Do Nothing
                logger.Debug(string.Format("CreateTransferCommandByPortModel [Exception]: {0}", ex.Message));

                _dbTool.SQLExec(_BaseDataService.LockEquip(_Equip, false), out tmpMsg, true);
            }
            finally
            {
                //Do Nothing
            }

            return result;
        }
        public bool CreateTransferCommandByTransferList(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, TransferList _transferList, out List<string> _arrayOfCmds)
        {
            bool result = false;
            string tmpMsg = "";
            _arrayOfCmds = new List<string>();
            DataTable dtTemp = new DataTable();
            string tableOrder = "";
            string _keyOfEnv = "";
            string _keyRTDEnv = "";
            string _keyCmd = "";

            try
            {
                _keyRTDEnv = _configuration["RTDEnvironment:type"];
                _keyOfEnv = string.Format("RTDEnvironment:commandsTable:{0}", _keyRTDEnv);
                //"RTDEnvironment:commandsTable:PROD"
                tableOrder = _configuration[_keyOfEnv] is null ? "workinprocess_sch" : _configuration[_keyOfEnv];

                string sql = "";
                string lotID = "";
                string CarrierID = "";

                //上機失敗皆回(in erack)
                NormalTransferModel normalTransfer = new NormalTransferModel();
                TransferList lstTransfer = new TransferList();
                normalTransfer.EquipmentID = "";
                normalTransfer.PortModel = "";
                normalTransfer.Transfer = new List<TransferList>();
                normalTransfer.LotID = lotID;

                CarrierID = _transferList.CarrierID;

                lstTransfer = new TransferList();
                lstTransfer.CommandType = _transferList.CommandType.Equals("") ? "DIRECT-TRANS" : _transferList.CommandType;
                lstTransfer.Source = _transferList.Source.Equals("") ? "*" : _transferList.Source;
                lstTransfer.Dest = _transferList.Dest.Equals("") ? "*" : _transferList.Dest;
                lstTransfer.CarrierID = CarrierID;
                lstTransfer.Quantity = _transferList.Quantity;
                lstTransfer.CarrierType = _transferList.CarrierType;

                normalTransfer.Transfer.Add(lstTransfer);

                normalTransfer.Replace = 1;

                //產生派送指令識別碼
                //U + 2022081502 + 00001
                //將所產生的指令加入WorkInProcess_Sch
                tmpMsg = "";
                if (normalTransfer.Transfer is not null)
                {
                    _keyCmd = Tools.GetCommandID(_dbTool);
                    if(lstTransfer.CommandType.Equals("MANUAL-DIRECT"))
                    {
                        normalTransfer.CommandID = _keyCmd.Insert(12, "M");
                    }
                    else
                    {
                        normalTransfer.CommandID = _keyCmd.Insert(12, "P");
                    }

                    SchemaWorkInProcessSch workinProcessSch = new SchemaWorkInProcessSch();
                    workinProcessSch.Cmd_Id = normalTransfer.CommandID;
                    workinProcessSch.Cmd_State = "";    //派送前為NULL
                    workinProcessSch.EquipId = normalTransfer.EquipmentID;
                    workinProcessSch.Cmd_Current_State = "";    //派送前為NULL
                    workinProcessSch.Priority = 10;             //預設優先權為10
                    workinProcessSch.Replace = normalTransfer.Transfer.Count > 0 ? normalTransfer.Transfer.Count - 1 : 0;

                    foreach (TransferList trans in normalTransfer.Transfer)
                    {
                        workinProcessSch.CarrierId = trans.CarrierID.Equals("") ? "*" : trans.CarrierID;
                        if (!workinProcessSch.CarrierId.Equals("*")) {
                            dtTemp = _dbTool.GetDataTable(_BaseDataService.SelectTableWorkInProcessSchByCarrier(trans.CarrierID, tableOrder));

                            if (dtTemp.Rows.Count > 0)
                                continue;
                        }

                        tmpMsg = "";
                        workinProcessSch.UUID = Tools.GetUnitID(_dbTool);
                        workinProcessSch.Cmd_Type = trans.CommandType.Equals("") ? "DIRECT-TRANS" : trans.CommandType;
                        workinProcessSch.CarrierType = trans.CarrierType.Equals("") ? "" : trans.CarrierType;
                        workinProcessSch.Source = trans.Source.Equals("*") ? "*" : trans.Source;
                        workinProcessSch.Dest = trans.Dest.Equals("*") ? "*" : trans.Dest;
                        workinProcessSch.Back = "*";

                        DataTable dtInfo = new DataTable { };
                        if (trans.CarrierID is not null)
                        {
                            dtInfo = _dbTool.GetDataTable(_BaseDataService.QueryLotInfoByCarrierID(trans.CarrierID));
                        }
                        workinProcessSch.LotID = dtInfo.Rows.Count <= 0 ? "" : dtInfo.Rows[0]["lotid"].ToString();
                        workinProcessSch.Customer = dtInfo.Rows.Count <= 0 ? "" : dtInfo.Rows[0]["customername"].ToString();

                        sql = _BaseDataService.InsertTableWorkinprocess_Sch(workinProcessSch, tableOrder);
                        if (_dbTool.SQLExec(sql, out tmpMsg, true))
                        { }

                    }
                }
                //加入_arrayOfCmds
                if (normalTransfer.Transfer.Count <= 0)
                    OracleSequence.BackOne(_dbTool, "command_streamCode");
                else
                { result = true; _arrayOfCmds.Add(normalTransfer.CommandID); }
            }
            catch (Exception ex)
            {
                //Do Nothing
            }
            finally
            {
                if (dtTemp != null)
                    dtTemp.Dispose(); 
                //Do Nothing
            }
            dtTemp = null;

            return result;
        }
        //Port Status=========================
        //0. None, 1. Init, 2. Swap, 3. Unload.
        //====================================
        public int CheckPortStatus(DataTable _dt, string _portModel)
        {
            int iState = 0;
            //0. None, 1. Init, 2. Swap, 3. Unload.

            DataRow[] dr1 = null;
            DataRow[] dr2 = null;

            string sql = "";
            //Port Type:
            //0. Out of Service
            //1. Transfer Blocked
            //2. Near Completion
            //3. Ready to Unload
            //4. Empty (Ready to load)
            //5. Reject and Ready to unload
            //6. Port Alarm
            try
            {
                if (int.Parse(_dt.Rows[0]["Port_Number"].ToString()) > 0)
                {
                    if (int.Parse(_dt.Rows[0]["Port_Number"].ToString()).Equals(1))
                    {
                        dr1 = _dt.Select("Port_Type='IO'");

                        if (dr1.Length > 0)
                        {
                            if (dr1[0]["Port_State"].Equals(4))
                            {   //Waiting to load ---Init

                                iState = 1;
                            }
                            else if (dr1[0]["Port_State"].Equals(3))
                            {   //Waiting to unload  --Swap, Unload

                                //如果有PM或關機
                                if (true)
                                {   //PM
                                    iState = 3; //Unload
                                }
                                else
                                {   //None PM
                                    iState = 2; //Swap
                                }
                            }
                            else
                            {   //None State is 0
                            }
                        }
                    }
                    else
                    {
                        int iRun = 0;
                        int iResult = 0;
                        while (iRun < int.Parse(_dt.Rows[0]["Port_Number"].ToString()))
                        {
                            switch (_dt.Rows[iRun]["Port_Type"].ToString())
                            {
                                case "IN":
                                    if (int.Parse(_dt.Rows[0]["Port_State"].ToString()).Equals(3))
                                    {
                                        iResult += 1;
                                    }
                                    break;
                                case "OUT":
                                    if (int.Parse(_dt.Rows[0]["Port_State"].ToString()).Equals(3))
                                    {
                                        iResult += 10;
                                    }
                                    break;
                                default:
                                    break;
                            }

                            iRun++;
                        }

                        switch (_portModel)
                        {
                            case "1I1OT1":
                            case "1I1OT2":
                                if (iResult == 0)
                                {   //Unload or Swap
                                    iState = 3;
                                }
                                else if (iResult == 11)
                                {   //Load
                                    iState = 4;
                                }
                                else
                                {   //None iState = 0
                                }
                                break;
                            case "2I2OT1":
                                if (iResult == 0)
                                {   //Unload or Swap
                                    iState = 3;
                                }
                                else if (iResult == 11)
                                {   //Load
                                    iState = 4;
                                }
                                else
                                {   //None iState = 0
                                }
                                break;
                            case "1IOT1":
                                // None
                                break;
                            default:
                                int iSample = 11 * int.Parse(_dt.Rows[0]["Port_Number"].ToString());

                                if (iResult == 0)
                                {   //Unload or Swap
                                    iState = 3;
                                }
                                else if (iResult == iSample)
                                {   //Load
                                    iState = 4;
                                }
                                else
                                {   //None iState = 0
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            { }
            
            dr1 = null;
            dr2 = null;

            return iState;
        }
        //Port Type===========================
        //Port Type:
        //0. Out of Service
        //1. Transfer Blocked
        //2. Near Completion
        //3. Ready to Unload
        //4. Empty (Ready to load)
        //5. Reject and Ready to unload
        //6. Port Alarm
        //9. Unknow
        //====================================
        public int GetPortStatus(DataTable _dt, string _portID, out string _portDesc)
        {
            DataRow[] dr1 = null;

            int iState = 0;
            _portDesc = "";
            //Port Type:
            //0. Out of Service
            //1. Transfer Blocked
            //2. Near Completion
            //3. Ready to Unload
            //4. Empty (Ready to load)
            //5. Reject and Ready to unload
            //6. Port Alarm
            try
            {
                dr1 = _dt.Select("Port_ID = '" + _portID + "'");
                switch (int.Parse(dr1[0]["Port_State"].ToString()))
                {
                    case 0:
                        iState = 0;
                        _portDesc = "Out of Service";
                        break;
                    case 1:
                        iState = 1;
                        _portDesc = "Transfer Blocked";
                        break;
                    case 2:
                        iState = 2;
                        _portDesc = "Near Completion";
                        break;
                    case 3:
                        iState = 3;
                        _portDesc = "Ready to Unload";
                        break;
                    case 4:
                        iState = 4;
                        _portDesc = "Ready to load";
                        break;
                    case 5:
                        iState = 5;
                        _portDesc = "Reject and Ready to unload";
                        break;
                    case 6:
                        iState = 6;
                        _portDesc = "Port Alarm";
                        break;
                    default:
                        iState = 9;
                        _portDesc = "Unknow";
                        break;
                }

            }
            catch (Exception ex)
            { iState = 99; _portDesc = string.Format("Exception:{0}", ex.Message); }
            
            dr1 = null;

            return iState;
        }
        public DataTable GetAvailableCarrier(DBTool _dbTool, string _carrierType, bool _isFull, string _RTDEnv)
        {
            string sql = "";
            DataTable dtAvailableCarrier = null;

            try
            {
                if (_RTDEnv.Equals("PROD"))
                    sql = string.Format(_BaseDataService.SelectAvailableCarrierByCarrierType(_carrierType, _isFull));
                else if (_RTDEnv.Equals("UAT"))
                    sql = string.Format(_BaseDataService.SelectAvailableCarrierForUATByCarrierType(_carrierType, _isFull));

                dtAvailableCarrier = _dbTool.GetDataTable(sql);
            }
            catch (Exception ex)
            { dtAvailableCarrier = null; }

            return dtAvailableCarrier;
        }
        public string GetLocatePort(string _locate, int _portNo, string _locationType)
        {
            string LocatePort = "";
            string tmpLocate = "";
            //
            try
            {
                switch (_locationType)
                {
                    case "ERACK":
                    case "STOCKER":
                    case "EQUIPMENT":
                        tmpLocate = "{0}_LP{1}";
                        LocatePort = string.Format(tmpLocate, _locate.Trim(), _portNo.ToString().PadLeft(2, '0'));
                        break;
                    default:
                        tmpLocate = "{0}_LP{1}";
                        LocatePort = string.Format(tmpLocate, _locate.Trim(), _portNo.ToString().PadLeft(2, '0'));
                        break;
                }

            }
            catch (Exception ex)
            { }

            return LocatePort;
        }
        public double TimerTool(string unit, string lastDateTime)
        {
            double dbTime = 0;
            DateTime curDT = DateTime.Now;
            try
            {
                DateTime tmpDT = Convert.ToDateTime(lastDateTime);
                TimeSpan timeSpan = new TimeSpan(curDT.Ticks - tmpDT.Ticks);

                switch (unit.ToLower())
                {
                    case "day":
                        dbTime = timeSpan.TotalDays;
                        break;
                    case "hours":
                        dbTime = timeSpan.TotalHours;
                        break;
                    case "minutes":
                        dbTime = timeSpan.TotalMinutes;
                        break;
                    case "seconds":
                        dbTime = timeSpan.TotalSeconds;
                        break;
                    case "milliseconds":
                        dbTime = timeSpan.TotalMilliseconds;
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            { }
            return dbTime;
        }
        public double TimerTool(string unit, string startDateTime, string lastDateTime)
        {
            double dbTime = 0;
            DateTime StartDT;
            DateTime LastDT;
            try
            {
                StartDT = Convert.ToDateTime(startDateTime);
                LastDT = Convert.ToDateTime(lastDateTime);
                TimeSpan timeSpan = new TimeSpan(LastDT.Ticks - StartDT.Ticks);

                switch (unit.ToLower())
                {
                    case "day":
                        dbTime = timeSpan.TotalDays;
                        break;
                    case "hours":
                        dbTime = timeSpan.TotalHours;
                        break;
                    case "minutes":
                        dbTime = timeSpan.TotalMinutes;
                        break;
                    case "seconds":
                        dbTime = timeSpan.TotalSeconds;
                        break;
                    case "milliseconds":
                        dbTime = timeSpan.TotalMilliseconds;
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            { }
            return dbTime;
        }
        public int GetExecuteMode(DBTool _dbTool)
        {
            int iExecMode = 1;
            string sql = "";
            DataTable dt = null;

            try
            {
                sql = string.Format(_BaseDataService.SelectRTDDefaultSet("ExecuteMode"));
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    iExecMode = int.Parse(dt.Rows[0]["PARAMVALUE"].ToString());
                }
            }
            catch (Exception ex)
            { dt = null; }
            finally
            {
                if (dt != null)
                    dt.Dispose(); 
            }
            dt = null;

            return iExecMode;
        }
        public bool CheckIsAvailableLot(DBTool _dbTool, string _lotId, string _machine)
        {
            bool isAvailableLot = false;
            string sql = "";
            DataTable dt=null;

            try
            {
                sql = string.Format(_BaseDataService.CheckIsAvailableLot(_lotId, _machine));
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                    isAvailableLot = true;
                else
                    isAvailableLot = false;
            }
            catch (Exception ex)
            { dt = null; }
            finally
            {
                if (dt != null)
                    dt.Dispose(); 
            }
            dt = null;

            return isAvailableLot;
        }
        public bool VerifyCustomerDevice(DBTool _dbTool, ILogger _logger, string _machine, string _customerName, string _lotid, out string _resultCode)
        {
            /*
            //1xxx 機台狀態 [1001] 不同客戶, 為換客戶後的第一批, 可執行但發提示, [1002] 同客戶最後一批,可以執行上機但需發Alarm
            //2xxx 機台狀態 [2001] Not Load Port Status 不可執行, [2002] Not Production Running 可以執行上機
            ///檢驗結果True: 屬於同一客戶批號產品, False: 屬於不同客戶批號產品
            ///結果為True, Result Code: 0000/0001:當前最後一筆
             */
            bool bResult = false;
            DataTable dtCurrentLot = null;
            DataTable dtLoadPortCurrentState = null;
            DataTable dt = null;
            string tmpSql = "";
            //string _lotid = "";
            string tmpMsg = "";
            _resultCode = "0000";

            try
            {
                //20230413V1.0 Modify by Vance
                tmpSql = _BaseDataService.GetLoadPortCurrentState(_machine);
                dtLoadPortCurrentState = _dbTool.GetDataTable(tmpSql);

                if (dtLoadPortCurrentState.Rows.Count <= 0)
                {
                    tmpMsg = "No any relevant information of load port at this machine.";
                    _resultCode = "2001";
                    return bResult;
                }

                foreach (DataRow dr in dtLoadPortCurrentState.Rows)
                {
                    if (dr["CustomerName"].ToString().Equals(""))
                        continue;

                    if (dr["CustomerName"].ToString().Equals(_customerName))
                        bResult = true;
                    else
                    {
                        //it is last record of this Customer lot.  //will change Customer.
                        _resultCode = "1001";
                        bResult = true;
                        break;
                    }
                }

                if (bResult)
                {
                    if (_resultCode.Equals("0000"))
                    {
                        //Not include Hold/Proc/Delete/Complite 4 status.
                        tmpSql = _BaseDataService.SelectTableProcessLotInfoByCustomer(_customerName, _machine);
                        dt = _dbTool.GetDataTable(tmpSql);

                        if (dt.Rows.Count == 1)
                        {
                            _resultCode = "1002";
                            //it is the Customer last lot. need alarm eng to clean line.
                        }

                        // 0000 is normal state of this customer.
                    }
                }
                else
                {
                    tmpMsg = "No product running on this machine.";
                    _resultCode = "2002";
                }
            }
            catch (Exception ex)
            { }
            finally
            {
                if (dt != null)
                    dt.Dispose();
                if (dtCurrentLot != null)
                    dtCurrentLot.Dispose();
                if (dtLoadPortCurrentState != null)
                    dtLoadPortCurrentState.Dispose(); 
            }
            dt = null;
            dtCurrentLot = null;
            dtLoadPortCurrentState = null;

            return bResult;
        }
        public bool GetLockState(DBTool _dbTool)
        {
            bool isLock = false;
            string sql = "";
            DataTable dt=null;

            try
            {
                sql = string.Format(_BaseDataService.GetLockStateLotInfo());
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    if (dt.Rows[0]["lockstate"].ToString().Equals("1"))
                        isLock = true;
                    else
                        isLock = false;

                }
            }
            catch (Exception ex)
            { dt = null; }
            finally
            {
                if (dt != null)
                    dt.Dispose(); 
            }
            dt = null;

            return isLock;
        }
        public bool ThreadLimitTraffice(Dictionary<string, string> _threadCtrl, string key, double _time, string _timeUnit, string _symbol)
        {
            bool bCtrl;
            double timediff = 0;
            string lastDateTime = "";
            string unit;

            try
            {
                lock (_threadCtrl)
                {
                    _threadCtrl.TryGetValue(key, out lastDateTime);
                }

                bCtrl = false;

                switch (_timeUnit.ToLower())
                {
                    case "dd":
                        unit = "Day";
                        timediff = TimerTool(unit, lastDateTime);
                        break;
                    case "hh":
                        unit = "Hours";
                        timediff = TimerTool(unit, lastDateTime);
                        break;
                    case "mi":
                        unit = "Minutes";
                        timediff = TimerTool(unit, lastDateTime);
                        break;
                    case "ms":
                        unit = "MilliSeconds";
                        timediff = TimerTool(unit, lastDateTime);
                        break;
                    case "ss":
                        unit = "seconds";
                        timediff = TimerTool(unit, lastDateTime);
                        break;
                    default:
                        break;
                }

                switch (_symbol.ToLower())
                {
                    case "<=":
                        if (timediff <= _time)
                            bCtrl = true;
                        else
                            bCtrl = false;
                        break;
                    case ">=":
                        if (timediff >= _time)
                            bCtrl = true;
                        else
                            bCtrl = false;
                        break;
                    case "=":
                        if (timediff == _time)
                            bCtrl = true;
                        else
                            bCtrl = false;
                        break;
                    case ">":
                        if (timediff > _time)
                            bCtrl = true;
                        else
                            bCtrl = false;
                        break;
                    case "<":
                    default:
                        if (timediff < _time)
                            bCtrl = true;
                        else
                            bCtrl = false;

                        break;
                }
            }
            catch (Exception ex)
            { bCtrl = false; }

            return bCtrl;
        }
        public bool AutoAssignCarrierType(DBTool _dbTool, out string tmpMessage)
        {
            bool bResult = false;
            string sql = "";
            string tmpSql = "";
            string tmpMsg = "";
            tmpMessage = "";
            DataTable dt = null;
            DataTable dt2 = null;

            try
            {
                sql = string.Format(_BaseDataService.SelectRTDDefaultSet("CarrierType"));
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    string keyValue = "";
                    string Desc = "";

                    foreach (DataRow dr in dt.Rows)
                    {
                        keyValue = "";
                        Desc = "";

                        keyValue = dr["ParamValue"] is not null ? dr["ParamValue"].ToString() : "";
                        Desc = dr["description"] is not null ? dr["description"].ToString() : "";

                        tmpSql = string.Format(_BaseDataService.QueryCarrierType(keyValue, Desc));
                        dt2 = _dbTool.GetDataTable(tmpSql);

                        if (dt2.Rows.Count > 0)
                        {
                            _dbTool.SQLExec(_BaseDataService.UpdateCarrierType(keyValue, Desc), out tmpMsg, true);
                        }
                    }

                    if (dt.Rows[0]["lockstate"].ToString().Equals("1"))
                    {
                        tmpMessage = "";
                        bResult = true;
                    }
                    else
                    {
                        tmpMessage = "";
                        bResult = false;
                    }

                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (dt != null)
                    dt.Dispose();
                if (dt2 != null)
                    dt2.Dispose();
            }
            dt = null;
            dt2 = null;

            return bResult;
        }
        public bool AutoSentInfoUpdate(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, out string tmpMessage)
        {
            bool bResult = false;
            string sql = "";
            string tmpSql = "";
            string tmpMsg = "";
            tmpMessage = "";
            DataTable dt = null;
            DataTable dt2 = null;
            DataTable dtTemp = null;
            List<string> args = new();
            APIResult apiResult = new APIResult();
            string _table = "";
            string _infoupdate = "";

            try
            {
                //_args.Split(',')
                _table = _configuration["eRackDisplayInfo:contained"].ToString().Split(',')[1];

                sql = _BaseDataService.QueryCarrierAssociateWhenOnErack(_table);
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    string lotid;
                    string carrierId;
                    foreach (DataRow dr in dt.Rows)
                    {
                        args = new();
                        apiResult = new APIResult();
                        carrierId = "";

                        lotid = dr["LOT_ID"].ToString().Equals("") ? "" : dr["LOT_ID"].ToString();
                        carrierId = dr["carrier_id"].ToString().Equals("") ? "" : dr["carrier_id"].ToString();

                        _infoupdate = dr["info_update_dt"].ToString().Equals("NULL") ? "" : dr["info_update_dt"].ToString();

                        if (dr["location_type"].ToString().Trim().Equals("ERACK") || dr["location_type"].ToString().Trim().Equals("STK"))
                        {

                            if (carrierId.Length > 0)
                            {
                                tmpMsg = string.Format("[AutoSentInfoUpdate: Flag LotId {0}]", lotid);
                                _logger.Info(tmpMsg);

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
                                string v_FORCE = "";
                                try
                                {
                                    v_CUSTOMERNAME = dr["CUSTOMERNAME"].ToString().Equals("") ? "" : dr["CUSTOMERNAME"].ToString();
                                    v_PARTID = dr["PARTID"].ToString().Equals("") ? "" : dr["PARTID"].ToString();
                                    v_LOTTYPE = dr["LOTTYPE"].ToString().Equals("") ? "" : dr["LOTTYPE"].ToString();

                                    sql = _BaseDataService.QueryErackInfoByLotID(_configuration["eRackDisplayInfo:contained"], lotid);
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
                                    }

                                    if(_infoupdate.Equals(""))
                                    {
                                        v_FORCE = "true";
                                    }
                                    else
                                    {
                                        if(TimerTool("minutes", _infoupdate) >= 3)
                                        { v_FORCE = "true"; }
                                        else
                                        { v_FORCE = "false"; }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    tmpMsg = string.Format("[AutoSentInfoUpdate: Column Issue. {0}]", ex.Message);
                                    _logger.Info(tmpMsg);
                                }

                                args.Add(lotid);
                                args.Add(v_STAGE.Equals("") ? dr["STAGE"].ToString() : v_STAGE);
                                args.Add("");//("machine");
                                args.Add("");//("desc");
                                args.Add(carrierId);
                                args.Add(v_CUSTOMERNAME);
                                args.Add(v_PARTID);//("PartID");
                                args.Add(v_LOTTYPE);//("LotType");
                                args.Add(v_AUTOMOTIVE);//("Automotive");
                                args.Add(v_STATE);//("State");
                                args.Add(v_HOLDCODE);//("HoldCode");
                                args.Add(v_TURNRATIO);//("TURNRATIO");
                                args.Add(v_EOTD);//("EOTD");
                                args.Add(v_HOLDREAS);//("v_HOLDREAS");
                                args.Add(v_POTD);//("v_POTD");
                                args.Add(v_WAFERLOT);//("v_WAFERLOT");
                                args.Add(v_FORCE);//("v_FORCE");
                                apiResult = SentCommandtoMCSByModel(_configuration, _logger, "InfoUpdate", args);

                                if (!carrierId.Equals(""))
                                {
                                    if (v_FORCE.ToLower().Equals("true"))
                                    {
                                        sql = _BaseDataService.CarrierTransferDTUpdate(carrierId, "InfoUpdate");
                                        _dbTool.SQLExec(sql, out tmpMsg, true);
                                    }
                                }
                            }
                            else
                            {
                                tmpMsg = string.Format("[CarrierLocationUpdate: Carrier [{0}] Not Exist.]", carrierId);
                                _logger.Debug(tmpMsg);

                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                apiResult = SentCommandtoMCSByModel(_configuration, _logger, "InfoUpdate", args);
                            }
                            Thread.Sleep(300);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (dt != null)
                    dt.Dispose();
                if (dt2 != null)
                    dt2.Dispose();
                if (dtTemp != null)
                    dtTemp.Dispose(); 
            }
            dt = null;
            dt2 = null;
            dtTemp = null;

            return bResult;
        }
        public bool AutoBindAndSentInfoUpdate(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, out string tmpMessage)
        {
            bool bResult = false;
            string sql = "";
            string tmpSql = "";
            string tmpMsg = "";
            tmpMessage = "";
            DataTable dt = null;
            DataTable dt2 = null;
            DataTable dtTemp = null;
            List<string> args = new();
            APIResult apiResult = new APIResult();

            try
            {
                sql = _BaseDataService.QueryCarrierAssociateWhenIsNewBind();
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    string lotid;
                    string carrierId;
                    foreach (DataRow dr in dt.Rows)
                    {
                        args = new();
                        apiResult = new APIResult();
                        carrierId = "";

                        lotid = dr["LOT_ID"].ToString().Equals("") ? "" : dr["LOT_ID"].ToString();
                        carrierId = dr["carrier_id"].ToString().Equals("") ? "" : dr["carrier_id"].ToString();

                        if (dr["location_type"].ToString().Trim().Equals("ERACK") || dr["location_type"].ToString().Trim().Equals("A") || dr["location_type"].ToString().Trim().Equals("STK"))
                        {

                            if (carrierId.Length > 0)
                            {
                                tmpMsg = string.Format("[AutoSentInfoUpdate: Flag LotId {0}]", lotid);
                                _logger.Info(tmpMsg);

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
                                string v_FORCE = "true";
                                try
                                {
                                    v_CUSTOMERNAME = dr["CUSTOMERNAME"].ToString().Equals("") ? "" : dr["CUSTOMERNAME"].ToString();
                                    v_PARTID = dr["PARTID"].ToString().Equals("") ? "" : dr["PARTID"].ToString();
                                    v_LOTTYPE = dr["LOTTYPE"].ToString().Equals("") ? "" : dr["LOTTYPE"].ToString();

                                    sql = _BaseDataService.QueryErackInfoByLotID(_configuration["eRackDisplayInfo:contained"], lotid);
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
                                    }
                                }
                                catch (Exception ex)
                                {
                                    tmpMsg = string.Format("[AutoSentInfoUpdate: Column Issue. {0}]", ex.Message);
                                    _logger.Info(tmpMsg);
                                }

                                args.Add(lotid);
                                args.Add(v_STAGE.Equals("") ? dr["STAGE"].ToString() : v_STAGE);
                                args.Add("");//("machine");
                                args.Add("");//("desc");
                                args.Add(carrierId);
                                args.Add(v_CUSTOMERNAME);
                                args.Add(v_PARTID);//("PartID");
                                args.Add(v_LOTTYPE);//("LotType");
                                args.Add(v_AUTOMOTIVE);//("Automotive");
                                args.Add(v_STATE);//("State");
                                args.Add(v_HOLDCODE);//("HoldCode");
                                args.Add(v_TURNRATIO);//("TURNRATIO");
                                args.Add(v_EOTD);//("EOTD");
                                args.Add(v_HOLDREAS);//("v_HOLDREAS");
                                args.Add(v_POTD);//("v_POTD");
                                args.Add(v_WAFERLOT);//("v_WAFERLOT");
                                args.Add(v_FORCE);//("v_FORCE");
                                apiResult = SentCommandtoMCSByModel(_configuration, _logger, "InfoUpdate", args);

                                if (!carrierId.Equals(""))
                                {
                                    if (v_FORCE.ToLower().Equals("true"))
                                    {
                                        sql = _BaseDataService.CarrierTransferDTUpdate(carrierId, "InfoUpdate");
                                        _dbTool.SQLExec(sql, out tmpMsg, true);
                                    }
                                }

                                if (!lotid.Equals(""))
                                {
                                    //檢查Lot的Total Qty是否為0 用Quantity取代 Total Qty
                                    //如果不同, 用Quantity 取代 Total Qty
                                    //相同則不變更
                                    tmpSql = _BaseDataService.QueryLotinfoQuantity(lotid);
                                    dt2 = _dbTool.GetDataTable(tmpSql);

                                    if (dt2.Rows.Count > 0)
                                    {
                                        int iTotalQty = int.Parse(dt2.Rows[0]["Total_Qty"].ToString());
                                        int iQuantity = int.Parse(dr["Quantity"].ToString());
                                        if ((iTotalQty == 0) || (iQuantity > iTotalQty))
                                        {
                                            //Sync Total and Quantity
                                            _dbTool.SQLExec(_BaseDataService.UpdateLotinfoTotalQty(lotid, iQuantity), out tmpMsg, true);
                                        }
                                    }
                                    else
                                    {
                                        //Lot 還未被Release時, New Bind狀態不被改掉 
                                        continue;
                                    }
                                }
                            }
                            else
                            {
                                tmpMsg = string.Format("[CarrierLocationUpdate: Carrier [{0}] Not Exist.]", carrierId);
                                _logger.Debug(tmpMsg);

                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                args.Add("");
                                apiResult = SentCommandtoMCSByModel(_configuration, _logger, "InfoUpdate", args);
                            }

                            //Clean EquipList when Rebind Lot
                            if (!lotid.Equals(""))
                            {
                                sql = _BaseDataService.EQPListReset(lotid);
                                _dbTool.SQLExec(sql, out tmpMsg, true);
                            }
                        }
                        else if (dr["location_type"].ToString().Trim().Equals("Sync"))
                        {
                            //Do Nothing. when lot info data been released.
                            continue;
                        }
                        else
                        {
                            if (!carrierId.Equals(""))
                            {
                                sql = _BaseDataService.ResetCarrierLotAssociateNewBind(carrierId);
                                _dbTool.SQLExec(sql, out tmpMessage, true);
                            }
                        }

                        if (apiResult.Success)
                        {
                            tmpMessage = "";
                            bResult = true;
                            sql = _BaseDataService.ResetCarrierLotAssociateNewBind(carrierId);
                            _dbTool.SQLExec(sql, out tmpMessage, true);
                        }
                        else
                        {
                            tmpMessage = apiResult.Message;
                            bResult = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (dt != null)
                    dt.Dispose();
                if (dt2 != null)
                    dt2.Dispose();
                if (dtTemp != null)
                    dtTemp.Dispose();
                args.Clear();
                if (apiResult != null)
                    apiResult.Dispose();
            }
            dt = null;
            dt2 = null;
            dtTemp = null;
            args = null;
            apiResult = null;

            return bResult;
        }
        public bool AutoUpdateRTDStatistical(DBTool _dbTool, out string tmpMessage)
        {
            bool bResult = false;
            string sql = "";
            string tmpSql = "";
            string tmpMsg = "";
            tmpMessage = "";
            DataTable dt = null;
            DataTable dt2 = null;

            try
            {
                sql = string.Format(_BaseDataService.QueryRTDStatisticalRecord(""));
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {

                    foreach (DataRow dr in dt.Rows)
                    {
                        DateTime tmpDateTime;
                        if (dr["recordtime"] is null)
                            continue;
                        tmpDateTime = DateTime.Parse(dr["recordtime"].ToString());
                        tmpSql = string.Format(_BaseDataService.QueryRTDStatisticalByCurrentHour(tmpDateTime));
                        dt2 = _dbTool.GetDataTable(tmpSql);

                        if (dt2.Rows.Count > 0)
                        {
                            string cdtType = String.Format("Type = '{0}'", dr["type"].ToString());
                            DataRow[] dr2 = dt2.Select(cdtType);
                            int iTimes = dr2.Length > 0 ? int.Parse(dr2[0]["times"].ToString()) + int.Parse(dr["times"].ToString()) : int.Parse(dr["times"].ToString());
                            _dbTool.SQLExec(_BaseDataService.UpdateRTDStatistical(tmpDateTime, dr["type"].ToString(), iTimes), out tmpMsg, true);
                            _dbTool.SQLExec(_BaseDataService.CleanRTDStatisticalRecord(tmpDateTime, dr["type"].ToString()), out tmpMsg, true);
                        }
                        else
                        {
                            _dbTool.SQLExec(_BaseDataService.InitialRTDStatistical(tmpDateTime.ToString("yyyy-MM-dd HH:mm:ss"), dr["type"].ToString()), out tmpMsg, true);
                            _dbTool.SQLExec(_BaseDataService.UpdateRTDStatistical(tmpDateTime, dr["type"].ToString(), int.Parse(dr["times"].ToString())), out tmpMsg, true);
                            _dbTool.SQLExec(_BaseDataService.CleanRTDStatisticalRecord(tmpDateTime, dr["type"].ToString()), out tmpMsg, true);
                        }
                    }

                    bResult = true;
                }
                else
                {
                    //Do Nothing
                }
            }
            catch (Exception ex)
            {
                bResult = false;
            }
            finally
            {
                if(dt != null)
                    dt.Dispose();
                if (dt2 != null)
                    dt2.Dispose();
            }
            dt = null;
            dt2 = null;

            return bResult;
        }
        private Dictionary<int, string> _lstAlarmCode = new Dictionary<int, string>();
        public Dictionary<int, string> ListAlarmCode
        {
            get { return _lstAlarmCode; }
            set { _lstAlarmCode = value; }
        }
        public bool CallRTDAlarm(DBTool _dbTool, int _alarmCode, string[] argv)
        {
            bool bResult = false;
            string tmpSQL = "";
            string error = "";
            string alarmMsg = "";
            Dictionary<int, string> lstAlarm = new Dictionary<int, string>();
            string _commandid = "";
            string _params = "";
            string _desc = "";
            string _eventTrigger = "";

            try
            {
                if (argv is not null)
                {

                    _commandid = argv is null ? "" : argv[0];
                    _params = argv is null ? "" : argv[1];
                    _desc = argv is null ? "" : argv[2];
                    _eventTrigger = argv is null ? "" : argv[3];
                }

                string[] tmpAryay = new string[11];
                if (ListAlarmCode.Count <= 0)
                {
                    
                    string[] aryAlarm = {
                        "10100,System,MCS,Error,Sent Command Failed,0,,{0},{1},{2},{3}",
                        "10101,System,MCS,Alarm,MCS Connection Failed,0,,{0},{1},{2},{3}",
                        "20100,System,Database Access,Alarm,Database Access Error,100,AlarmSet,{0},{1},{2},{3}",
                        "20101,System,Database Access,Alarm,Database Access Success,101,AlarmReset,{0},{1},{2},{3}",
                        "30000,System,RTD,Issue,Dispatch overtime,1,Auto Clean Commands,{0},{1},{2},{3}",
                        "30001,System,RTD,Issue,Dispatch overtime,2,Auto Hold Lot,{0},{1},{2},{3}",
                        "30002,System,RTD,Alarm,3 times fail,0,Load Port disable,{0},{1},{2},{3}",
                        "90000,System,RTD,INFO,TESE ALARM,0,,{0},{1},{2},{3}"
                    };

                    int _iAlarmCode = 0;
                    foreach (string alarm in aryAlarm)
                    {
                        tmpAryay = alarm.Split(',');
                        _iAlarmCode = int.Parse(tmpAryay[0]);
                        //string[] tmp = alarm.Split(',');
                        //_iAlarmCode = int.Parse(tmp[0]);

                        lstAlarm.Add(_iAlarmCode, alarm);
                    }

                    ListAlarmCode = lstAlarm;
                }

                alarmMsg = ListAlarmCode[_alarmCode];

                if (!alarmMsg.Split(',')[1].Equals(""))
                {
                    tmpSQL = _BaseDataService.InsertRTDAlarm(tmpAryay);
                }

                if (_commandid.Equals(""))
                    tmpSQL = string.Format(tmpSQL, _commandid, _params, _desc, _eventTrigger);
                else
                    tmpSQL = string.Format(tmpSQL, _commandid, _params, _desc, _eventTrigger);

                if(!tmpSQL.Equals(""))
                    _dbTool.SQLExec(tmpSQL, out error, true);

                if (error.Equals(""))
                    bResult = true;
            }
            catch (Exception ex)
            {
                //Do Nothing
            }

            return bResult;
        }
        public ResultMsg CheckCurrentLotStatebyWebService(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, string _lotId)
        {
            ResultMsg retMsg = new ResultMsg();
            string tmpMsg = "";
            string errMsg = "";
            string funcCode = "CheckCurrentLotStatebyWebService";
            List<string> TesterMachineList = new List<string>();

            string url = _configuration["WebService:url"];
            string username = _configuration["WebService:username"];
            string password = _configuration["WebService:password"];
            string webServiceMode = "soap11";

            DataTable dt = null;
            string tmpSql = "";
            JCETWebServicesClient jcetWebServiceClient = new JCETWebServicesClient();
            JCETWebServicesClient.ResultMsg resultMsg = new JCETWebServicesClient.ResultMsg();
            try
            {
                jcetWebServiceClient = new JCETWebServicesClient();
                jcetWebServiceClient._url = url;
                resultMsg = new JCETWebServicesClient.ResultMsg();
                resultMsg = jcetWebServiceClient.CurrentLotState(webServiceMode, username, password, _lotId);
                string result3 = resultMsg.retMessage;

#if DEBUG
                //_logger.Info(string.Format("Info:{0}", tmpMsg));
#else
#endif

                if (resultMsg.status)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(result3);
                    XmlNode xn = xmlDoc.SelectSingleNode("Beans");

                    XmlNodeList xnlA = xn.ChildNodes;
                    String member_valodation = "";
                    String member_validation_message = "";
                    string member_currentState = "";
                    foreach (XmlNode xnA in xnlA)
                    {
                        Console.WriteLine(xnA.Name);
                        if ((xnA.Name) == "Status")
                        {
                            XmlElement xeB = (XmlElement)xnA;
                            if ((xeB.GetAttribute("Value")) == "SUCCESS")
                            {
                                member_valodation = "OK";
                                continue;
                            }
                            else
                            {
                                member_valodation = "NG";
                            }
                        }
                        if (member_valodation.Equals("OK"))
                        {
                            if ((xnA.Name) == "Msg")
                            {
                                XmlElement xeB = (XmlElement)xnA;
                                member_currentState = xeB.GetAttribute("Value").Equals("") ? "" : xeB.GetAttribute("Value");
                            }
                            break;
                        }
                        if ((xnA.Name) == "ErrMsg")
                        {
                            XmlElement xeB = (XmlElement)xnA;
                            member_validation_message = xeB.GetAttribute("Value");
                        }

                        Console.WriteLine(member_valodation);
                    }
                    if (member_valodation == "OK")
                    {
                        string lotState = "";
                        if (member_currentState.Equals("D"))
                        {
                            lotState = "WAIT";
                            //Check Lot Info, if state not in WAIT, update to WAIT
                            tmpSql = _BaseDataService.ConfirmLotinfoState(_lotId, "HOLD");
                            dt = _dbTool.GetDataTable(tmpSql);
                            if (dt.Rows.Count > 0)
                                _dbTool.SQLExec(_BaseDataService.UpdateLotinfoState(_lotId, lotState), out errMsg, true);
                        }
                        else
                        {
                            lotState = "HOLD";
                            //Hold Lot (Lot State)
                            tmpSql = _BaseDataService.ConfirmLotinfoState(_lotId, "WAIT");
                            dt = _dbTool.GetDataTable(tmpSql);
                            if (dt.Rows.Count > 0)
                                _dbTool.SQLExec(_BaseDataService.UpdateLotinfoState(_lotId, lotState), out errMsg, true);
                        }

                        if (errMsg.Equals(""))
                        {
                            retMsg.status = true;
                            retMsg.retMessage = String.Format("The Lot state [{0}] has been change to [{1}].", _lotId, lotState);
                        }
                        else
                        {
                            retMsg.status = false;
                            retMsg.retMessage = string.Format("DB update issue: {0}", errMsg);
                        }
                    }
                    else
                    {
                        tmpMsg = string.Format("The Lot Id [{0}] is not invalid.", _lotId);
                        retMsg.status = false;
                        retMsg.retMessage = string.Format("lotid issue: {0}", tmpMsg);
                    }
                }
                else
                {
                    retMsg.status = false;
                    retMsg.retMessage = string.Format("WebService issue: {0}", resultMsg.retMessage);
                }
            }
            catch (Exception ex)
            {
                tmpMsg = string.Format("Unknow issue: [{0}][Exception] {1}", funcCode, ex.Message);
                _logger.Debug(ex.Message);
            }
            finally
            {
                jcetWebServiceClient = null;
                resultMsg = null;
            }

            return retMsg;
        }
        public class ResultMsg
        {
            public bool status { get; set; }
            public string retMessage { get; set; }
            public string remark { get; set; }
        }
        public bool AutoGeneratePort(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, string _equipId, string _portModel, out string _errMessage)
        {
            bool bResult = false;
            _errMessage = "";
            string tmpMsg = "";
            string tmp = "";
            DataTable dt = new DataTable();
            DataRow dr;
            DataTable dtDefaultSet = new DataTable();
            DataRow[] drDefaultSet;
            DataTable dtEeqPortSet = new DataTable();
            string _Port_Model = "";
            int _Port_Number = 0;
            string _WorkGroup = "";
            string _Port_Type = "";
            string tmpPortId = "{0}_LP{1}";
            string EqpPortId = "";
            string portType = "";
            string carrierType = "";
            string EqpTypeID = "";
            string tmpMessage = "";
            DataSet dtSet = new DataSet();
            DataTable dtPortType = new DataTable();
            DataTable dtCarrierType = new DataTable();

            try
            {
                dt = _dbTool.GetDataTable(_BaseDataService.SelectTableEquipmentPortsInfoByEquipId(_equipId));
                if (dt.Rows.Count > 0)
                {
                    _Port_Model = dt.Rows[0]["Port_Model"] is null ? "" : dt.Rows[0]["Port_Model"].ToString();
                    _Port_Number = dt.Rows[0]["Port_Number"] is null ? 0 : dt.Rows[0]["Port_Number"].ToString().Equals("") ? 0 : int.Parse(dt.Rows[0]["Port_Number"].ToString());
                    _WorkGroup = dt.Rows[0]["WorkGroup"] is null ? "" : dt.Rows[0]["WorkGroup"].ToString();
                    EqpTypeID = dt.Rows[0]["Equip_TypeId"] is null ? "" : dt.Rows[0]["Equip_TypeId"].ToString();

                    try
                    {
                        if (!_portModel.Equals(_Port_Model))
                        {
                            tmpMsg = "[AutoGeneratePort] Equipment [{0}] Port Model been Change. From [{1}] To [{2}]";
                            tmpMessage = String.Format(tmpMsg, _equipId, _Port_Model, _portModel);
                            _logger.Debug(tmpMessage);

                            _dbTool.SQLExec(_BaseDataService.DeleteEqpPortSet(_equipId, _Port_Model), out tmpMsg, true);
                        }

                        if (_Port_Number <= 0)
                        {
                            bResult = false;
                            tmpMsg = "[SetEquipmentPortModel] The Equipment [{0}] Port Number not set. please check : {0}";
                            _errMessage = String.Format(tmpMsg, _equipId);
                            _logger.Debug(_errMessage);
                            return bResult;
                        }

                        if (_WorkGroup.Equals(""))
                        {
                            bResult = false;
                            tmpMsg = "[SetEquipmentPortModel] The Equipment [{0}] Workgroup not set. please check.";
                            _errMessage = String.Format(tmpMsg, _equipId);
                            _logger.Debug(_errMessage);
                            return bResult;
                        }

                        //dtDefaultSet = _dbTool.GetDataTable(_BaseDataService.SelectRTDDefaultSet("PortTypeMapping"));
                        //drDefaultSet = dtDefaultSet.Select(string.Format("paramtype = \'{0}\'", EqpTypeID));
                        dtDefaultSet = _dbTool.GetDataTable(_BaseDataService.QueryPortModelMapping(EqpTypeID));

                        if (dtDefaultSet.Rows.Count > 0)
                        {
                            portType = dtDefaultSet.Rows[0]["PortTypeMapping"].ToString().Equals("") ? "" : dtDefaultSet.Rows[0]["PortTypeMapping"].ToString();

                            if (portType.Equals(""))
                            {
                                tmp = "This Equipment Port Type [{0}] have not set Port Type Mapping in Default set.";
                                tmpMsg = tmpMsg.Equals("") ? string.Format(tmp, EqpTypeID) : string.Format(tmpMsg + " & " + tmp, EqpTypeID);
                            }

                            carrierType = dtDefaultSet.Rows[0]["CarrierTypeMapping"].ToString().Equals("") ? "" : dtDefaultSet.Rows[0]["CarrierTypeMapping"].ToString();

                            if (carrierType.Equals(""))
                            {
                                tmp = "This Equipment Port Type [{0}] have not set Carrier Type Mapping in Default set.";
                                tmpMsg = tmpMsg.Equals("") ? string.Format(tmp, EqpTypeID) : string.Format(tmpMsg + " & " + tmp, EqpTypeID);
                            }

                            if (!tmpMsg.Equals(""))
                            {
                                bResult = false;
                                _errMessage = String.Format("[AutoGeneratePort] {0}", tmpMsg);
                                _logger.Debug(_errMessage);
                                return bResult;
                            }
                        }
                        else
                        {
                            bResult = false;
                            tmp = "This Equipment Port Type [{0}] have not set for Port Type and Carrier Type Mapping in default set. Please check.";
                            tmpMsg = tmpMsg.Equals("") ? string.Format(tmp, EqpTypeID) : string.Format(tmpMsg + " & " + tmp, EqpTypeID);
                            _errMessage = String.Format("[AutoGeneratePort] {0}", tmpMsg);
                            _logger.Debug(_errMessage);
                            return bResult;
                        }

                        switch (_portModel)
                        {
                            case "1IOT1":
                            case "1I1OT1":
                            case "1I1OT2":
                            case "2I2OT1":
                                string s1IOT1 = "{'ID':1,'TYPE':'IN'}";
                                string sCarrierType = "{'CarrierType':[{'ID':1,'TYPE':'MetalCassette'}, {'ID':2,'TYPE':'MetalCassette'}]}";
                                dtSet = JsonConvert.DeserializeObject<DataSet>(portType);
                                dtPortType = new DataTable();
                                dtPortType = dtSet.Tables[_portModel];
                                dtSet = JsonConvert.DeserializeObject<DataSet>(carrierType);
                                dtCarrierType = dtSet.Tables[_portModel];
                                break;
                            default:
                                tmpMsg = "[SetEquipmentPortModel] Alarm : Equipment Id [{0}]. PortModel is invalid. please check.";
                                _errMessage = String.Format(tmpMsg, _equipId);
                                break;
                        }

                        string[] tmpArray = new string[7];
                        //(equipid, port_model, port_seq, port_type, port_id, carrier_type, near_stocker, create_dt, modify_dt, lastmodify_dt, port_state, workgroup)
                        tmpArray[0] = _equipId;
                        tmpArray[1] = _portModel;
                        for (int i = 1; i <= _Port_Number; i++)
                        {
                            tmpArray[2] = i.ToString();
                            //port Type
                            DataRow[] drAA;
                            drAA = dtPortType.Select(string.Format("ID = {0}", i.ToString()));
                            tmpArray[3] = drAA[0]["TYPE"].ToString();
                            //Eqp port Id
                            EqpPortId = string.Format(tmpPortId, _equipId, i);
                            tmpArray[4] = EqpPortId;

                            drAA = dtCarrierType.Select(string.Format("ID = {0}", i.ToString()));
                            tmpArray[5] = drAA[0]["TYPE"].ToString();

                            //Workgroup
                            tmpArray[6] = _WorkGroup;

                            dtEeqPortSet = _dbTool.GetDataTable(_BaseDataService.QueryEqpPortSet(_equipId, i.ToString()));
                            if (dtEeqPortSet.Rows.Count <= 0)
                            {
                                _dbTool.SQLExec(_BaseDataService.InsertTableEqpPortSet(tmpArray), out tmpMsg, true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        bResult = false;
                        tmpMsg = "[SetEquipmentPortModel] Exception occurred : Equipment Id [{0}].  {1}";
                        _errMessage = String.Format(tmpMsg, _equipId, ex.Message);
                    }
                }
                else
                {
                    bResult = false;
                    tmpMsg = "The Equipment Id [{0}] is not exists.";
                    _errMessage = String.Format(tmpMsg, _equipId);
                }
            }
            catch (Exception ex)
            {
                bResult = false;
                tmpMsg = "[SetEquipmentPortModel] Unknow Error : Equipment Id [{0}]. {1}";
                _errMessage = String.Format(tmpMsg, _equipId, ex.Message);
            }
            finally
            {
                if (dt != null)
                    dt.Dispose();
                if (dtCarrierType != null)
                    dtCarrierType.Dispose();
                if (dtDefaultSet != null)
                    dtDefaultSet.Dispose();
                if (dtEeqPortSet != null)
                    dtEeqPortSet.Dispose();
                if (dtPortType != null)
                    dtPortType.Dispose();
                if (dtSet != null)
                    dtSet.Dispose();
            }
            dt = null;
            dtCarrierType = null;
            dtDefaultSet = null;
            dtEeqPortSet = null;
            dtPortType = null;
            dtSet = null;
            dr = null;
            drDefaultSet = null;

            return bResult;
        }
        public class TypeContent
        {
            /// <summary>
            /// 
            /// </summary>
            public int ID { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string TYPE { get; set; }
        }
        public class TypeMapping
        {
            public TypeContent TypeContent { get; set; }
        }
        public bool AutoHoldForDispatchIssue(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, out string tmpMessage)
        {
            bool bResult = false;
            string sql = "";
            string tmpSql = "";
            string tmpMsg = "";
            string _errMessage = "";
            tmpMessage = "";
            DataTable dt = null;
            DataTable dt2 = null;

            string tableOrder = "";
            string _keyOfEnv = "";
            string _keyRTDEnv = "";

            try
            {
                _keyRTDEnv = _configuration["RTDEnvironment:type"];
                _keyOfEnv = string.Format("RTDEnvironment:commandsTable:{0}", _keyRTDEnv);
                //"RTDEnvironment:commandsTable:PROD"
                tableOrder = _configuration[_keyOfEnv] is null ? "workinprocess_sch" : _configuration[_keyOfEnv];

                sql = _BaseDataService.QueryProcLotInfo();
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    string lotid;
                    string carrierId;
                    string lastModifyDT;
                    string commandId;
                    string GId;
                    bool bAutoHold = false;
                    foreach (DataRow dr in dt.Rows)
                    {
                        bAutoHold = false;
                        lotid = dr["LOTID"].ToString().Equals("") ? "" : dr["LOTID"].ToString();
                        carrierId = "";

                        if (!lotid.Equals(""))
                        {
                            sql = _BaseDataService.SelectTableWorkInProcessSchByLotId(lotid, tableOrder);
                            dt2 = _dbTool.GetDataTable(sql);

                            if (dt2.Rows.Count > 0)
                            {
                                foreach (DataRow dr2 in dt2.Rows)
                                {
                                    carrierId = dr2["carrierid"].ToString().Equals("") ? "" : dr2["carrierid"].ToString();
                                    lastModifyDT = dr2["lastModify_dt"].ToString().Equals("") ? "" : dr2["lastModify_dt"].ToString();
                                    commandId = dr2["cmd_id"].ToString().Equals("") ? "" : dr2["cmd_id"].ToString();
                                    GId = dr2["uuid"].ToString().Equals("") ? "" : dr2["uuid"].ToString();

                                    if (TimerTool("hours", lastModifyDT) >= 1)
                                    {
                                        sql = _BaseDataService.UpdateTableWorkInProcessSchHisByUId(GId);
                                        _dbTool.SQLExec(sql, out tmpMsg, true);
                                        sql = _BaseDataService.DeleteWorkInProcessSchByGId(GId, tableOrder);
                                        _dbTool.SQLExec(sql, out tmpMsg, true);

                                        if (tmpMsg.Equals(""))
                                        {
                                            string[] tmpString = new string[] { GId, "", "" };
                                            CallRTDAlarm(_dbTool, 30000, tmpString);

                                            bAutoHold = true;
                                            break;
                                        }
                                        else
                                        {
                                            bResult = false;
                                            tmpMsg = "[AutoHoldForDispatchIssue] WorkinProcessSch Clean Failed : {0}";
                                            _errMessage = String.Format(tmpMsg, tmpMsg);
                                            _logger.Debug(_errMessage);
                                        }
                                    }
                                }
                            }
                        }

                        if (bAutoHold)
                        {
                            //_dbTool.SQLExec(_BaseDataService.UpdateTableLotInfoState(lotid, "HOLD"), out tmpMsg, true);
                            _dbTool.SQLExec(_BaseDataService.UpdateTableCarrierTransferByCarrier(carrierId, "SYSHOLD"), out tmpMsg, true);

                            if (tmpMsg.Equals(""))
                            {
                                string[] tmpString = new string[] { lotid, "", "" };
                                CallRTDAlarm(_dbTool, 30001, tmpString);

                                bResult = true;
                                break;
                            }
                            else
                            {
                                bResult = false;
                                tmpMsg = "[AutoHoldForDispatchIssue] Auto Hold Lot Failed : {0}";
                                _errMessage = String.Format(tmpMsg, tmpMsg);
                                _logger.Debug(_errMessage);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                bResult = false;
                tmpMsg = "[AutoHoldForDispatchIssue] Unknow Error : {0}";
                _errMessage = String.Format(tmpMsg, ex.Message);
                _logger.Debug(_errMessage);
            }
            finally
            {
                if (dt != null)
                    dt.Dispose();
                if (dt2 != null)
                    dt2.Dispose();
            }
            dt = null;
            dt2 = null;

            return bResult;
        }
        public bool TriggerCarrierInfoUpdate(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, string _lotid)
        {
            bool bResult = false;
            string tmpMsg = "";
            string sql = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            string _infoupdate = "";

            try
            {
                sql = string.Format(_BaseDataService.CheckLocationByLotid(_lotid));
                dt = _dbTool.GetDataTable(sql);
                if (dt.Rows.Count > 0)
                {
                    tmpMsg = string.Format("[TriggerCarrierInfoUpdate][CheckLocationByLotid][{0}][{1}]", _lotid, _configuration["eRackDisplayInfo:contained"]);
                    _logger.Debug(tmpMsg);

                    _infoupdate = dt.Rows[0]["info_update_dt"].ToString().Equals("NULL") ? "" : dt.Rows[0]["info_update_dt"].ToString();

                    List<string> args = new();
                    string v_LOT_ID = "";
                    string v_STAGE = "";
                    string v_carrier_id = "";
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
                    string v_FORCE = "";
                    try
                    {
                        v_carrier_id = dt.Rows[0]["carrier_id"].ToString().Equals("") ? "" : dt.Rows[0]["carrier_id"].ToString();
                        v_LOT_ID = _lotid;
                        v_CUSTOMERNAME = dt.Rows[0]["CUSTOMERNAME"].ToString().Equals("") ? "" : dt.Rows[0]["CUSTOMERNAME"].ToString();
                        v_PARTID = dt.Rows[0]["PARTID"].ToString().Equals("") ? "" : dt.Rows[0]["PARTID"].ToString();
                        v_LOTTYPE = dt.Rows[0]["LOTTYPE"].ToString().Equals("") ? "" : dt.Rows[0]["LOTTYPE"].ToString();
                        v_STAGE = dt.Rows[0]["stage"].ToString().Equals("") ? "" : dt.Rows[0]["stage"].ToString();

                        sql = _BaseDataService.QueryErackInfoByLotID(_configuration["eRackDisplayInfo:contained"], v_LOT_ID);
                        dtTemp = _dbTool.GetDataTable(sql);

                        if (dtTemp.Rows.Count > 0)
                        {
                            v_STAGE = !v_STAGE.Equals("") ? "" : dtTemp.Rows[0]["STAGE"].ToString();
                            v_AUTOMOTIVE = dtTemp.Rows[0]["AUTOMOTIVE"].ToString().Equals("") ? "" : dtTemp.Rows[0]["AUTOMOTIVE"].ToString();
                            v_STATE = dtTemp.Rows[0]["STATE"].ToString().Equals("") ? "" : dtTemp.Rows[0]["STATE"].ToString();
                            v_HOLDCODE = dtTemp.Rows[0]["HOLDCODE"].ToString().Equals("") ? "" : dtTemp.Rows[0]["HOLDCODE"].ToString();
                            v_TURNRATIO = dtTemp.Rows[0]["TURNRATIO"].ToString().Equals("") ? "0" : dtTemp.Rows[0]["TURNRATIO"].ToString();
                            v_EOTD = dtTemp.Rows[0]["EOTD"].ToString().Equals("") ? "" : dtTemp.Rows[0]["EOTD"].ToString();
                            v_POTD = dtTemp.Rows[0]["POTD"].ToString().Equals("") ? "" : dtTemp.Rows[0]["POTD"].ToString();
                            v_HOLDREAS = dtTemp.Rows[0]["HoldReas"].ToString().Equals("") ? "" : dtTemp.Rows[0]["HoldReas"].ToString();
                            v_WAFERLOT = dtTemp.Rows[0]["waferlotid"].ToString().Equals("") ? "" : dtTemp.Rows[0]["waferlotid"].ToString();
                        }

                        if (_infoupdate.Equals(""))
                        {
                            v_FORCE = "false";
                        }
                        else
                        {
                            if (TimerTool("minutes", _infoupdate) >= 3)
                            { v_FORCE = "true"; }
                            else
                            { v_FORCE = "false"; }
                        }
                    }
                    catch (Exception ex)
                    {
                        tmpMsg = string.Format("[TriggerCarrierInfoUpdate: Column Issue. {0}]", ex.Message);
                        _logger.Debug(tmpMsg);
                    }

                    args.Add(v_LOT_ID);
                    args.Add(v_STAGE.Equals("") ? dt.Rows[0]["STAGE"].ToString() : v_STAGE);
                    args.Add("");//("machine");
                    args.Add("");//("desc");
                    args.Add(v_carrier_id);
                    args.Add(v_CUSTOMERNAME);
                    args.Add(v_PARTID);//("PartID");
                    args.Add(v_LOTTYPE);//("LotType");
                    args.Add(v_AUTOMOTIVE);//("Automotive");
                    args.Add(v_STATE);//("State");
                    args.Add(v_HOLDCODE);//("HoldCode");
                    args.Add(v_TURNRATIO);//("TURNRATIO");
                    args.Add(v_EOTD);//("v_EOTD");
                    args.Add(v_HOLDREAS);//("v_HOLDREAS");
                    args.Add(v_POTD);//("v_POTD");
                    args.Add(v_WAFERLOT);//("v_WAFERLOT");
                    args.Add(v_FORCE);//("EOTD");
                    SentCommandtoMCSByModel(_configuration, _logger, "InfoUpdate", args);

                    if (!v_carrier_id.Equals(""))
                    {
                        if (v_FORCE.ToLower().Equals("true"))
                        {
                            sql = _BaseDataService.CarrierTransferDTUpdate(v_carrier_id, "InfoUpdate");
                            _dbTool.SQLExec(sql, out tmpMsg, true);
                        }
                    }
                }

                bResult = true;
            }
            catch (Exception ex)
            {
                bResult = false;
                tmpMsg = string.Format("Send InfoUpdate Fail, Exception: {0}", ex.Message);
                _logger.Debug(tmpMsg);
            }
            finally
            {
                if (dt != null)
                    dt.Dispose();
                if (dtTemp != null)
                    dtTemp.Dispose(); 
            }
            dt = null;
            dtTemp = null;

            return bResult;
        }
        public bool DoInsertPromisStageEquipMatrix(DBTool _dbTool, ILogger _logger, string _stage, string _EqpType, string _EquipIds, string _userId, out string _errMsg)
        {
            bool bResult = false;
            string tmpMsg = "";
            string sql = "";
            DataTable dt = null;
            string[] lstEquip;
            string Equips = "";

            try
            {
                if (!_EquipIds.Equals(""))
                {
                    if (_EquipIds.IndexOf(',') > 0)
                    {
                        lstEquip = _EquipIds.Split(',');

                        foreach (string equipid in lstEquip)
                        {
                            if (Equips.Equals(""))
                            {
                                Equips = string.Format("'{0}'", equipid.Trim());
                            }
                            else
                            {
                                Equips = Equips + string.Format(", '{0}'", equipid.Trim());
                            }
                        }
                    }
                    else
                        Equips = string.Format("'{0}'", _EquipIds.Trim());
                }

                sql = string.Format(_BaseDataService.InsertPromisStageEquipMatrix(_stage, _EqpType, Equips, _userId));
                _dbTool.SQLExec(sql, out tmpMsg, true);
                if (tmpMsg.Equals(""))
                {
                    bResult = true;
                    _errMsg = tmpMsg;
                }
                else
                {
                    bResult = false;
                    _errMsg = tmpMsg;
                    _logger.Debug(tmpMsg);
                }

                _errMsg = tmpMsg;
                bResult = true;
            }
            catch (Exception ex)
            {
                bResult = false;
                tmpMsg = string.Format("Unknow Issue, Exception: {0}", ex.Message);
                _errMsg = tmpMsg;
                _logger.Debug(tmpMsg);
            }
            finally
            {
                if (dt != null)
                    dt.Dispose();              
            }
            dt = null;

            return bResult;
        }
        public bool SyncEQPStatus(DBTool _dbTool, ILogger _logger)
        {
            bool bResult = false;
            string tmpMsg = "";
            string tmpEquipid = "";
            string sql = "";
            string tmpMsg2 = "";
            DataTable dt = null;
            string _machine = "";
            string _current = "";
            string _downstate = "";

#if DEBUG
            //Do Nothing
            bResult = true;
            tmpEquipid = "";
            _machine = "";
            _current = "";
            _downstate = "";
#else
            try
            {
                sql = string.Format(_BaseDataService.CheckRealTimeEQPState());
                dt = _dbTool.GetDataTable(sql);
                if (dt.Rows.Count > 0)
                {
                    //有不同時, 進行同步
                    foreach (DataRow dr in dt.Rows)
                    {
                        tmpEquipid = dr["equipid"].ToString().Trim();
                        _machine = dr["equipid"].ToString().Trim();
                        _current = dr["equipid"].ToString().Trim();
                        _downstate = dr["equipid"].ToString().Trim();

                        try
                        {
                            tmpMsg2 = string.Format("Sync EQ[{0}] State from RTS: Machine[{1}], Current[{2}], Down[{3}]", tmpEquipid, _machine, _current, _downstate);
                            _logger.Info(tmpMsg2);

                            _dbTool.SQLExec(_BaseDataService.UpdateCurrentEQPStateByEquipid(tmpEquipid), out tmpMsg, true);
                        }
                        catch (Exception ex)
                        { }
                    }
                }

                bResult = true;
            }
            catch (Exception ex)
            {
            }
            finally
            {
                if (dt != null)
                    dt.Dispose(); 
            }
             dt = null;  
#endif

            return bResult;
        }
        public bool RecoverDatabase(DBTool _dbTool, ILogger _logger, string _message)
        {
            string tmpMsg;
            string tmp2Msg;

            if (_message.IndexOf("ORA-03150") > 0 || _message.IndexOf("ORA-02063") > 0 || _message.IndexOf("ORA-12614") > 0
                || _message.IndexOf("Object reference not set to an instance of an object") > 0
                || _message.IndexOf("Database access problem") > 0)
            {
                //Jcet database connection exception occurs, the connection will automatically re-established 
                tmpMsg = "";
                tmp2Msg = "";

                if (_dbTool.dbPool.CheckConnet(out tmpMsg))
                {
                    _dbTool.DisConnectDB(out tmp2Msg);

                    if (!tmp2Msg.Equals(""))
                    {
                        _logger.Debug(string.Format("DB disconnect failed [{0}]", tmp2Msg));
                    }
                    else
                    {
                        _logger.Debug(string.Format("Database disconect."));
                    }

                    if (!_dbTool.IsConnected)
                    {
                        _logger.Debug(string.Format("Database re-established."));
                        _dbTool.ConnectDB(out tmp2Msg);
                    }
                }
                else
                {
                    if (!_dbTool.IsConnected)
                    {
                        string[] _argvs = new string[] { "", "", "", "" };
                        if (CallRTDAlarm(_dbTool, 20100, _argvs))
                        {
                            _logger.Debug(string.Format("Database re-established."));
                            _dbTool.ConnectDB(out tmp2Msg);
                        }
                    }
                }

                if (!tmp2Msg.Equals(""))
                {
                    _logger.Debug(string.Format("DB re-established failed [{0}]", tmp2Msg));
                }
                else
                {
                    string[] _argvs = new string[] { "", "", "", ""};
                    if (CallRTDAlarm(_dbTool, 20101, _argvs))
                    {
                        _logger.Debug(string.Format("DB re-connection sucess", tmp2Msg));
                    }
                }
            }
            return true;
        }
        public bool SyncExtenalCarrier(DBTool _dbTool, IConfiguration _configuration, ILogger _logger)
        {
            bool bResult = false;
            string tmpMsg = "";
            string tmpEquipid = "";
            string sql = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataTable dtTemp2 = null;
            string dblinkLcas = "";
            bool enableSync = false;
            bool isTable = false;

            try
            {
                if (_configuration["SyncExtenalData:SyncCST:Model"].Equals("Table"))
                    isTable = true;

                if (_configuration["SyncExtenalData:SyncCST:Enable"].Equals("True"))
                    enableSync = true;

                if (enableSync)
                {
                    if (isTable)
                    {
#if DEBUG
                        dblinkLcas = _configuration["SyncExtenalData:SyncCST:Table:Debug"];
#else
                        dblinkLcas = _configuration["SyncExtenalData:SyncCST:Table:Prod"];
#endif
                        sql = string.Format(_BaseDataService.QueryExtenalCarrierInfo(dblinkLcas));
                    }
                    else
                    {
                        sql = "get sql from sql file";
                    }
                }
                else
                { return true; }

                dt = _dbTool.GetDataTable(sql);
                if (dt.Rows.Count > 0)
                {
                    CarrierLotAssociate carrierLotAssociate = new CarrierLotAssociate();

                    string dateTime = "";
                    string carrierfeature = "";
                    string carrierType = "";
                    //有不同時, 進行同步
                    foreach (DataRow dr in dt.Rows)
                    {
                        carrierfeature = "";
                        carrierType = "";

                        try
                        {
                            carrierLotAssociate = new CarrierLotAssociate();

                            dateTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                            carrierLotAssociate.CarrierID = dr["CSTID"].ToString().Trim();
                            carrierLotAssociate.TagType = "LF";
                            carrierfeature = carrierLotAssociate.CarrierID.Substring(0, 4);

                            //if (carrierfeature.Equals("EW12") || carrierfeature.Equals("EW13"))
                            //    carrierType = "HD";
                            //else if (carrierfeature.Equals("12SL") || carrierfeature.Equals("13SL") || carrierfeature.Equals("25SL"))
                            //    carrierType = "Foup";
                            //else if (carrierfeature.Equals("EWLB"))
                            //    carrierType = "HD"; //20240205 Foup to HD
                            //else if (carrierLotAssociate.CarrierID.Substring(0, 3).Equals("EFA"))
                            //    carrierType = "Foup";
                            //else
                            //    carrierType = "";

                            if (carrierfeature.Equals("EW12"))
                                carrierType = "EW12";
                            else if (carrierfeature.Equals("EW13"))
                                carrierType = "EW13";
                            else if (carrierfeature.Equals("12SL"))
                                carrierType = "12SL";
                            else if (carrierfeature.Equals("13SL"))
                                carrierType = "13SL";
                            else if (carrierfeature.Equals("25SL"))
                                carrierType = "25SL";
                            else if (carrierfeature.Equals("EWLB"))
                                carrierType = "HD"; //20240205 Foup to HD
                            else if (carrierLotAssociate.CarrierID.Substring(0, 3).Equals("EFA"))
                                carrierType = "Foup";
                            else
                                carrierType = "";

                            carrierLotAssociate.CarrierType = carrierType; //dr["CSTTYPE"].ToString().Trim();
                            carrierLotAssociate.AssociateState = "Associated With Lot";
                            carrierLotAssociate.ChangeStateTime = "";
                            carrierLotAssociate.LotID = dr["LOTID"].ToString().Trim();
                            carrierLotAssociate.Quantity = dr["LOTQTY"].ToString().Trim();
                            carrierLotAssociate.ChangeStation = "SyncEwlbCarrier";
                            carrierLotAssociate.ChangeStationType = "A";
                            carrierLotAssociate.UpdateTime = dateTime;
                            carrierLotAssociate.UpdateBy = "RTD";
                            carrierLotAssociate.CreateBy = dr["USERID"].ToString().Trim();
                            carrierLotAssociate.NewBind = "1";

                            int doLogic = 0;

                            dtTemp = _dbTool.GetDataTable(_BaseDataService.QueryCarrierInfoByCarrierId(carrierLotAssociate.CarrierID));
                            if (dtTemp.Rows.Count <= 0)
                            {
                                doLogic = 1;
                            }
                            else
                            {
                                if (!carrierLotAssociate.LotID.Equals(dtTemp.Rows[0]["lot_id"].ToString()))
                                {
                                    doLogic = 2;
                                }
                                else if (!carrierLotAssociate.Quantity.Equals(dtTemp.Rows[0]["quantity"].ToString()))
                                {
                                    doLogic = 2;
                                }
                                else
                                {
                                    doLogic = 0;
                                }

                            }

                            try {
                                if (doLogic.Equals(1) || doLogic.Equals(2))
                                {
                                    dtTemp2 = _dbTool.GetDataTable(_BaseDataService.QueryLCASInfoByLotID(carrierLotAssociate.LotID));

                                    if (dtTemp2.Rows.Count > 0)
                                    {
                                        CarrierLotAssociate _carrierLotAssociate = new CarrierLotAssociate();
                                        foreach (DataRow dr2 in dtTemp2.Rows)
                                        {
                                            try
                                            {
                                                _carrierLotAssociate = new CarrierLotAssociate();
                                                _carrierLotAssociate.CarrierType = dr2["Carrier_Type"].ToString().Trim();
                                                _carrierLotAssociate.CarrierID = dr2["Carrier_id"].ToString().Trim();
                                                _carrierLotAssociate.AssociateState = dr2["Associate_state"].ToString().Trim();
                                                _carrierLotAssociate.ChangeStateTime = dr2["Change_state_Time"].ToString().Trim();
                                                _carrierLotAssociate.LotID = dr2["LOT_ID"].ToString().Trim();
                                                _carrierLotAssociate.Quantity = dr2["Quantity"].ToString().Trim();
                                                _carrierLotAssociate.ChangeStation = dr2["Change_station"].ToString().Trim();
                                                _carrierLotAssociate.ChangeStationType = dr2["Change_Station_Type"].ToString().Trim(); ;
                                                _carrierLotAssociate.UpdateTime = dateTime;
                                                _carrierLotAssociate.UpdateBy = "RTD";
                                                _carrierLotAssociate.CreateBy = carrierLotAssociate.CreateBy;
                                                _carrierLotAssociate.NewBind = dr2["New_Bind"].ToString().Trim();

                                                _logger.Debug(string.Format("[SyncCST][Unbind][Carrier ID: {0}], Lot ID: {1}", _carrierLotAssociate.CarrierID, _carrierLotAssociate.LotID));

                                                _dbTool.SQLExec(_BaseDataService.UnbindLot(_carrierLotAssociate), out tmpMsg, true);

                                                if(!tmpMsg.Equals(""))
                                                    _logger.Debug(string.Format("[SyncCST][Unbind][Carrier ID: {0}, Failed: {1}", _carrierLotAssociate.CarrierID, tmpMsg));
                                            }
                                            catch (Exception ex) { }
                                        }
                                    }
                                }
                            }
                            catch(Exception ex) { }

                            if (doLogic.Equals(1))
                            {
                                tmpMsg = "";
                                _dbTool.SQLExec(_BaseDataService.InsertCarrierLotAsso(carrierLotAssociate), out tmpMsg, true);
                                if (!tmpMsg.Equals(""))
                                    _logger.Debug(string.Format("[InsertCarrierLotAsso Failed.][Exception: {0}][Carrier ID: {1}]", tmpMsg, carrierLotAssociate.CarrierID));
                                tmpMsg = "";
                                _dbTool.SQLExec(_BaseDataService.InsertCarrierTransfer(carrierLotAssociate.CarrierID, carrierType, carrierLotAssociate.Quantity), out tmpMsg, true);
                                if (!tmpMsg.Equals(""))
                                    _logger.Debug(string.Format("[InsertCarrierTransfer Failed.][Exception: {0}][Carrier ID: {1}]", tmpMsg, carrierLotAssociate.CarrierID));

                                _logger.Debug(string.Format("[SyncCST][{0}][Carrier ID: {1}], Lot ID: {2}", doLogic, carrierLotAssociate.CarrierID, carrierLotAssociate.LotID));
                            }
                            else if (doLogic.Equals(2))
                            {
                                tmpMsg = "";
                                _dbTool.SQLExec(_BaseDataService.UpdateLastCarrierLotAsso(carrierLotAssociate), out tmpMsg, true);
                                if (!tmpMsg.Equals(""))
                                    _logger.Debug(string.Format("[UpdateLastCarrierLotAsso Failed.][Exception: {0}][Carrier ID: {1}]", tmpMsg, carrierLotAssociate.CarrierID));
                                tmpMsg = "";
                                _dbTool.SQLExec(_BaseDataService.UpdateCarrierLotAsso(carrierLotAssociate), out tmpMsg, true);
                                if (!tmpMsg.Equals(""))
                                    _logger.Debug(string.Format("[UpdateCarrierLotAsso Failed.][Exception: {0}][Carrier ID: {1}]", tmpMsg, carrierLotAssociate.CarrierID));
                                tmpMsg = "";
                                _dbTool.SQLExec(_BaseDataService.UpdateCarrierTransfer(carrierLotAssociate.CarrierID, carrierType, carrierLotAssociate.Quantity), out tmpMsg, true);
                                if (!tmpMsg.Equals(""))
                                    _logger.Debug(string.Format("[UpdateCarrierTransfer Failed.][Exception: {0}][Carrier ID: {1}]", tmpMsg, carrierLotAssociate.CarrierID));

                                _logger.Debug(string.Format("[SyncCST][{0}][Carrier ID: {1}], Lot ID: {2}", doLogic, carrierLotAssociate.CarrierID, carrierLotAssociate.LotID));
                            }
                        }
                        catch (Exception ex)
                        { }
                    }
                }

                bResult = true;
            }
            catch (Exception ex)
            {
                _logger.Debug(string.Format("Sync Extenal Carrier Data Failed. [Exception: {0}]", ex.Message));
            }
            finally
            {
                if (dt != null)
                    dt.Dispose();
                if (dtTemp != null)
                    dtTemp.Dispose();
            }
            dt = null;
            dtTemp = null;

            return bResult;
        }
        public string GetExtenalTables(IConfiguration _configuration, string _method, string _func)
        {
            string strTable = "";
            bool enableSync = false;
            bool isTable = false;
            string cfgString = "";

            try
            {
                cfgString = string.Format("{0}:{1}:Model", _method, _func);
                if (_configuration[cfgString].Equals("Table"))
                    isTable = true;

                cfgString = string.Format("{0}:{1}:Enable", _method, _func);
                if (_configuration[cfgString].Equals("True"))
                    enableSync = true;

                if (enableSync)
                {
                    if (isTable)
                    {
#if DEBUG
                        cfgString = string.Format("{0}:{1}:Table:Debug", _method, _func);
#else
                        cfgString = string.Format("{0}:{1}:Table:Prod", _method, _func);
#endif
                        strTable = _configuration[cfgString];
                    }
                }
                else
                {

#if DEBUG
                    strTable = "ADS_INFO";
#else
                    strTable = "semi_int.rtd_cis_ads_vw@SEMI_INT";
#endif
                }
            }
            catch (Exception ex)
            { }

            return strTable;
        }

        //Equipment State===========================
        //0. DOWN (disable)
        //1. PM (Error)
        //2. IDLE 
        //3. UP (Run)
        //12. IDLE With Warinning
        //13. UP With Warning
        //====================================
        public string GetEquipStat(int _equipState)
        {
            string eqState = "DOWN";

            try
            {
                switch (_equipState)
                {
                    case 1:
                        eqState = "PM";
                        break;
                    case 2:
                        eqState = "IDLE";
                        break;
                    case 3:
                        eqState = "UP";
                        break;
                    case 12:
                        eqState = "IDLE With Warning";
                        break;
                    case 13:
                        eqState = "UP With Warning";
                        break;
                    case 0:
                    default:
                        eqState = "DOWN";
                        break;
                }
            }
            catch (Exception ex)
            { }

            return eqState;
        }
        public Global loadGlobalParams(DBTool _dbTool)
        {
            Global _global = new Global();
            string sql = "";
            DataTable dtTemp = null;

            try
            {
                sql = _BaseDataService.SelectRTDDefaultSetByType("GlobalParams");
                dtTemp = _dbTool.GetDataTable(sql);
                //Default
                _global.CheckQueryAvailableTestercuteMode.Time = 60;
                _global.CheckQueryAvailableTestercuteMode.TimeUnit = "seconds";
                _global.ChkLotInfo.Time = 60;
                _global.ChkLotInfo.TimeUnit = "seconds";

                //read from setting
                if (dtTemp.Rows.Count > 0)
                {
                    foreach(DataRow dr in dtTemp.Rows)
                    {
                        if(dr["Parameter"].Equals("CheckLotInfo.Time"))
                        {
                            _global.ChkLotInfo.Time = int.Parse(dr["ParamValue"].ToString());
                        }
                        if (dr["Parameter"].Equals("CheckLotInfo.TimeUnit"))
                        {
                            _global.ChkLotInfo.TimeUnit = dr["ParamValue"].ToString();
                        }
                        if (dr["Parameter"].Equals("CheckQueryAvailableTestercute.Time"))
                        {
                            _global.CheckQueryAvailableTestercuteMode.Time = int.Parse(dr["ParamValue"].ToString());
                        }
                        if (dr["Parameter"].Equals("CheckQueryAvailableTestercute.TimeUnit"))
                        {
                            _global.CheckQueryAvailableTestercuteMode.TimeUnit = dr["ParamValue"].ToString();
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                
            }
            finally
            {
                if (dtTemp != null)
                    dtTemp.Dispose(); 
            }
            dtTemp = null;

            return _global;
        }
        public bool PreDispatchToErack(DBTool _dbTool, IConfiguration _configuration, ConcurrentQueue<EventQueue> _eventQueue, ILogger _logger)
        {
            bool bResult = false;
            string tmpMsg = "";
            string sql = "";
            DataTable dtTemp = null;
            DataTable dtTemp2 = null;
            DataTable dtTemp3 = null;
            DataTable dtTemp4 = null;
            EventQueue _eventQ = new EventQueue();
            string funcName = "MoveCarrier";
            TransferList transferList = new TransferList();
            string tableName = "";
            string tableOrder = "";
            string carrierId = "";
            string _keyOfEnv = "";
            string _keyRTDEnv = "";

            string _targetpoint = "";
            string _currentLocate = "";
            string _destStage = "";
            string _eqpWorkgroup = "";
            string _lotID = "";
            string _adsTable = "";

            string _adsStage = "";
            string _adsPkg = "";

            string _sideWarehouse = "";
            bool _swSideWh = false;
            bool _onSideWH = false;

            try
            {
                _keyRTDEnv = _configuration["RTDEnvironment:type"];
                _keyOfEnv = string.Format("RTDEnvironment:commandsTable:{0}", _keyRTDEnv);
                //"RTDEnvironment:commandsTable:PROD"
                tableOrder = _configuration[_keyOfEnv] is null ? "workinprocess_sch" : _configuration[_keyOfEnv];
                tableName = _configuration["PreDispatchToErack:lotState:tableName"]　is null ? "lot_Info" : _configuration["PreDispatchToErack:lotState:tableName"];

                _adsTable = _configuration["CheckLotStage:Table"] is null ? "lot_Info" : _configuration["CheckLotStage:Table"];

                if (_keyRTDEnv.ToUpper().Equals("PROD"))
                    sql = _BaseDataService.QueryPreTransferList(tableName);
                else if (_keyRTDEnv.ToUpper().Equals("UAT"))
                    sql = _BaseDataService.QueryPreTransferListForUat(tableName);
                else
                    _logger.Debug(string.Format("RTDEnvironment setting failed. current set is [{0}]", _keyRTDEnv));

                dtTemp = _dbTool.GetDataTable(sql);

                if (dtTemp.Rows.Count > 0)
                {
                    foreach (DataRow dr in dtTemp.Rows)
                    {

                        ///check is lot locate in SideWarehouse
                        /////workgroup , stage, SideWarehouse

                        transferList = new TransferList();
                        carrierId = "";
                        sql = _BaseDataService.CheckCarrierLocate(dr["in_eRack"].ToString(), dr["locate"].ToString());
                        dtTemp2 = _dbTool.GetDataTable(sql);

                        if (dtTemp2.Rows.Count <= 0)
                        {
                            carrierId = dr["carrier_ID"].ToString().Equals("") ? "*" : dr["carrier_ID"].ToString();
                            sql = _BaseDataService.CheckPreTransfer(carrierId, tableOrder);
                            dtTemp3 = _dbTool.GetDataTable(sql);
                            if (dtTemp3.Rows.Count > 0)
                                continue;

                            //20240202 Add midways logic for pre-transfer
                            _eqpWorkgroup = dr["workgroup"].ToString().Equals("") ? "" : dr["workgroup"].ToString();
                            _lotID = dr["lot_ID"].ToString().Equals("") ? "*" : dr["lot_ID"].ToString();

                            sql = _BaseDataService.GetMidwayPoint(_adsTable, _eqpWorkgroup, _lotID);
                            try {
                                dtTemp3 = null;
                                dtTemp3 = _dbTool.GetDataTable(sql);
                            }
                            catch (Exception ex) { }

                            if (dtTemp3 is not null && dtTemp3.Rows.Count > 0)
                            {
                                _targetpoint = dtTemp3.Rows[0]["midway_point"].ToString();
                                _logger.Info(string.Format("Midways logic: lotID [{0}], workgroup [{1}], midway point [{2}]", _lotID, _eqpWorkgroup, _targetpoint));
                            }
                            else
                            {
                                //直接到終點
                                _targetpoint = dr["in_eRack"].ToString();
                            }

                            try {
                                _destStage = dr["stage"].ToString();

                                sql = _BaseDataService.QueryWorkgroupSet(_eqpWorkgroup, _destStage);
                                dtTemp3 = _dbTool.GetDataTable(sql);
                                if(dtTemp3.Rows.Count>0)
                                {
                                    _sideWarehouse = dtTemp3.Rows[0]["SideWarehouse"].ToString();
                                    _swSideWh = dtTemp3.Rows[0]["swsidewh"].ToString().Equals("1") ? true : false;
                                }

                                if (_swSideWh)
                                {
                                    sql = _BaseDataService.CheckLocateofSideWh(dr["locate"].ToString(), _sideWarehouse);
                                    dtTemp3 = _dbTool.GetDataTable(sql);
                                    if (dtTemp3.Rows.Count > 0)
                                    {
                                        _onSideWH = true;
                                    }
                                    else
                                    {
                                        _onSideWH = false;
                                    }
                                }
                            }
                            catch(Exception ex) { }

                            if (_onSideWH)
                            { continue; }
                            else
                            {

                                _eventQ = new EventQueue();
                                _eventQ.EventName = funcName;

                                transferList.CarrierID = carrierId;
                                transferList.LotID = dr["lot_ID"].ToString().Equals("") ? "*" : dr["lot_ID"].ToString();
                                transferList.Source = "*";
                                transferList.Dest = _targetpoint;
                                transferList.CommandType = "Pre-Transfer";
                                transferList.CarrierType = dr["CarrierType"].ToString();

                                try
                                {
                                    _adsStage = "";
                                    _adsPkg = "";
                                    tmpMsg = "";
                                    dtTemp4 = null;
                                    sql = _BaseDataService.QueryDataByLot(tableName, _lotID);
                                    dtTemp4 = _dbTool.GetDataTable(sql);

                                    if (dtTemp4.Rows.Count > 0)
                                    {
                                        _adsStage = dtTemp4.Rows[0]["stage"].ToString();
                                        _adsPkg = dtTemp4.Rows[0]["pkgfullname"].ToString();

                                        //log ads information for debug 20240313
                                        tmpMsg = string.Format("[{0}][{1}][{2}][{3}][{4}][ADS: {5} / {6}]", "Pre-Transfer", transferList.LotID, transferList.CarrierID, dr["locate"].ToString(), transferList.Dest, _adsStage, _adsPkg);
                                    }
                                    else
                                    {
                                        tmpMsg = string.Format("[{0}][{1}][{2}][{3}][{4}][ADS: No Data]", "Pre-Transfer", transferList.LotID, transferList.CarrierID, dr["locate"].ToString(), transferList.Dest);
                                    }
                                    _logger.Info(tmpMsg);
                                }
                                catch (Exception ex)
                                {
                                    tmpMsg = string.Format("[{0}][{1}][{2}][{3}]", "Exception", "Pre-Transfer", transferList.LotID, dr["locate"].ToString());
                                    _logger.Info(tmpMsg);
                                }

                                tmpMsg = string.Format("[{0}][{1} / {2} / {3} / {4} / {5}]", transferList.CommandType, transferList.LotID, transferList.CarrierID, transferList.Source, transferList.Dest, transferList.CarrierType);
                                _eventQ.EventObject = transferList;
                                _eventQueue.Enqueue(_eventQ);
                            }
                        }
                    }
                }

                bResult = true;
            }
            catch (Exception ex)
            {
                _logger.Debug(string.Format("PreDispatchToErack Unknow Error. [Exception: {0}]", ex.Message));
            }
            finally
            {
                if (dtTemp != null)
                    dtTemp.Dispose();
                if (dtTemp2 != null)
                    dtTemp2.Dispose();
                if (dtTemp3 != null)
                    dtTemp3.Dispose();
            }
            dtTemp = null;
            dtTemp2 = null;
            dtTemp3 = null;

            return bResult;
        }
        public DataTable GetLotInfo(DBTool _dbTool, string _department, ILogger _logger)
        {
            string funcName = "GetLotInfo";
            string tmpMsg = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";

            try
            {

                //// 查詢資料
                dt = _dbTool.GetDataTable(_BaseDataService.SelectTableLotInfoByDept(_department));
                dr = dt.Select();
                if (dt.Rows.Count > 0)
                {
                    return dt;
                }
            }
            catch (Exception ex)
            {
                tmpMsg = string.Format("Function:{0}, Received:[{1}]", funcName, ex.Message);
                _logger.Debug(tmpMsg);

                return null;
            }
            finally
            {
                
            }
            dr = null;

            return null;
        }
        public bool CarrierLocationUpdate(DBTool _dbTool, IConfiguration _configuration, CarrierLocationUpdate value, ILogger _logger)
        {
            string funcName = "CarrierLocationUpdate";
            string tmpMsg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";

            try
            {
                var jsonStringName = new JavaScriptSerializer();
                var jsonStringResult = jsonStringName.Serialize(value);
                _logger.Info(string.Format("Function:{0}, Received:[{1}]", funcName, jsonStringResult));

                //// 查詢資料
                dt = _dbTool.GetDataTable(_BaseDataService.SelectTableCarrierTransferByCarrier(value.CarrierID.Trim()));
                dr = dt.Select();
                if (dt.Rows.Count > 0)
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
                        if (dtTemp.Rows.Count > 0)
                        {
                            if (dtTemp.Rows[0]["isLock"].ToString().Equals("1"))
                            {
                                sql = String.Format(_BaseDataService.UnLockLotInfoWhenReady(dtTemp.Rows[0]["lot_id"].ToString()));
                                _dbTool.SQLExec(sql, out tmpMsg, true);
                            }
                        }
                    }
                }
                else { 
                    return false; 
                }
            }
            catch (Exception ex)
            {
                tmpMsg = string.Format("Function:{0}, Received:[{1}]", funcName, ex.Message);
                _logger.Debug(tmpMsg);

                return false;
            }
            finally
            {
                if (dt != null)
                    dt.Dispose();
                if (dtTemp != null)
                    dtTemp.Dispose();
            }
            dt = null;
            dtTemp = null;
            dr = null;

            return true;
        }
        public bool CommandStatusUpdate(DBTool _dbTool, IConfiguration _configuration, CommandStatusUpdate value, ILogger _logger)
        {
            string funcName = "CommandStatusUpdate";
            string tmpMsg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            bool bExecSql = false;
            int FailedNum = 0;
            string tableOrder = "";
            string _keyOfEnv = "";
            string _keyRTDEnv = "";

            try
            {
                _keyRTDEnv = _configuration["RTDEnvironment:type"];
                _keyOfEnv = string.Format("RTDEnvironment:commandsTable:{0}", _keyRTDEnv);
                //"RTDEnvironment:commandsTable:PROD"
                tableOrder = _configuration[_keyOfEnv] is null ? "workinprocess_sch" : _configuration[_keyOfEnv];

                var jsonStringName = new JavaScriptSerializer();
                var jsonStringResult = jsonStringName.Serialize(value);
                _logger.Info(string.Format("Function:{0}, Received:[{1}]", funcName, jsonStringResult));

                _dbTool.SQLExec(_BaseDataService.UpdateTableWorkInProcessSchHisByCmdId(value.CommandID.Trim()), out tmpMsg, true);

                while (true)
                {
                    try
                    {
                        sql = _BaseDataService.SelectTableWorkInProcessSchByCmdId(value.CommandID, tableOrder);
                        dt = _dbTool.GetDataTable(sql);

                        if (dt.Rows.Count > 0)
                        {
                            //if (!dt.Rows[0]["cmd_type"].ToString().Equals("Pre-Transfer"))
                            //{
                                bExecSql = _dbTool.SQLExec(_BaseDataService.UpdateTableWorkInProcessSchByCmdId(value.Status, value.LastStateTime, value.CommandID.Trim(), tableOrder), out tmpMsg, true);

                                if (bExecSql)
                                    break;
                            //}
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
                    return false;
                }
            }
            catch (Exception ex)
            {
                tmpMsg = string.Format("Function:{0}, Received:[{1}]", funcName, ex.Message);
                _logger.Debug(tmpMsg);

                return false;
            }
            finally
            {
                if (dt != null)
                    dt.Dispose();
                if (dtTemp != null)
                    dtTemp.Dispose();
            }
            dt = null;
            dtTemp = null;
            dr = null;

            return true;
        }
        public bool EquipmentPortStatusUpdate(DBTool _dbTool, IConfiguration _configuration, AEIPortInfo value, ILogger _logger)
        {
            string funcName = "EquipmentPortStatusUpdate";
            string tmpMsg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string _lastLot = "";

            try
            {
                var jsonStringName = new JavaScriptSerializer();
                var jsonStringResult = jsonStringName.Serialize(value);
                _logger.Info(string.Format("Function:{0}, Received:[{1}]", funcName, jsonStringResult));

                /// 查詢資料
                sql = string.Format(_BaseDataService.SelectTableEQP_Port_SetByPortId(value.PortID)) ;
                dt = _dbTool.GetDataTable(sql);
                string tCondition = string.Format("Port_Seq = {0}", value.PortID);
                dr = dt.Select("");
                
                if (dt.Rows.Count > 0)
                {
                    if (!dr[0]["Port_State"].ToString().Equals(value.PortTransferState))
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

                        return true;

                    }
                }
            }
            catch (Exception ex)
            {
                tmpMsg = string.Format("Function:{0}, Received:[{1}]", funcName, ex.Message);
                _logger.Debug(tmpMsg);

                return false;
            }
            finally
            {
                if (dt != null)
                    dt.Dispose();
                if (dtTemp != null)
                    dtTemp.Dispose();
            }
            dt = null;
            dtTemp = null;
            dr = null;

            return true;
        }
        public bool EquipmentStatusUpdate(DBTool _dbTool, IConfiguration _configuration, AEIEQInfo value, ILogger _logger)
        {
            string funcName = "EquipmentStatusUpdate";
            string tmpMsg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string eqState = "";
            Boolean _isDisabled = false;

            try
            {
                var jsonStringName = new JavaScriptSerializer();
                var jsonStringResult = jsonStringName.Serialize(value);
                _logger.Info(string.Format("Function:{0}, Received:[{1}]", funcName, jsonStringResult));

                eqState = GetEquipStat(value.EqState);

                foreach(var strPort in value.PortInfoList)
                {
                    //SelectTableEQP_Port_SetByPortId
                    sql = string.Format(_BaseDataService.SelectTableEQP_Port_SetByPortId(strPort.PortID));
                    dtTemp = _dbTool.GetDataTable(sql);
                    if (dtTemp.Rows.Count > 0)
                    {
                        if (dtTemp.Rows[0]["disable"].ToString().Equals("0"))
                        {
                            int _portstate = dtTemp.Rows[0]["port_state"] is null ? 0 : int.Parse(dtTemp.Rows[0]["port_state"].ToString());//port_state
                            if (!strPort.PortTransferState.Equals(_portstate))
                            {
                                //update port
                                //UpdateTableEQP_Port_Set
                                _dbTool.SQLExec(_BaseDataService.UpdateTableEQP_Port_Set(value.EqID, strPort.PortID.Split("_LP")[1].ToString(), strPort.PortTransferState.ToString()), out tmpMsg, true);
                            }
                        }
                    }
                }
                //// 查詢資料
                sql = string.Format(_BaseDataService.SelectTableEQP_STATUSByEquipId(value.EqID));
                dt = _dbTool.GetDataTable(sql);
                dr = dt.Select();

                if (dt.Rows.Count > 0)
                {
                    if (!dt.Rows[0]["Curr_Status"].ToString().Equals(eqState))
                    {
                        sql = string.Format(_BaseDataService.UpdateTableEQP_STATUS(value.EqID, eqState));
                        _dbTool.SQLExec(sql, out tmpMsg, true);

                        if (SyncEQPStatus(_dbTool, _logger))
                        {
                            //Do Nothing.
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                tmpMsg = string.Format("Function:{0}, Received:[{1}]", funcName, ex.Message);
                _logger.Debug(tmpMsg);

                return false;
            }
            finally
            {
                if (dt != null)
                    dt.Dispose();
                if (dtTemp != null)
                    dtTemp.Dispose(); 
            }
            dt = null;
            dtTemp = null;
            dr = null;

            return true;
        }
        public bool NightOrDay(string _currDateTime)
        {
            bool _Night = false;

            try
            {
                if (_currDateTime.Equals(""))
                    return _Night;

                //將時間轉換為DateTime
                DateTime date = Convert.ToDateTime(_currDateTime);

                //處於開始時間和結束時間的判斷式 //目前班別為早8晚8為日班/晚8早8為夜班
                if (date.Hour >= 20 || date.Hour < 8)
                {
                    _Night = true;
                }
                else
                {
                    _Night = false;
                }
            }
            catch(Exception ex)
            {

            }

            return _Night;
        }
        public List<HisCommandStatus> GetHistoryCommands(DBTool _dbTool, Dictionary<string, string> _alarmDetail, string StartDateTime, string CurrentDateTime, string Unit, string Zone)
        {
            List<HisCommandStatus> foo;
            string funcName = "GetHistoryCommands";
            string tmpMsg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();
            foo = new List<HisCommandStatus>();
            DateTime dtStart;
            DateTime dtEnd;
            double dHour = 0;
            double dunit = 1;
            DateTime dtStartUTCTime;
            DateTime dtCurrUTCTime;
            DateTime dtLocStartTime;
            DateTime dtLocTime;
            bool isNightShift = false;

            try
            {
                if (Zone.Contains("-"))
                {
                    dunit = 1;
                    dHour = dunit * double.Parse(Zone.Replace("-", ""));
                }
                else if (Zone.Contains("+"))
                {
                    dunit = -1;
                    dHour = dunit * double.Parse(Zone.Replace("+", ""));
                }
                else
                {
                    dunit = -1;
                    dHour = dunit * double.Parse(Zone);
                }

                if (CurrentDateTime.Equals(""))
                {
                    dtLocStartTime = DateTime.Now;
                    dtLocTime = DateTime.Now;
                    dtStartUTCTime = DateTime.Now;
                    dtCurrUTCTime = DateTime.Now;
                }
                else
                {
                    dtLocTime = DateTime.Parse(CurrentDateTime);
                    dtCurrUTCTime = DateTime.Parse(CurrentDateTime).AddHours(dHour);

                    if (StartDateTime is null || StartDateTime.Equals(""))
                    {
                        dtLocStartTime = dtLocTime;
                        dtStartUTCTime = DateTime.Parse(CurrentDateTime).AddHours(dHour);
                    }
                    else
                    {
                        dtLocStartTime = DateTime.Parse(StartDateTime);
                        dtStartUTCTime = DateTime.Parse(StartDateTime).AddHours(dHour);
                    }
                }

                isNightShift = NightOrDay(CurrentDateTime);

                switch(Unit.ToUpper())
                {
                    case "YEAR":
                            dtStart = DateTime.Parse(dtLocStartTime.ToString("yyyy-MM-dd ") + " 00:00").AddHours(dHour);
                            dtEnd = DateTime.Parse(dtLocTime.AddDays(1).ToString("yyyy-MM-dd ") + " 00:00").AddHours(dHour);
                        break;
                    case "MONTH":
                            dtStart = DateTime.Parse(dtLocStartTime.ToString("yyyy-MM-dd ") + " 00:00").AddHours(dHour);
                            dtEnd = DateTime.Parse(dtLocTime.AddDays(1).ToString("yyyy-MM-dd ") + " 00:00").AddHours(dHour);
                        break;
                    case "WEEK":
                            dtStart = DateTime.Parse(dtLocStartTime.ToString("yyyy-MM-dd ") + " 00:00").AddHours(dHour);
                        dtEnd = DateTime.Parse(dtLocTime.AddDays(1).ToString("yyyy-MM-dd ") + " 00:00").AddHours(dHour);
                        break;
                    case "SHIFT":

                        DateTime date = Convert.ToDateTime(CurrentDateTime);

                        if (isNightShift)
                        {
                            if(date.Hour < 8)
                            {
                                dtStart = DateTime.Parse(dtLocStartTime.AddDays(-1).ToString("yyyy-MM-dd ") + " 20:00").AddHours(dHour);
                                dtEnd = DateTime.Parse(dtLocTime.ToString("yyyy-MM-dd ") + " 08:00").AddHours(dHour);
                            }
                            else
                            {
                                dtStart = DateTime.Parse(dtLocStartTime.ToString("yyyy-MM-dd ") + " 20:00").AddHours(dHour);
                                dtEnd = DateTime.Parse(dtLocTime.AddDays(1).ToString("yyyy-MM-dd ") + " 08:00").AddHours(dHour);
                            }
                        }
                        else
                        {
                            dtStart = DateTime.Parse(dtLocStartTime.ToString("yyyy-MM-dd ") + " 08:00").AddHours(dHour);
                            dtEnd = DateTime.Parse(dtLocTime.ToString("yyyy-MM-dd ") + " 20:00").AddHours(dHour);
                        }
                        break;
                    case "DAY":
                    default:

                        dtStart = DateTime.Parse(dtLocStartTime.ToString("yyyy-MM-dd ") + " 00:00").AddHours(dHour);
                        dtEnd = DateTime.Parse(dtLocTime.AddDays(1).ToString("yyyy-MM-dd ") + " 00:00").AddHours(dHour);

                        break;
                }

                sql = _BaseDataService.GetHistoryCommands(dtStart.ToString("yyyy/MM/dd HH:mm:ss"), dtEnd.ToString("yyyy/MM/dd HH:mm:ss"));
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    HisCommandStatus HisCommand;
                    string sReason = "";
                    foreach (DataRow row in dt.Rows)
                    {
                        //"CommandID", "CarrierID", "LotID", "CommandType", "Source", "Dest", "AlarmCode", "Reason", "createdAt", "LastStateTime"
                        HisCommand = new HisCommandStatus();
                        HisCommand.CommandID = row["CommandID"].ToString();
                        HisCommand.CarrierID = row["CarrierID"].ToString();
                        HisCommand.LotID = row["LotID"].ToString();
                        HisCommand.CommandType = row["CommandType"].ToString();
                        HisCommand.Source = row["Source"].ToString();
                        HisCommand.Dest = row["Dest"].ToString();
                        try
                        { 
                            sReason = _alarmDetail[row["AlarmCode"].ToString()] is null ? "" : _alarmDetail[row["AlarmCode"].ToString()];
                        }
                        catch (Exception ex) { }
                        HisCommand.AlarmCode = row["AlarmCode"].ToString();
                        HisCommand.Reason = sReason;
                        DateTime dtCreatedAt = DateTime.Parse(row["CreatedAt"].ToString());
                        HisCommand.CreatedAt = dtCreatedAt.AddHours(8).ToString("yyyy/MM/dd HH:mm:ss");
                        DateTime dtLastStateTime = DateTime.Parse(row["LastStateTime"].ToString());
                        HisCommand.LastStateTime = dtLastStateTime.ToString("yyyy/MM/dd HH:mm:ss");

                        foo.Add(HisCommand);
                    }
                }

                if (tmpMsg.Equals(""))
                {

                }
                else
                {

                }
            }
            catch (Exception ex)
            {
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
        public bool CheckEquipLookupTable(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, string _equip, string _lotid)
        {
            string funcName = "CheckEquipLookupTable";
            string tmpMsg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            bool bResult = false;

            try
            {
                sql = _BaseDataService.CheckLookupTable(_configuration["CheckEqpLookupTable:Table"], _equip, _lotid);
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    tmpMsg = String.Format("Check Lookup Table Success. [{0}][{1}][{2}][{3}]", funcName, _equip, _lotid, dt.Rows[0]["equip2"].ToString());
                    bResult = true;
                }
                else
                {
                    tmpMsg = String.Format("Check Lookup Table Failed. [{0}][{1}][{2}][{3}]", funcName, _equip, _lotid, dt.Rows[0]["equip2"].ToString());
                    _logger.Debug(tmpMsg);
                    bResult = false;
                }
            }
            catch (Exception ex)
            {
                tmpMsg = String.Format("Unknow Issue. [{0}][{1}][{2}][{3}]", funcName, _equip, _lotid, ex.ToString());
                _logger.Debug(tmpMsg);
                bResult = false;
            }
            return true;
        }
        public DataTable ConvertCSVtoDataTable(string strFilePath)
        {
            DataTable dt = new DataTable();
            string str = System.AppDomain.CurrentDomain.BaseDirectory;

            //Result: C:\xxx\xxx\

            using (StreamReader sr = new StreamReader(strFilePath))
            {
                string[] headers = sr.ReadLine().Split(',');
                foreach (string header in headers)
                {
                    dt.Columns.Add(header);
                }
                while (!sr.EndOfStream)
                {
                    string[] rows = sr.ReadLine().Split(',');
                    DataRow dr = dt.NewRow();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = rows[i];
                    }
                    dt.Rows.Add(dr);
                }

            }

            return dt;
        }
        public DataTable GetAvailableCarrierByLocate(DBTool _dbTool, string _carrierType, string _locate, bool _isFull, string _workgroup)
        {
            string sql = "";
            DataTable dtAvailableCarrier = null;

            try
            {
                sql = string.Format(_BaseDataService.GetAvailableCarrierByLocateOrderbyQTime("", "", _carrierType, "", _locate, _isFull, true, _workgroup));
                dtAvailableCarrier = _dbTool.GetDataTable(sql);
            }
            catch (Exception ex)
            { dtAvailableCarrier = null; }

            return dtAvailableCarrier;
        }
        public DataTable GetAvailableCarrierByLocate(DBTool _dbTool, IConfiguration _configuration, string _carrierType, string _lotID, string _locate, bool _isFull, string _RTDEnv, string _workgroup)
        {
            string sql = "";
            DataTable dtAvailableCarrier = null;

            try
            {
                if(_RTDEnv.Equals("PROD"))
                    sql = string.Format(_BaseDataService.GetAvailableCarrierByLocateOrderbyQTime(_configuration["SyncExtenalData:AdsInfo:Table:Prod"], _configuration["QTimeTable:Table:Prod"], _carrierType, _lotID, _locate, _isFull, true, _workgroup));
                else if(_RTDEnv.Equals("UAT"))
                    sql = string.Format(_BaseDataService.GetAvailableCarrierForUatByLocateOrderbyQTime(_configuration["SyncExtenalData:AdsInfo:Table:Prod"], _configuration["QTimeTable:Table:Prod"], _carrierType, _lotID, _locate, _isFull, true, _workgroup));

                dtAvailableCarrier = _dbTool.GetDataTable(sql);
            }
            catch (Exception ex)
            { dtAvailableCarrier = null; }

            return dtAvailableCarrier;
        }
        public DataTable GetAvailableCarrierForFVC(DBTool _dbTool, IConfiguration _configuration, string _carrierType, List<string> _agvs, bool _isFull, string _RTDEnv)
        {
            string sql = "";
            DataTable dtAvailableCarrier = null;

            try
            {
                if (_RTDEnv.Equals("PROD"))
                    sql = string.Format(_BaseDataService.GetAvailableCarrierForFVC(_configuration["SyncExtenalData:AdsInfo:Table:Prod"], _configuration["QTimeTable:Table:Prod"], _carrierType, _agvs, _isFull));
                else if (_RTDEnv.Equals("UAT"))
                    sql = string.Format(_BaseDataService.GetAvailableCarrierForFVC(_configuration["SyncExtenalData:AdsInfo:Table:Prod"], _configuration["QTimeTable:Table:Prod"], _carrierType, _agvs, _isFull));

                dtAvailableCarrier = _dbTool.GetDataTable(sql);
            }
            catch (Exception ex)
            { dtAvailableCarrier = null; }

            return dtAvailableCarrier;
        }
        public bool SyncQtimeforOnlineLot(DBTool _dbTool, IConfiguration _configuration)
        {
            bool bResult = false;
            string sql = "";
            string tmpMsg = "";
            string _table = "";
            DataTable dt = null;

            try
            {
#if DEBUG
                _table = _configuration["SyncExtenalData:AdsInfo:Table:Debug"];
#else
_table = _configuration["SyncExtenalData:AdsInfo:Table:Prod"];
#endif


                sql = string.Format(_BaseDataService.QueryQtimeOfOnlineCarrier(_table, ""));
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        try
                        {
                            if (!row["qtime1"].ToString().Equals(row["qtime"].ToString()))
                            {
                                float iqtime1 = 0;
                                float iqtime = 0;
                                string pkgfullname1 = "";
                                string pkgfullname = "";
                                string lotRecipe = "";
                                string adsRecipe = "";
                                try
                                {
                                    iqtime1 = float.Parse(row["qtime1"].ToString());
                                    iqtime = float.Parse(row["qtime"].ToString());
                                    pkgfullname1 = row["pkgfullname1"].ToString();
                                    pkgfullname = row["pkgfullname"].ToString();
                                    lotRecipe = row["recipe2"].ToString();
                                    adsRecipe = row["recipe"].ToString();
                                }
                                catch(Exception ex) { }

                                if (!iqtime.Equals(iqtime1))
                                {
                                    if (iqtime > iqtime1)
                                    {
                                        if (!pkgfullname.Equals(pkgfullname1))
                                        {
                                            sql = _BaseDataService.UpdateQtimeToLotInfo(row["lot_id"].ToString(), iqtime, pkgfullname);
                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                        }
                                        else
                                        {
                                            sql = _BaseDataService.UpdateQtimeToLotInfo(row["lot_id"].ToString(), iqtime, "");
                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                        }

                                    }
                                    else
                                    {
                                        if (!iqtime.Equals(0))
                                        {
                                            sql = _BaseDataService.UpdateQtimeToLotInfo(row["lot_id"].ToString(), iqtime, "");
                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                        }
                                    }
                                }

                                if (!lotRecipe.Equals(adsRecipe))
                                {
                                    if (!adsRecipe.Equals(""))
                                    {
                                        sql = _BaseDataService.UpdateRecipeToLotInfo(row["lot_id"].ToString(), adsRecipe);
                                        _dbTool.SQLExec(sql, out tmpMsg, true);
                                    }
                                }
                            }
                        }
                        catch (Exception ex) { }
                    }
                }

                bResult = true;
            }
            catch (Exception ex)
            { dt = null; }

            return bResult;
        }
        public bool CheckPORCAndReset(DBTool _dbTool)
        {
            string funcName = "CheckPORCAndReset";
            string tmpMsg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string _lotid = "";
            bool bResult = false;

            try
            {
                sql = _BaseDataService.GetPROCLotInfo();
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        _lotid = row["lotid"].ToString();

                        if(!_lotid.Trim().Equals(""))
                        {
                            sql = _BaseDataService.ResetRTDStateByLot(_lotid);
                            _dbTool.SQLExec(sql, out tmpMsg, true);
                        }
                    }
                }

                bResult = true;
            }
            catch (Exception ex)
            {
                bResult = false;
            }
            return bResult;
        }
        public bool VerifyErackSlots(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, string keyCode)
        {
            return true;
        }
        public bool AutoTriggerFurneceBatchin(DBTool _dbTool, IConfiguration _configuration, ILogger _logger)
        {
            APIResult foo = new APIResult();
            bool _result = false;
            string sql = "";
            DataTable dt = null;
            string _equipid = "";

            try
            {
                //QueryFurneceEQP
                sql = string.Format(_BaseDataService.QueryFurneceEQP());
                dt = _dbTool.GetDataTable(sql);

                if(dt.Rows.Count > 0)
                {
                    _equipid = "";
                    foreach (DataRow row in dt.Rows)
                    {
                        _equipid = row["equipid"].ToString();

                        foo = SentBatchEvent(_dbTool, _configuration, _logger, false, _equipid);
                    }

                    _result = true;
                }
            }
            catch (Exception ex) { _result = false; }

            return _result;
        }
        public APIResult SentBatchEvent(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, bool isManual, string _equip)
        {
            List<string> args = new();
            APIResult foo = new APIResult();
            string sql = "";
            string tmpMsg = "";
            DataTable dtAvaileCarrier = null;
            DataTable dt = null;
            DataTable dtTemp = null;
            string _inErack = "";
            string _outErack = "";
            string _dummyLocate = "";
            string _sideWH = "";
            int _totalQty = 0;
            int _quantity = 0;
            int _currTotalQty = 0;
            int _minimunQty = 0;
            int _maximunQty = 0;
            int _CarrierCount = 0;
            string _workgroup = "";

            try
            {
                
                string ineRack = "Test";
                string _carrierType = "";

                List<string> _lstTemp = new();
                string _sector = "";
                string _lstCarrierID = "";
                string _slotID = "";

                //QueryLocateBySector
                sql = string.Format(_BaseDataService.QueryFurneceOutErack(_equip));
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    _inErack = "";
                    foreach (DataRow row in dt.Rows)
                    {
                        _inErack = row["in_erack"].ToString();
                        _sideWH = row["sidewarehouse"].ToString();
                        _minimunQty = int.Parse(row["minmumQty"].ToString());
                        _carrierType = row["carrier_type"].ToString();
                        _maximunQty = int.Parse(row["maximumQty"].ToString());
                        _workgroup = row["workgroup"].ToString();
                    }
                }

                dtTemp = _dbTool.GetDataTable(_BaseDataService.QueryLocateBySector(_sideWH));
                if (dtTemp.Rows.Count > 0)
                {
                    int _start = 0;
                    int _end = 0;
                    int _find = 0;
                    int _Len = 0;
                    string _secTemp = "";

                    foreach (DataRow drSector in dtTemp.Rows)
                    {
                        _sector = drSector["sector"].ToString();
                        _sector = _sector.Replace("\",\"", "\"#\"");
                        _sector = _sector.Replace("\"", "").Replace("{", "").Replace("}", "");

                        _find = _sector.IndexOf(_inErack);
                        if(_find > 0)
                            _start = _sector.IndexOf(_inErack) + ineRack.Length;
                        else
                            _start = _find + ineRack.Length + 1;

                        Console.WriteLine(_start);
                        _Len = _sector.IndexOf("#", _start);

                        Console.WriteLine(_Len);

                        if (_find > 0)
                        {
                            if (_start > _find)
                            {
                                if(_start > _Len) 
                                {
                                    _end = _start + 1;
                                    _secTemp = _sector.Substring(_start);
                                }
                                else
                                {
                                    _end = _Len - _start - 1;
                                    //_secTemp = _sector.Substring(_start + 1, _end);
                                    //_secTemp = _sector.Substring(_start);
                                    _secTemp = _sector.Substring(_start + 1, _end);
                                }

                            }
                            else
                            {
                                _end = _Len - _start;
                                _secTemp = _sector.Substring(_start, _end);
                            }
                        }
                        else
                        {
                            _secTemp = _sector.Substring(_start);

                            if (_secTemp.IndexOf("#") > 0)
                            {
                                _secTemp = _secTemp.Substring(0, _secTemp.IndexOf("#"));
                            }
                        }

                        _sector = _secTemp;

                        if(_secTemp.LastIndexOf(':') < 0)
                            _lstTemp.Add(string.Format("{0}:{1}", drSector["erackID"].ToString(), _sector));
                        else
                            _lstTemp.Add(string.Format("{0}{1}", drSector["erackID"].ToString(), _sector));
                    }
                }

                if(_lstTemp.Count <= 0)
                {
                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = "Sector not exist."
                    };

                    return foo;
                }

                dtAvaileCarrier = GetAvailableCarrierForFVC(_dbTool, _configuration, _carrierType, _lstTemp, true, "PROD");

                if (dtAvaileCarrier.Rows.Count > 0)
                {
                    _currTotalQty = 0;
                    _CarrierCount = 0;

                    foreach (DataRow draCarrier in dtAvaileCarrier.Rows)
                    {
                        try
                        {

                            _quantity = int.Parse(draCarrier["quantity"].ToString());

                            if ((_currTotalQty + _quantity) > _maximunQty)
                                continue;
                            else
                            {
                                if (_lstCarrierID.Equals(""))
                                {
                                    _lstCarrierID = string.Format("{0}", draCarrier["carrier_id"].ToString());
                                    _slotID = string.Format("{0}", draCarrier["portno"].ToString());
                                }
                                else
                                {
                                    _lstCarrierID = string.Format("{0},{1}", _lstCarrierID, draCarrier["carrier_id"].ToString());
                                    _slotID = string.Format("{0},{1}", _slotID, draCarrier["portno"].ToString());
                                }

                                _CarrierCount++;

                                _currTotalQty = _currTotalQty + _quantity;
                            }
                        }
                        catch (Exception ex) { }
                    }
                }

                if (_currTotalQty > 0)
                {
                    if(isManual)
                    {
                        //Save to effectiveslot
                        sql = _BaseDataService.UpdateEffectiveSlot(_slotID, _workgroup);
                        _dbTool.SQLExec(sql, out tmpMsg, true);

                        //Manual do not check quantity;
                        _totalQty = _CarrierCount;
                        args.Add(_equip);//("Equip") 
                        args.Add(_lstCarrierID);//("_lstCarrierID") 
                        args.Add(_totalQty.ToString());//("_totalQty") 

                        foo = SentCommandtoMCSByModel(_configuration, _logger, "Batch", args);

                        if (foo.Success.Equals(true))
                        {
                            sql = _BaseDataService.UpdateFVCStatus(_equip, 1);
                            _dbTool.SQLExec(sql, out tmpMsg, true);
                        }
                        else
                        {
                            sql = _BaseDataService.UpdateFVCStatus(_equip, 0);
                            _dbTool.SQLExec(sql, out tmpMsg, true);
                        }
                    }
                    else
                    {
                        if ((_currTotalQty >= _minimunQty))
                        {
                            //Save to effectiveslot
                            sql = _BaseDataService.UpdateEffectiveSlot(_slotID, _workgroup);
                            _dbTool.SQLExec(sql, out tmpMsg, true);

                            //_lstCarrierID = "12CA0011,12CA0022,12CA0033";
                            _totalQty = dtAvaileCarrier.Rows.Count;
                            args.Add(_equip);//("Equip") 
                            args.Add(_lstCarrierID);//("_lstCarrierID") 
                            args.Add(_totalQty.ToString());//("_totalQty") 

                            foo = SentCommandtoMCSByModel(_configuration, _logger, "Batch", args);

                            if (foo.Success.Equals(true))
                            {
                                sql = _BaseDataService.UpdateFVCStatus(_equip, 1);
                                _dbTool.SQLExec(sql, out tmpMsg, true);
                            }
                            else
                            {
                                sql = _BaseDataService.UpdateFVCStatus(_equip, 0);
                                _dbTool.SQLExec(sql, out tmpMsg, true);
                            }
                        }
                        else
                        {
                            foo = new APIResult()
                            {
                                Success = false,
                                State = "NG",
                                Message = "Insufficient Quantity."
                            };
                        }
                    }
                }
            }
            catch (Exception ex) { }

            return foo;
        }

        public bool TransferCarrierToSideWH(DBTool _dbTool, IConfiguration _configuration, ConcurrentQueue<EventQueue> _eventQueue, ILogger _logger)
        {
            bool bResult = false;
            string tmpMsg = "";
            string sql = "";
            DataTable dtTemp = null;
            DataTable dtTemp2 = null;
            DataTable dtTemp3 = null;
            DataTable dtTemp4 = null;
            EventQueue _eventQ = new EventQueue();
            string funcName = "MoveCarrier";
            TransferList transferList = new TransferList();
            string tableName = "";
            string tableOrder = "";
            string carrierId = "";
            string _keyOfEnv = "";
            string _keyRTDEnv = "";

            string _targetpoint = "";
            string _currentLocate = "";
            string _destStage = "";
            string _eqpWorkgroup = "";
            string _lotID = "";
            string _adsTable = "";

            string _adsStage = "";
            string _adsPkg = "";
            string _sideWarehouse = "";
            string _sector = "";
            string[] _sectorlist;

            int _processQty = 0;
            int _loadportQty = 0;
            int _preparecarrierForSideWH = 0;
            int _preparesettingforSideWh = 0;
            bool isFurnace = false;
            string _lotStage = "";

            try
            {
                _keyRTDEnv = _configuration["RTDEnvironment:type"];
                _keyOfEnv = string.Format("RTDEnvironment:commandsTable:{0}", _keyRTDEnv);
                //"RTDEnvironment:commandsTable:PROD"
                tableOrder = _configuration[_keyOfEnv] is null ? "workinprocess_sch" : _configuration[_keyOfEnv];
                tableName = _configuration["PreDispatchToErack:lotState:tableName"] is null ? "lot_Info" : _configuration["PreDispatchToErack:lotState:tableName"];

                _adsTable = _configuration["CheckLotStage:Table"] is null ? "lot_Info" : _configuration["CheckLotStage:Table"];

                if (_keyRTDEnv.ToUpper().Equals("PROD"))
                    sql = _BaseDataService.QueryTransferListForSideWH(tableName);
                else if (_keyRTDEnv.ToUpper().Equals("UAT"))
                    sql = _BaseDataService.QueryTransferListForSideWH(tableName);
                else
                    _logger.Debug(string.Format("RTDEnvironment setting failed. current set is [{0}]", _keyRTDEnv));

                dtTemp = _dbTool.GetDataTable(sql);

                if (dtTemp.Rows.Count > 0)
                {
                    _logger.Info(string.Format("[{0}][{1}][{2}]", "VFC TEST", funcName, dtTemp.Rows.Count));

                    foreach (DataRow dr in dtTemp.Rows)
                    {
                        _currentLocate = dr["locate"].ToString().Equals("") ? "*" : dr["locate"].ToString();
                        _destStage = dr["stage"].ToString().Equals("") ? "NA" : dr["stage"].ToString();
                        _eqpWorkgroup = dr["workgroup"].ToString().Equals("") ? "NA" : dr["workgroup"].ToString();
                        _lotStage = dr["lotstage"].ToString().Equals("") ? "NA" : dr["lotstage"].ToString();

                        isFurnace = dr["isFurnace"].ToString().Equals("1") ? true : false;
                        ///check is lot locate in SideWarehouse
                        /////workgroup , stage, SideWarehouse
                        _processQty = 0;
                        _loadportQty = 0;

                        _sideWarehouse = dr["SideWarehouse"].ToString().Equals("") ? "*" : dr["SideWarehouse"].ToString();

                        if (_sideWarehouse.Equals("*"))
                            continue;

                        try
                        {
                            _logger.Info(string.Format("[{0}][{1}][{2}]", "VFC TEST", "SideWarehouse", _sideWarehouse));

                            dtTemp3 = null;
                            sql = _BaseDataService.QueryRackByGroupID(_sideWarehouse);
                            dtTemp3 = _dbTool.GetDataTable(sql);
                        }
                        catch (Exception ex) { }

                        if (dtTemp3.Rows.Count > 0)
                        {
                            try {
                                _targetpoint = dtTemp3.Rows[0]["erackID"].ToString();
                                _sector = dtTemp3.Rows[0]["sector"].ToString().Replace("\"","").Replace("}", "").Replace("{", "");

                                _logger.Info(string.Format("[{0}][{1}][{2}]", "VFC TEST", _targetpoint, _sector));
                            }
                            catch(Exception ex) {
                                _logger.Info(string.Format("[{0}][{1}][{2}]", "VFC TEST", "EXCEPTION", ex.Message));
                            }
                        }
                        else
                        {
                            _logger.Info(string.Format("[{0}][{1}][{2}]", "VFC TEST", "QueryRackByGroupID", dtTemp3.Rows.Count));
                            continue;
                        }

                        try
                        {
                            //Get setting from workgroup set
                            sql = _BaseDataService.QueryWorkgroupSet(_eqpWorkgroup, _destStage);
                            dtTemp3 = _dbTool.GetDataTable(sql);
                            if (dtTemp3.Rows.Count > 0)
                            {
                                _preparesettingforSideWh = int.Parse(dtTemp3.Rows[0]["preparecarrierForSideWH"].ToString());
                            }

                            //calculate process qty by stage
                            if (isFurnace)
                            {
                                _logger.Info(string.Format("[{0}][{1}][{2}]", "VFC TEST", "preparesettingforSideWh", _preparesettingforSideWh));
                                //get all carrier locate 
                                //when locate and portno are met,  +1
                                //get all dest in workinprocess sch
                                //when dest is _targetpoint +1
                                int _vfcstatus = 0;
                                string _equipment = "";
                                string _portNo = "";

                                sql = _BaseDataService.GetEqpInfoByWorkgroupStage(_eqpWorkgroup, _destStage, _lotStage);
                                dtTemp3 = _dbTool.GetDataTable(sql);
                                if (dtTemp3.Rows.Count > 0)
                                {
                                    _vfcstatus = int.Parse(dtTemp3.Rows[0]["FVCSTATUS"].ToString());
                                    _equipment = dtTemp3.Rows[0]["EQPID"].ToString();

                                    _logger.Info(string.Format("[{0}][{1}][{2}]", "VFC TEST", "_vfcstatus", _vfcstatus));

                                    //sent batch checkin before
                                    if (!_vfcstatus.Equals(0))
                                        continue;

                                    //string[] _contantSecter = 
                                    _logger.Info(string.Format("[{0}][{1}][{2}]", "VFC TEST", "_sector", _sector));
                                    if(!_sector.Trim().Equals(""))
                                        _sectorlist = _sector.Split(':')[1].Split(',');
                                    else
                                        _sectorlist = _sector.Split(':');

                                    sql = _BaseDataService.GetCarrierByLocate(_targetpoint);
                                    dtTemp2 = _dbTool.GetDataTable(sql);
                                    if (dtTemp2.Rows.Count > 0)
                                    {
                                        foreach (DataRow drCarrier in dtTemp2.Rows)
                                        {
                                            _portNo = drCarrier["portno"].ToString();

                                            if(((IList) _sectorlist).Contains(_portNo))
                                                _processQty++;
                                        }

                                        _logger.Info(string.Format("[{0}][{1}][{2}]", "VFC TEST", "_processQty", _processQty));
                                    }
                                }
                            }
                            else
                            {
                                //side warehouse logic for normally workgroup 
                                sql = _BaseDataService.CalculateProcessQtyByStage(_eqpWorkgroup, _destStage, _lotStage);
                                dtTemp3 = _dbTool.GetDataTable(sql);
                                if (dtTemp3.Rows.Count > 0)
                                {
                                    _processQty = int.Parse(dtTemp3.Rows[0]["processQty"].ToString());
                                }
                            }

                            //calculate loadport qty by stage
                            sql = _BaseDataService.CalculateLoadportQtyByStage(_eqpWorkgroup, _destStage, _lotStage);
                            dtTemp3 = _dbTool.GetDataTable(sql);
                            if (dtTemp3.Rows.Count > 0)
                            {
                                _loadportQty = int.Parse(dtTemp3.Rows[0]["totalportqty"].ToString());
                                _preparecarrierForSideWH = _loadportQty * _preparesettingforSideWh;
                                _logger.Info(string.Format("[{0}][{1}][{2}]", "VFC TEST", "_loadportQty", _loadportQty));
                                _logger.Info(string.Format("[{0}][{1}][{2}]", "VFC TEST", "_preparecarrierForSideWH", _preparecarrierForSideWH));
                            }
                        }
                        catch (Exception ex) { }

                        if (_processQty >= _preparecarrierForSideWH)
                            continue;

                        transferList = new TransferList();
                        carrierId = "";

                        carrierId = dr["carrier_ID"].ToString().Equals("") ? "*" : dr["carrier_ID"].ToString();

                        sql = _BaseDataService.CheckPreTransfer(carrierId, tableOrder);
                        dtTemp3 = _dbTool.GetDataTable(sql);
                        if (dtTemp3.Rows.Count > 0)
                        {
                            _logger.Info(string.Format("[{0}][{1}][{2}]", "VFC TEST", "CheckPreTransfer", carrierId));
                            continue;
                        }

                        _logger.Info(string.Format("[{0}][{1}][{2}]", "VFC TEST", "Create Transfer", carrierId));

                        //20240202 Add midways logic for pre-transfer
                        _eqpWorkgroup = dr["workgroup"].ToString().Equals("") ? "" : dr["workgroup"].ToString();
                        _lotID = dr["lot_ID"].ToString().Equals("") ? "*" : dr["lot_ID"].ToString();

                        _eventQ = new EventQueue();
                        _eventQ.EventName = funcName;

                        transferList.CarrierID = carrierId;
                        transferList.LotID = dr["lot_ID"].ToString().Equals("") ? "*" : dr["lot_ID"].ToString();
                        transferList.Source = "*";
                        transferList.Dest = _sideWarehouse;
                        transferList.CommandType = "Pre-Transfer";
                        transferList.CarrierType = dr["CarrierType"].ToString();

                        try
                        {
                            _adsStage = "";
                            _adsPkg = "";
                            tmpMsg = "";
                            dtTemp4 = null;
                            sql = _BaseDataService.QueryDataByLot(tableName, _lotID);
                            dtTemp4 = _dbTool.GetDataTable(sql);

                            if (dtTemp4.Rows.Count > 0)
                            {
                                _adsStage = dtTemp4.Rows[0]["stage"].ToString();
                                _adsPkg = dtTemp4.Rows[0]["pkgfullname"].ToString();

                                //log ads information for debug 20240313
                                tmpMsg = string.Format("[{0}][{1}][{2}][{3}][ADS: {4} / {5}]", "Pre-Transfer", transferList.LotID, transferList.CarrierID, transferList.Dest, _adsStage, _adsPkg);
                            }
                            else
                            {
                                tmpMsg = string.Format("[{0}][{1}][{2}][{3}][ADS: No Data]", "Pre-Transfer", transferList.LotID, transferList.CarrierID, transferList.Dest);
                            }
                            _logger.Info(tmpMsg);
                        }
                        catch (Exception ex)
                        {
                            tmpMsg = string.Format("[{0}][{1}][{2}]", "Exception", "Pre-Transfer", transferList.LotID);
                            _logger.Info(tmpMsg);
                        }

                        tmpMsg = string.Format("[{0}][{1} / {2} / {3} / {4} / {5} / {6}]", funcName, transferList.CommandType, transferList.LotID, transferList.CarrierID, transferList.Source, transferList.Dest, transferList.CarrierType);
                        _eventQ.EventObject = transferList;
                        _eventQueue.Enqueue(_eventQ);
                    }
                }

                bResult = true;
            }
            catch (Exception ex)
            {
                _logger.Debug(string.Format("TransferCarrierToSideWH Unknow Error. [Exception: {0}]", ex.Message));
            }
            finally
            {
                if (dtTemp != null)
                    dtTemp.Dispose();
                if (dtTemp2 != null)
                    dtTemp2.Dispose();
                if (dtTemp3 != null)
                    dtTemp3.Dispose();
            }
            dtTemp = null;
            dtTemp2 = null;
            dtTemp3 = null;

            return bResult;
        }
        public bool TransferCarrier(ConcurrentQueue<EventQueue> _eventQueue, TransferList _transferList)
        {
            bool bResult = false;

            return bResult;
        }
        public bool AutounlockportWhenNoOrder(DBTool _dbTool, IConfiguration _configuration, ILogger _logger)
        {
            bool bResult = false;
            string sql = "";
            DataTable dt;
            DataTable dt2;
            DataTable dtTemp;
            string _equip = "";
            string _portId = "";
            string _tableOrder = "workinprocess_sch";
            string _lastModifyDt = "";
            string errMsg = "";
            string _funcName = "AutounlockportWhenNoOrder";
            string _portState = "";

            try
            {
                //QueryFurneceEQP
                sql = string.Format(_BaseDataService.QueryIslockPortId());
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        try { 
                            _equip = row["equipid"].ToString();
                            _portId = row["port_id"].ToString();
                            _lastModifyDt = row["lastModify_dt"].ToString();
                            _portState = row["port_state"].ToString();

                            dt2 = _dbTool.GetDataTable(_BaseDataService.SelectTableWorkInProcessSchByEquipPort(_equip, _portId, _tableOrder));
                            if(dt2.Rows.Count <= 0)
                            {
                                //重取lastModify Dt 防止中間有command 產生
                                sql = string.Format(_BaseDataService.SelectTableEQP_Port_SetByPortId(_portId));
                                dtTemp = _dbTool.GetDataTable(sql);

                                if (dtTemp.Rows.Count > 0)
                                {
                                    _lastModifyDt = dtTemp.Rows[0]["lastModify_dt"].ToString();
                                }

                                if (TimerTool("minutes", _lastModifyDt) >= 5)
                                {
                                    _dbTool.SQLExec(_BaseDataService.LockEquipPortByPortId(_portId, false), out errMsg, true);

                                    if (!errMsg.Equals(""))
                                    {
                                        _logger.Info(string.Format("[{0}][{1}][Unlock Fail][{2}][{3}]", _funcName, _portId, _lastModifyDt, _portState));
                                    }
                                    else
                                    {
                                        _logger.Info(string.Format("[{0}][{1}][Auto Unlock][{2}][{3}]", _funcName, _portId, _lastModifyDt, _portState));
                                    }
                                }
                            }
                        }catch (Exception ex) { }
                    }
                }
            }catch(Exception ex)
            { }

            return bResult;
        }
        public bool TriggerAlarms(DBTool _dbTool, IConfiguration configuration, ILogger _logger)
        {
            //sent eMail & SMS & Call JCET CIM Actions

            string funcName = "TriggerAlarms";
            string tmpMsg = "";
            string ErrMsg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            bool result = false;
            RTDAlarms rtdAlarms = new RTDAlarms();

            try
            {
                sql = _BaseDataService.QueryRTDAlarms();
                dt = _dbTool.GetDataTable(sql);

                /*
                Sample email alert.
Email Group: CISRTDALERT.SCS @jcetglobal.com
1.
Subject: Device Setup Pre - Alert

Tool: XXXXX
Next Lot: XXXXXXXX.1
Current Lot: 83747807.1
Mfg Device: 3SDC00011A - WL - B - 00.01
Customer Device: XXXXXXXX
2.
Subject: Device Setup Alert
Tool: XXXXX
Next Lot: XXXXXXXX.1
Mfg Device for next lot: XXXXXXXXXXXXXXX
Customer Device for next lot: XXXXXXXX
Last lot: YYYYYYYY.1
Mfg Device for last lot: YYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY
Customer Device for last lot: YYYYYYYYYYY

                */

                if (dt.Rows.Count > 0)
                {
                    string idxAlarm = "";
                    string AlarmCode = "";
                    string eventTrigger = configuration["RTDAlarm:Condition"] is null ? "eMail:true$SMS:true$repeat:false$hours:0$mints:10" : configuration["RTDAlarm:Condition"];
                    string tmpAlarmType = "";
                    string tempMsg = "";
                    List<string> tmpParams = new List<string>();
                    MailMessage JcetAlarmMsg = new MailMessage();
                    string _tempEqpID = "";
                    string _tempPortID = "";

                    try
                    {
                        foreach (DataRow drTemp in dt.Rows)
                        {
                            try
                            {
                                _tempEqpID = "";
                                _tempPortID = "";

                                rtdAlarms = new RTDAlarms();

                                rtdAlarms.UnitType = drTemp["UnitType"] is null ? "" : drTemp["UnitType"].ToString();
                                rtdAlarms.UnitID = drTemp["UnitID"] is null ? "" : drTemp["UnitID"].ToString();
                                rtdAlarms.Level = drTemp["Level"] is null ? "" : drTemp["Level"].ToString();
                                rtdAlarms.Code = drTemp["Code"] is null ? 0 : int.Parse(drTemp["Code"].ToString());
                                rtdAlarms.Cause = drTemp["Cause"] is null ? "" : drTemp["Cause"].ToString();
                                rtdAlarms.SubCode = drTemp["SubCode"] is null ? "" : drTemp["SubCode"].ToString();
                                rtdAlarms.Detail = drTemp["Detail"] is null ? "" : drTemp["Detail"].ToString();
                                rtdAlarms.CommandID = drTemp["CommandID"] is null ? "" : drTemp["CommandID"].ToString();
                                rtdAlarms.Params  = drTemp["Params"] is null ? "" : drTemp["Params"].ToString();
                                rtdAlarms.Description = drTemp["Description"] is null ? "" : drTemp["Description"].ToString();
                                //rtdAlarms.CreateAt = drTemp["CreateAt"] is null ? "" : drTemp["CreateAt"].ToString();
                                //rtdAlarms.lastUpdated = drTemp["lastUpdated"] is null ? "" : drTemp["Description"].ToString();

                                idxAlarm = drTemp["IDX"].ToString();
                                AlarmCode = drTemp["code"].ToString();
                                eventTrigger = drTemp["EVENTTRIGGER"] is null ? "" : drTemp["EVENTTRIGGER"].ToString();
                                tmpParams = new List<string>();

                                if (!eventTrigger.Equals(""))
                                {
                                    string strTemp = "";

                                    try
                                    {
                                        //"eMail:true$SMS:true$repeat:true$hours:0$mints:10";
                                        string[] tmpTrigger = eventTrigger.Split('$');
                                        foreach (var parm in tmpTrigger)
                                        {
                                            string[] tmpKey = parm.Split(':');

                                            if (strTemp.Equals(""))
                                                strTemp = string.Format("'{0}':{1}", tmpKey[0], tmpKey[1]);
                                            else
                                                strTemp = strTemp + string.Format(",'{0}':{1}", tmpKey[0], tmpKey[1]);
                                        }

                                        if (eventTrigger.Equals(""))
                                        {
                                            sql = _BaseDataService.UpdateRTDAlarms(true, string.Format("{0},{1},{2},{3},{4}", drTemp["UnitID"].ToString(), drTemp["Code"].ToString(), drTemp["SubCode"].ToString(), drTemp["CommandID"].ToString(), drTemp["Params"].ToString()), "");
                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                            continue;
                                        }

                                        strTemp = "{" + strTemp + "}";

                                        try
                                        {
                                            var TempJsonConvert = JsonConvert.DeserializeObject<JcetAlarm>(strTemp);//反序列化
                                        }
                                        catch (Exception ex) { continue; }
                                    }
                                    catch (Exception ex) { continue; }
                                    //--params= "eMail":true, "SMS":true, "repeat":true, "hours":0, "mints":10, 
                                    //Newtonsoft.Json反序列化
                                    var JsonJcetAlarm = JsonConvert.DeserializeObject<JcetAlarm>(strTemp);//反序列化
                                    var JsonObject = JObject.Parse(strTemp);

                                    //tmpSmsMsg = "lotid:{0}$equipid:{1}$partid:{2}$customername:{3}$stage:{4}$nextlot:{5}$nextpart:{6}";
                                    try
                                    {
                                        //"eMail:true$SMS:true$repeat:true$hours:0$mints:10";
                                        strTemp = "";
                                        strTemp = rtdAlarms.Params.Replace('{', ' ').Replace('}', ' ');

                                        if (!strTemp.Equals(""))
                                        {
                                            strTemp = "{" + strTemp + "}";

                                            try
                                            {
                                                var TempJsonConvert = JsonConvert.DeserializeObject<JcetAlarm>(strTemp);//反序列化
                                            }
                                            catch (Exception ex) { continue; }
                                        }
                                        else
                                        {
                                            strTemp = "{ \"Result\":\"None\"}";
                                        }
                                    }
                                    catch (Exception ex) { }

                                    //--params= "eMail":true, "SMS":true, "repeat":true, "hours":0, "mints":10, 
                                    //Newtonsoft.Json反序列化
                                    var JsonJcetParams = JsonConvert.DeserializeObject<JcetAlarm>(strTemp);//反序列化
                                    var JsonParamsObject = JObject.Parse(strTemp);

                                    _tempEqpID = GetJArrayValue((JObject)JsonParamsObject, "EquipID");
                                    _tempPortID = GetJArrayValue((JObject)JsonParamsObject, "PortID");

                                    switch (AlarmCode)
                                    {
                                        case "1001":
                                            try
                                            {
                                                tmpParams.Add(string.Format("Device Setup Pre-Alert"));
                                                tmpParams.Add(string.Format(JsonJcetParams.lotid));
                                                tmpParams.Add(string.Format(JsonJcetParams.EquipID));
                                                tempMsg = string.Format(@"Tool: {0}
Next Lot: {1}
Current Lot: {2}
Mfg Device: {3}
Customer Device: {4}", JsonJcetParams.EquipID, JsonJcetParams.nextlot, JsonJcetParams.lotid, JsonJcetParams.stage, JsonJcetParams.partid);

                                                //tmpSmsMsg = "lotid:{0}$equipid:{1}$partid:{2}$customername:{3}$stage:{4}$nextlot:{5}$nextpart:{6}";

                                                _logger.Info(tempMsg);
                                                tmpParams.Add(string.Format(tempMsg));
                                            }
                                            catch (Exception ex)
                                            {
                                                tmpMsg = String.Format("[Exception]:[{0}][{1}]", AlarmCode, ex.Message);
                                                _logger.Info(tmpMsg);
                                            }
                                            break;
                                        case "1002":
                                            try
                                            {
                                                tmpParams.Add(string.Format("Device Setup Alert"));
                                                tmpParams.Add(string.Format(JsonJcetParams.nextlot));
                                                tmpParams.Add(string.Format(JsonJcetParams.EquipID));
                                                tempMsg = string.Format(@"Tool: {0}
Next Lot: {1}
Mfg Device for next lot: {2}
Customer Device for next lot: {3}
Last lot: {4}
Mfg Device for last lot: {5}
Customer Device for last lot: {6}", JsonJcetParams.EquipID, JsonJcetParams.nextlot, JsonJcetParams.nextpart, JsonJcetParams.customername, JsonJcetParams.lotid, JsonJcetParams.partid, JsonJcetParams.customername);

                                                _logger.Info(tempMsg);
                                                tmpParams.Add(string.Format(tempMsg));
                                            }
                                            catch (Exception ex)
                                            {
                                                tmpMsg = String.Format("[Exception]:[{0}][{1}]", AlarmCode, ex.Message);
                                                _logger.Info(tmpMsg);
                                            }
                                            break;
                                        case "20051":
                                            //eRack Offline
                                            //Subject content.
                                            tmpParams.Add(string.Format("e-Rack {0} offline, Setup Alert", rtdAlarms.UnitID));
                                            //target.
                                            tmpParams.Add(rtdAlarms.UnitID);
                                            //Type.
                                            tmpParams.Add(rtdAlarms.UnitType);
                                            //Body
                                            tempMsg = string.Format(@"UnitType: {0}
UnitID: {1}
Code: {2}
Cause: {3}
Last lot: {4}
SubCode: {5}
Detail: {6}", rtdAlarms.UnitType, rtdAlarms.UnitID, rtdAlarms.Code, rtdAlarms.Cause, "", rtdAlarms.SubCode, rtdAlarms.Detail);

                                            tmpParams.Add(string.Format(tempMsg));
                                            break;
                                        case "20052":
                                            //eRack water level High
                                            //Subject content.
                                            tmpParams.Add(string.Format("e-Rack {0} water level High, Setup Alert", rtdAlarms.UnitID));
                                            //target.
                                            tmpParams.Add(rtdAlarms.UnitID);
                                            //Type.
                                            tmpParams.Add(rtdAlarms.UnitType);
                                            //Body
                                            tempMsg = string.Format(@"UnitType: {0}
UnitID: {1}
Code: {2}
Cause: {3}
Last lot: {4}
SubCode: {5}
Detail: {6}", rtdAlarms.UnitType, rtdAlarms.UnitID, rtdAlarms.Code, rtdAlarms.Cause, "", rtdAlarms.SubCode, rtdAlarms.Detail);

                                            tmpParams.Add(string.Format(tempMsg));
                                            break;
                                        case "20053":
                                            //eRack water level full
                                            //Subject content.
                                            tmpParams.Add(string.Format("e-Rack {0} water level Full, Setup Alert", rtdAlarms.UnitID));
                                            //target.
                                            tmpParams.Add(rtdAlarms.UnitID);
                                            //Type.
                                            tmpParams.Add(rtdAlarms.UnitType);
                                            //Body
                                            tempMsg = string.Format(@"UnitType: {0}
UnitID: {1}
Code: {2}
Cause: {3}
Last lot: {4}
SubCode: {5}
Detail: {6}", rtdAlarms.UnitType, rtdAlarms.UnitID, rtdAlarms.Code, rtdAlarms.Cause, "", rtdAlarms.SubCode, rtdAlarms.Detail);

                                            tmpParams.Add(string.Format(tempMsg));
                                            break;
                                        case "30002":
                                            //eRack water level full
                                            //Subject content.
                                            tmpParams.Add(string.Format("3 times error, the port [{0}] closed now.", rtdAlarms.CommandID));
                                            //target.
                                            tmpParams.Add(rtdAlarms.UnitID);
                                            //Type.
                                            tmpParams.Add(rtdAlarms.UnitType);
                                            //Body
                                            tempMsg = string.Format(@"Please check the tool ID [{0}] get the {1} : {2} 3 times error. Port closed now. ", rtdAlarms.CommandID, rtdAlarms.Code, rtdAlarms.Cause);

                                            tmpParams.Add(string.Format(tempMsg));
                                            break;
                                        default:
                                            tmpParams.Add(string.Format("TSC Alarm, the toolid [{0}] get alarm [{1}].", JsonJcetParams.PortID, rtdAlarms.Code));
                                            //target.
                                            tmpParams.Add(rtdAlarms.UnitID);
                                            //Type.
                                            tmpParams.Add(rtdAlarms.UnitType);
                                            //Body
                                            if(_tempPortID.Equals(""))
                                            {
                                                try { 
                                                    sql = _BaseDataService.QueryPortInfobyPortID(rtdAlarms.CommandID);
                                                    dtTemp = _dbTool.GetDataTable(sql);

                                                    if (dtTemp.Rows.Count > 0)
                                                    {
                                                        switch (dtTemp.Rows[0]["cmd_type"].ToString())
                                                        {
                                                            case "LOAD":
                                                                _tempPortID = dtTemp.Rows[0]["dest"].ToString();
                                                                break;
                                                            case "UNLOAD":
                                                                _tempPortID = dtTemp.Rows[0]["source"].ToString();
                                                                break;
                                                            default:
                                                                _tempPortID = dtTemp.Rows[0]["dest"].ToString();
                                                                break;
                                                        }
                                                    }
                                                }
                                                catch (Exception ex) { }
                                            }

                                            tempMsg = string.Format(@"Please check the tool ID [{0}] get the {1} : {2} now.", _tempPortID, rtdAlarms.Code, rtdAlarms.Cause);

                                            tmpParams.Add(string.Format(tempMsg));
                                            break;
                                    }

                                    if (JsonJcetAlarm.eMail)
                                    {
                                        string _alarmBy = "";
                                        string _tmpKey = "";
                                        string _tmpState = "";
                                        ///寄送Mail
                                        try
                                        {
                                            JcetAlarmMsg = new MailMessage();

                                            _alarmBy = configuration[string.Format("MailSetting:AlarmBy")];
                                            if (_alarmBy.ToUpper().Equals("ALARMBYWORKGROUP"))
                                            {
                                                _tmpState = _alarmBy;
                                                //string _tempEqpID = JsonJcetParams.EquipID;
                                                //string _tempPortID = JsonJcetParams.PortID;

                                                //rtdAlarms.CommandID
                                                try {
                                                    sql = _BaseDataService.QueryPortInfobyPortID(_tempPortID);
                                                    dtTemp = _dbTool.GetDataTable(sql);
                                                    if (dtTemp.Rows.Count > 0)
                                                    {
                                                        _tmpKey = dtTemp.Rows[0]["workgroup"].ToString().Trim();
                                                        if (configuration[string.Format("MailSetting:{0}:{1}", _alarmBy, _tmpKey)] is null)
                                                        {
                                                            //no set send to default alarm mail
                                                            JcetAlarmMsg.To.Add(configuration["MailSetting:AlarmMail"]);
                                                        }
                                                        else
                                                        {
                                                            if (configuration[string.Format("MailSetting:{0}:{1}", _alarmBy, _tmpKey)].Contains(","))
                                                            {
                                                                string[] lsMail = configuration[string.Format("MailSetting:{0}:{1}", _alarmBy, _tmpKey)].Split(',');
                                                                foreach (string theMail in lsMail)
                                                                {
                                                                    JcetAlarmMsg.To.Add(theMail.Trim());
                                                                }
                                                            }
                                                            else
                                                            {
                                                                JcetAlarmMsg.To.Add(configuration[string.Format("MailSetting:{0}:{1}", _alarmBy, _tmpKey)]);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        tmpMsg = string.Format("[{0}][{1}][{2}]", "QueryPortInfobyPortID", _tempPortID, configuration["MailSetting:AlarmMail"]);
                                                        _logger.Info(tmpMsg);
                                                        JcetAlarmMsg.To.Add(configuration["MailSetting:AlarmMail"]);
                                                    }
                                                }
                                                catch (Exception ex) {
                                                    tmpMsg = string.Format("[{0}][{1}][{2}]", "Exception", _tmpState, ex.Message);
                                                    _logger.Info(tmpMsg);
                                                    JcetAlarmMsg.To.Add(configuration["MailSetting:AlarmMail"]);
                                                }
                                            }
                                            else
                                            {
                                                _tmpState = _alarmBy;

                                                try {
                                                    if (configuration[string.Format("MailSetting:{0}:{1}", _alarmBy, AlarmCode)] is null)
                                                    {
                                                        //no set send to default alarm mail
                                                        if (configuration["MailSetting:AlarmMail"].Contains(","))
                                                        {
                                                            string[] lsMail = configuration["MailSetting:AlarmMail"].Split(',');
                                                            foreach (string theMail in lsMail)
                                                            {
                                                                JcetAlarmMsg.To.Add(theMail.Trim());
                                                            }
                                                        }
                                                        else
                                                        {
                                                            JcetAlarmMsg.To.Add(configuration["MailSetting:AlarmMail"]);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (configuration[string.Format("MailSetting:{0}:{1}", _alarmBy, AlarmCode)].Contains(","))
                                                        {
                                                            string[] lsMail = configuration[string.Format("MailSetting:{0}:{1}", _alarmBy, AlarmCode)].Split(',');
                                                            foreach (string theMail in lsMail)
                                                            {
                                                                JcetAlarmMsg.To.Add(theMail.Trim());
                                                            }
                                                        }
                                                        else
                                                        {
                                                            JcetAlarmMsg.To.Add(configuration[string.Format("MailSetting:{0}:{1}", _alarmBy, AlarmCode)]);
                                                        }
                                                    }
                                                }
                                                catch(Exception ex) {
                                                    tmpMsg = string.Format("[{0}][{1}][{2}]", "Exception", _tmpState, ex.Message);
                                                    _logger.Info(tmpMsg);
                                                    JcetAlarmMsg.To.Add(configuration["MailSetting:AlarmMail"]);
                                                }
                                            }


                                            _tmpState = "JcetAlarmMsg";
                                            //msg.To.Add("b@b.com");可以發送給多人
                                            //msg.CC.Add("c@c.com");
                                            //msg.CC.Add("c@c.com");可以抄送副本給多人 
                                            //這裡可以隨便填，不是很重要
                                            JcetAlarmMsg.From = new MailAddress(configuration["MailSetting:username"], configuration["MailSetting:EntryBy"], Encoding.UTF8);
                                            /* 上面3個參數分別是發件人地址（可以隨便寫），發件人姓名，編碼*/
                                            JcetAlarmMsg.Subject = tmpParams[0];//郵件標題
                                            JcetAlarmMsg.SubjectEncoding = Encoding.UTF8;//郵件標題編碼
                                            JcetAlarmMsg.Body = tmpParams[3]; //郵件內容
                                            JcetAlarmMsg.BodyEncoding = Encoding.UTF8;//郵件內容編碼 
                                            JcetAlarmMsg.IsBodyHtml = true;//是否是HTML郵件 

                                            //tmpMsg = string.Format("{0}{1}", tmpAlarmMsg.Subject, tmpAlarmMsg.Body);
                                            _tmpState = "MailController";
                                            MailController MailCtrl = new MailController();
                                            MailCtrl.Config = configuration;
                                            MailCtrl.Logger = _logger;
                                            MailCtrl.DB = _dbTool;
                                            MailCtrl.MailMsg = JcetAlarmMsg;

                                            _tmpState = "SendMail";

                                            if(!_tempPortID.Equals(""))
                                                MailCtrl.SendMail();

                                            tmpMsg = string.Format("SendMail: [{0}], [{1}]", JcetAlarmMsg.Subject, JcetAlarmMsg.Body);
                                            _logger.Info(tmpMsg);
                                        }
                                        catch (Exception ex)
                                        {
                                            tmpMsg = String.Format("SendMail failed. [{0}][Exception]: {1}", _tmpState, ex.Message);
                                            _logger.Debug(tmpMsg);
                                        }
                                    }

                                    if (JsonJcetAlarm.SMS)
                                    {
                                        //tmpMsg = string.Format("{0}{1}", JcetAlarmMsg.Subject, JcetAlarmMsg.Body);

                                        ///發送SMS 
                                        try
                                        {
                                            tmpMsg = "";
                                            sql = string.Format(_BaseDataService.InsertSMSTriggerData(tmpParams[1], tmpParams[2], tmpParams[0], "N", configuration["MailSetting:EntryBy"]));
                                            tmpMsg = string.Format("Send SMS: SQLExec[{0}]", sql);
                                            _logger.Info(tmpMsg);
                                            _dbTool.SQLExec(sql, out tmpMsg, true);
                                        }
                                        catch (Exception ex)
                                        {
                                            tmpMsg = String.Format("Insert SMS trigger data failed. [Exception]: {0}", ex.Message);
                                            _logger.Debug(tmpMsg);
                                        }
                                    }

                                    if (JsonJcetAlarm.action)
                                    {
                                        tmpMsg = string.Format("{0}{1}", JcetAlarmMsg.Subject, JcetAlarmMsg.Body);

                                        string scenario = JsonObject.Property("scenario") == null ? "Shutdown" : JsonObject["scenario"].ToString();
                                        if (scenario.Equals("Shutdown") || JsonJcetAlarm.scenario.Equals("Shutdown"))
                                        {
                                            string webServiceMode = "soap11";

                                            JCETWebServicesClient jcetWebServiceClient = new JCETWebServicesClient();
                                            JCETWebServicesClient.ResultMsg resultMsg = new JCETWebServicesClient.ResultMsg();
                                            JObject oResp = null;

                                            try
                                            {
                                                jcetWebServiceClient._url = ""; //CIMAPP006 會依function自動生成url
                                                resultMsg = new JCETWebServicesClient.ResultMsg();
                                                resultMsg = jcetWebServiceClient.CIMAPP006("rtsdown", webServiceMode, JsonJcetAlarm.EquipID, configuration["WebService:username"], configuration["WebService:password"], "lotid");
                                                string result3 = resultMsg.retMessage;
                                                string tmpCert = "";
                                                string resp_Code = "";
                                                string resp_Msg = "";

                                                if (resultMsg.status)
                                                {
                                                    oResp = JObject.Parse(resultMsg.retMessage);
                                                }
                                                else
                                                {
                                                    tmpMsg = string.Format("An unknown exception occurred in the web service. Please call IT-CIM deportment. [Exception] {0}", resultMsg.retMessage);
                                                    _logger.Debug(tmpMsg);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                tmpMsg = string.Format("An unknown exception occurred in the web service. Please call IT-CIM deportment. [Exception] {0}", ex.Message);
                                                _logger.Debug(tmpMsg);
                                            }
                                        }
                                    }

                                    if (JsonJcetAlarm.repeat)
                                    {
                                        tmpMsg = string.Format("{0}{1}", JcetAlarmMsg.Subject, JcetAlarmMsg.Body);
                                    }
                                    else
                                    {
                                        sql = _BaseDataService.UpdateRTDAlarms(true, string.Format("{0},{1},{2},{3},{4}", drTemp["UnitID"].ToString(), drTemp["Code"].ToString(), drTemp["SubCode"].ToString(), drTemp["CommandID"].ToString(), drTemp["Params"].ToString()), "");
                                        _dbTool.SQLExec(sql, out tmpMsg, true);
                                    }
                                }
                                else
                                {
                                    //沒有設定的直接切換為None new 
                                    sql = _BaseDataService.UpdateRTDAlarms(true, string.Format("{0},{1},{2},{3},{4}", drTemp["UnitID"].ToString(), drTemp["Code"].ToString(), drTemp["SubCode"].ToString(), drTemp["CommandID"].ToString(), drTemp["Params"].ToString()), "");
                                    _dbTool.SQLExec(sql, out tmpMsg, true);
                                }
                            }
                            catch (Exception ex)
                            {
                                result = false;
                                ErrMsg = string.Format("[{0}][{1}][{2}][{3}][{4}]", "Exception", funcName, "InForeach", idxAlarm, ex.Message);
                            }

                            if (!ErrMsg.Equals(""))
                                _logger.Info(ErrMsg);
                        }

                        result = true;
                    }
                    catch (Exception ex)
                    {
                        result = false;
                        ErrMsg = string.Format("[{0}][{1}][{2}][{3}]", "Exception", funcName, "Foreach", ex.Message);
                    }
                }
                else
                {
                    result = true;
                }
            }
            catch (Exception ex)
            {
                result = false;
                ErrMsg = string.Format("[{0}][{1}][{2}][{3}]", "Exception", funcName, "OutSide", ex.Message);
            }

            if (!ErrMsg.Equals(""))
                _logger.Info(ErrMsg);

            return result;
        }
        public string GetJArrayValue(JObject _JArray, string key)
        {
            string value = "";
            //foreach (JToken item in _JArray.Children())
            //{
            //var itemProperties = item.Children<JProperty>();
            //If the property name is equal to key, we get the value
            //var myElement = itemProperties.FirstOrDefault(x => x.Name == key.ToString());
            //value = myElement.Value.ToString(); //It run into an exception here because myElement is null
            //break;
            //}
            try {
                if (_JArray.TryGetValue(key, out JToken makeToken))
                {
                    value = (string)makeToken;
                }
            }
            catch(Exception ex) { }
 
            return value;
        }
        public string TryConvertDatetime(string _datetime)
        {
            string value = "";
            DateTime _tmpDate;

            try
            {
                
                DateTime.TryParse(_datetime, out _tmpDate);
                value = _tmpDate.ToString("yyyy/MM/dd HH:mm:ss");
            }
            catch (Exception ex) { }

            return value;
        }
        public bool SyncEotdData(DBTool _dbTool, IConfiguration configuration, ILogger _logger)
        {
            Boolean value = false;
            DataTable dt = null;
            DataTable dtTemp = null;
            string tmpMsg = "";

            //// 查詢儲存貨架資料
            dt = _dbTool.GetDataTable(_BaseDataService.QueryAllLotOnERack());
            if (dt.Rows.Count > 0)
            {
                string _lotid = "";
                string _eotd = "";
                string _newEotd = "";
                string _exceptionMsg = "";

                foreach (DataRow dr2 in dt.Rows)
                {
                    try {

                        _lotid = dr2["lotid"].ToString();
                        _eotd = dr2["eotd"] is null ? "" : dr2["eotd"].ToString();

                        dtTemp = _dbTool.GetDataTable(_BaseDataService.GetNewEotdByLot(_lotid));
                        if (dtTemp.Rows.Count > 0)
                        {
                            _newEotd = dtTemp.Rows[0]["eotd"] is null? "" : dtTemp.Rows[0]["eotd"].ToString();
                        }

                        if (!_newEotd.Equals(""))
                        {
                            if (!_newEotd.Equals(_eotd))
                            {
                                _dbTool.SQLExec(_BaseDataService.UpdateEotdToLotInfo(_lotid, _newEotd), out tmpMsg, true);
                                if (!tmpMsg.Equals(""))
                                {
                                    _exceptionMsg = string.Format("[{0}][{1}][{2}]", "SyncEotdData", "UpdateEotdToLotInfo", tmpMsg);
                                    _logger.Debug(_exceptionMsg);
                                }
                            }
                        }
                    }
                    catch(Exception ex) {
                        _exceptionMsg = string.Format("[{0}][{1}][{2}]", "SyncEotdData", "Exception", ex.Message);
                        _logger.Debug(_exceptionMsg);
                    }
                }
            }

            return value;
        }
    }
}
