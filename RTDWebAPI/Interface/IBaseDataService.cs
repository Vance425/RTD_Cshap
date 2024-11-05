﻿using RTDWebAPI.Commons.DataRelated.SQLSentence;
using RTDWebAPI.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RTDWebAPI.Interface
{
    public interface IBaseDataService
    {
        string CheckIsAvailableLot(string lotId, string equipment);
        string GetEquipState(int _iState);
        string InsertTableLotInfo(string _resourceTable, string LotID);
        string QueryERackInfo();
        string InsertTableEqpStatus();
        string InsertTableWorkinprocess_Sch(SchemaWorkInProcessSch _workInProcessSch, string _table);
        string InsertInfoCarrierTransfer(string _carrierId);
        string DeleteWorkInProcessSchByCmdId(string _commandID, string _table);
        string DeleteWorkInProcessSchByGId(string _gID, string _table);
        string UpdateLockWorkInProcessSchByCmdId(string _commandID, string _table);
        string UpdateUnlockWorkInProcessSchByCmdId(string _commandID, string _table);
        string SelectAvailableCarrierByCarrierType(string _carrierType, bool isFull);
        string SelectTableCarrierAssociateByCarrierID(string CarrierID);
        string QueryLotInfoByCarrierID(string CarrierID);
        string QueryErackInfoByLotID(string _args, string _lotID);
        string SelectTableCarrierAssociateByLotid(string lotID);
        string SelectTableCarrierAssociate2ByLotid(string lotID);
        string SelectTableCarrierAssociate3ByLotid(string lotID);
        string GetCarrierByLocate(string _locate, int _number);
        string QueryCarrierByLot(string lotID);
        string QueryCarrierByLocate(string locate, string _table);
        string QueryCarrierByLocateType(string locateType, string eqpId, string _table);
        string QueryEquipmentByLot(string lotID);
        string QueryEquipmentPortIdByEquip(string equipId);
        string QueryReworkEquipment();
        string QuerySemiAutoEquipmentPortId(bool _semiAuto);
        string QueryEquipmentPortInfoByEquip(string equipId);
        string SelectTableWorkInProcessSch(string _table);
        string SelectTableWorkInProcessSchUnlock(string _table);
        string SelectTableWorkInProcessSchByCmdId(string CommandID, string _table);
        string QueryRunningWorkInProcessSchByCmdId(string CommandID, string _table);
        string QueryInitWorkInProcessSchByCmdId(string CommandID, string _table);
        string SelectTableWorkInProcessSchByLotId(string _lotid, string _table);
        string SelectTableWorkInProcessSchByEquip(string _Equip, string _table);
        string SelectTableWorkInProcessSchByEquipPort(string _Equip, string _PortId, string _table);
        string SelectTableWorkInProcessSchByPortId(string _PortId, string _table);
        string SelectTableWorkInProcessSchByCarrier(string _CarrierId, string _table);
        string QueryWorkInProcessSchByPortIdForUnload(string _PortId, string _table);
        string SelectTableEquipmentStatus([Optional] string Department);
        string SelectEquipmentPortInfo();
        string SelectTableEQPStatusInfoByEquipID(string EquipId);
        string SelectTableEQP_STATUSByEquipId(string EquipId);
        string SelectEqpStatusWaittoUnload();
        string SelectEqpStatusIsDownOutPortWaittoUnload();
        string SelectEqpStatusReadytoload();
        string SelectTableEquipmentPortsInfoByEquipId(string EquipId);
        string SelectTableEQP_Port_SetByEquipId(string EquipId);
        string SelectTableEQP_Port_SetByPortId(string EquipId);
        string SelectLoadPortCarrierByEquipId(string EquipId);
        string SelectTableCheckLotInfo(string _resourceTable);
        string SelectTableCheckLotInfoNoData(string _resourceTable);
        string SelectTableCarrierTransfer();
        string UpdateTableCarrierTransferByCarrier(string CarrierId, string State);
        string SelectTableCarrierTransferByCarrier(string CarrierID);
        string SelectTableCarrierType(string _carrierID);
        string SelectTableLotInfo();
        string SelectTableLotInfoByDept(string Dept);
        string SelectTableProcessLotInfo();
        string ReflushProcessLotInfo(bool _OnlyStage);
        string SelectTableProcessLotInfoByCustomer(string _customerName, string _equip);
        string QueryLastModifyDT();
        string SelectTableADSData(string _resourceTable);
        string SelectTableLotInfoByLotid(string LotId);
        string SelectTableLotInfoOfInit();
        string SelectTableLotInfoOfWait();
        string SelectTableLotInfoOfReady();
        string SelectTableEQUIP_MATRIX(string EqpId, string StageCode);
        string ShowTableEQUIP_MATRIX();
        string SelectPrefmap(string EqpId);
        string SelectCarrierAssociateIsTrue();
        string SelectRTDDefaultSet(string _parameter);
        string SelectRTDDefaultSetByType(string _parameter);
        string UpdateTableWorkInProcessSchByCmdId(string _cmd_Current_State, string _lastModify_DT, string _commandID, string _table);
        string UpdateTableWorkInProcessSchByUId(string _updateState, string _lastModify_DT, string _UID, string _table);
        string UpdateTableWorkInProcessSchHisByUId(string _uid);
        string UpdateTableWorkInProcessSchHisByCmdId(string _commandID);
        string UpdateTableCarrierTransfer(CarrierLocationUpdate oCarrierLoc);
        string UpdateTableReserveCarrier(string _carrierID, bool _reserve);
        string UpdateTableCarrierTransfer(CarrierLocationUpdate oCarrierLoc, int _haveMetalRing);
        string CarrierLocateReset(CarrierLocationUpdate oCarrierLoc, int _haveMetalRing);
        string UpdateTableEQP_STATUS(string EquipId, string CurrentStatus);
        string UpdateTableEQP_Port_Set(string EquipId, string PortSeq, string NewStatus);
        string UpdateTableLotInfoReset(string LotID);
        string UpdateTableLotInfoEquipmentList(string _lotID, string _lstEquipment);
        string UpdateTableLotInfoState(string LotID, string State);
        string UpdateTableLastModifyByLot(string LotID);
        string UpdateTableLotInfoSetCarrierAssociateByLotid(string LotID);
        string UpdateTableLotInfoToReadyByLotid(string LotID);
        string UpdateLotInfoSchSeqByLotid(string LotID, int _SchSeq);
        string UpdateTableLotInfoSetCarrierAssociate2ByLotid(string LotID);
        string UpdateTableRTDDefaultSet(string _parameter, string _paramvalue, string _modifyBy);
        string GetLoadPortCurrentState(string _equipId);
        string UpdateSchSeq(string _Customer, string _Stage, int _SchSeq, int _oriSeq);
        string UpdateSchSeqByLotId(string _lotId, string _Customer, int _SchSeq);
        string InsertSMSTriggerData(string _eqpid, string _stage, string _desc, string _flag, string _username);
        string SchSeqReflush(bool _OnlyStage);
        string LockLotInfo(bool _lock);
        string GetLockStateLotInfo();
        string ReflushWhenSeqZeroStateWait();
        string SyncNextStageOfLot(string _resourceTable, string _lotid);
        string SyncNextStageOfLotNoPriority(string _resourceTable, string _lotid);
        string UpdateLotInfoWhenCOMP(string _commandId, string _table);
        string UpdateLotInfoWhenFail(string _commandId, string _table);
        string UpdateEquipCurrentStatus(string _current, string _equipid);
        string QueryEquipmentStatusByEquip(string _equip);
        string QueryCarrierInfoByCarrierId(string _carrierId);
        string QueryCarrierType(string _carrierType, string _typeKey);
        string UpdateCarrierType(string _carrierType, string _typeKey);
        string QueryWorkinProcessSchHis(string _startTime, string _endTime);
        string QueryStatisticalOfDispatch(DateTime dtCurrUTCTime, string _statisticalUnit, string _type);
        string CalcStatisticalTimesFordiffZone(bool isStart, DateTime dtStartTime, DateTime dtEndTime, string _statisticalUnit, string _type, double _zone);
        string QueryRtdNewAlarm();
        string QueryAllRtdAlarm();
        string UpdateRtdAlarm(string _time);
        string QueryCarrierAssociateWhenIsNewBind();
        string QueryCarrierAssociateWhenOnErack(string _table);
        string ResetCarrierLotAssociateNewBind(string _carrierId);
        string QueryRTDStatisticalByCurrentHour(DateTime _datetime);
        string InitialRTDStatistical(string _datetime, string _type);
        string UpdateRTDStatistical(DateTime _datetime, string _type, int _count);
        string InsertRTDStatisticalRecord(string _datetime, string _commandid, string _type);
        string QueryRTDStatisticalRecord(string _datetime);
        string CleanRTDStatisticalRecord(DateTime _datetime, string _type);
        string SelectWorkgroupSet(string _EquipID);
        string InsertRTDAlarm(string[] _alarm);
        string InsertRTDAlarm(RTDAlarms _Alarms);
        string UpdateLotinfoState(string _lotID, string _state);
        string ConfirmLotinfoState(string _lotID, string _state);
        string QueryLotinfoQuantity(string _lotID);
        string UpdateLotinfoTotalQty(string _lotID, int _TotalQty);
        string CheckQtyforSameLotId(string _lotID, string _carrierType);
        string QueryQuantity2ByCarrier(string _carrierID);
        string QueryQuantityByCarrier(string _carrierID);
        string QueryEqpPortSet(string _equipId, string _portSeq);
        string InsertTableEqpPortSet(string[] _params);
        string QueryWorkgroupSet(string _Workgroup);
        string QueryWorkgroupSet(string _Workgroup, string _stage);
        string QueryWorkgroupSetAndUseState(string _Workgroup);
        string CreateWorkgroup(string _Workgroup);
        string UpdateWorkgroupSet(string _Workgroup, string _InRack, string _OutRack);
        string DeleteWorkgroup(string _Workgroup);
        string UpdateEquipWorkgroup(string _equip, string _Workgroup);
        string UpdateEquipPortSetWorkgroup(string _equip, string _Workgroup);
        string UpdateEquipPortModel(string _equip, string _portModel, int _portNum);
        string DeleteEqpPortSet(string _Equip, string _portModel);
        string QueryPortModelMapping(string _eqpTypeID);
        string QueryPortModelDef();
        string QueryProcLotInfo();
        string LockMachineByLot(string _lotid, int _Quantity, int _lock);
        string UpdateLotInfoForLockMachine(string _lotid);
        string CheckLocationByLotid(string _lotid);
        string QueryEQPType();
        string QueryEQPIDType();
        string InsertPromisStageEquipMatrix(string _stage, string _equipType, string _equipids, string _userId);
        string DeletePromisStageEquipMatrix(string _stage, string _equipType, string _equipids);
        string SyncStageInfo();
        string CheckRealTimeEQPState();
        string UpdateCurrentEQPStateByEquipid(string _equipid);
        string QueryEquipListFirst(string _lotid, string _equipid);
        string QueryRackByGroupID(string _groupID);
        string QueryExtenalCarrierInfo(string _table);
        string InsertCarrierLotAsso(CarrierLotAssociate _carrierLotAsso);
        string InsertCarrierTransfer(string _carrierId, string _typeKey, string _quantity);
        string UpdateCarrierLotAsso(CarrierLotAssociate _carrierLotAsso);
        string UpdateLastCarrierLotAsso(CarrierLotAssociate _carrierLotAsso);
        string UpdateCarrierTransfer(string _carrierId, string _typeKey, string _quantity);
        string LockEquip(string _equip, bool _lock);
        string QueryEquipLockState(string _equip);
        string QueryPreTransferList(string _lotTable);
        string CheckCarrierLocate(string _inErack, string _locate);
        string CheckPreTransfer(string _carrierid, string _table);
        string ManualModeSwitch(string _equip, bool _autoMode);
        string QueryLotStageWhenStageChange(string _table);
        string QueryReserveStateByEquipid(string _equipid);
        string InsertEquipReserve(string _args);
        string UpdateEquipReserve(string _args);
        string LockLotInfoWhenReady(string _lotID);
        string UnLockLotInfoWhenReady(string _lotID);
        string UnLockAllLotInfoWhenReadyandLock();
        string QueryLotInfoByCarrier(string _carrierid);
        string CheckReserveState(string _args);
        string UpdateStageByLot(string _lotid, string _stage);
        string QueryLastLotFromEqpPort(string EquipId, string PortSeq);
        string UpdateLastLotIDtoEQPPortSet(string EquipId, string PortSeq, string LastLotID);
        string ConfirmLotInfo(string _lotid);
        string IssueLotInfo(string _lotid);
        string CheckLotStage(string _table, string _lotid);
        public string EQPListReset(string LotID);
        string GetEquipCustDevice(string EquipID);
        string CheckMetalRingCarrier(string _carrierID);
        string UpdatePriorityByLotid(string _lotID, int _priority);
        string QueryDataByLotid(string _lotID, string _table);
        string HisCommandAppend(HisCommandStatus hisCommandStatus);
        string GetWorkinprocessSchByCommand(string command, string _table);
        string GetRTSEquipStatus(string _table, string equipid);
        string GetHistoryCommands(string StartTime, string EndTime);
        string ResetRTDStateByLot(string LotID);
        string UpdateCustDeviceByEquipID(string _equipID, string _custDevice);
        string UpdateCustDeviceByLotID(string _lotID, string _custDevice);
        string InsertHisTSCAlarm(TSCAlarmCollect _alarmCollect);
        string SetPreDispatching(string _Workgroup, string _Type);
        string CalcStatisticalTimes(string StartTime, string EndTime);
        string QueryCarrierByCarrierID(string _carrierID);
        string QueryListAlarmDetail();
        string QueryAlarmDetailByCode(string _alarmCode);
        string UpdateLotAgeByLotID(string _lotID, string _lotAge);
        string QueryAvailbleAOIMachineByLotid(string _lotID, string _table);
        string CheckMeasurementAndThickness(string _table, string _lotID, string _Stage, string _EquipID);
        string LockEquipPortByPortId(string _portID, bool _lock);
        string CheckLotStageHold(string _table, string _lotid);
        string CheckPortStateIsUnload(string _eqpip);
        string QueryLCASInfoByCarrierID(string _carrierID);
        string QueryOrderWhenOvertime(string _overtime, string _table);
        string AutoResetCarrierReserveState(string _table);
        string GetQTimeLot(string _table, string _lotid);
        string CheckLookupTable(string _table, string _equip, string _lotid);
        string EnableEqpipPort(string _portID, Boolean _enabled);
        string QueryHistoryCommandsByCommandID(string _commandID);
        string UpdateAlarmCodeByCommandID(string _commandID, string _alarmCode);
        string QueryLookupTable(string _table, string _lotid);
        string CheckCarrierTypeOfQTime(string _equipid, string _stage, string _carrierType);
        string SelectAvailableCarrierByCarrierTypeLocate(string _carrierType, string _locate, bool isFull);
        string CheckReserveTimeByEqpID(string _equipID);
        string GetUserAccountType(string _userID);
        string QueryQTimeData(string _adsTable, string _qTimeTable, string _lotid, string _locate);
        string GetAvailableCarrierByLocateOrderbyQTime(string _adsTable, string _qTimeTable, string _carrierType, string _lotid, string _locate, bool isFull, bool _layerFirst, string _workgroup);
        string QueryQtimeOfOnlineCarrier(string _table, string _lotid);
        string UpdateQtimeToLotInfo(string _lotid, float _qtime, string _pkgfullname);
        string UpdateStageToLotInfo(string _lotid, string _stage, string _pkgfullname);
        string QueryeRackDisplayBylotid(string _table, string _lotid);
        string UpdateEotdToLotInfo(string _lotid, string _eotd);
        string GetAvailableCarrierForUatByLocateOrderbyQTime(string _adsTable, string _qTimeTable, string _carrierType, string _lotid, string _locate, bool isFull, bool _layerFirst, string _workgroup);
        string SelectAvailableCarrierForUATByCarrierType(string _carrierType, bool isFull);
        string QueryPreTransferListForUat(string _lotTable);
        string SelectAvailableCarrierForUatByCarrierTypeLocate(string _carrierType, string _locate, bool isFull);
        string QueryCarrierByCarrierId(string _carrierId);
        string UpdateCarrierToUAT(string _carrierId, bool _isuat);
        string GetLastStageOutErack(string _lotid, string _table, string _eqpworkgroup);
        string GetPROCLotInfo();
        string SetResetPreTransfer(string _Workgroup, string _stage, Boolean _set);
        string GetMidwayPoint(string _adstable, string _workgroup, string _lotid);
        string QueryMidwaysSet(string _workgroup, string _stage, string _locate);
        string QueryDataByLot(string _table, string _lotID);
        string QueryPortInfobyPortID(string _portID);
        string ChangePortInfobyPortID(string _portID, string _attribute, string _value, string _userid);
        string QueryCarrierInfoByCarrier(string CarrierID);
        string UpdateFVCStatus(string _equipID, int _status);
        string GetAvailableCarrierForFVC(string _adsTable, string _qTimeTable, string _carrierType, List<string> _agvs, bool isFull);
        string QueryLocateBySector(string _sector);
        string QueryFurneceEQP();
        string QueryFurneceOutErack(string _equipid);
        string GetAllOfSysHoldCarrier();
        string UpdateTableEQUIP_MATRIX(string EqpId, string StageCode, bool _turnOn);
        string UpdateEffectiveSlot(string _lstSlot, string _workgroup);
        string QueryEQUIP_MATRIX(string EqpId, string Stage);
        string CheckCarrierNumberforVFC(string _erackID, string _lstPortNo);
        string UpdateCarrierTypeForEQPort(string _portID, string _carrierType);
        string QueryCarrierType();
        string UpdateRecipeToLotInfo(string _lotid, string _recipe);
        string QueryRecipeSetting(string _equipid);
        string QueryCurrentEquipRecipe(string _equipid);
        string QueryCarrierOnRack(string _workgroup, string _equip);
        string UnbindLot(CarrierLotAssociate _carrierLotAsso);
        string QueryLCASInfoByLotID(string _lotID);
        string QueryTransferListForSideWH(string _lotTable);
        string QueryTransferListUIForSideWH(string _lotTable);
        string CalculateLoadportQtyByStage(string _workgroup, string _stage, string _lotstage);
        string CalculateProcessQtyByStage(string _workgroup, string _stage, string _lotstage);
        string CheckLocateofSideWh(string _locate, string _sideWh);
        string CleanLotInfo(int _iDays);
        string GetEqpInfoByWorkgroupStage(string _workgroup, string _stage, string _lotstage);
        string GetCarrierByLocate(string _locate);
        string ResetFurnaceState(string _equipid);
        string ResetWorkgroupforFurnace(string _equipid);
        string QueryIslockPortId();
        string QueryEqpStatusNotSame(string _table);
        string UpdateEquipMachineStatus(string _machine, string _current, string _down, string _equipid);
        string QueryStageCtrlListByPortNo(string _portID);
        string QueryRTDAlarmAct(string _portID);
        string QueryRTDAlarmStatisByCode(string _portID, string _alarmCode);
        string CleanPortAlarmStatis(string _portID);
        string UpdateRetryTime(string _portID, string _alarmCode, int _failedTime);
        string InsertPortAlarmStatis(string _portID, string _alarmCode, int _failedTime);
        string QueryPortAlarmStatis(string _portID, string _alarmCode);
        string QueryRTDAlarms();
        string QueryExistRTDAlarms(string _args);
        string UpdateRTDAlarms(bool _reset, string _args, string _detail);
        string GetDispatchingPriority(string _lotID);
        string GetCarrierTypeByPort(string _portID);
        string SetResetCheckLookupTable(string _Workgroup, string _Stage, Boolean _set);
        string SetResetParams(string _Workgroup, string _Stage, string _paramsName, Boolean _set);
        string GetHisWorkinprocessSchByCommand(string command);
        string GetSetofLookupTable(string _workgroup, string _stage);
        string QueryRTDServer(string _server);
        string InsertRTDServer(string _server);
        string UadateRTDServer(string _server);
        string QueryResponseTime(string _server);
        string InsertResponseTime(string _server);
        string UadateResponseTime(string _server);
        string UadateHoldTimes(string _lotid);
        string CheckRcpConstraint(string _table, string _equip, string _lotid);
        string WriteNewLocationForCarrier(CarrierLocationUpdate _carrierLocation, string _lotid);
        string SetWorkgroupSetByWorkgroupStage(string _Workgroup, string _Stage, string _params, bool _Sw);
        string QueryCommandsTypeByCarrierID(string _carrierID);
        string QueryMaxPriorityByCmdID(string _commandID);
        string QueryStageControl();
        string CloneWorkgroupsForWorkgroupSetByWorkgroup(string _newWorkgroup, string _Workgroup);
        string CloneStageForWorkgroupSetByWorkgroupStage(string _newStage, string _workgroup, string _stage);
        string SetParameterWorkgroupSetByWorkgroupStage(string _Workgroup, string _Stage, string _paramsName, object _paramsValue);
        string QueryMidways(QueryMidways _cond);
        string InsertMidways(InsertMidways _cond);
        string TurnOnMidways(string _idx, bool turnOn);
        string ResetMidways(string _idx, bool _del);
        string ResetMidways(string _workgroup, string _stage, bool _del);
        string QueryStageControlAllList(StageControlList _cond);
        string QueryStageControlList(StageControlList _cond);
        string InsertStageControl(StageControlList _cond);
        string TurnOnStageControl(TurnOnStageControl _cond, bool turnOn);
        string ResetStageControl(StageControlList _cond, bool _del);
        string DeleteStageControl(StageControlList _cond, bool _remove);
        string CarrierTransferDTUpdate(string _carrierid, string _updateType);
        string QueryAllLotOnERack();
        string GetNewEotdByLot(string _lotid);
        string GetDataFromTableByLot(string _table, string _colname, string _lotid);
        string UpdateTurnRatioToLotInfo(string _lotid, string _turnratio);
        string ResetSchseqByModel(string _seq, string _cdt);
        string SelectEquipPortInfo2();
        string QueryERackInfo2();
        string UpdatePriorityForWorkgroupSet(string _Workgroup, string _stage, int _priority);
    }
}
