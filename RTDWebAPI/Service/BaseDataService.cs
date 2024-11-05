using RTDWebAPI.Commons.DataRelated.SQLSentence;
using RTDWebAPI.Interface;
using RTDWebAPI.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RTDWebAPI.Service
{
    public class BaseDataService : IBaseDataService
    {
        public string CheckIsAvailableLot(string lotId, string equipMent)
        {
            string strSQL = string.Format("select * from LOT_INFO a left join carrier_lot_associate b on b.lot_id = a.lotid left join carrier_transfer c on c.carrier_id = b.carrier_id where a.lotid = '{0}' and instr(a.equiplist, '{1}') > 0 and c.reserve = 0", lotId, equipMent);
            return strSQL;
        }
        public string GetEquipState(int _iState)
        {
            string State = "";
            switch (_iState)
            {
                case 0:
                    State = "DOWN";
                    break;
                case 1:
                    State = "PM";
                    break;
                case 2:
                    State = "IDLE";
                    break;
                case 3:
                    State = "UP";
                    break;
                case 4:
                    State = "PAUSE";
                    break;
                default:
                    State = String.Format("UNKNOW,{0}", _iState.ToString());
                    break;
            }
            return State;
        }
        public string InsertTableLotInfo(string _resourceTable, string LotID)
        {
            string strSQL = "";

            strSQL = string.Format(@"insert into LOT_INFO (lotid, stage, customername, priority, wfr_qty, dies_qty, carrier_asso, equip_asso, equiplist, state, Rtd_State, planstarttime, starttime, partid, lottype, lot_age, create_dt, lastmodify_dt, custDevice)
                                            select a.lotid, a.stage, a.customername, a.priority, a.wfr_qty, a.dies_qty, 'N' as carrier_asso, 'N' as equip_asso, '' as equiplist, a.state, 'INIT' as rtd_state,
                                            a.planstarttime, a.starttime, a.partid, a.lottype, a.lot_age, 
                                            sysdate as create_dt, sysdate as lastmodify_dt, '' as custDevice from {0} a where lotid = '{1}'", _resourceTable, LotID);

            return strSQL;
        }
        public string InsertTableEqpStatus()
        {
            string strSQL = "";
#if DEBUG
            strSQL = string.Format(@"insert into EQP_STATUS (equipid, equip_dept, Equip_TypeID, Equip_Type, Machine_State, Curr_Status, Down_state, port_Model, Port_Number, Workgroup, Near_Stocker, create_dt, lastmodify_dt)
                                            select equipid, equip_dept, TypeID as Equip_TypeID, Equip_Type, Machine_State, Curr_Status, Down_state, '' as port_Model, 
                                            '' as Port_Number, Equip_Type as Workgroup, '' as Near_Stocker, sysdate as create_dt, sysdate as lastmodify_dt
                                            from EQP_STATUS_INFO 
                                            where equipid in (
                                            select equipid from (
                                            select e.equipid, case when d.equipid is null then 'New' else 'Old' end as State from (
                                            select distinct c.equipid from (
                                            select a.equipid from EQP_STATUS_INFO a 
                                            union 
                                            select b.equipid from EQP_STATUS b) c) e
                                            left join eqp_status d on d.equipid=e.equipid)
                                            where state = 'New')");
#else
            strSQL = string.Format(@"insert into EQP_STATUS (equipid, equip_dept, Equip_TypeID, Equip_Type, Machine_State, Curr_Status, Down_state, port_Model, Port_Number, Workgroup, Near_Stocker, create_dt, lastmodify_dt)                              
                                            select f.equipid, g.equip_dept, f.equip_typeid, g.equip_type, f.machine_state, f.curr_status, f.down_state, f.port_model, f.port_number, g.equip_type as workgroup, f.near_stocker, f.create_dt, f.lastmodify_dt from (
                                            select equipid,  TypeID as Equip_TypeID,  Machine_State, Curr_Status, Down_state, '' as port_Model, 
                                            '' as Port_Number,  '' as Near_Stocker, sysdate as create_dt, sysdate as lastmodify_dt
                                            from rts_active@CIMDB3.world 
                                            where equipid in (
                                            select equipid from (
                                            select e.equipid, case when d.equipid is null then 'New' else 'Old' end as State from (
                                            select distinct c.equipid from (
                                            select a.equipid from rts_active@CIMDB3.world a 
                                            union 
                                            select b.equipid from eqp_status b) c) e
                                            left join eqp_status d on d.equipid=e.equipid)
                                            where state = 'New')) f
                                            left join rts_equipment@CIMDB3.world g on g.equipid = f.equipid");
#endif
            return strSQL;
        }
        public string InsertTableWorkinprocess_Sch(SchemaWorkInProcessSch _workInProcessSch, string _table)
        {
            //string _table = "workinprocess_sch";
            string _values = string.Format(@"'{0}', '{1}', '{2}', '{3}', ' ', ' ', '{4}', '{5}', '{6}', '{7}', {8}, {9}, '*', 0, '{10}', '{11}', sysdate, sysdate, {12}, {13}, {14}",
                            _workInProcessSch.UUID, _workInProcessSch.Cmd_Id, _workInProcessSch.Cmd_Type, _workInProcessSch.EquipId, _workInProcessSch.CarrierId,
                            _workInProcessSch.CarrierType, _workInProcessSch.Source, _workInProcessSch.Dest, _workInProcessSch.Priority, _workInProcessSch.Replace, _workInProcessSch.LotID, _workInProcessSch.Customer, _workInProcessSch.Quantity, _workInProcessSch.Total, _workInProcessSch.IsLastLot);


            string strSQL = string.Format(@"insert into {0} (uuid, cmd_id, cmd_type, equipid, cmd_state, cmd_current_state, carrierid, carriertype, source, dest, priority, replace, back, isLock, lotid, customer, create_dt, modify_dt, quantity, total, islastlot)
                            values ({1})", _table, _values);

            return strSQL;
        }
        public string InsertInfoCarrierTransfer(string _carrierId)
        {
            string strSQL = "";
            strSQL = string.Format(@"insert into CARRIER_TRANSFER (carrier_id, type_key, carrier_state, enable, create_dt, modify_dt, lastmodify_dt)
                                            select carrier_id, '', 'OFFLINE', 1, create_dt, modify_dt, sysdate from carrier_info where carrier_id='{0}'", _carrierId);

            return strSQL;
        }
        public string DeleteWorkInProcessSchByCmdId(string _commandID, string _table)
        {
            //string _table = "workinprocess_sch";
            string tmpString = "delete {0} where cmd_id = '{1}'";
            string strSQL = string.Format(tmpString, _table, _commandID);
            return strSQL;
        }
        public string DeleteWorkInProcessSchByGId(string _gID, string _table)
        {
            //string _table = "workinprocess_sch";
            string tmpString = "delete {0} where uuid = '{1}'";
            string strSQL = string.Format(tmpString, _table, _gID);
            return strSQL;
        }
        public string UpdateLockWorkInProcessSchByCmdId(string _commandID, string _table)
        {
            //string _table = "workinprocess_sch";
            string tmpString = "update {0} set IsLock = 1 where cmd_id = '{1}'";
            string strSQL = string.Format(tmpString, _table, _commandID);
            return strSQL;
        }
        public string UpdateUnlockWorkInProcessSchByCmdId(string _commandID, string _table)
        {
            //string _table = "workinprocess_sch";
            string tmpString = "update {0} set IsLock = 0, lastModify_dt = sysdate where cmd_id = '{1}'";
            string strSQL = string.Format(tmpString, _table, _commandID);
            return strSQL;
        }
        public string SelectAvailableCarrierByCarrierType(string _carrierType, bool isFull)
        {
            string strSQL = "";
            if (isFull)
            {
                strSQL = @"select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, case when d.total_qty is null then 0 else d.total_qty end as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                left join LOT_INFO d on d.lotid=c.lot_id
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat = 0 and d.state not in ('HOLD')
                                    and a.location_type in ('ERACK','STOCKER') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '" + _carrierType.Trim() + "') and c.lot_id is not null order by d.sch_seq";
            }
            else
            {
                strSQL = @"select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, 0 as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat = 0
                                    and a.location_type in ('ERACK','STOCKER') and c.associate_state not in ('Initiated', 'Associated With Lot', 'Error', 'Associated With None') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '" + _carrierType.Trim() + "') and a.locate in (select paramvalue from RTD_DEFAULT_SET where parameter='EmptyERack' and paramtype='" + _carrierType.Trim() + "') and c.associate_state = 'Unknown' and c.lot_id is null";
            }
            return strSQL;
        }
        public string SelectAvailableCarrierForUATByCarrierType(string _carrierType, bool isFull)
        {
            string strSQL = "";
            if (isFull)
            {
                strSQL = @"select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, case when d.total_qty is null then 0 else d.total_qty end as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                left join LOT_INFO d on d.lotid=c.lot_id
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat = 1 and d.state not in ('HOLD')
                                    and a.location_type in ('ERACK','STOCKER') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '" + _carrierType.Trim() + "') and c.lot_id is not null order by d.sch_seq";
            }
            else
            {
                strSQL = @"select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, 0 as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat = 1 
                                    and a.location_type in ('ERACK','STOCKER') and c.associate_state not in ('Initiated', 'Associated With Lot', 'Error', 'Associated With None') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '" + _carrierType.Trim() + "') and a.locate in (select paramvalue from RTD_DEFAULT_SET where parameter='EmptyERack' and paramtype='" + _carrierType.Trim() + "') and c.associate_state = 'Unknown' and c.lot_id is null";
            }
            return strSQL;
        }
        public string SelectTableCarrierAssociateByCarrierID(string CarrierID)
        {
            string strSQL = string.Format(@"select distinct c.command_type, a.* from CARRIER_LOT_ASSOCIATE a 
left join carrier_transfer b on b.carrier_id =a.carrier_id
left join carrier_type_set c on c.type_key=b.type_key
where a.carrier_id = '{0}'", CarrierID);
            return strSQL;
        }
        public string QueryLotInfoByCarrierID(string CarrierID)
        {
            string tmpWhere = "";
            string tmpSet = "";
            string strSQL = "";

            //a.lot_id as lotid, c.lotType, c.stage, c.state, c.CUSTOMERNAME, c.HOLDCODE, c.HOLDREAS
            strSQL = string.Format(@"select distinct a.carrier_id, a.tag_type, a.associate_state, a.lot_id, a.quantity, a.last_lot_id, a.last_change_station,
                                    a.create_by, b.carrier_asso, b.equip_asso, b.equiplist, b.state, b.customername, b.stage, b.partid, b.lottype, 
                                    b.rtd_state, b.sch_seq, b.islock, b.total_qty, b.lockmachine, b.comp_qty, b.custdevice, b.lotid, nvl(b.priority, 0) as priority from CARRIER_LOT_ASSOCIATE a
                                            left join LOT_INFO b on b.lotid = a.lot_id
                                            where a.carrier_id = '{0}'", CarrierID);
            return strSQL;
        }

        public string QueryErackInfoByLotID(string _args, string _lotID)
        {
            string tmpWhere = "";
            string tmpSet = "";
            string strSQL = "";
            string[] tmpParams = _args.Split(',');
            string _work = tmpParams[0];
            string _table = tmpParams[1];

            if (_lotID.Trim().Equals(""))
                return strSQL;

            switch (_work.ToLower())
            {
                case "cis":
                    tmpSet = "a.lotid, a.partname, a.customername, a.wl_waferlotid as waferlotid, a.stage, a.state, a.lottype, a.waiting_inspection as HoldReas, a.holdcode as holdcode, a.automotive, '' as turnratio, ''  as eotd, a.potd";
                    break;
                case "ewlb":
                    //--lotid, partname, customername, stage, state, lottype, hold_code, eotd, automotive, turnratio
                    tmpSet = "a.lotid, a.partname, a.customername, a.stage, a.state, a.lottype, a.hold_code as holdcode, a.automotive, a.turnratio, to_char(a.eotd, 'DD/MM/YYYY')  as eotd, '' as potd, '' as waferlotid, '' as HoldReas";
                    break;
                default:
                    tmpSet = "a.lotid, c.lotType, c.stage, c.state, c.CUSTOMERNAME, c.HOLDCODE, c.HOLDREAS, b.Partid, '' as Automotive, '' as waferlotid";
                    break;
            }

            //a.lot_id as lotid, c.lotType, c.stage, c.state, c.CUSTOMERNAME, c.HOLDCODE, c.HOLDREAS
            strSQL = string.Format(@"select distinct {0} from {1} a
                                            where a.lotid = '{2}'", tmpSet, _table, _lotID);
            return strSQL;
        }
        public string SelectTableADSData(string _resourceTable)
        {
            string strSQL = "";

            strSQL = string.Format(@"select a.lotid, a.customername, a.priority, a.wfr_qty, a.dies_qty, 'N' as carrier_asso, 'N' as equip_asso, '' as equiplist, 'INIT' as state,
                                            sysdate as create_dt, sysdate as lastmoify_dt from {0} a", _resourceTable);

            return strSQL;
        }
        public string SelectTableCarrierTransfer()
        {
            string strSQL = string.Format(@"select distinct a.*,b.type_key as type,b.command_type, c.lot_id  from CARRIER_TRANSFER a
                                            left join CARRIER_TYPE_SET b on b.type_key=a.type_key
                                            left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id");
            return strSQL;
        }
        public string SelectTableCarrierTransferByCarrier(string CarrierID)
        {
            string strSQL = string.Format(@"select distinct a.Carrier_Id, a.Carrier_State, a.Locate, a.portno, a.location_type, a.metal_ring, 
                                                b.type_key as carrier_type, b.command_type, c.lot_id, c.quantity, d.stage from CARRIER_TRANSFER a
                                            left join CARRIER_TYPE_SET b on b.type_key=a.type_key
                                            left join CARRIER_LOT_ASSOCIATE c on c.carrier_id=a.carrier_id
                                            left join LOT_INFO d on d.lotid = c.lot_id
                                            where Enable = '1' and a.carrier_id = '{0}'", CarrierID);
            return strSQL;
        }
        public string SelectTableCarrierType(string _lotID)

        {
            string strSQL = string.Format(@"select * from CARRIER_LOT_ASSOCIATE a
                                            left join CARRIER_TRANSFER b on b.carrier_id=a.carrier_id
                                            left join CARRIER_TYPE_SET c on c.type_key = b.type_key
                                            where b.carrier_state='ONLINE' and b.enable = 1 
                                                   and a.lot_id = '{0}'", _lotID.Trim());
            return strSQL;
        }
        public string SelectTableCarrierAssociateByLotid(string lotID)
        {
            string strSQL = string.Format(@"select a.Carrier_Id, a.Carrier_State, a.location_type, a.metal_ring, a.reserve, a.enable, a.lastModify_dt,
                                            b.carrier_type, b.command_type, c.lot_id, c.quantity, c.update_time from CARRIER_TRANSFER a
                                            left join CARRIER_TYPE_SET b on b.type_key=a.type_key
                                            left join CARRIER_LOT_ASSOCIATE c on c.carrier_id=a.carrier_id
                                            where a.enable = 1 and c.lot_id = '{0}'", lotID.Trim());
            return strSQL;
        }
        public string SelectTableCarrierAssociate2ByLotid(string lotID)
        {
            string strSQL = string.Format("select * from CARRIER_LOT_ASSOCIATE a left join CARRIER_TRANSFER b on b.carrier_id=a.carrier_id where a.lot_id = '{0}'", lotID);
            return strSQL;
        }
        public string SelectTableCarrierAssociate3ByLotid(string lotID)
        {
            string strSQL = string.Format("select a.carrier_id from CARRIER_LOT_ASSOCIATE a left join CARRIER_TRANSFER b on b.carrier_id=a.carrier_id where(a.lot_id is null and a.last_lot_id = '{0}') or (a.lot_id is not null and a.lot_id = '{0}')", lotID);
            return strSQL;
        }
        public string GetCarrierByLocate(string _locate, int _number)
        {
            string strSQL = string.Format(@"select carrier_id from CARRIER_TRANSFER where locate = '{0}' and portno = {1}", _locate, _number);
            return strSQL;
        }
        public string QueryCarrierByLot(string lotID)
        {
            string strSQL = string.Format(@"select a.carrier_id from CARRIER_TRANSFER a
                                            left join CARRIER_LOT_ASSOCIATE b on b.carrier_id=a.carrier_id
                                            where b.lot_id = '{0}' order by a.carrier_id", lotID);
            return strSQL;
        }
        public string QueryCarrierByLocate(string locate, string _table)
        {
            string strSQL = "";

            //semi_int.actl_ewlberack_vw@semi_int
            //20240731 change sequence priority > EOTD > layer >>order by c.sch_seq asc, eotd(b.lot_id) asc, a.carrier_id >>order by eotd(b.lot_id) asc, c.sch_seq asc, a.carrier_id
            ///20241028 eotd(b.lot_id) change to c.enddate //20241104 rollback
#if DEBUG
            strSQL = string.Format(@"select a.carrier_id, b.lot_id, eotd, nvl(c.sch_seq, 0) as sch_seq from CARRIER_TRANSFER a
                                            left join CARRIER_LOT_ASSOCIATE b on b.carrier_id=a.carrier_id
                                            left join lot_info c on c.lotid=b.lot_id
                                            where a.locate = '{0}' order by eotd asc, c.sch_seq asc, a.carrier_id", locate);
#else
            strSQL = string.Format(@"select a.carrier_id, b.lot_id, eotd(b.lot_id) as eotd, nvl(c.sch_seq, 0) as sch_seq from CARRIER_TRANSFER a
                                            left join CARRIER_LOT_ASSOCIATE b on b.carrier_id=a.carrier_id
                                            left join lot_info c on c.lotid=b.lot_id
                                            where a.locate = '{0}' order by eotd(b.lot_id) asc, c.sch_seq asc, a.carrier_id", locate);
#endif
            return strSQL;
        }
        public string QueryCarrierByLocateType(string locateType, string eqpId, string _table)
        {
            string strSQL = "";
            //semi_int.actl_ewlberack_vw@semi_int
            //20241028 eotd(b.lot_id) change to c.enddate   //20241104 rollback
#if DEBUG
            strSQL = string.Format(@"select a.carrier_id, b.lot_id, eotd, nvl(c.sch_seq, 0) as sch_seq, c.equiplist, a.location_type from CARRIER_TRANSFER a
                                            left join CARRIER_LOT_ASSOCIATE b on b.carrier_id=a.carrier_id
                                            left join lot_info c on c.lotid = b.lot_id and c.rtd_state = 'READY'
                                            where a.location_type = '{0}' and b.lot_id is not null and instr(c.equiplist, '{1}') > 0
                                            order by eotd asc, c.sch_seq, a.carrier_id", locateType, eqpId);
#else
            strSQL = string.Format(@"select a.carrier_id, b.lot_id, eotd(b.lot_id) as eotd, nvl(c.sch_seq, 0) as sch_seq, c.equiplist, a.location_type from CARRIER_TRANSFER a
                                            left join CARRIER_LOT_ASSOCIATE b on b.carrier_id=a.carrier_id
                                            left join lot_info c on c.lotid = b.lot_id and c.rtd_state = 'READY'
                                            where a.location_type = '{0}' and b.lot_id is not null and instr(c.equiplist, '{1}') > 0
                                            order by eotd(b.lot_id) asc, c.sch_seq, a.carrier_id", locateType, eqpId);
#endif
            return strSQL;
        }
        public string QueryEquipmentByLot(string lotID)
        {
            string strSQL = string.Format("select equiplist from LOT_INFO where lotid = '{0}'", lotID);
            return strSQL;
        }
        public string QueryEquipmentPortIdByEquip(string equipId)
        {
            string strSQL = string.Format(@"select b.port_id from EQP_STATUS a 
                                            left join EQP_PORT_SET b on b.equipid = a.equipid
                                            where a.equipid = '{0}'", equipId);
            return strSQL;
        }
        public string QueryReworkEquipment()
        {
            string strSQL = string.Format(@"select b.port_id from EQP_STATUS a 
                                            left join EQP_PORT_SET b on b.equipid = a.equipid
                                            where a.manualmode = 1 and b.port_id is not null");
            return strSQL;
        }
        public string QuerySemiAutoEquipmentPortId(bool _semiAuto)
        {
            int _auto = 0;
            if (_semiAuto)
                _auto = 1;
            else
                _auto = 0;

            string strSQL = string.Format(@"select b.port_id from EQP_STATUS a 
                                            left join EQP_PORT_SET b on b.equipid = a.equipid
                                            where a.automode = {0} and b.port_id is not null", _auto);
            return strSQL;
        }
        public string QueryEquipmentPortInfoByEquip(string equipId)
        {
            /*string strSQL = string.Format(@"select distinct a.equipid, b.port_seq, b.port_id, b.port_state, b.port_type, b.carrier_type, c.description from eqp_status a
                                            left join EQP_PORT_SET b on b.equipid = a.equipid
                                            left join RTD_DEFAULT_SET c on c.paramvalue = b.port_state and c.parameter = 'EqpPortType'
                                            where a.equipid = '{0}'
                                            order by b.port_seq", equipId);*/

            string strSQL = string.Format(@"select distinct a.equipid, b.port_seq, b.port_id, b.port_state, b.port_type, b.carrier_type, c.description, b.disable, b.isLock from eqp_status a
                                            left join EQP_PORT_SET b on b.equipid = a.equipid
                                            left join RTD_DEFAULT_SET c on c.paramvalue = b.port_state and c.parameter = 'EqpPortType'
                                            where a.equipid = '{0}'
                                            order by b.port_seq", equipId);

            return strSQL;
        }
        public string SelectTableWorkInProcessSch(string _table)
        {
            //string _table = "workinprocess_sch";
            string strSQL = string.Format("select * from {0}", _table);
            return strSQL;
        }
        public string SelectTableWorkInProcessSchUnlock(string _table)
        {
            //string _table = "workinprocess_sch";
            string strSQL = string.Format("select * from {0} where isLock = 0 ", _table);
            return strSQL;
        }
        public string SelectTableWorkInProcessSchByCmdId(string CommandID, string _table)
        {
            //string _table = "workinprocess_sch";
            string strSQL = string.Format("select * from {0} where cmd_Id = '{1}' order by cmd_type", _table, CommandID);
            return strSQL;
        }
        public string QueryRunningWorkInProcessSchByCmdId(string CommandID, string _table)
        {
            //string _table = "workinprocess_sch";
            string strSQL = string.Format("select * from {0} where cmd_Id = '{1}' and cmd_current_state not in ('Running', 'Init')", _table, CommandID);
            return strSQL;
        }
        public string QueryInitWorkInProcessSchByCmdId(string CommandID, string _table)
        {
            //string _table = "workinprocess_sch";
            string strSQL = string.Format("select * from {0} where cmd_Id = '{1}' and cmd_current_state = 'Initial'", _table, CommandID);
            return strSQL;
        }
        public string SelectTableWorkInProcessSchByLotId(string _lotid, string _table)
        {
            //string _table = "workinprocess_sch";
            string strSQL = string.Format("select * from {0} where lotid = '{1}'", _table, _lotid);
            return strSQL;
        }
        public string SelectTableWorkInProcessSchByEquip(string _Equip, string _table)
        {
            //string _table = "workinprocess_sch";
            string strSQL = string.Format("select * from {0} where EquipID = '{1}'", _table, _Equip);
            return strSQL;
        }
        public string SelectTableWorkInProcessSchByEquipPort(string _Equip, string _PortId, string _table)
        {
            //string _table = "workinprocess_sch";
            string strSQL = string.Format("select * from {0} where EquipID = '{1}' and (Source = '{2}' or Dest = '{2}')", _table, _Equip, _PortId);
            return strSQL;
        }
        public string SelectTableWorkInProcessSchByPortId(string _PortId, string _table)
        {
            //string _table = "workinprocess_sch";
            string strSQL = string.Format("select * from {0} where Dest = '{1}'", _table, _PortId);
            return strSQL;
        }
        public string SelectTableWorkInProcessSchByCarrier(string _CarrierId, string _table)
        {
            //string _table = "workinprocess_sch";
            string strSQL = string.Format("select * from {0} where carrierid = '{1}'", _table, _CarrierId);
            return strSQL;
        }
        public string QueryWorkInProcessSchByPortIdForUnload(string _PortId, string _table)
        {
            //string _table = "workinprocess_sch";
            string strSQL = string.Format("select * from {0} where Source = '{1}'", _table, _PortId);
            return strSQL;
        }
        public string SelectTableEQPStatusInfoByEquipID(string EquipId)
        {
            string strSQL = "";
#if DEBUG
            strSQL = string.Format("select * from EQP_STATUS a left join EQP_PORT_SET b on b.equipid=a.equipid where a.equipid = '{0}'", EquipId);
#else
            strSQL = string.Format("select * from EQP_STATUS a left join EQP_PORT_SET b on b.equipid=a.equipid where a.manualmode=0 and a.equipid = '{0}'", EquipId);
#endif
            return strSQL;
        }
        public string SelectTableEQP_STATUSByEquipId(string EquipId)
        {
            string strSQL = "";
#if DEBUG
            strSQL = string.Format("select a.*, b.*, b.carrier_type as carrierType_M, c.in_erack, c.out_erack, c.pretransfer, c.usefailerack, c.f_erack, c.qtime_low, c.qtime_high, nvl(d.Dt_Start, 'Null') DT_START, nvl(d.Dt_End, 'Null') DT_END, nvl(d.effective, 0) Effective, nvl(d.expired, 1) Expired from EQP_STATUS a left join EQP_PORT_SET b on b.equipid=a.equipid left join (select distinct workgroup, in_erack, out_erack, pretransfer,usefailerack, f_erack, qtime_low, qtime_high, priority, checkeqplookuptable , noqtimecarriertype, isFurnace from workgroup_set) c on c.workgroup=a.workgroup left join EQP_RESERVE_TIME d on d.equipid = a.equipid where a.equipid = '{0}' and b.islock=0 order by b.port_seq asc", EquipId);
#else
            strSQL = string.Format("select a.*, b.*, b.carrier_type as carrierType_M, c.in_erack, c.out_erack, c.pretransfer, c.usefailerack, c.f_erack, c.qtime_low, c.qtime_high, nvl(d.Dt_Start, 'Null') DT_START, nvl(d.Dt_End, 'Null') DT_END, nvl(d.effective, 0) Effective, nvl(d.expired, 1) Expired from EQP_STATUS a left join EQP_PORT_SET b on b.equipid=a.equipid left join (select distinct workgroup, in_erack, out_erack, pretransfer,usefailerack, f_erack, qtime_low, qtime_high, priority, checkeqplookuptable, noqtimecarriertype, isFurnace from workgroup_set) c on c.workgroup=a.workgroup left join EQP_RESERVE_TIME d on d.equipid = a.equipid where a.equipid = '{0}' and b.islock=0 order by b.port_seq asc", EquipId);
#endif
            return strSQL;
        }
        public string SelectTableEquipmentStatus([Optional] string Department)
        {
            string tmpWhere = Department is null ? "" : Department.Trim().Equals("") ? "" : string.Format(" where equip_dept = '{0}'", Department);
            string strSQL = string.Format(@"select a.*,b.dt_start,b.dt_end,b.effective,b.expired, nvl(c.IsFurnace, 0) as IsFurnace from EQP_STATUS a
                                        left join eqp_reserve_time b on b.equipid = a.equipid
                                        left join workgroup_set c on c.workgroup = a.workgroup and c.stage='DEFAULT' {0}", tmpWhere);
            return strSQL;
        }
        public string SelectEqpStatusWaittoUnload()
        {
            string strSQL = string.Format(@"select * from EQP_STATUS a 
                                            left join EQP_PORT_SET b on b.equipid=a.equipid 
                                            left join WORKGROUP_SET c on c.workgroup=a.workgroup 
                                            where a.curr_status = 'UP' and b.port_state in (3, 5)");
            return strSQL;
        }
        public string SelectEqpStatusIsDownOutPortWaittoUnload()
        {
            string strSQL = string.Format(@"select * from EQP_STATUS a 
                                    left join EQP_PORT_SET b on b.equipid=a.equipid and b.port_type in ('OUT','IN','IO')
                                    left join WORKGROUP_SET c on c.workgroup=a.workgroup 
                                    where a.curr_status = 'DOWN' and a.down_state = 'IDLE'  and b.port_state in (3, 5)");
            return strSQL;
        }
        public string SelectEqpStatusReadytoload()
        {
            //string strSQL = string.Format(@"select distinct a.*,b.*,c.* from EQP_STATUS a 
            //                                left join EQP_PORT_SET b on b.equipid=a.equipid and b.port_type = 'OUT'
            //                                left join WORKGROUP_SET c on c.workgroup=a.workgroup 
            //                                left join EQP_PORT_SET d on d.equipid=a.equipid and d.port_type = 'IN'
            //                                where a.curr_status = 'UP' and d.port_state not in (0) and b.port_state in (4)");
            //如果需要卡IN 不能為0時, 可用以下寫法
            //where a.curr_status = 'UP' and b.port_state in (4) and d.port_state not in (0) 

            string strSQL = string.Format(@"select a.equipid, b.port_model, b.port_id, b.port_seq, b.port_type, b.port_state from EQP_STATUS a 
                                            left join EQP_PORT_SET b on b.equipid=a.equipid and b.port_type in ('IO','IN','OUT')
                                            left join WORKGROUP_SET c on c.workgroup=a.workgroup 
                                            where a.manualmode = 0 and a.curr_status = 'UP' and b.port_state in (4)
                                            union                                            
                                            select a.equipid, b.port_model, b.port_id, b.port_seq, b.port_type, b.port_state from EQP_STATUS a 
                                            left join EQP_PORT_SET b on b.equipid=a.equipid and b.port_type in ('IO','IN','OUT')
                                            left join WORKGROUP_SET c on c.workgroup=a.workgroup 
                                            where a.manualmode = 0 and a.curr_status = 'DOWN' and a.down_state = 'IDLE' and b.port_state in (4)
                                            order by port_seq, port_id");
            return strSQL;
        }
        public string SelectTableEquipmentPortsInfoByEquipId(string EquipId)
        {
            string strSQL = string.Format("select a.*, nvl(c.manualmode, 0) manualmode, nvl(b.Dt_Start, 'Null') DT_START, nvl(b.Dt_End, 'Null') DT_END, nvl(b.effective, 0) Effective, nvl(b.expired, 1) Expired from EQP_PORT_SET a left join EQP_RESERVE_TIME b on b.equipid = a.equipid left join EQP_STATUS c on c.equipid=a.equipid where a.EquipId = '{0}'", EquipId);
            return strSQL;
        }
        public string SelectTableEQP_Port_SetByEquipId(string EquipId)
        {
            string strSQL = string.Format("select * from EQP_PORT_SET where EquipId = '{0}' ", EquipId);
            return strSQL;
        }
        public string SelectLoadPortCarrierByEquipId(string EquipId)
        {
            string strSQL = string.Format("select * from CARRIER_TRANSFER a left join CARRIER_TYPE_SET b on b.type_key = a.type_key where a.locate = '{0}' ", EquipId);
            return strSQL;
        }
        public string SelectTableEQP_Port_SetByPortId(string EquipId)
        {
            string strSQL = string.Format("select * from EQP_PORT_SET where Port_ID = '{0}' ", EquipId);
            return strSQL;
        }
        public string SelectTableCheckLotInfo(string _resourceTable)
        {
            string strSQL = "";

            strSQL = string.Format(@"select distinct * from (
                          select a.lotid, case when b.lotid is null then 'New' else case when a.stage=b.stage and b.rtd_state <> 'COMPLETED' then b.rtd_state else 'NEXT' end end as state, 
                          case when b.rtd_state is null then 'NONE' else b.rtd_state end as oriState, b.lastModify_dt from {0} a 
                          left join LOT_INFO b on b.lotid=a.lotid ) where oriState  not in ('HOLD')
                          order by lotid", _resourceTable);

            return strSQL;
        }
        public string SelectTableCheckLotInfoNoData(string _resourceTable)
        {
            string strSQL = "";

            strSQL = string.Format(@"select a.lotid, 'Remove' as state, a.rtd_state as oriState from lot_info a 
                        where a.lotid not in (select lotid from {0})
                            and a.rtd_state not in ('COMPLETED', 'DELETED')", _resourceTable);

            return strSQL;
        }
        public string QueryERackInfo()
        {
            string strSQL = @"select * from rack where status = 'up'";
            return strSQL;
        }
        public string SelectTableLotInfo()
        {
            string strSQL = @"select * from LOT_INFO where rtd_state not in ('DELETED') 
                                and to_date(to_char(trunc(starttime, 'DD'),'yyyy/MM/dd HH24:mi:ss'), 'yyyy/MM/dd HH24:mi:ss') <= sysdate
                                order by customername, stage, rtd_state, sch_seq desc";
            return strSQL;
        }
        public string SelectTableLotInfoByDept(string Dept)
        {
            string tmpAnd = Dept is null ? "" : Dept.Trim().Equals("") ? "" : string.Format(" and d.equip_dept = '{0}'", Dept);
            string strSQL = string.Format(@"select c.*, d.equip_dept, ct.carrier_state from LOT_INFO c
                                            left join (select distinct a.stage, b.equip_dept from PROMIS_STAGE_EQUIP_MATRIX a
                                            left join EQP_STATUS b on b.equipid=a.eqpid) d on d.stage=c.stage
left join carrier_lot_associate ca on ca.lot_id=c.lotid
left join carrier_transfer ct on ct.carrier_id=ca.carrier_id
                                            where c.rtd_state not in ('DELETED') 
                                            and to_date(to_char(trunc(c.starttime, 'DD'),'yyyy/MM/dd HH24:mi:ss'), 'yyyy/MM/dd HH24:mi:ss') <= sysdate
                                            {0}
                                            order by c.customername, c.stage, c.rtd_state, c.sch_seq desc", tmpAnd);
            return strSQL;
        }
        public string SelectTableProcessLotInfo()
        {
            string strSQL = @"select * from LOT_INFO where rtd_state not in ('INIT','HOLD','PROC','DELETED', 'COMPLETED') 
                            and to_date(to_char(trunc(starttime, 'DD'),'yyyy/MM/dd HH24:mi:ss'), 'yyyy/MM/dd HH24:mi:ss') <= sysdate 
                            order by customername, stage, rtd_state, lot_age desc";
            return strSQL;
        }
        public string ReflushProcessLotInfo(bool _OnlyStage)
        {
            //20241104 reflush schedule seq do by all carrier  //remove and c.location_type in ('ERACK','STK')
            string _orderby = "";

            if (_OnlyStage)
                _orderby = "a.stage, a.rtd_state, a.priority desc , a.lot_age desc";
            else
                _orderby = "a.customername, a.stage, a.rtd_state, a.priority desc, a.lot_age desc";

            string strSQL = string.Format(@"select a.*, '' as adslot, '' as eotd, 0 as turnratio3, 0 as stageage from LOT_INFO a left join carrier_lot_associate b on b.lot_id = a.lotid left join carrier_transfer c on c.carrier_id = b.carrier_id where a.rtd_state not in ('HOLD','PROC','DELETED', 'COMPLETED') 
                            and to_date(to_char(trunc(a.starttime, 'DD'),'yyyy/MM/dd HH24:mi:ss'), 'yyyy/MM/dd HH24:mi:ss') <= sysdate
                            and c.enable=1
                            order by {0}", _orderby);

            //2024/06/03 Remove customername  //order by  a.customername, a.stage, a.rtd_state, a.priority asc , a.lot_age desc";
            return strSQL;
        }
        public string SelectTableProcessLotInfoByCustomer(string _customerName, string _equip)
        {
            string strSQL = string.Format(@"select * from LOT_INFO where rtd_state not in ('HOLD','PROC','DELETED', 'COMPLETED') and state not in ('HOLD')
                            and to_date(to_char(trunc(starttime, 'DD'),'yyyy/MM/dd HH24:mi:ss'), 'yyyy/MM/dd HH24:mi:ss') <= sysdate 
                            and customername = '{0}'  and equiplist is not null and equiplist like '%{1}%' order by stage, sch_seq", _customerName, _equip);
            return strSQL;
        }
        public string QueryLastModifyDT()
        {
            string strSQL = @"select max(lastmodify_dt) as lastmodify from LOT_INFO";
            return strSQL;
        }
        public string SelectTableLotInfoByLotid(string LotId)
        {
            string strSQL = string.Format("select * from LOT_INFO where lotid = '{0}'", LotId);
            return strSQL;  //select * from PROMIS_STAGE_EQUIP_MATRIX where eqpid = 'CTWT-01' and stage = 'B049TAPING';
        }
        public string SelectTableLotInfoOfInit()
        {
            string strSQL = @"select * from LOT_INFO a where a.rtd_state = 'INIT'";
            return strSQL;
        }
        public string SelectTableLotInfoOfWait()
        {
            string strSQL = @"select * from LOT_INFO a where a.rtd_state = 'WAIT'";
            return strSQL;
        }
        public string SelectTableLotInfoOfReady()
        {
            string strSQL = @"select a.* from LOT_INFO a
                            left join carrier_lot_associate b on b.lot_id = a.lotid
                            left join carrier_transfer c on c.carrier_id = b.carrier_id
                            where a.rtd_state = 'READY' and c.carrier_state = 'ONLINE' and equiplist is not null 
                            and isLock=0 order by a.priority, a.sch_seq asc";
            return strSQL;
        }
        public string SelectTableEQUIP_MATRIX(string EqpId, string StageCode)
        {
            string strSQL = string.Format("select eqpid from PROMIS_STAGE_EQUIP_MATRIX where eqpid = '{0}' and stage = '{1}' and enable = 1", EqpId, StageCode);
            //string strSQL = string.Format("select eqpid from PROMIS_STAGE_EQUIP_MATRIX where eqpid = '{0}' and stage = '{1}'", EqpId, StageCode);
            return strSQL;
        }
        public string ShowTableEQUIP_MATRIX()
        {
            string strSQL = "select * from PROMIS_STAGE_EQUIP_MATRIX";
            return strSQL;
        }
        public string SelectPrefmap(string EqpId)
        {
            string strSQL = string.Format("select * from EQP_CUST_PREF_MAP where eqpid = '{0}'", EqpId);
            return strSQL;
        }
        public string SelectTableEquipmentStatusInfo()
        {
            string strSQL = "";
#if DEBUG
            strSQL = @"select * from EQP_STATUS_INFO";
#else
            strSQL = @"select * from EQP_STATUS_INFO";
#endif
            return strSQL;
        }
        public string SelectEquipmentPortInfo()
        {
            string strSQL = "";
#if DEBUG
            strSQL = @"select * from (
                        select distinct a.equipid, b.port_seq, b.port_id from EQP_STATUS a
                        left join EQP_PORT_SET b on b.equipid = a.equipid where b.port_seq is not null
                        ) order by equipid, port_seq";
#else
            strSQL = @"select * from (
                        select distinct a.equipid, b.port_seq, b.port_id from EQP_STATUS a
                        left join EQP_PORT_SET b on b.equipid = a.equipid where b.port_seq is not null
                        ) order by equipid, port_seq";
#endif
            return strSQL;
        }
        public string SelectCarrierAssociateIsTrue()
        {
            string strSQL = @"select * from LOT_INFO a where a.carrier_asso = 'Y'";
            return strSQL;
        }
        public string SelectRTDDefaultSet(string _parameter)
        {
            string strCondition = _parameter.Equals("") ? "" : string.Format(" where parameter = '{0}'", _parameter);
            string strSQL = string.Format(@"select Parameter, ParamType, ParamValue, description from RTD_DEFAULT_SET{0}", strCondition);
            return strSQL;
        }
        public string SelectRTDDefaultSetByType(string _parameter)
        {
            string strCondition = _parameter.Equals("") ? "" : string.Format(" where ParamType = '{0}'", _parameter);
            string strSQL = string.Format(@"select Parameter, ParamType, ParamValue, description from RTD_DEFAULT_SET{0}", strCondition);
            return strSQL;
        }
        public string UpdateTableWorkInProcessSchByCmdId(string _cmd_Current_State, string _lastModify_DT, string _commandID, string _table)
        {
            //string _table = "workinprocess_sch";

            if (_lastModify_DT.Equals(""))
                _lastModify_DT = DateTime.Now.ToString("yyyy-MM-d HH:mm:ss");

            string tmpString = "update {0} set Cmd_State = '{1}', Cmd_Current_State = '{1}', LastModify_DT = TO_DATE(\'{2}\', \'yyyy-MM-dd hh24:mi:ss\') " +
                "where cmd_id = '{3}'";
            string strSQL = string.Format(tmpString, _table, _cmd_Current_State, _lastModify_DT, _commandID);
            return strSQL;
        }
        public string UpdateTableWorkInProcessSchByUId(string _updateState, string _lastModify_DT, string _UID, string _table)
        {
            string tmpString = "";
            string strSQL = "";
            string strSet = "";
            //string _table = "workinprocess_sch";

            switch (_updateState)
            {
                case "Initial":
                    tmpString = "set Cmd_Current_State = 'Initial', initial_DT = TO_DATE(\'{0}\', \'yyyy-mm-dd hh:mi:ss\'), LastModify_DT = TO_DATE(\'{0}\', \'yyyy-mm-dd hh:mi:ss\') " +
                            "where UID = '{1}'";
                    strSet = string.Format(tmpString, _lastModify_DT, _UID);
                    strSQL = string.Format("update {0} {1}", _table, strSet);
                    break;
                case "Wait":
                    tmpString = "set Cmd_Current_State = 'Wait', WaitingQueue_DT = TO_DATE(\'{0}\', \'yyyy-mm-dd hh:mi:ss\'), LastModify_DT = TO_DATE(\'{0}\', \'yyyy-mm-dd hh:mi:ss\') " +
                            "where UID = '{1}'";
                    strSet = string.Format(tmpString, _lastModify_DT, _UID);
                    strSQL = string.Format("update {0} {1}", _table, strSet);
                    break;
                case "Dispatch":
                    tmpString = "set Cmd_Current_State = 'Dispatch', ExecuteQueue_DT = TO_DATE(\'{0}\', \'yyyy-mm-dd hh:mi:ss\'), LastModify_DT = TO_DATE(\'{0}\', \'yyyy-mm-dd hh:mi:ss\') " +
                            "where UID = '{1}'";
                    strSet = string.Format(tmpString, _lastModify_DT, _UID);
                    strSQL = string.Format("update {0} {1}", _table, strSet);
                    break;
                default:
                    tmpString = "set Cmd_Current_State = '{0}', LastModify_DT = TO_DATE(\'{1}\', \'yyyy-mm-dd hh:mi:ss\') " +
                            "where UID = '{2}'";
                    strSet = string.Format(tmpString, _updateState, _lastModify_DT, _UID);
                    strSQL = string.Format("update {0} {1}", _table, strSet);
                    break;
            }

            return strSQL;
        }
        public string UpdateTableWorkInProcessSchHisByCmdId(string _commandID)
        {
            string tmpString = "insert into WORKINPROCESS_SCH_HIS select * from WORKINPROCESS_SCH where cmd_id = '{0}'";
            string strSQL = string.Format(tmpString, _commandID);
            return strSQL;
        }
        public string UpdateTableWorkInProcessSchHisByUId(string _uid)
        {
            string tmpString = "insert into WORKINPROCESS_SCH_HIS select * from WORKINPROCESS_SCH where uuid = '{0}'";
            string strSQL = string.Format(tmpString, _uid);
            return strSQL;
        }
        public string UpdateTableCarrierTransfer(CarrierLocationUpdate oCarrierLoc)
        {
            string strCarrier = oCarrierLoc.CarrierID;
            string strLocation = oCarrierLoc.Location;
            string modifyTime = "";

            string tmpString = "update CARRIER_TRANSFER set Locate = '{0}', Modify_DT = sysdate, " +
                            "LastModify_DT = sysdate " +
                            "where CARRIER_ID = '{1}'";
            string strSQL = string.Format(tmpString, strLocation, strCarrier);
            return strSQL;
        }
        public string UpdateTableCarrierTransferByCarrier(string CarrierId, string State)
        {
            string tmpString = "update CARRIER_TRANSFER set State = '{0}', Modify_DT = sysdate, " +
                            "LastModify_DT = sysdate " +
                            "where CARRIER_ID = '{1}'";
            string strSQL = string.Format(tmpString, State, CarrierId);
            return strSQL;
        }
        public string UpdateTableReserveCarrier(string _carrierID, bool _reserve)
        {
            int iReserve = 0;
            if (_reserve)
                iReserve = 1;
            else
                iReserve = 0;

            string tmpString = "update CARRIER_TRANSFER set reserve = {0}, Modify_DT = sysdate, " +
                                "LastModify_DT = sysdate " +
                                "where CARRIER_ID = '{1}'";
            string strSQL = string.Format(tmpString, iReserve.ToString(), _carrierID);
            return strSQL;
        }
        public string UpdateTableCarrierTransfer(CarrierLocationUpdate oCarrierLoc, int _haveMetalRing)
        {
            string strCarrier = oCarrierLoc.CarrierID.Trim();
            string strLocate = "";
            string strPort = "1";
            if (oCarrierLoc.Location.Contains("_LP"))
            {
                strLocate = oCarrierLoc.Location.Split("_LP")[0].ToString();
                strPort = oCarrierLoc.Location.Split("_LP")[1].ToString();
            }
            string strLocation = "";
            if (strLocate.Equals(""))
            {
                strLocate = oCarrierLoc.Location;
            }
            string strLocationType = oCarrierLoc.LocationType;
            string modifyTime = "";
            string sState = "";
            if (!strLocate.Equals(""))
                sState = "ONLINE";
            else
                sState = "OFFLINE";

            string tmpString = "update CARRIER_TRANSFER set Carrier_State = '{0}', Locate = '{1}', PortNo = {2}, LOCATION_TYPE = '{3}', METAL_RING = '{4}', RESERVE = 0, Modify_DT = sysdate, " +
                            "LastModify_DT = sysdate " +
                            "where CARRIER_ID = '{5}'";
            string strSQL = string.Format(tmpString, sState, strLocate, int.Parse(strPort).ToString(), strLocationType, _haveMetalRing, strCarrier);
            return strSQL;
        }
        public string CarrierLocateReset(CarrierLocationUpdate oCarrierLoc, int _haveMetalRing)
        {
            string strCarrier = oCarrierLoc.CarrierID.Trim();
            string strLocate = "";
            string strPort = "1";
            if (oCarrierLoc.Location.Contains("_LP"))
            {
                strLocate = oCarrierLoc.Location.Split("_LP")[0].ToString();
                strPort = oCarrierLoc.Location.Split("_LP")[1].ToString();
            }
            string strLocation = "";
            if (strLocate.Equals(""))
            {
                strLocate = oCarrierLoc.Location;
            }
            string strLocationType = oCarrierLoc.LocationType;
            string modifyTime = "";
            string sState = "";

            sState = "OFFLINE";

            string tmpString = "update CARRIER_TRANSFER set Carrier_State = '{0}', Locate = '', PortNo = 0, LOCATION_TYPE = '', METAL_RING = 0, RESERVE = 0, Modify_DT = sysdate, " +
                            "LastModify_DT = sysdate " +
                            "where Locate = '{1}' and PortNo = {2}";
            string strSQL = string.Format(tmpString, sState, strLocate, int.Parse(strPort).ToString(), strLocationType, _haveMetalRing, strCarrier);
            return strSQL;
        }
        public string UpdateTableEQP_STATUS(string EquipId, string CurrentStatus)
        {
            //string state = GetEquipState(int.Parse(CurrentStatus));
            string strSQL = string.Format("update EQP_STATUS set Curr_Status = '{0}', Modify_dt = sysdate, lastModify_dt = sysdate where EquipId = '{1}'", CurrentStatus, EquipId);
            return strSQL;
        }
        public string UpdateTableEQP_Port_Set(string EquipId, string PortSeq, string NewStatus)
        {
            string strSQL = string.Format("update EQP_PORT_SET set Port_State = {0}, Modify_dt = sysdate, lastModify_dt = sysdate where EquipId = '{1}' and Port_Seq = '{2}' ", NewStatus, EquipId, PortSeq);
            return strSQL;
        }
        public string UpdateTableLotInfoReset(string LotID)
        {
            string strSQL = string.Format(@"update LOT_INFO set carrier_asso = 'N', equip_asso = 'N', equiplist = '', sch_seq = 0, 
                                               rtd_state = 'INIT' where lotid = '{0}'", LotID);
            //rtd_state = 'INIT', lastmodify_dt = sysdate where lotid = '{0}'", LotID);
            return strSQL;
        }
        public string UpdateTableLotInfoEquipmentList(string _lotID, string _lstEquipment)
        {
            string strSQL = string.Format(@"update LOT_INFO set equip_asso = 'Y', equiplist = '{0}', 
                                               rtd_state = 'WAIT', lastmodify_dt = sysdate where lotid = '{1}'", _lstEquipment, _lotID);
            //rtd_state = 'WAIT', lastmodify_dt = sysdate where lotid = '{1}'", _lstEquipment, _lotID);
            return strSQL;
        }
        public string UpdateTableLotInfoState(string LotID, string State)
        {
            string strSQL = string.Format(@"update LOT_INFO set rtd_state = '{0}', sch_seq = 0 where lotid = '{1}'", State, LotID);
            //string strSQL = string.Format(@"update LOT_INFO set rtd_state = '{0}', sch_seq = 0, lastmodify_dt = sysdate where lotid = '{1}'", State, LotID);
            return strSQL;
        }
        public string UpdateTableLastModifyByLot(string LotID)
        {
            string strSQL = string.Format(@"update LOT_INFO set lastmodify_dt = sysdate where lotid = '{0}'", LotID);
            return strSQL;
        }
        public string UpdateTableLotInfoSetCarrierAssociateByLotid(string LotID)
        {
            //string strSQL = string.Format(@"update LOT_INFO set carrier_asso = 'Y', rtd_state= 'WAIT', lastmodify_dt = sysdate where lotid = '{0}'", LotID);
            string strSQL = string.Format(@"update LOT_INFO set carrier_asso = 'Y', rtd_state= 'WAIT' where lotid = '{0}'", LotID);
            return strSQL;
        }
        public string UpdateTableLotInfoToReadyByLotid(string LotID)
        {
            string strSQL = string.Format(@"update LOT_INFO set rtd_state= 'READY', lastmodify_dt = sysdate where lotid = '{0}'", LotID);
            return strSQL;
        }
        public string UpdateLotInfoSchSeqByLotid(string LotID, int _SchSeq)
        {
            //string strSQL = string.Format(@"update LOT_INFO set sch_seq = {0}, lastmodify_dt = sysdate where lotid = '{1}'", _SchSeq, LotID);
            string strSQL = string.Format(@"update LOT_INFO set sch_seq = {0} where lotid = '{1}'", _SchSeq, LotID);
            return strSQL;
        }
        public string UpdateTableLotInfoSetCarrierAssociate2ByLotid(string LotID)
        {
            string strSQL = string.Format(@"update LOT_INFO set carrier_asso = 'N', rtd_state= 'WAIT' where lotid = '{0}'", LotID);
            //string strSQL = string.Format(@"update LOT_INFO set carrier_asso = 'N', rtd_state= 'WAIT', lastmodify_dt = sysdate where lotid = '{0}'", LotID);
            return strSQL;
        }
        public string UpdateTableRTDDefaultSet(string _parameter, string _paramvalue, string _modifyBy)
        {
            string strSQL = string.Format(@"update rtd_default_set set paramvalue = '{0}', modifyBy = '{1}', lastModify_DT = sysdate where parameter = '{2}'", _paramvalue, _modifyBy, _parameter);
            return strSQL;
        }
        public string GetLoadPortCurrentState(string _equipId)
        {
            string strSQL = string.Format(@"select a.equipid, a.port_model, a.port_seq, a.port_type, a.port_id, a.carrier_type, a.port_state, b.carrier_id, a.lastlotid as lot_id, d.customername from eqp_port_set a
                                            left join CARRIER_TRANSFER b on b.locate = a.equipid and b.portno = a.port_seq
                                            left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = b.carrier_id
                                            left join LOT_INFO d on d.lotid = a.lastlotid
                                            where a.equipid = '{0}' and a.port_type in ('IN', 'IO')", _equipId);
            return strSQL;
        }
        public string UpdateSchSeq(string _Customer, string _Stage, int _SchSeq, int _oriSeq)
        {
            string strSQL = "";

            if (_SchSeq == 0 && _oriSeq > _SchSeq)
            {
                strSQL = string.Format(@"update LOT_INFO set sch_seq = sch_seq - 1 where customername = '{0}' and stage = '{1}'
                                            and rtd_state not in ('HOLD','PROC','DELETED', 'COMPLETED') 
                                            and to_date(to_char(trunc(starttime, 'DD'),'yyyy/MM/dd HH24:mi:ss'), 'yyyy/MM/dd HH24:mi:ss') <= sysdate  
                                            and sch_seq > {2}", _Customer, _Stage, _oriSeq);
            }
            else if (_oriSeq - _SchSeq > 0)
            {
                strSQL = string.Format(@"update LOT_INFO set sch_seq = sch_seq + 1 where customername = '{0}' and stage = '{1}'
                                            and rtd_state not in ('HOLD','PROC','DELETED', 'COMPLETED') 
                                            and to_date(to_char(trunc(starttime, 'DD'),'yyyy/MM/dd HH24:mi:ss'), 'yyyy/MM/dd HH24:mi:ss') <= sysdate  
                                            and sch_seq >= {2} and sch_seq < {3}", _Customer, _Stage, _SchSeq, _oriSeq);
            }
            else
            {
                strSQL = string.Format(@"update LOT_INFO set sch_seq = sch_seq - 1 where customername = '{0}' and stage = '{1}'
                                            and rtd_state not in ('HOLD','PROC','DELETED', 'COMPLETED') 
                                            and to_date(to_char(trunc(starttime, 'DD'),'yyyy/MM/dd HH24:mi:ss'), 'yyyy/MM/dd HH24:mi:ss') <= sysdate  
                                            and sch_seq > {2} and sch_seq <= {3}", _Customer, _Stage, _oriSeq, _SchSeq);
            }
            return strSQL;
        }
        public string UpdateSchSeqByLotId(string _lotId, string _Customer, int _SchSeq)
        {
            //string strSQL = string.Format(@"update lot_info set sch_seq = {0}, lastmodify_dt = sysdate 
            string strSQL = string.Format(@"update LOT_INFO set sch_seq = {0} 
                                    where customername = '{1}' and lotid = '{2}'", _SchSeq, _Customer, _lotId);
            return strSQL;
        }
        public string InsertSMSTriggerData(string _eqpid, string _stage, string _desc, string _flag, string _username)
        {
            string strSQL = string.Format(@"insert into RTD_SMS_TRIGGER_DATA
                                            (eqpid, stage, description, flag, entry_by, entry_time)
                                            values
                                            ('{0}', '{1}', '{2}', '{3}', '{4}', sysdate)", _eqpid, _stage, _desc, _flag, _username);
            return strSQL;
        }
        public string SchSeqReflush(bool _OnlyStage)
        {
            string _column = "";
            string _groupby = "";
            string strSQL = "";

            if (_OnlyStage)
            {
                _column = "stage, count(sch_seq) as iCount";
                _groupby = "stage";
            }
            else
            {
                _column = "customername, stage, count(sch_seq) as iCount";
                _groupby = "customername, stage";
            }

            strSQL = string.Format(@"select {0} from (
                                                select * from lot_info where rtd_state not in ('HOLD','PROC','DELETED', 'COMPLETED') and carrier_asso = 'Y' 
                                                and to_date(to_char(trunc(starttime, 'DD'),'yyyy/MM/dd HH24:mi:ss'), 'yyyy/MM/dd HH24:mi:ss') <= sysdate )
                                            group by {1}", _column, _groupby);
            return strSQL;
        }
        public string LockLotInfo(bool _lock)
        {
            string strSQL = "";

            if (_lock)
            {
                strSQL = "update rtd_default_set set paramvalue = '1', lastmodify_dt = sysdate where parameter= 'LotInfo' and paramtype = 'IsLock'";
            }
            else
            {
                strSQL = "update rtd_default_set set paramvalue = '0', lastmodify_dt = sysdate where parameter= 'LotInfo' and paramtype = 'IsLock'";
            }

            return strSQL;
        }
        public string GetLockStateLotInfo()
        {
            string strSQL = "select paramvalue as lockstate from rtd_default_set where parameter= 'LotInfo' and paramtype = 'IsLock'";

            return strSQL;
        }
        public string ReflushWhenSeqZeroStateWait()
        {
            string strSQL = "select * from lot_info where rtd_state = 'WAIT' and Sch_seq = 0";

            return strSQL;
        }
        public string SyncNextStageOfLot(string _resourceTable, string _lotid)
        {
            string strSQL = "";

            strSQL = string.Format(@"update lot_info set (priority,carrier_asso,equip_asso,equiplist,lastmodify_dt,rtd_state,
                                        state,wfr_qty,dies_qty,stage,lottype,lot_age,planstarttime,starttime,lockmachine,comp_qty) 
                                        = (select priority, 'N', 'N', '', sysdate, 'INIT', state,wfr_qty,
                                        dies_qty,stage,lottype,lot_age,planstarttime,starttime,0,0 from {0} where lotid = '{1}')
                                        where lotid = '{1}'", _resourceTable, _lotid);

            return strSQL;
        }
        public string SyncNextStageOfLotNoPriority(string _resourceTable, string _lotid)
        {
            string strSQL = "";

            strSQL = string.Format(@"update lot_info set (carrier_asso,equip_asso,equiplist,lastmodify_dt,rtd_state,
                                        state,wfr_qty,dies_qty,stage,lottype,lot_age,planstarttime,starttime,lockmachine,comp_qty) 
                                        = (select 'N', 'N', '', sysdate, 'INIT', state,wfr_qty,
                                        dies_qty,stage,lottype,lot_age,planstarttime,starttime,0,0 from {0} where lotid = '{1}')
                                        where lotid = '{1}'", _resourceTable, _lotid);

            return strSQL;
        }
        public string UpdateLotInfoWhenCOMP(string _commandId, string _table)
        {
            //string _table = "workinprocess_sch";
            string strSQL = string.Format(@"update lot_info set RTD_state='COMPLETED', sch_seq=0, lastModify_dt=sysdate where lotid in (
                                            select distinct lotid from {0} where cmd_id = '{1}' and lotid <> ' ')", _table, _commandId);
            return strSQL;
        }
        public string UpdateLotInfoWhenFail(string _commandId, string _table)
        {
            //string _table = "workinprocess_sch";
            string strSQL = string.Format(@"update lot_info set RTD_state='READY', sch_seq=0, lastModify_dt=sysdate where lotid in (
                                            select distinct lotid from {0} where cmd_id = '{1}' and lotid <> ' ')", _table, _commandId);
            return strSQL;
        }
        public string UpdateEquipCurrentStatus(string _current, string _equipid)
        {
            string strSQL = string.Format(@"update eqp_status set curr_status = '{0}', lastmodify_dt = sysdate where equipid = '{1}'", _current.Replace(" ", ""), _equipid);
            return strSQL;
        }
        public string QueryEquipmentStatusByEquip(string _equip)
        {
            //string strSQL = string.Format("select machine_state, curr_status, down_state, manualmode, lastModify_dt from eqp_status where equipid = '{0}'", _equip);
            string strSQL = string.Format("select a.workgroup, a.machine_state, a.curr_status, a.down_state, a.manualmode, a.lastModify_dt, nvl(b.Dt_Start, 'Null') as DT_START, nvl(b.Dt_End, 'Null') as Dt_End, nvl(b.effective, 0) as effective, nvl(b.expired, 1) as expired from eqp_status a left join EQP_RESERVE_TIME b on b.equipid = a.equipid where a.equipid = '{0}'", _equip);
            return strSQL;
        }
        public string QueryCarrierInfoByCarrierId(string _carrierId)
        {
            string strSQL = string.Format("select a.carrier_id, a.quantity, a.reserve, b.lot_id from carrier_transfer a left join CARRIER_LOT_ASSOCIATE b on b.carrier_id = a.carrier_id where a.carrier_id = '{0}'", _carrierId);
            return strSQL;
        }
        public string QueryCarrierType(string _carrierType, string _typeKey)
        {
            string strSQL = string.Format(@"select * from carrier_lot_associate a
                                        left join carrier_transfer b on b.carrier_id = a.carrier_id
                                        where a.carrier_type like '%{0}%' and b.type_key not in ('{1}')", _carrierType, _typeKey);
            return strSQL;
        }
        public string UpdateCarrierType(string _carrierType, string _typeKey)
        {
            string strSQL = string.Format(@"update carrier_transfer set type_key = '{1}' where carrier_id in (
                                    select a.carrier_id from carrier_lot_associate a
                                    left join carrier_transfer b on b.carrier_id=a.carrier_id
                                    where a.carrier_type like '%{0}%' and b.type_key not in ('{1}'))", _carrierType, _typeKey);
            return strSQL;
        }
        public string QueryWorkinProcessSchHis(string _startTime, string _endTime)
        {
            string tmpCdt = " where cmd_state in ('Success','Failed'){0}";
            string tmpTime = "";
            if (_startTime.Trim().Equals("") && _endTime.Trim().Equals(""))
                tmpTime = "";
            else if (!_startTime.Equals("") && _endTime.Equals(""))
                tmpTime = string.Format(" and create_dt > to_date('{0}', 'yyyy/MM/dd HH24:mi:ss')", _startTime);
            else if (_startTime.Equals("") && !_endTime.Equals(""))
                tmpTime = string.Format(" and create_dt < to_date('{0}', 'yyyy/MM/dd HH24:mi:ss')", _endTime);
            else if (!_startTime.Equals("") && !_endTime.Equals(""))
                tmpTime = string.Format(" and create_dt between to_date('{0}', 'yyyy/MM/dd HH24:mi:ss') and to_date('{1}', 'yyyy/MM/dd HH24:mi:ss')", _startTime, _endTime);

            tmpCdt = string.Format(" where cmd_state in ('Success','Failed'){0}", tmpTime);

            string strSQL = string.Format(@"select uuid, cmd_id, cmd_type, Equipid, cmd_state, carrierid, carrierType, Source, Dest, Priority, replace, back, lotid, customer, create_dt, modify_dt, lastmodify_dt from workinprocess_sch_his{0} order by create_dt", tmpCdt);

            return strSQL;
        }
        public string QueryStatisticalOfDispatch(DateTime dtCurrUTCTime, string _statisticalUnit, string _type)
        {
            string tmpCdt = " where {0}";
            string tmpGroupBy = " group by {0}";
            string tmpOrderBy = " order by {0}";
            string tmpColumns;
            string tmpCdtType = " and type in ({0})";
            string strSQL = "";

            try
            {

                if (_type.ToLower().Equals("total"))
                {
                    tmpCdtType = string.Format(tmpCdtType, "'T'");
                }
                else if (_type.ToLower().Equals("success"))
                {
                    tmpCdtType = string.Format(tmpCdtType, "'S'");
                }
                else if (_type.ToLower().Equals("failed"))
                {
                    tmpCdtType = string.Format(tmpCdtType, "'F'");
                }
                else
                {
                    tmpCdtType = "";
                }


                switch (_statisticalUnit.ToLower())
                {
                    case "years":
                        tmpColumns = " years, months, sum(times) as time, type ";
                        tmpCdt = string.Format(tmpCdt, string.Format("years = {0}{1}", dtCurrUTCTime.Year.ToString(), tmpCdtType));
                        tmpGroupBy = string.Format(tmpGroupBy, "years, months, type");
                        tmpOrderBy = string.Format(tmpOrderBy, "years");
                        break;
                    case "months":
                        tmpColumns = " years, months, days, sum(times) as time, type ";
                        tmpCdt = string.Format(tmpCdt, string.Format("years = {0} and months = {1}{2}", dtCurrUTCTime.Year.ToString(), dtCurrUTCTime.Month.ToString(), tmpCdtType));
                        tmpGroupBy = string.Format(tmpGroupBy, "years, months, days, type");
                        tmpOrderBy = string.Format(tmpOrderBy, "years, months");
                        break;
                    case "days":
                        tmpColumns = " years, months, days, hours, sum(times) as time, type ";
                        tmpCdt = string.Format(tmpCdt, string.Format("years = {0} and months = {1} and days = {2}{3}", dtCurrUTCTime.Year.ToString(), dtCurrUTCTime.Month.ToString(), dtCurrUTCTime.Day.ToString(), tmpCdtType));
                        tmpGroupBy = string.Format(tmpGroupBy, "years, months, days, hours, type");
                        tmpOrderBy = string.Format(tmpOrderBy, "years, months, days");
                        break;
                    case "shift":
                        tmpColumns = " years, months, days, sum(times) as time ";
                        tmpCdt = string.Format(tmpCdt, string.Format("years = {0} and months = {1} and days = {2}{3}", dtCurrUTCTime.Year.ToString(), dtCurrUTCTime.Month.ToString(), dtCurrUTCTime.Day.ToString(), tmpCdtType));
                        tmpGroupBy = string.Format(tmpGroupBy, "years, months, days");
                        tmpOrderBy = string.Format(tmpOrderBy, "years, months, days");
                        break;
                    case "year":
                        tmpColumns = " years, sum(times) as time ";
                        tmpCdt = string.Format(tmpCdt, string.Format("years = {0}{1}", dtCurrUTCTime.Year.ToString(), tmpCdtType));
                        tmpGroupBy = string.Format(tmpGroupBy, "years");
                        tmpOrderBy = string.Format(tmpOrderBy, "years");
                        break;
                    case "month":
                        tmpColumns = " years, months, sum(times) as time ";
                        tmpCdt = string.Format(tmpCdt, string.Format("years = {0} and months = {1}{2}", dtCurrUTCTime.Year.ToString(), dtCurrUTCTime.Month.ToString(), tmpCdtType));
                        tmpGroupBy = string.Format(tmpGroupBy, "years, months");
                        tmpOrderBy = string.Format(tmpOrderBy, "years, months");
                        break;
                    case "day":
                        tmpColumns = " years, months, days, sum(times) as time ";
                        tmpCdt = string.Format(tmpCdt, string.Format("years = {0} and months = {1} and days = {2}{3}", dtCurrUTCTime.Year.ToString(), dtCurrUTCTime.Month.ToString(), dtCurrUTCTime.Day.ToString(), tmpCdtType));
                        tmpGroupBy = string.Format(tmpGroupBy, "years, months, days");
                        tmpOrderBy = string.Format(tmpOrderBy, "years, months, days");
                        break;
                    default:
                        tmpColumns = " * ";
                        break;
                }

                strSQL = string.Format(@"select {0} from RTD_Statistical{1}{2}{3}", tmpColumns, tmpCdt, tmpGroupBy, tmpOrderBy);
            }
            catch (Exception ex)
            { }

            return strSQL;
        }
        public string CalcStatisticalTimesFordiffZone(bool isStart, DateTime dtStartTime, DateTime dtEndTime, string _statisticalUnit, string _type, double _zone)
        {
            string tmpCdt = " where {0}";
            string tmpGroupBy = " group by {0}";
            string tmpOrderBy = " order by {0}";
            string tmpColumns;
            string tmpCdtType = " and type in ({0})";
            string strSQL = "";
            string tmpHourCdt = " and hours in ({0})";

            try
            {

                if (_type.ToLower().Equals("total"))
                {
                    tmpCdtType = string.Format(tmpCdtType, "'T'");
                }
                else if (_type.ToLower().Equals("success"))
                {
                    tmpCdtType = string.Format(tmpCdtType, "'S'");
                }
                else if (_type.ToLower().Equals("failed"))
                {
                    tmpCdtType = string.Format(tmpCdtType, "'F'");
                }
                else
                {
                    tmpCdtType = "";
                }

                //int iStart = Convert.ToInt16(24 + _zone);
                int iStart = dtStartTime.Hour;
                int iEnd = dtEndTime.Hour;

                string tmpHour = "";
                int iHour = 24;
                if (isStart)
                {
                    if (iStart < iEnd)
                        iHour = iEnd;


                    for (int i = iStart; i < iHour; i++)
                    {
                        tmpHour = tmpHour.Equals("") ? tmpHour + i.ToString() : tmpHour + "," + i.ToString();
                    }
                }
                else
                {
                    if (iStart > iEnd)
                    {
                        for (int i = 0; i < iEnd; i++)
                        {
                            tmpHour = tmpHour.Equals("") ? tmpHour + i.ToString() : tmpHour + "," + i.ToString();
                        }
                    }
                }

                if (tmpHour.Equals(""))
                    tmpHour = "99";

                tmpHourCdt = String.Format(tmpHourCdt, tmpHour);


                switch (_statisticalUnit.ToLower())
                {
                    case "years":
                        tmpColumns = " years, sum(times) as time, type ";
                        tmpCdt = string.Format(tmpCdt, string.Format("years = {0}{1}", dtStartTime.Year.ToString(), tmpCdtType));
                        tmpGroupBy = string.Format(tmpGroupBy, "years, type");
                        tmpOrderBy = string.Format(tmpOrderBy, "years");
                        break;
                    case "months":
                        tmpColumns = " years, months, days, sum(times) as time, type ";
                        tmpCdt = string.Format(tmpCdt, string.Format("years = {0} and months = {1}{2}", dtStartTime.Year.ToString(), dtStartTime.Month.ToString(), tmpCdtType));
                        tmpGroupBy = string.Format(tmpGroupBy, "years, months, days, type");
                        tmpOrderBy = string.Format(tmpOrderBy, "years, months");
                        break;
                    case "days":
                        tmpColumns = " years, months, days, sum(times) as time, type ";
                        tmpCdt = string.Format(tmpCdt, string.Format("years = {0} and months = {1} and days = {2}{3}{4}", dtStartTime.Year.ToString(), dtStartTime.Month.ToString(), dtStartTime.Day.ToString(), tmpHourCdt, tmpCdtType));
                        tmpGroupBy = string.Format(tmpGroupBy, "years, months, days, type");
                        tmpOrderBy = string.Format(tmpOrderBy, "years, months, days");
                        break;
                    case "year":
                        tmpColumns = " years, sum(times) as time ";
                        tmpCdt = string.Format(tmpCdt, string.Format("years = {0}{1}", dtStartTime.Year.ToString(), tmpCdtType));
                        tmpGroupBy = string.Format(tmpGroupBy, "years");
                        tmpOrderBy = string.Format(tmpOrderBy, "years");
                        break;
                    case "month":
                        tmpColumns = " years, months, sum(times) as time ";
                        tmpCdt = string.Format(tmpCdt, string.Format("years = {0} and months = {1}{2}", dtStartTime.Year.ToString(), dtStartTime.Month.ToString(), tmpCdtType));
                        tmpGroupBy = string.Format(tmpGroupBy, "years, months");
                        tmpOrderBy = string.Format(tmpOrderBy, "years, months");
                        break;
                    case "shift":
                        tmpColumns = " years, months, days, sum(times) as time ";
                        //string theDay = int.Parse(tmpHour.Split(',')[0].ToString()) >= 20 ? dtStartTime.Day.ToString() : dtStartTime.AddDays(1).Day.ToString();
                        tmpCdt = string.Format(tmpCdt, string.Format("years = {0} and months = {1} and days = {2}{3}{4}", dtStartTime.Year.ToString(), dtStartTime.Month.ToString(), dtStartTime.Day.ToString(), tmpHourCdt, tmpCdtType));
                        tmpGroupBy = string.Format(tmpGroupBy, "years, months, days");
                        tmpOrderBy = string.Format(tmpOrderBy, "years, months, days");
                        break;
                    case "day":
                        tmpColumns = " years, months, days, sum(times) as time ";
                        tmpCdt = string.Format(tmpCdt, string.Format("years = {0} and months = {1} and days = {2}{3}{4}", dtStartTime.Year.ToString(), dtStartTime.Month.ToString(), dtStartTime.Day.ToString(), tmpHourCdt, tmpCdtType));
                        tmpGroupBy = string.Format(tmpGroupBy, "years, months, days");
                        tmpOrderBy = string.Format(tmpOrderBy, "years, months, days");
                        break;
                    default:
                        tmpColumns = " * ";
                        break;
                }

                strSQL = string.Format(@"select {0} from RTD_Statistical{1}{2}{3}", tmpColumns, tmpCdt, tmpGroupBy, tmpOrderBy);
            }
            catch (Exception ex)
            { }

            return strSQL;
        }
        public string QueryRtdNewAlarm()
        {
            string strSQL = "select rownum, b.* from ( select a.*from RTD_ALARM a where a.\"new\" = 1 order by a.\"createdAt\" desc) b where rownum < 100";
            return strSQL;
        }
        public string QueryAllRtdAlarm()
        {
            string strSQL = "select * from RTD_ALARM";
            return strSQL;
        }
        public string UpdateRtdAlarm(string _time)
        {
            string tmpSQL = "update rtd_alarm set \"new\" = 0, \"last_updated\" = sysdate where \"createdAt\" <= to_date('{0}', 'yyyy/MM/dd HH24:mi:ss') and \"new\" = 1";
            string strSQL = string.Format(tmpSQL, _time);
            return strSQL;
        }
        public string QueryCarrierAssociateWhenIsNewBind()
        {
            string strSQL = "";
            string strSet = "";

            strSQL = string.Format("select distinct a.carrier_id, a.lot_id, b.locate, b.portno, b.location_type, c.customername, c.partid, c.lottype, c.stage, a.quantity from CARRIER_LOT_ASSOCIATE a left join CARRIER_TRANSFER b on b.carrier_id = a.carrier_id left join LOT_INFO c on c.lotid = a.lot_id where a.new_bind = 1 and locate is not null", strSet);
            return strSQL;
        }
        public string QueryCarrierAssociateWhenOnErack(string _table)
        {
            string strSQL = "";
            string strSet = "";

            strSQL = string.Format("select distinct a.carrier_id, a.lot_id, b.locate, b.portno, b.location_type, c.customername, c.partid, c.lottype, c.stage, a.quantity, d.stage as stage1, nvl(to_char(info_update_dt, 'yyyy/MM/dd HH24:mi:ss;'), 'NULL') info_update_dt from CARRIER_LOT_ASSOCIATE a left join CARRIER_TRANSFER b on b.carrier_id = a.carrier_id left join LOT_INFO c on c.lotid = a.lot_id left join {0} d on c.lotid = d.lotid where b.location_type = 'ERACK' and a.lot_id is not null and locate is not null", _table);
            return strSQL;
        }
        public string ResetCarrierLotAssociateNewBind(string _carrierId)
        {
            string tmpSQL = "update CARRIER_LOT_ASSOCIATE set New_Bind = 0 where carrier_id = '{0}' and New_Bind = 1";
            string strSQL = string.Format(tmpSQL, _carrierId);
            return strSQL;
        }
        public string QueryRTDStatisticalByCurrentHour(DateTime _datetime)
        {
            string tmpSQL = String.Format(" where years = {0} and months = {1} and days = {2} and hours = {3}",
                _datetime.Year.ToString(), _datetime.Month.ToString(), _datetime.Day.ToString(), _datetime.Hour.ToString());

            string strSQL = string.Format("select  years, months, days, hours, times, type  from RTD_STATISTICAL{0}", tmpSQL);
            return strSQL;
        }
        public string InitialRTDStatistical(string _datetime, string _type)
        {
            DateTime tmpDatetime = DateTime.Parse(_datetime);
            string tmpSQL = "insert into RTD_STATISTICAL (years, months, days, hours, times, type) values ({0}, {1}, {2}, {3}, 0, '{4}')";
            string strSQL = string.Format(tmpSQL, tmpDatetime.Year.ToString(), tmpDatetime.Month.ToString(), tmpDatetime.Day.ToString(), tmpDatetime.Hour.ToString(), _type);
            return strSQL;
        }
        public string UpdateRTDStatistical(DateTime _datetime, string _type, int _count)
        {
            string tmpSQL = String.Format(" where years = {0} and months = {1} and days = {2} and hours = {3} and type = '{4}'",
                _datetime.Year.ToString(), _datetime.Month.ToString(), _datetime.Day.ToString(), _datetime.Hour.ToString(), _type);

            string strSQL = string.Format("update RTD_STATISTICAL set times = {0}{1}", _count, tmpSQL);
            return strSQL;
        }
        public string InsertRTDStatisticalRecord(string _datetime, string _commandid, string _type)
        {
            DateTime tmpDatetime = DateTime.Parse(_datetime);
            string tmpSQL = "insert into RTD_STATISTICAL_RECORD (years, months, days, hours, commandid, type, recordtime) values ({0}, {1}, {2}, {3}, '{4}', '{5}', TO_DATE(\'{6}\', \'yyyy-MM-dd hh24:mi:ss\'))";
            string strSQL = string.Format(tmpSQL, tmpDatetime.Year.ToString(), tmpDatetime.Month.ToString(), tmpDatetime.Day.ToString(), tmpDatetime.Hour.ToString(), _commandid, _type, _datetime);
            return strSQL;
        }
        public string QueryRTDStatisticalRecord(string _datetime)
        {
            string tmpSQL = "";
            if (!_datetime.Equals(""))
            {
                try
                {
                    DateTime tmpDatetime = DateTime.Parse(_datetime);

                    tmpSQL = String.Format(" where years = {0} and months = {1} and days = {2} and hours = {3}",
                        tmpDatetime.Year.ToString(), tmpDatetime.Month.ToString(), tmpDatetime.Day.ToString(), tmpDatetime.Hour.ToString());
                }
                catch (Exception ex)
                { }
            }
            string strSQL = string.Format("select years, months, days, hours, type, sum(times) as times,  max(recordtime) as recordtime from RTD_STATISTICAL_RECORD{0} group by years, months, days, hours, type order by years, months, days, hours, type", tmpSQL);
            return strSQL;
        }
        public string CleanRTDStatisticalRecord(DateTime _datetime, string _type)
        {
            string tmpSQL = String.Format(" where years = {0} and months = {1} and days = {2} and hours = {3} and type = '{4}'",
                _datetime.Year.ToString(), _datetime.Month.ToString(), _datetime.Day.ToString(), _datetime.Hour.ToString(), _type);

            string strSQL = string.Format("delete RTD_STATISTICAL_RECORD {0}", tmpSQL);
            return strSQL;
        }
        public string InsertRTDAlarm(string[] _alarmCode)
        {
            //string[] _alarmCode = _alarm.Split('#');
            //10000,SYSTEM,RTD,INFO,TESE ALARM,0,,00001,Params,Description
            string strSQL = string.Format(@"insert into rtd_alarm (""unitType"", ""unitID"", ""level"", ""code"", ""cause"", ""subCode"", ""detail"", ""commandID"", ""params"", ""description"", ""new"", ""createdAt"", ""last_updated"", ""eventTrigger"")
                                        values ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', 1, sysdate, sysdate, '{10}')",
                                        _alarmCode[1], _alarmCode[2], _alarmCode[3], _alarmCode[0], _alarmCode[4], _alarmCode[5], _alarmCode[6], _alarmCode[7], _alarmCode[8], _alarmCode[9], _alarmCode[10]);
            return strSQL;
        }
        public string InsertRTDAlarm(RTDAlarms _Alarms)
        {
            //10000,SYSTEM,RTD,INFO,TESE ALARM,0,,00001,Params,Description
            string strSQL = string.Format(@"insert into rtd_alarm (""unitType"", ""unitID"", ""level"", ""code"", ""cause"", ""subCode"", ""detail"", ""commandID"", ""params"", ""description"", ""new"", ""createdAt"", ""last_updated"", ""eventTrigger"")
                                        values ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', 1, sysdate, sysdate, '{10}')",
                                        _Alarms.UnitType, _Alarms.UnitID, _Alarms.Level, _Alarms.Code, _Alarms.Cause, _Alarms.SubCode, _Alarms.Detail, _Alarms.CommandID, _Alarms.Params, _Alarms.Description, _Alarms.EventTrigger);
            return strSQL;
        }
        public string SelectWorkgroupSet(string _EquipID)
        {
            string tmpCoditions = "";
            if (!_EquipID.Equals(""))
                tmpCoditions = string.Format("and a.equipid = '{0}'", _EquipID);

            string strSQL = string.Format(@"select a.equipid, a.workgroup, b.in_erack, b.out_erack, b.usefailerack, b.f_erack, b.stage, b.pretransfer, b.QTIME_LOW, b.QTIME_HIGH, b.priority as prio, b.checkeqplookuptable, b.uselaststage, b.IsFurnace, b.dummy_locate, b.effectiveslot, b.maximumqty, b.checkcustdevice, b.stagecontroller, b.cannotsame, b.aoimeasurement, b.rcpconstraint, b.wip_warehouse, b.enablewipwarehouse, b.bindworkgroup, b.preparenextworkgroup, b.nextworkgroup, b.prepareqty, b.sidewarehouse, b.swsidewh, b.onlysidewh, b.limitforsidewh, b.preparecarrierforsidewh, b.cannot, b.dummycarrier from eqp_status a
                                            left join workgroup_set b on b.workgroup=a.workgroup 
                                            where 1=1 {0}", tmpCoditions);
            return strSQL;
        }
        public string UpdateLotinfoState(string _lotID, string _state)
        {
            string strSQL = string.Format("update lot_info set state = '{0}', lastModify_dt = sysdate where lotid = '{1}'", _state, _lotID);
            return strSQL;
        }
        public string ConfirmLotinfoState(string _lotID, string _state)
        {
            string strSQL = string.Format("select * from lot_info where lotid = '{1}' and state = '{0}'", _state, _lotID);
            return strSQL;
        }
        public string QueryLotinfoQuantity(string _lotID)
        {
            string strSQL = string.Format("select * from lot_info where lotid = '{0}'", _lotID);
            return strSQL;
        }
        public string UpdateLotinfoTotalQty(string _lotID, int _TotalQty)
        {
            string strSQL = string.Format("update lot_info set total_qty = {0} where lotid = '{1}'", _TotalQty, _lotID);
            return strSQL;
        }
        public string CheckQtyforSameLotId(string _lotID, string _carrierType)
        {
            string tmpSql = @"with avail as (
                            select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, case when d.total_qty is null then 0 else d.total_qty end as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                left join LOT_INFO d on d.lotid=c.lot_id
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and d.state not in ('HOLD')
                                    and a.location_type in ('ERACK','STOCKER') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '{0}') 
                                and c.lot_id is not null order by d.sch_seq ) 
                            select lot_id, count(carrier_id) as NumOfCarr, sum(quantity) as qty, total_qty from avail where lot_id = '{1}' group by lot_id, total_qty";

            string strSQL = string.Format(tmpSql, _carrierType, _lotID);
            return strSQL;
        }
        public string QueryQuantity2ByCarrier(string _carrierID)
        {
            string strSQL = string.Format(@"select a.carrier_id, b.lot_id, b.quantity, b.total as total_qty from carrier_transfer a
                                            left join carrier_lot_associate b on a.carrier_id = b.carrier_id
                                            where a.carrier_id = '{0}'", _carrierID);
            return strSQL;
        }
        public string QueryQuantityByCarrier(string _carrierID)
        {
            string strSQL = string.Format(@"select a.carrier_id, b.lot_id, b.quantity, c.total_qty from carrier_transfer a
                                            left join carrier_lot_associate b on a.carrier_id = b.carrier_id
                                            left join lot_info c on c.lotid = b.lot_id
                                            where a.carrier_id = '{0}'", _carrierID);
            return strSQL;
        }
        public string QueryEqpPortSet(string _equipId, string _portSeq)
        {
            string strSQL = string.Format(@"select * from eqp_port_set a
                                            where a.equipid = '{0}' and a.port_seq = {1}", _equipId, _portSeq);
            return strSQL;
        }
        public string InsertTableEqpPortSet(string[] _params)
        {
            string strSQL = string.Format(@"insert into eqp_port_set (equipid, port_model, port_seq, port_type, port_id, carrier_type, near_stocker, create_dt, modify_dt, lastmodify_dt, port_state, workgroup)
                                        values ('{0}', '{1}', {2}, '{3}', '{4}', '{5}', '', sysdate, sysdate, sysdate, 0, '{6}')",
                                        _params[0], _params[1], _params[2], _params[3], _params[4], _params[5], _params[6]);
            return strSQL;
        }
        public string QueryWorkgroupSet(string _Workgroup)
        {
            string tmpWhere = " ";

            if (!_Workgroup.Equals(""))
                tmpWhere = string.Format("where workgroup = '{0}'", _Workgroup);

            string strSQL = string.Format(@"select * from workgroup_set {0}", tmpWhere);
            return strSQL;
        }
        public string QueryWorkgroupSet(string _Workgroup, string _stage)
        {
            string tmpWhere = " ";

            if (!_Workgroup.Equals(""))
                tmpWhere = string.Format("where workgroup = '{0}'", _Workgroup);

            if (!_stage.Equals(""))
            {
                if(tmpWhere.Equals(""))
                    tmpWhere = string.Format("where stage = '{0}'", _stage);
                else
                    tmpWhere = string.Format("{0} and stage = '{1}'", tmpWhere, _stage);
            }

            string strSQL = string.Format(@"select * from workgroup_set {0}", tmpWhere);
            return strSQL;
        }
        public string QueryWorkgroupSetAndUseState(string _Workgroup)
        {
            string tmpWhere = " ";

            if (!_Workgroup.Equals(""))
                tmpWhere = string.Format("where a.workgroup = '{0}'", _Workgroup);

            string strSQL = string.Format(@"select a.workgroup, a.in_Erack, a.Out_Erack, case when b.eqpnum is null then 'None' else 'USE' end UseState from workgroup_set a
                                            left join (select count(equipid) as eqpnum, workgroup from eqp_port_set group by workgroup) b
                                            on b.workgroup = a.workgroup {0}", tmpWhere);
            return strSQL;
        }
        public string CreateWorkgroup(string _Workgroup)
        {
            string strSQL = string.Format(@"insert into workgroup_set (workgroup, in_erack, out_erack, create_dt, modify_dt, lastmodify_dt)
                                            values ('{0}', '', '', sysdate, sysdate, sysdate)", _Workgroup);
            return strSQL;
        }
        public string UpdateWorkgroupSet(string _Workgroup, string _InRack, string _OutRack)
        {
            string tmpSet = "";
            string strSQL = "";

            if (!_Workgroup.Equals(""))
            {
                strSQL = string.Format("where workgroup = '{0}'", _Workgroup);

                if (!_InRack.Equals(""))
                    tmpSet = string.Format("{0}", tmpSet.Equals("") ? tmpSet + string.Format("set in_erack = '{0}'", _InRack.Trim()) : tmpSet + string.Format(", in_erack = '{0}'", _InRack.Trim()));

                if (!_OutRack.Equals(""))
                    tmpSet = string.Format("{0}", tmpSet.Equals("") ? tmpSet + string.Format("set out_erack = '{0}'", _OutRack.Trim()) : tmpSet + string.Format(", out_erack = '{0}'", _OutRack.Trim()));

                if (!tmpSet.Equals(""))
                    tmpSet = string.Format("{0},lastModify_dt = {1}", tmpSet, "sysdate");

                strSQL = string.Format(@"update workgroup_set {0} {1}
                                            ", tmpSet, strSQL);
            }

            return strSQL;
        }
        public string DeleteWorkgroup(string _Workgroup)
        {
            string strSQL = "";

            if (!_Workgroup.Equals(""))
                strSQL = string.Format(@"delete workgroup_set where workgroup = '{0}'", _Workgroup);

            return strSQL;
        }
        public string UpdateEquipWorkgroup(string _equip, string _Workgroup)
        {
            string tmpSet = "";
            string strSQL = "";

            if (!_equip.Equals(""))
            {
                strSQL = string.Format("where equipid = '{0}'", _equip);

                if (!_Workgroup.Equals(""))
                    tmpSet = string.Format("{0}", tmpSet.Equals("") ? tmpSet + string.Format("set Workgroup = '{0}'", _Workgroup) : tmpSet + string.Format(", Workgroup = '{0}'", _Workgroup));
                else
                    return strSQL;

                if (!tmpSet.Equals(""))
                    tmpSet = string.Format("{0},lastModify_dt = {1}", tmpSet, "sysdate");

                strSQL = string.Format(@"update eqp_status {0} {1}
                                            ", tmpSet, strSQL);
            }

            return strSQL;
        }
        public string UpdateEquipPortSetWorkgroup(string _equip, string _Workgroup)
        {
            string tmpSet = "";
            string strSQL = "";

            if (!_equip.Equals(""))
            {
                strSQL = string.Format("where equipid = '{0}'", _equip);

                if (!_Workgroup.Equals(""))
                    tmpSet = string.Format("{0}", tmpSet.Equals("") ? tmpSet + string.Format("set Workgroup = '{0}'", _Workgroup) : tmpSet + string.Format(", Workgroup = '{0}'", _Workgroup));
                else
                    return strSQL;

                if (!tmpSet.Equals(""))
                    tmpSet = string.Format("{0},lastModify_dt = {1}", tmpSet, "sysdate");

                strSQL = string.Format(@"update eqp_port_set {0} {1}
                                            ", tmpSet, strSQL);
            }

            return strSQL;
        }
        public string UpdateEquipPortModel(string _equip, string _portModel, int _portNum)
        {
            string tmpSet = "";
            string strSQL = "";

            //update eqp_status set port_Model = 'ABC', port_Number = 4, lastModify_dt = sysdate where equipid = 'CTDS-10';
            if (!_equip.Equals(""))
            {
                strSQL = string.Format("where equipid = '{0}'", _equip);

                if (!_portModel.Equals(""))
                    tmpSet = string.Format("{0}", tmpSet.Equals("") ? tmpSet + string.Format("set port_Model = '{0}'", _portModel) : tmpSet + string.Format(", port_Model = '{0}'", _portModel));

                if (_portNum >= 0)
                    tmpSet = string.Format("{0}", tmpSet.Equals("") ? tmpSet + string.Format("set port_Number = {0}", _portNum) : tmpSet + string.Format(", port_Number = {0}", _portNum));

                if (!tmpSet.Equals(""))
                    tmpSet = string.Format("{0},lastModify_dt = {1}", tmpSet, "sysdate");

                strSQL = string.Format(@"update eqp_status {0} {1}
                                            ", tmpSet, strSQL);
            }

            return strSQL;
        }
        public string DeleteEqpPortSet(string _Equip, string _portModel)
        {
            string strSQL = "";

            if (!_Equip.Equals(""))
            {
                strSQL = string.Format(@"delete Eqp_port_set where equipid = '{0}' and port_Model = '{1}'", _Equip, _portModel);
            }

            return strSQL;
        }
        public string QueryPortModelMapping(string _eqpTypeID)
        {
            string tmpSet = "";
            string strSQL = "";
            //            select* from rtd_default_set a
            //              left join rtd_portmodel_def b on b.portmodel = a.paramvalue
            //              where a.parameter = 'PortModelMapping' and a.paramtype = 'WLCSP_DIE_SALES\TAPER/DETAPER'

            if (!_eqpTypeID.Equals(""))
            {
                strSQL = string.Format(@"select Parameter, ParamType, PortModel, PortNumber, Port_Type_Mapping as PortTypeMapping, Carrier_Type_Mapping as CarrierTypeMapping from rtd_default_set a
                                    left join rtd_portmodel_def b on b.portmodel = a.paramvalue
                                    where a.parameter = 'PortModelMapping' and a.paramtype = '{0}'", _eqpTypeID);
            }

            return strSQL;
        }
        public string QueryPortModelDef()
        {
            string tmpWhere = " ";

            string strSQL = string.Format(@"select PortModel from rtd_portmodel_def", tmpWhere);
            return strSQL;
        }
        public string QueryProcLotInfo()
        {
            string tmpWhere = " ";

            string strSQL = string.Format(@"select * from lot_info where rtd_state = 'PROC'", tmpWhere);
            return strSQL;
        }
        public string LockMachineByLot(string _lotid, int _Quantity, int _lock)
        {
            string tmpWhere = String.Format(" where lotid = '{0}'", _lotid);
            string tmpLockMachine = _lock == 1 ? " set lockmachine = 1 {0}" : " set lockmachine = 0 {0}";

            string tmpSet = string.Format(tmpLockMachine, _Quantity > 0 ? string.Format(", Comp_qty = {0}", _Quantity) : "");

            string strSQL = string.Format(@"update lot_info {0}{1}", tmpSet, tmpWhere);
            return strSQL;
        }
        public string UpdateLotInfoForLockMachine(string _lotid)
        {
            string tmpWhere = String.Format(" where lotid = '{0}'", _lotid);

            string strSQL = string.Format(@"update lot_info set state = 'WAIT', rtd_state = 'READY', lastModify_dt = sysdate {0}", tmpWhere);
            return strSQL;
        }
        public string CheckLocationByLotid(string _lotid)
        {
            string tmpWhere = "";
            string tmpSet = "";
            string strSQL = "";

            tmpWhere = String.Format(" where a.lot_id = '{0}' and b.location_type in ('ERACK', 'STK')", _lotid);

            strSQL = string.Format(@"select distinct a.carrier_id, a.carrier_type, a.associate_state, a.lot_id, a.quantity, b.type_key, b.carrier_state, b.locate, b.portno, 
                                b.enable, b.location_type, b.metal_ring, b.reserve, b.state, c.equiplist, c.state, c.customername, c.stage, c.partid,
                                c.lotType, c.rtd_state, c.total_qty, b.info_update_dt from CARRIER_LOT_ASSOCIATE a 
                                left join CARRIER_TRANSFER b on b.carrier_id=a.carrier_id 
                                left join LOT_INFO c on c.lotid = a.lot_id {0}", tmpWhere);
            return strSQL;
        }
        public string QueryEQPType()
        {
            string strSQL = string.Format(@"select distinct equip_typeid from eqp_status where equip_type is not null");
            return strSQL;
        }
        public string QueryEQPIDType()
        {
            string strSQL = string.Format(@"select distinct equipid, equip_typeid from eqp_status where equip_type is not null");
            return strSQL;
        }
        public string InsertPromisStageEquipMatrix(string _stage, string _equipType, string _equipids, string _userId)
        {
            string tmpEqpType = string.Format(" and a.equip_typeid = '{0}' ", _equipType);
            string tmpEquips;
            if (!_equipids.Equals(""))
                tmpEquips = string.Format(" and a.equipid in ({0}) ", _equipids);
            else
                tmpEquips = "";

            string tmpWhere = String.Format(" where a.equip_type is not null {0}{1}", tmpEqpType, tmpEquips);

            string strSQL = string.Format(@"Insert into promis_stage_equip_matrix
                                            select a.equipid, '{0}' as stage, a.equip_typeid, '{1}' as entryby, 
                                            sysdate as entrydate from eqp_status a {2}", _stage, _userId, tmpWhere);
            return strSQL;
        }
        public string DeletePromisStageEquipMatrix(string _stage, string _equipType, string _equipids)
        {
            string tmpWhere = "";
            string tmpCdt = "";

            if (!_stage.Equals(""))
            {
                if (tmpCdt.Equals(""))
                    tmpCdt = string.Format("stage = '{0}'", _stage);
                else
                    tmpCdt = string.Format("{0} and stage = '{1}'", tmpCdt, _stage);
            }

            if (!_equipType.Equals(""))
            {
                if (tmpCdt.Equals(""))
                    tmpCdt = string.Format("eqptype = '{0}'", _equipType);
                else
                    tmpCdt = string.Format("{0} and eqptype = '{1}'", tmpCdt, _equipType);
            }

            if (!_equipids.Equals(""))
            {
                string[] lstEqpid;
                string tmpEqpid = "";
                if (_equipids.IndexOf(',') > 0)
                {
                    lstEqpid = _equipids.Split(',');
                    foreach (string eqpid in lstEqpid)
                    {
                        tmpEqpid = tmpEqpid.Equals("") ? string.Format("'{0}'", eqpid) : string.Format("{0},'{1}'", tmpEqpid, eqpid);
                    }
                }
                else
                {
                    tmpEqpid = string.Format("'{0}'", _equipids);
                }

                if (tmpCdt.Equals(""))
                    tmpCdt = string.Format("eqpid in ({0})", tmpEqpid);
                else
                    tmpCdt = string.Format("{0} and eqpid in ({1})", tmpCdt, tmpEqpid);
            }

            if (tmpWhere.Equals(""))
            {
                tmpWhere = tmpCdt.Equals("") ? "" : String.Format(" where {0}", tmpCdt);
            }

            string strSQL = string.Format(@"delete promis_stage_equip_matrix {0}", tmpWhere);
            return strSQL;
        }
        //public string UpdateEQPPortModel(string _EquipID, string _PortModel)
        //{

        //    string strSQL = string.Format(@"insert into rtd_alarm
        //                                ('unitType', 'unitID', 'level', 'code', 'cause', 'subCode', 'detail', 'commandID', 'params', 'description', 'new', 'createdAt', 'last_updated')
        //                                values ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', 1, sysdate, sysdate)",
        //                                _alarmCode[0], _alarmCode[1], _alarmCode[2], _alarmCode[3], _alarmCode[4], _alarmCode[5], _alarmCode[6], _alarmCode[7], _alarmCode[8], _alarmCode[9]);
        //    return strSQL;
        //}
        public string SyncStageInfo()
        {
            string strSQL = string.Format(@"insert into RTD_DEFAULT_SET 
                select 'PromisStage' as parameter, 'string' as paramtype, stage as paramvalue, 'RTD' as modifyby,  sysdate as lastmodify_dt, '' as description  from (
                select a.stage, b.paramvalue, case when b.paramvalue is null then 'New' end as State  from (select distinct stage from lot_info) a
                left join RTD_DEFAULT_SET b on b.parameter = 'PromisStage' and b.paramvalue = a.stage) where state = 'New'");
            return strSQL;
        }
        public string CheckRealTimeEQPState()
        {
            string strSQL = "";

#if DEBUG
            //Do Nothing
#else
            strSQL = string.Format(@"select a.equipid, b. machine_state, b.curr_status, b.down_state, b.up_date from eqp_status a
                                        left join rts_active@CIMDB3.world b on b.equipid=a.equipid
                                        where a.curr_status <> b.curr_status or a.down_state <> b.down_state");
#endif
            return strSQL;
        }
        public string UpdateCurrentEQPStateByEquipid(string _equipid)
        {
            string strSQL = "";
#if DEBUG
            //Do Nothing
#else
            strSQL = string.Format(@"update eqp_status a set machine_state = (select case when machine_state is null then 'Know' else machine_state end as machine_state from rts_active@CIMDB3.world where equipid = a.equipid), 
                                    curr_status = (select case when curr_status is null then 'Know' else curr_status end as curr_status from rts_active@CIMDB3.world where equipid = a.equipid), 
                                    down_state = (select case when down_state is null then 'Know' else down_state end as down_state from rts_active@CIMDB3.world where equipid = a.equipid), 
                                    lastmodify_dt = sysdate
                                    where a.equipid = '{0}'", _equipid);
#endif
            return strSQL;
        }
        public string QueryEquipListFirst(string _lotid, string _equipid)
        {
            string strSQL = "";

            strSQL = string.Format(@"select * from lot_info where lotid = '{0}' and equiplist like '{1}%'", _lotid, _equipid);
            return strSQL;
        }
        public string QueryRackByGroupID(string _groupID)
        {
            string strSQL = string.Format(@"select * from rack where ""groupID"" = '{0}' union select * from rack where ""erackID"" = '{0}' union select * from rack where ""groupID"" like '%{0}|%' or ""groupID"" like '%|{0}%' union select * from rack where sector like '%{0}%'", _groupID);
            return strSQL;
        }
        public string QueryExtenalCarrierInfo(string _table)
        {
            string strSQL = string.Format(@"select distinct * from (
                                                select * from {0} Where lotid not in (select lot_id from CARRIER_LOT_ASSOCIATE) or cstid not in (select carrier_id from carrier_transfer)
                                            union all
                                                select a.* from {0} a
                                                left join CARRIER_LOT_ASSOCIATE b on b.carrier_id=a.cstid and nvl(b.lot_id,'NA') = a.lotid and b.quantity != a.lotqty 
                                                where b.carrier_id is not null
                                            union all
                                                select a.* from {0} a
                                                left join CARRIER_LOT_ASSOCIATE b on b.carrier_id=a.cstid and nvl(b.lot_id,'NA') != a.lotid
                                                where b.carrier_id is not null
                                            union all
                                                select a.* from {0} a
                                                left join carrier_transfer b on b.carrier_id=a.cstid and  b.quantity != a.lotqty 
                                                where b.carrier_id is not null)"
                                            , _table);
            return strSQL;
        }
        public string InsertCarrierLotAsso(CarrierLotAssociate _carrierLotAsso)
        {
            string strSQL = string.Format(@"insert into carrier_lot_associate (carrier_id, tag_type, carrier_type, associate_state, change_state_time, lot_id, 
                                         quantity, change_station, change_station_type, update_by, update_time, create_by, new_bind)
                                         values ('{0}', '{1}', '{2}', 'Associated With Lot', sysdate, '{3}', 
                                         {4}, 'SyncEwlbCarrier', 'Sync', 'RTD', to_date('{5}', 'yyyy/MM/dd HH24:mi:ss'), '{6}', 1)", _carrierLotAsso.CarrierID,
                                         _carrierLotAsso.TagType, _carrierLotAsso.CarrierType, _carrierLotAsso.LotID, _carrierLotAsso.Quantity, _carrierLotAsso.UpdateTime, _carrierLotAsso.CreateBy);
            return strSQL;
        }
        public string UpdateCarrierLotAsso(CarrierLotAssociate _carrierLotAsso)
        {
            string strSQL = string.Format(@"update carrier_lot_associate set carrier_id='{0}', tag_type='{1}', carrier_type='{2}', associate_state='Associated With Lot', change_state_time=sysdate, lot_id='{3}', 
                                         quantity={4}, change_station='SyncEwlbCarrier', change_station_type='Sync', update_by='RTD', update_time=to_date('{5}', 'yyyy/MM/dd HH24:mi:ss'), create_by='{6}', new_bind=1 
                                         where carrier_id='{7}'", _carrierLotAsso.CarrierID,
                                         _carrierLotAsso.TagType, _carrierLotAsso.CarrierType, _carrierLotAsso.LotID, _carrierLotAsso.Quantity, _carrierLotAsso.UpdateTime, _carrierLotAsso.CreateBy, _carrierLotAsso.CarrierID);
            return strSQL;
        }
        public string UpdateLastCarrierLotAsso(CarrierLotAssociate _carrierLotAsso)
        {
            string strSQL = string.Format(@"update CARRIER_LOT_ASSOCIATE set last_associate_state=associate_state, last_lot_id=lot_id, last_change_state_time=sysdate, last_change_station=change_station, 
                                            last_change_station_type=change_station_type where carrier_id = '{0}'", _carrierLotAsso.CarrierID);
            return strSQL;
        }
        public string InsertCarrierTransfer(string _carrierId, string _typeKey, string _quantity)
        {
            string strSQL = string.Format(@"insert into carrier_transfer (carrier_id, type_key, location_type, create_dt, quantity) 
                                        values ('{0}', '{1}', 'Sync', sysdate, {2})", _carrierId, _typeKey, _quantity);
            return strSQL;
        }
        public string UpdateCarrierTransfer(string _carrierId, string _typeKey, string _quantity)
        {
            string strSQL = string.Format(@"update carrier_transfer set carrier_id='{0}', type_key='{1}', location_type='Sync', Modify_dt=sysdate, quantity={2},
                                            carrier_state='OFFLINE', locate='', portno='', enable=1, metal_ring=0, reserve=0, state='Normal'
                                            where carrier_id = '{3}'", _carrierId, _typeKey, _quantity, _carrierId);
            return strSQL;
        }
        public string LockEquip(string _equip, bool _lock)
        {
            string strSQL = "";

            if (_lock)
            {
                strSQL = String.Format("update eqp_status set islock = 1 where equipid = '{0}'", _equip);
            }
            else
            {
                strSQL = String.Format("update eqp_status set islock = 0 where equipid = '{0}'", _equip);
            }

            return strSQL;
        }
        public string QueryEquipLockState(string _equip)
        {
            string strSQL = string.Format(@"select * from eqp_status where equipid = '{0}' and islock = 1", _equip);
            return strSQL;
        }
        public string QueryPreTransferList(string _lotTable)
        {
            string strSQL = "";
            //actlinfo_lotid_vw
            //rtd_ewlb_ew_mds_tw_rule_tbl_vw , Q-Time table
            //order condition: gonow desc, qtime1 desc, priority asc, lot_age desc
            //Round((carr.qtime-carr.minallowabletw)/nullif((carr.maxallowabletw-carr.minallowabletw), 0), 3) as qTime1
            //Round((g.qtime - g.MINALLOWABLETW) / nullif((g.MAXALLOWABLETW - g.MINALLOWABLETW), 0) , 3) as qtime1
            //select nvl(3-2/nullif(1-0,0),0) from dual;
            //nvl((g.qtime-g.MINALLOWABLETW)/nullif((g.MAXALLOWABLETW-g.MINALLOWABLETW),0), 0) as qtime1

//20241028 eotd(g.lot_id) change to g.enddate   //20241104 rollback

            strSQL = string.Format(@"select layer, g.carrier_id, g.command_type as carrierType, g.locate, g.lot_id, g.stg1, g.stg2, g.stage, g.in_erack, g.workgroup, g.priority as lot_priority, g.lot_age, g.qtime, g.minallowabletw, g.maxallowabletw, round(nvl((g.qtime-g.MINALLOWABLETW)/nullif((g.MAXALLOWABLETW-g.MINALLOWABLETW),0), 0), 3) as qtime1,
case when g.maxallowabletw-g.qtime<=3 then 'Y' else 'N' end gonow from (
select distinct substr(c1.stage, 0, 3) as layer, a.carrier_id, a.locate, b1.command_type, b.lot_id, e.stage stg1,e1.stage stg2, case when nvl(e.stage, 'NA') <> 'NA' then e.stage else e1.stage end stage, 
case when nvl(e.stage, 'NA') <> 'NA' then e.in_erack else e1.in_erack end in_erack, d.workgroup,
c1.priority, c1.lot_age, nvl(c1.qtime, 0) as qtime, nvl(f.MINALLOWABLETW, 0) MINALLOWABLETW, nvl(f.MAXALLOWABLETW, 0 ) MAXALLOWABLETW, 
c1.enddate, c1.sch_seq 
from carrier_transfer a
left join carrier_lot_associate b on b.carrier_id=a.carrier_id
left join carrier_type_set b1 on b1.type_key=a.type_key
left join lot_info c1 on c1.lotid=b.lot_id
left join {0} c on c.lotid=b.lot_id
left join (select distinct m.stage, eqp.workgroup from promis_stage_equip_matrix m 
left join eqp_status eqp on eqp.equipid=m.eqpid) d on d.stage=c.stage
left join workgroup_set e on e.workgroup=d.workgroup and e.stage=c.stage and e.pretransfer=1
left join workgroup_set e1 on e1.workgroup=d.workgroup and e1.stage='DEFAULT' and e1.pretransfer=1
left join rtd_ewlb_ew_mds_tw_rule_tbl_vw f on f.CUSTOMER=c.customername and f.TOSTAGE=c.stage and f.PROCESSGROUP=c.pkgfullname
where a.enable=1 and a.carrier_state='ONLINE' and a.locate is not null and a.uat=0
and a.location_type in ('ERACK','STK') 
and a.reserve=0 and a.state not in ('HOLD', 'SYSHOLD')
and c.lotid is not null and c.state in ('WAIT')
and (e.in_erack is not null or e1.in_erack is not null)) g
order by layer desc, eotd(g.lot_id) asc, gonow desc, qtime1 desc, priority desc, g.sch_seq asc", _lotTable);

            //20241104 pretransfer condition lot_age desc change to sch_seq asc
            return strSQL;
        }
        public string QueryPreTransferListForUat(string _lotTable)
        {
            string strSQL = "";
            //actlinfo_lotid_vw
            //rtd_ewlb_ew_mds_tw_rule_tbl_vw , Q-Time table
            //order condition: gonow desc, qtime1 desc, priority asc, lot_age desc

            //20241028 eotd(g.lot_id) change to g.enddate   //20241104 rollback

            strSQL = string.Format(@"select layer, g.carrier_id, g.command_type as carrierType, g.locate, g.lot_id, g.stg1, g.stg2, g.stage, g.in_erack, g.workgroup, g.priority as lot_priority, g.lot_age, g.qtime, g.minallowabletw, g.maxallowabletw, round(nvl((g.qtime-g.MINALLOWABLETW)/nullif((g.MAXALLOWABLETW-g.MINALLOWABLETW),0), 0), 3) as qtime1,
case when g.maxallowabletw-g.qtime<=3 then 'Y' else 'N' end gonow from (
select distinct substr(c1.stage, 0, 3) as layer, a.carrier_id, a.locate, b1.command_type, b.lot_id, e.stage stg1,e1.stage stg2, case when nvl(e.stage, 'NA') <> 'NA' then e.stage else e1.stage end stage, 
case when nvl(e.stage, 'NA') <> 'NA' then e.in_erack else e1.in_erack end in_erack, d.workgroup,
c1.priority, c1.lot_age, nvl(c1.qtime, 0) as qtime, nvl(f.MINALLOWABLETW, 0) MINALLOWABLETW, nvl(f.MAXALLOWABLETW, 0 ) MAXALLOWABLETW, 
c1.enddate, c1.sch_seq 
from carrier_transfer a
left join carrier_lot_associate b on b.carrier_id=a.carrier_id
left join carrier_type_set b1 on b1.type_key=a.type_key
left join lot_info c1 on c1.lotid=b.lot_id
left join {0} c on c.lotid=b.lot_id
left join (select distinct m.stage, eqp.workgroup from promis_stage_equip_matrix m 
left join eqp_status eqp on eqp.equipid=m.eqpid) d on d.stage=c.stage
left join workgroup_set e on e.workgroup=d.workgroup and e.stage=c.stage and e.pretransfer=1
left join workgroup_set e1 on e1.workgroup=d.workgroup and e1.stage='DEFAULT' and e1.pretransfer=1
left join rtd_ewlb_ew_mds_tw_rule_tbl_vw f on f.CUSTOMER=c.customername and f.TOSTAGE=c.stage and f.PROCESSGROUP=c.pkgfullname
where a.enable=1 and a.carrier_state='ONLINE' and a.locate is not null and a.uat=1
and a.location_type='ERACK' 
and a.reserve=0 and a.state not in ('HOLD','SYSHOLD')
and c.lotid is not null and c.state in ('WAIT')
and e.in_erack is not null) g
order by layer desc, eotd(g.lot_id) asc, gonow desc, qtime1 desc, priority desc, g.sch_seq asc", _lotTable);

            //20241104 pretransfer condition lot_age desc change to sch_seq asc

            return strSQL;
        }
        public string CheckCarrierLocate(string _inErack, string _locate)
        {
            string strSQL = string.Format(@"select ""erackID"" from (
                                        select ""erackID"" from rack where ""erackID"" = '{0}'
                                        union select ""erackID"" from rack where ""groupID"" = '{0}'
                                        union select ""erackID"" from rack where inStr(sector,'{0}') > 0)
                                        where ""erackID"" = '{1}'", _inErack, _locate);
            return strSQL;
        }

        public string CheckPreTransfer(string _carrierid, string _table)
        {
            //string _table = "workinprocess_sch";
            string strSQL = string.Format(@"select * from {0} where cmd_type='Pre-Transfer' and carrierid = '{1}'", _table, _carrierid);
            return strSQL;
        }
        //select * from workinprocess_sch where cmd_type='Pre-Transfer' and carrierid = '12CA0051'
        public string ManualModeSwitch(string _equip, bool _manualMode)
        {
            string strSQL = "";

            if (_manualMode)
            {
                strSQL = String.Format("update eqp_status set manualmode=1, modify_dt=sysdate where equipid = '{0}'", _equip);
            }
            else
            {
                strSQL = String.Format("update eqp_status set manualmode=0, modify_dt=sysdate where equipid = '{0}'", _equip);
            }

            return strSQL;
        }

        public string QueryLotStageWhenStageChange(string _table)
        {
            string strSQL = "";

            strSQL = String.Format(@"select distinct a.carrier_id, b.lot_id, d.stage from (
                                    select carrier_id from carrier_transfer where location_type in ('ERACK','STK')) a
                                    left join carrier_lot_associate b on b.carrier_id=a.carrier_id
                                    left join lot_info c on c.lotid = b.lot_id and c.state = 'HOLD'
                                    left join {0} d on d.lotid=b.lot_id
                                    where c.stage <> d.stage", _table);

            return strSQL;
        }
        public string QueryReserveStateByEquipid(string _equipid)
        {
            string strSQL = "";
            string strWhere = "";
            strWhere = string.Format(" where equipid = '{0}'", _equipid);

            strSQL = string.Format(@"select * from eqp_reserve_time{0}", strWhere);
            return strSQL;
        }
        public string InsertEquipReserve(string _args)
        {
            string strSQL = "";
            string strWhere = "";
            string[] args = _args.Split(",");
            string _equipid = args[0].Trim();
            string _timeStart = args[1].Trim();
            string _timeEnd = args[2].Trim();
            string _reserveBy = args[3].Trim();

            strSQL = string.Format(@"insert into eqp_reserve_time (equipid, dt_start, dt_end, reserveby, create_dt)
                                    values ('{0}', '{1}', '{2}', '{3}', sysdate)", _equipid, _timeStart, _timeEnd, _reserveBy);
            return strSQL;
        }
        public string UpdateEquipReserve(string _args)
        {
            string strSQL = "";
            string strWhere = "";
            string strSet = "";
            string[] args = _args.Split(",");
            string _type = args[0].Trim();
            string _equipId = args[1].Trim();
            string _reserveBy = args[2].Trim();
            string _timeStart = "";
            string _timeEnd = "";
            string _effective = "";
            string _expired = "";


            switch (_type.ToLower())
            {
                case "settime":
                    _timeStart = args[3].Trim();
                    _timeEnd = args[4].Trim();
                    strSet = string.Format("dt_start='{0}', dt_end='{1}', effective=0, expired=0, reserveby='{2}',", _timeStart, _timeEnd, _reserveBy);
                    break;
                case "reset":
                    strSet = string.Format("effective=0, expired=0, reserveby='{0}',", _reserveBy);
                    break;
                case "effective":
                    _effective = args[3].Trim();
                    strSet = string.Format("effective={0}, ", _effective);
                    break;
                case "expired":
                    _expired = args[3].Trim();
                    if (_expired.Equals("1"))
                        strSet = string.Format("effective=0, expired={0}, ", _expired);
                    else if (_expired.Equals("0"))
                        strSet = string.Format("expired={0}, ", _expired);
                    break;
                default:
                    break;
            }

            strWhere = string.Format("where equipid = '{0}'", _equipId);

            strSQL = string.Format(@"update eqp_reserve_time set {0}lastmodify_dt=sysdate {1}", strSet, strWhere);
            return strSQL;
        }
        public string LockLotInfoWhenReady(string _lotID)
        {
            string tmpString = "update lot_info set IsLock = 1 where lotid = '{0}'";
            string strSQL = string.Format(tmpString, _lotID);
            return strSQL;
        }
        public string UnLockLotInfoWhenReady(string _lotID)
        {
            string tmpString = "update lot_info set IsLock = 0 where lotid = '{0}'";
            string strSQL = string.Format(tmpString, _lotID);
            return strSQL;
        }
        public string UnLockAllLotInfoWhenReadyandLock()
        {
            string tmpString = "update lot_info set IsLock = 0 where IsLock = 1 and rtd_state = 'READY'";
            string strSQL = string.Format(tmpString);
            return strSQL;
        }

        public string QueryLotInfoByCarrier(string _carrierid)
        {
            string strSQL = "";

            //20241028 eotd(b.lotid) change to b.enddate    //20241104 roll back

#if DEBUG
            strSQL = string.Format(@"select a.carrier_id, a.lot_id, b.islock, eotd, b.custdevice from carrier_lot_associate a 
                                               left join lot_info b on b.lotid = a.lot_id
                                               where a.carrier_id = '{0}'", _carrierid);
#else
            strSQL = string.Format(@"select a.carrier_id, a.lot_id, b.islock, eotd(b.lotid) as eotd, b.custdevice from carrier_lot_associate a 
                                               left join lot_info b on b.lotid = a.lot_id
                                               where a.carrier_id = '{0}'", _carrierid);
#endif

            return strSQL;
        }
        public string CheckReserveState(string _args)
        {
            string strSQL = "";
            string strWhere = "";
            string _type = _args;

            switch (_type.ToLower())
            {
                case "reservestart":
                    strWhere = string.Format(" where effective=0 and expired=0");
                    break;
                case "reserveend":
                    strWhere = string.Format(" where effective=1 and expired=0");
                    break;
                default:
                    break;
            }



            strSQL = string.Format(@"select * from eqp_reserve_time{0}", strWhere);
            return strSQL;
        }
        public string UpdateStageByLot(string _lotid, string _stage)
        {
            string tmpString = "update lot_info set EQUIPLIST='', EQUIP_ASSO='N', stage='{0}' where lotid='{1}'";
            string strSQL = string.Format(tmpString, _stage, _lotid);
            return strSQL;
        }
        public string QueryLastLotFromEqpPort(string EquipId, string PortSeq)
        {
            string strSQL = string.Format(@"select b.lot_id, b.last_lot_id, case when b.lot_id is not null then b.lot_id else b.last_lot_id end as lastLot from carrier_transfer a
                                            left join carrier_lot_associate b on b.carrier_id=a.carrier_id 
                                            where a.locate='{0}' and a.portno={1}", EquipId, PortSeq);

            return strSQL;
        }
        public string UpdateLastLotIDtoEQPPortSet(string EquipId, string PortSeq, string LastLotID)
        {
            string strSQL = string.Format("update EQP_PORT_SET set lastLotid = '{0}' where EquipId = '{1}' and Port_Seq = '{2}' ", LastLotID, EquipId, PortSeq);
            return strSQL;
        }
        public string ConfirmLotInfo(string _lotid)
        {
            string strSQL = string.Format(@"select * from LOT_INFO where rtd_state not in ('HOLD','PROC','DELETED', 'COMPLETED') and state='WAIT' and equiplist is null and lotid='{0}'", _lotid);
            return strSQL;
        }
        public string IssueLotInfo(string _lotid)
        {
            string strSQL = string.Format(@"select * from LOT_INFO where rtd_state not in ('HOLD','PROC','DELETED', 'COMPLETED') 
                            and to_date(to_char(trunc(starttime, 'DD'),'yyyy/MM/dd HH24:mi:ss'), 'yyyy/MM/dd HH24:mi:ss') <= sysdate
                            and lotid = '{0}'
                            order by  customername, stage, rtd_state, lot_age desc", _lotid);
            return strSQL;
        }

        public string CheckLotStage(string _table, string _lotid)
        {
            //230417V1.0 增加條件！此lot 不可為剛下機台仍處於HOLD狀態的料
            //string strSQL = string.Format(@"select a.lotid, a.stage as stage1, a.state, b.stage as stage2 from {0} a 
            //                    left join lot_info b on b.lotid = a.lotid 
            //                    where a.lotid='{1}' and b.stage=a.stage and b.state not in ('HOLD')", _table, _lotid);
            string strSQL = "";

            strSQL = string.Format(@"select a.lotid, a.stage as stage1, b.stage as stage2, a.state as state1, b.state as state2 from lot_info a 
                            left join {0} b on b.lotid = a.lotid 
                            where a.lotid='{1}' and b.state not in ('HOLD')", _table, _lotid);

            return strSQL;
        }
        public string EQPListReset(string LotID)
        {
            string strSQL = string.Format(@"update LOT_INFO set equip_asso = 'N', equiplist = '' where lotid = '{0}'", LotID);
            //rtd_state = 'INIT', lastmodify_dt = sysdate where lotid = '{0}'", LotID);
            return strSQL;
        }
        public string GetEquipCustDevice(string EquipID)
        {

            string strSQL = string.Format(@"select device from rts.v_rts_equipment@cimdb3.world where equipid='{0}'", EquipID);
#if DEBUG
            strSQL = string.Format(@"select device from eqp_status where equipid='{0}'", EquipID);
#else
            strSQL = string.Format(@"select equipid, nvl(device, '') as device from rts.v_rts_equipment@cimdb3.world where equipid='{0}'", EquipID);
#endif
            //rtd_state = 'INIT', lastmodify_dt = sysdate where lotid = '{0}'", LotID);
            return strSQL;
        }
        public string CheckMetalRingCarrier(string _carrierID)
        {
            string strSQL = string.Format(@"select a.lot_id, a.carrier_id, a.associate_state, a.total, a.quantity, b.type_key, b.locate, b.portno, b.carrier_state, b.location_type, b.reserve, c.command_type  from carrier_lot_associate a 
                                        left join carrier_transfer b on b.carrier_id = a.carrier_id 
                                        left join carrier_type_set c on c.type_key = b.type_key
                                        where b.location_type in ('ERACK','STK') and a.lot_id = (select lot_id||'R' from carrier_lot_associate where carrier_id = '{0}')", _carrierID);

            return strSQL;
        }
        public string UpdatePriorityByLotid(string _lotID, int _priority)
        {
            string strSQL = string.Format(@"update lot_info set priority = {0} where lotid = '{1}'", _priority, _lotID);

            return strSQL;
        }

        public string QueryDataByLotid(string _table, string _lotID)
        {
            string strSQL = string.Format(@"select * from {0} where lotid = '{1}'", _table, _lotID);

            return strSQL;
        }
        public string HisCommandAppend(HisCommandStatus hisCommandStatus)
        {
            string strSQL = string.Format(@"insert into HIS_COMMANDS (""CommandID"", ""CarrierID"", ""LotID"", ""CommandType"", ""Source"", ""Dest"", ""AlarmCode"", ""Reason"", ""createdAt"", ""LastStateTime"")
                                        values('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', TO_DATE('{8}', 'yyyy/MM/dd HH24:mi:ss'), TO_DATE('{9}', 'yyyy/MM/dd HH24:mi:ss'))",
                                        hisCommandStatus.CommandID, hisCommandStatus.CarrierID, hisCommandStatus.LotID, hisCommandStatus.CommandType, hisCommandStatus.Source, hisCommandStatus.Dest, hisCommandStatus.AlarmCode, hisCommandStatus.Reason, hisCommandStatus.CreatedAt, hisCommandStatus.LastStateTime);
            return strSQL;
        }
        public string GetWorkinprocessSchByCommand(string command, string _table)
        {
            //string _table = "workinprocess_sch";

            string strSQL = string.Format(@"select cmd_id, cmd_type, equipid, cmd_state, cmd_current_state, carrierid, carriertype, source, dest, lotid, customer,
                                            to_char(create_dt,'yyyy-mm-dd hh24:mi:ss') as create_dt,  to_char(lastModify_dt,'yyyy-mm-dd hh24:mi:ss') as lastModify_dt
                                            from {0} where cmd_Id = '{1}'",
                                            _table, command);
            return strSQL;
        }
        public string GetRTSEquipStatus(string _table, string equipid)
        {
            string strSQL = string.Format(@"select * from {0} where equipid = '{1}'",
                                            _table, equipid);
            return strSQL;
        }
        //value.CurrentDateTime, value.Unit, value.Zone
        public string GetHistoryCommands(string StartTime, string EndTime)
        {
            /*
            string strSQL = string.Format(@"select a.IDX, a.""CommandID"", a.""CarrierID"", a.""LotID"", a.""CommandType"", a.""Source"", a.""Dest"", a.""AlarmCode"", a.""createdAt"", a.""LastStateTime"", b.""AlarmText"" as Reason from his_commands a
                                        left join alarm_detail b on b.""AlarmCode"" = a.""AlarmCode"" where a.""LastStateTime"" between to_date('{0}','yyyy/mm/dd hh24:mi:ss') and to_date('{1}','yyyy/mm/dd hh24:mi:ss')",
                                            StartTime, EndTime);*/

            string strSQL = string.Format(@"select * from his_commands a
                                         where a.""LastStateTime"" between to_date('{0}','yyyy/mm/dd hh24:mi:ss') and to_date('{1}','yyyy/mm/dd hh24:mi:ss')",
                                            StartTime, EndTime);

            return strSQL;
        }
        //select * from rts_active@CIMDB3.world where equipid = '3PBG3-D'
        public string ResetRTDStateByLot(string LotID)
        {
            string strSQL = string.Format(@"update LOT_INFO set state='WAIT', 
                                               rtd_state = 'WAIT' where lotid = '{0}'", LotID);

            return strSQL;
        }
        //update eqp_status set custDevice = '' where equipid = '3PBG1-D'
        public string UpdateCustDeviceByEquipID(string _equipID, string _custDevice)
        {
            string strSQL = string.Format(@"update eqp_status set custDevice = '{0}' where equipid = '{1}'", _custDevice, _equipID);

            return strSQL;
        }
        public string UpdateCustDeviceByLotID(string _lotID, string _custDevice)
        {
            string strSQL = string.Format(@"update lot_info set custDevice = '{0}' where lotID = '{1}'", _custDevice, _lotID);

            return strSQL;
        }
        public string InsertHisTSCAlarm(TSCAlarmCollect _alarmCollect)
        {
            string strSQL = string.Format(@"insert into his_tscalarm ( ""ALID"", ""ALTX"", ""ALType"", ""ALSV"", ""UnitType"", ""UnitID"", ""Level"", ""SubCode"")
                            values ({0}, '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}') "
                        , _alarmCollect.ALID, _alarmCollect.ALTX, _alarmCollect.ALType, _alarmCollect.ALSV, _alarmCollect.UnitType, _alarmCollect.UnitID, _alarmCollect.Level, _alarmCollect.SubCode);

            return strSQL;
        }
        public string SetPreDispatching(string _Workgroup, string _Type)
        {
            string tmpSet = "";
            string strSQL = "";
            int iPreTransfer = -1;

            if (!_Workgroup.Equals(""))
            {
                strSQL = string.Format("where workgroup = '{0}'", _Workgroup);

                if (_Type.Trim().ToUpper().Equals("SET"))
                    iPreTransfer = 1;
                else if (_Type.Trim().ToUpper().Equals("RESET"))
                    iPreTransfer = 0;
                else
                    iPreTransfer = 0;

                if (iPreTransfer != -1)
                {
                    tmpSet = string.Format("set pretransfer = {0}", iPreTransfer.ToString());
                }

                if (!tmpSet.Equals(""))
                    tmpSet = string.Format("{0},lastModify_dt = {1}", tmpSet, "sysdate");

                strSQL = string.Format(@"update workgroup_set {0} {1}
                                            ", tmpSet, strSQL);
            }

            return strSQL;
        }
        public string CalcStatisticalTimes(string StartTime, string EndTime)
        {
            /*
            string strSQL = string.Format(@"select a.IDX, a.""CommandID"", a.""CarrierID"", a.""LotID"", a.""CommandType"", a.""Source"", a.""Dest"", a.""AlarmCode"", a.""createdAt"", a.""LastStateTime"", b.""AlarmText"" as Reason from his_commands a
                                        left join alarm_detail b on b.""AlarmCode"" = a.""AlarmCode"" where a.""LastStateTime"" between to_date('{0}','yyyy/mm/dd hh24:mi:ss') and to_date('{1}','yyyy/mm/dd hh24:mi:ss')",
                                            StartTime, EndTime);*/

            /*            string strSQL = string.Format(@"select Status, count(""CommandID"") times  from (
                                            select ""CommandID"", ""CarrierID"", ""LotID"", ""CommandType"", ""createdAt"", ""LastStateTime"", case ""AlarmCode"" when '0' then 'Success' else 'Failed' end as Status  from his_commands a
                                            where a.""LastStateTime"" between to_date('{0}','yyyy/mm/dd hh24:mi:ss') and to_date('{1}','yyyy/mm/dd hh24:mi:ss')
                                            ) where ""CommandType"" in ('LOAD') and ""LotID"" is not null
                                            group by Status", StartTime, EndTime);*/

            string strSQL = string.Format(@"select Status, count(""IDX"") times  from (
                                select ""IDX"", ""CommandID"", ""CarrierID"", ""LotID"", ""CommandType"", ""createdAt"", ""LastStateTime"", case ""AlarmCode"" when '0' then 'Success' else 'Failed' end as Status  from his_commands a
                                where a.""LastStateTime"" between to_date('{0}','yyyy/mm/dd hh24:mi:ss') and to_date('{1}','yyyy/mm/dd hh24:mi:ss')
                                ) group by Status", StartTime, EndTime);

            return strSQL;
        }
        public string QueryCarrierByCarrierID(string _carrierID)
        {

            string strSQL = string.Format(@"select * from carrier_transfer where carrier_id = '{0}'",
                                            _carrierID);

            return strSQL;
        }
        public string QueryListAlarmDetail()
        {

            string strSQL = string.Format(@"select * from alarm_detail");

            return strSQL;
        }
        public string QueryAlarmDetailByCode(string _alarmCode)
        {

            string strSQL = string.Format(@"select * from alarm_detail where ""AlarmCode"" = '{0}'", _alarmCode);

            return strSQL;
        }
        public string UpdateLotAgeByLotID(string _lotID, string _lotAge)
        {

            string strSQL = string.Format(@"update lot_info set lot_age = {0} where lotid = '{1}'",
                                            _lotAge, _lotID);

            return strSQL;
        }
        public string QueryAvailbleAOIMachineByLotid(string _lotID, string _table)
        {
            //_table = "odsu3.actl_ewlb_equip_temp_vw@stmes15ods.stats.com.sg";

            string strSQL = string.Format(@"select lotid, stage, equip, equip2 from {0}  where lotid='{1}'", _table, _lotID);

            return strSQL;
        }
        public string CheckMeasurementAndThickness(string _table, string _lotID, string _Stage, string _EquipID)
        {
            string tmpCdts = "";
            string strSQL = "";

            if (_lotID.Trim().Equals(""))
                tmpCdts = string.Format(@" where stage='{0}' and equipid='{1}'", _Stage, _EquipID);
            else if (_EquipID.Trim().Equals(""))
                tmpCdts = string.Format(@" where lotid='{0}' and stage='{1}'", _lotID, _Stage);
            else
                tmpCdts = string.Format(@" where lotid='{0}' and stage='{1}' and equipid='{2}'", _lotID, _Stage, _EquipID);

            strSQL = string.Format(@"select EQUIPID from {0}{1}", _table, tmpCdts);

            return strSQL;
        }
        public string LockEquipPortByPortId(string _portID, bool _lock)
        {
            string tmpString = "update eqp_port_set {0} where port_id = '{1}'";
            string tmpCdt = "";
            if (_lock)
                tmpCdt = "set islock = 1, lastModify_dt = sysdate";
            else
                tmpCdt = "set islock = 0, lastModify_dt = sysdate";
            string strSQL = string.Format(tmpString, tmpCdt, _portID);

            return strSQL;
        }
        public string CheckLotStageHold(string _table, string _lotid)
        {
            //230417V1.0 增加條件！此lot 不可為剛下機台仍處於HOLD狀態的料
            //string strSQL = string.Format(@"select a.lotid, a.stage as stage1, a.state, b.stage as stage2 from {0} a 
            //                    left join lot_info b on b.lotid = a.lotid 
            //                    where a.lotid='{1}' and b.stage=a.stage and b.state not in ('HOLD')", _table, _lotid);
            string strSQL = "";
            if (!_lotid.Trim().Equals(""))
            {
                strSQL = string.Format(@"select a.lotid, a.stage as stage1, b.stage as stage2 from lot_info a 
                                left join {0} b on b.lotid = a.lotid 
                                where a.lotid='{1}' and b.state not in ('HOLD')", _table, _lotid);
            }

            return strSQL;
        }
        //select * from eqp_port_set where equipid = '3AOI19-R' and port_state not in (3,5)
        public string CheckPortStateIsUnload(string _eqpip)
        {
            string strSQL = string.Format(@"select * from eqp_port_set where equipid = '{0}' and port_state in (3,5)", _eqpip);

            return strSQL;
        }
        public string QueryLCASInfoByCarrierID(string _carrierID)
        {
            string strSQL = string.Format(@"SELECT * from carrier_lot_associate where carrier_id = '{0}'", _carrierID);

            return strSQL;
        }
        public string QueryOrderWhenOvertime(string _overtime, string _table)
        {
            //string _table = "workinprocess_sch";

            string strSQL = string.Format(@"select * from {0} where create_dt + numtodsinterval({1},'minute') < sysdate and cmd_current_state in ('Initial', ' ')", _table, _overtime);

            return strSQL;
        }
        public string AutoResetCarrierReserveState(string _table)
        {
            //string _table = "workinprocess_sch";

            string strSQL = string.Format(@"update carrier_transfer set reserve=0, lastmodify_dt=sysdate, modify_dt=sysdate where  carrier_id in (select carrier_id from (select a.carrier_id, case when b.cmd_id is null then 'NA' else b.cmd_id end cmdstate from carrier_transfer a left join {0} b on b.carrierid = a.carrier_id where a.carrier_state='ONLINE' and a.reserve = 1 ) where cmdstate='NA')", _table);

            return strSQL;
        }
        public string GetQTimeLot(string _table, string _lotid)
        {
            //230417V1.0 增加條件！此lot 不可為剛下機台仍處於HOLD狀態的料
            //string strSQL = string.Format(@"select a.lotid, a.stage as stage1, a.state, b.stage as stage2 from {0} a 
            //                    left join lot_info b on b.lotid = a.lotid 
            //                    where a.lotid='{1}' and b.stage=a.stage and b.state not in ('HOLD')", _table, _lotid);
            string strSQL = "";
            if (_lotid.Trim().Equals(""))
            {
                strSQL = string.Format(@"select a.lotid, a.stage as stage1, b.stage as stage2, b.QTime, b.QTime_remarks from lot_info a 
                                left join {0} b on b.lotid = a.lotid 
                                where a.lotid='{1}'", _table, _lotid);
            }

            return strSQL;
        }
        public string CheckLookupTable(string _table, string _equip, string _lotid)
        {
            //231030V1.0 增加Lookup Table for Check the equip can do this product logic
            string strSQL = string.Format(@"select equip2 from {0} where lotid='{1}' and equip2 like '%{2}%'", _table, _lotid, _equip);

            return strSQL;
        }
        public string EnableEqpipPort(string _portID, Boolean _enabled)
        {
            string tmpSet = "";
            string strSQL = "";

            if (!_portID.Equals(""))
            {
                strSQL = string.Format("where port_id = '{0}'", _portID);

                if (_enabled)
                    tmpSet = string.Format("set port_state=0, Disable=1, failedtime=0");
                else
                    tmpSet = string.Format("set port_state=0, Disable=0");

                strSQL = string.Format(@"update eqp_port_set {0} {1}
                                            ", tmpSet, strSQL);
            }

            return strSQL;
        }
        //value.CurrentDateTime, value.Unit, value.Zone
        public string QueryHistoryCommandsByCommandID(string _commandID)
        {
            string strSQL = string.Format(@"select * from his_commands where ""CommandID"" = '{0}'", _commandID);

            return strSQL;
        }
        //value.CurrentDateTime, value.Unit, value.Zone
        public string UpdateAlarmCodeByCommandID(string _alarmCode, string _commandID)
        {
            string strSQL = string.Format(@"update his_commands set ""AlarmCode""={0} where ""CommandID"" = '{1}'", _alarmCode, _commandID);

            return strSQL;
        }
        public string QueryLookupTable(string _table, string _lotid)
        {
            string strWhere = "where lotid {0}";
            string tmpLots = "";

            if (_lotid.IndexOf(',') > 0)
            {
                string[] lstLots = _lotid.Split(',');
                foreach (string aLot in lstLots)
                {
                    if (tmpLots.Equals(""))
                        tmpLots = string.Format("'{0}'", aLot.Trim());
                    else
                        tmpLots = string.Format("{0},'{1}'", tmpLots, aLot);
                }

                strWhere = string.Format(strWhere, string.Format("in ({0})", tmpLots));
            }
            else
            {
                strWhere = string.Format(strWhere, string.Format("= '{0}'", _lotid));
            }
            //查詢Lookup Table是否有設定
            string strSQL = string.Format(@"select * from {0} {1}", _table, strWhere);

            return strSQL;
        }
        //value.CurrentDateTime, value.Unit, value.Zone
        public string CheckCarrierTypeOfQTime(string _equipid, string _stage, string _carrierType)
        {
            string strSQL = string.Format(@"select * from workgroup_set a left join eqp_status b on b.workgroup=a.workgroup where b.equipid='{0}' and a.stage='{1}' and a.NoQTimeCarrierType like '%{2}%'", _equipid, _stage, _carrierType);

            return strSQL;
        }
        public string SelectAvailableCarrierByCarrierTypeLocate(string _carrierType, string _locate, bool isFull)
        {
            string strSQL = "";
            if (isFull)
            {
#if DEBUG
                strSQL = @"select * from (select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, case when d.total_qty is null then 0 else d.total_qty end as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType, d.sch_seq, nvl(d.qtime, 0) as qtime from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                left join LOT_INFO d on d.lotid=c.lot_id
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=0 and d.state not in ('HOLD')
                                    and a.location_type in ('ERACK','STOCKER') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '" + _carrierType.Trim() + "') and c.lot_id is not null ) carr left join rack rack on rack.\"erackID\"=carr.locate where(rack.\"groupID\" = '" + _locate.Trim() + "' or rack.\"erackID\" = '" + _locate.Trim() + "') order by sch_seq, Qtime desc";
#else
                strSQL = @"select * from (select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, case when d.total_qty is null then 0 else d.total_qty end as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType, d.sch_seq, nvl(d.qtime, 0) as qtime from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                left join LOT_INFO d on d.lotid=c.lot_id
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=0 and d.state not in ('HOLD')
                                    and a.location_type in ('ERACK','STOCKER') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '" + _carrierType.Trim() + "') and c.lot_id is not null ) carr left join rack rack on rack.\"erackID\"=carr.locate where(rack.\"groupID\" = '" + _locate.Trim() + "' or rack.\"erackID\" = '" + _locate.Trim() + "') order by sch_seq, Qtime desc";
#endif
            }
            else
            {
                strSQL = @"select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, 0 as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD', 'SYSHOLD') and a.reserve = 0 and a.uat=0
                                    and a.location_type in ('ERACK','STOCKER') and c.associate_state not in ('Initiated', 'Associated With Lot', 'Error', 'Associated With None') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '" + _carrierType.Trim() + "') and a.locate in (select paramvalue from RTD_DEFAULT_SET where parameter='EmptyERack' and paramtype='" + _carrierType.Trim() + "') and c.associate_state = 'Unknown' and c.lot_id is null";
            }
            return strSQL;
        }
        public string SelectAvailableCarrierForUatByCarrierTypeLocate(string _carrierType, string _locate, bool isFull)
        {
            string strSQL = "";
            if (isFull)
            {
#if DEBUG
                strSQL = @"select * from (select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, case when d.total_qty is null then 0 else d.total_qty end as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType, d.sch_seq, nvl(d.qtime, 0) as qtime from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                left join LOT_INFO d on d.lotid=c.lot_id
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=1 and d.state not in ('HOLD')
                                    and a.location_type in ('ERACK','STOCKER') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '" + _carrierType.Trim() + "') and c.lot_id is not null ) carr left join rack rack on rack.\"erackID\"=carr.locate where(rack.\"groupID\" = '" + _locate.Trim() + "' or rack.\"erackID\" = '" + _locate.Trim() + "') order by sch_seq, Qtime desc";
#else
                strSQL = @"select * from (select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, case when d.total_qty is null then 0 else d.total_qty end as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType, d.sch_seq, nvl(d.qtime, 0) as qtime from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                left join LOT_INFO d on d.lotid=c.lot_id
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=1 and d.state not in ('HOLD')
                                    and a.location_type in ('ERACK','STOCKER') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '" + _carrierType.Trim() + "') and c.lot_id is not null ) carr left join rack rack on rack.\"erackID\"=carr.locate where(rack.\"groupID\" = '" + _locate.Trim() + "' or rack.\"erackID\" = '" + _locate.Trim() + "') order by sch_seq, Qtime desc";
#endif
            }
            else
            {
                strSQL = @"select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, 0 as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=1
                                    and a.location_type in ('ERACK','STOCKER') and c.associate_state not in ('Initiated', 'Associated With Lot', 'Error', 'Associated With None') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '" + _carrierType.Trim() + "') and a.locate in (select paramvalue from RTD_DEFAULT_SET where parameter='EmptyERack' and paramtype='" + _carrierType.Trim() + "') and c.associate_state = 'Unknown' and c.lot_id is null";
            }
            return strSQL;
        }

        public string CheckReserveTimeByEqpID(string _equipID)
        {
            string strSQL = string.Format(@"select * from eqp_reserve_time where effective=1 and expired=0 and to_date(dt_end,'yyyy/mm/dd hh24:mi:ss') > sysdate and sysdate >= to_date(dt_start,'yyyy/mm/dd hh24:mi:ss') and equipid = '{0}'", _equipID);

            return strSQL;
        }

        public string GetUserAccountType(string _userID)
        {
            string strSQL = string.Format(@"select name, username, acc_type from ""user"" where username = '{0}'", _userID);

            return strSQL;
        }
        public string QueryQTimeData(string _adsTable, string _qTimeTable, string _lotid, string _locate)
        {
            string _strWhere = "";
            string strSQL = "";

            if (!_lotid.Trim().Equals(""))
            {
                _strWhere = string.Format("where c.lotid = '{0}'", _lotid);
            }
            else
            {
                if (!_lotid.Trim().Equals(""))
                    _strWhere = string.Format("where a.locate = '{0}' and a.reserve=0 ", _locate);
                else
                {
                    return strSQL;
                }
            }
            //nvl((g.qtime-g.MINALLOWABLETW)/nullif((g.MAXALLOWABLETW-g.MINALLOWABLETW),0), 0) as qtime1
            strSQL = string.Format(@"select carrier_id, lotid, locate, portno, qtime, minallowabletw, maxallowabletw, before3hr, qTime1, qTime2, q_hrs, case when qTime2>=1 then 'Y' else 'N' end as goNow from (
select a.carrier_id, c.lotid, a.locate, a.portno, c1.qtime, nvl(d.minallowabletw, 0) as minallowabletw, nvl(d.maxallowabletw, 0) as maxallowabletw, nvl(d.maxallowabletw-3, 0) as before3hr, round(nvl((c1.qtime-d.minallowabletw)/nullif((d.maxallowabletw-d.minallowabletw), 0), 0), 3) as qTime1,
round(nvl((c1.qtime-d.minallowabletw)/nullif((d.maxallowabletw-3-d.minallowabletw), 0), 0), 3) as qTime2, d.q_hrs from carrier_transfer a
left join carrier_lot_associate b on b.carrier_id=a.carrier_id
left join lot_info c on c.lotid=b.lot_id
left join rtd_ewlb_ads_vw c1 on c1.lotid=c.lotid
left join rtd_ewlb_qtime_vw d on d.lotid=c.lotid and d.stage=c1.stage and c1.pkgfullname=d.processgroup
{0}) order by goNow desc, qtime1 desc", _strWhere);

            return strSQL;
        }
        public string GetAvailableCarrierByLocateOrderbyQTime(string _adsTable, string _qTimeTable, string _carrierType, string _lotid, string _locate, bool isFull, bool _layerFirst, string _workgroup)
        {
            string _strWhere = "";
            string strSQL = "";

            if (isFull)
            {
                if (!_lotid.Trim().Equals(""))
                {
                    _strWhere = string.Format("where carr.lot_id='{0}' ", _lotid);
                }
                else
                {
                    if (!_locate.Trim().Equals(""))
                        _strWhere = string.Format(@"where (rack.""groupID"" = '{0}' or rack.""erackID"" = '{0}') ", _locate);
                    else
                    {
                        return strSQL;
                    }
                }

#if DEBUG
                _adsTable = "rtd_ewlb_ads_vw";
                _qTimeTable = "rtd_ewlb_ew_mds_tw_rule_tbl_vw";

                strSQL = string.Format(@"select carr.carrier_id, carr.carrier_state, carr.locate, carr.portno, carr.enable, carr.location_type,carr.metal_ring, carr.quantity, carr.total_qty,
                                    carr.carrier_type, carr.command_type, carr.tag_type, carr.lot_id, carr.carrier_type as carrierType, carr.sch_seq, carr.qtime, carr.minallowabletw, carr.maxallowabletw, 
Round((carr.qtime-carr.minallowabletw)/nullif((carr.maxallowabletw-carr.minallowabletw), 0), 3) as qTime1,
case when carr.maxallowabletw > 0 then case when carr.maxallowabletw-carr.qtime < 3 then 'Y' else 'N' end else 'N' end as gonow, carr.custdevice, carr.lot_age, carr.lot_priority from 
(select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, case when d.total_qty is null then 0 else d.total_qty end as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType, d.sch_seq, nvl(d.qtime, 0) as qtime, nvl(to_number(f.minallowabletw), 0) as minallowabletw, nvl(to_number(f.maxallowabletw), 0) as maxallowabletw, d.custdevice, d.lot_age, case when d.priority > 70 then d.priority else 0 end as lot_priority from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                left join LOT_INFO d on d.lotid=c.lot_id
                                left join {1} f on f.CUSTOMER=d.customername and f.TOSTAGE=d.stage and f.PROCESSGROUP=d.pkgfullname
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=0 and d.state not in ('HOLD')
                                    and a.location_type in ('ERACK','STK') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '{3}') and c.lot_id is not null ) carr 
                                    left join rack rack on rack.""erackID""=carr.locate {2}
                                    order by gonow desc, lot_priority desc, qtime1 desc, sch_seq", _adsTable, _qTimeTable, _strWhere, _carrierType);
#else

if(_layerFirst)
{
strSQL = string.Format(@"select carr.carrier_id, carr.carrier_state, carr.locate, carr.portno, carr.enable, carr.location_type,carr.metal_ring, carr.quantity, carr.total_qty,
                                    carr.carrier_type, carr.command_type, carr.tag_type, carr.lot_id, carr.carrier_type as carrierType, carr.sch_seq, carr.qtime, carr.minallowabletw, carr.maxallowabletw, 
Round((carr.qtime-carr.minallowabletw)/nullif((carr.maxallowabletw-carr.minallowabletw), 0), 3) as qTime1,
case when carr.maxallowabletw > 0 then case when carr.maxallowabletw-carr.qtime < 3 then 'Y' else 'N' end else 'N' end as gonow, carr.wpriority, carr.custdevice, carr.lot_age, carr.lot_priority from 
(select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, case when d.total_qty is null then 0 else d.total_qty end as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType, d.sch_seq, nvl(d.qtime, 0) as qtime, nvl(to_number(f.minallowabletw), 0) as minallowabletw, nvl(to_number(f.maxallowabletw), 0) as maxallowabletw, nvl(g.priority, 20) as wPriority, d.custdevice, d.lot_age, case when d.priority > 70 then d.priority else 0 end as lot_priority from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                left join LOT_INFO d on d.lotid=c.lot_id
                                left join {1} f on f.CUSTOMER=d.customername and f.TOSTAGE=d.stage and f.PROCESSGROUP=d.pkgfullname
                                left join workgroup_set g on g.workgroup='{4}' and g.stage=d.stage
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=0 and d.state not in ('HOLD')
                                    and a.location_type in ('ERACK','STK') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '{3}') and c.lot_id is not null ) carr 
                                    left join rack rack on rack.""erackID""=carr.locate {2}
                                    order by gonow desc, lot_priority desc, qtime1 desc, wpriority desc, sch_seq", _adsTable, _qTimeTable, _strWhere, _carrierType, _workgroup);
}
else
{
strSQL = string.Format(@"select carr.carrier_id, carr.carrier_state, carr.locate, carr.portno, carr.enable, carr.location_type,carr.metal_ring, carr.quantity, carr.total_qty,
                                    carr.carrier_type, carr.command_type, carr.tag_type, carr.lot_id, carr.carrier_type as carrierType, carr.sch_seq, carr.qtime, carr.minallowabletw, carr.maxallowabletw, 
Round((carr.qtime-carr.minallowabletw)/nullif((carr.maxallowabletw-carr.minallowabletw), 0), 3) as qTime1,
case when carr.maxallowabletw > 0 then case when carr.maxallowabletw-carr.qtime < 3 then 'Y' else 'N' end else 'N' end as gonow, carr.custdevice, carr.lot_age, carr.lot_priority from 
(select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, case when d.total_qty is null then 0 else d.total_qty end as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType, d.sch_seq, nvl(d.qtime, 0) as qtime, nvl(to_number(f.minallowabletw), 0) as minallowabletw, nvl(to_number(f.maxallowabletw), 0) as maxallowabletw, d.custdevice, d.lot_age, case when d.priority > 70 then d.priority else 0 end as lot_priority from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                left join LOT_INFO d on d.lotid=c.lot_id
                                left join {1} f on f.CUSTOMER=d.customername and f.TOSTAGE=d.stage and f.PROCESSGROUP=d.pkgfullname
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=0 and d.state not in ('HOLD')
                                    and a.location_type in ('ERACK','STK') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '{3}') and c.lot_id is not null ) carr 
                                    left join rack rack on rack.""erackID""=carr.locate {2}
                                    order by gonow desc, lot_priority desc, qtime1 desc, sch_seq", _adsTable, _qTimeTable, _strWhere, _carrierType);
}
#endif
            }
            else
            {
                strSQL = @"select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, 0 as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=0
                                    and a.location_type in ('ERACK','STK') and c.associate_state not in ('Initiated', 'Associated With Lot', 'Error', 'Associated With None') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '" + _carrierType.Trim() + ")' and a.locate in (select paramvalue from RTD_DEFAULT_SET where parameter='EmptyERack' and paramtype='" + _carrierType.Trim() + "') and c.associate_state = 'Unknown' and c.lot_id is null";
            }

            return strSQL;
        }
        public string GetAvailableCarrierForUatByLocateOrderbyQTime(string _adsTable, string _qTimeTable, string _carrierType, string _lotid, string _locate, bool isFull, bool _layerFirst, string _workgroup)
        {
            string _strWhere = "";
            string strSQL = "";

            if (isFull)
            {
                if (!_lotid.Trim().Equals(""))
                {
                    _strWhere = string.Format("where carr.lot_id='{0}' ", _lotid);
                }
                else
                {
                    if (!_locate.Trim().Equals(""))
                        _strWhere = string.Format(@"where (rack.""groupID"" = '{0}' or rack.""erackID"" = '{0}') ", _locate);
                    else
                    {
                        return strSQL;
                    }
                }

                //20241028 eotd(carr.lot_id) change to carr.enddate ' 20241104 rollback 

#if DEBUG
                _adsTable = "rtd_ewlb_ads_vw";
                _qTimeTable = "rtd_ewlb_ew_mds_tw_rule_tbl_vw";

                strSQL = string.Format(@"select layer, carr.carrier_id, carr.carrier_state, carr.locate, carr.portno, carr.enable, carr.location_type,carr.metal_ring, carr.quantity, carr.total_qty,
                                    carr.carrier_type, carr.command_type, carr.tag_type, carr.lot_id, carr.carrier_type as carrierType, carr.sch_seq, carr.qtime, carr.minallowabletw, carr.maxallowabletw, 
round(nvl((carr.qtime-carr.minallowabletw)/nullif((carr.maxallowabletw-carr.minallowabletw), 0), 0), 3) as qTime1,
case when carr.maxallowabletw > 0 then case when carr.maxallowabletw-carr.qtime < 3 then 'Y' else 'N' end else 'N' end as gonow, carr.custdevice, carr.lot_age, carr.lot_priority from 
(select substr(d.stage, 0, 3) as layer, a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, case when d.total_qty is null then 0 else d.total_qty end as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType, d.sch_seq, nvl(d.qtime, 0) as qtime, nvl(to_number(f.minallowabletw), 0) as minallowabletw, nvl(to_number(f.maxallowabletw), 0) as maxallowabletw, d.custdevice, d.lot_age, case when d.priority > 70 then d.priority else 0 end as lot_priority, d.enddate from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                left join LOT_INFO d on d.lotid=c.lot_id
                                left join {1} f on f.CUSTOMER=d.customername and f.TOSTAGE=d.stage and f.PROCESSGROUP=d.pkgfullname
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=1 and d.state not in ('HOLD')
                                    and a.location_type in ('ERACK','STOCKER') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '{3}') and c.lot_id is not null ) carr 
                                    left join rack rack on rack.""erackID""=carr.locate {2}
                                    order by lot_priority desc, layer desc, eotd(carr.lot_id), gonow desc, qtime1 desc, sch_seq", _adsTable, _qTimeTable, _strWhere, _carrierType);
#else
if(_layerFirst)
{
strSQL = string.Format(@"select layer, carr.carrier_id, carr.carrier_state, carr.locate, carr.portno, carr.enable, carr.location_type,carr.metal_ring, carr.quantity, carr.total_qty,
                                    carr.carrier_type, carr.command_type, carr.tag_type, carr.lot_id, carr.carrier_type as carrierType, carr.sch_seq, carr.qtime, carr.minallowabletw, carr.maxallowabletw, 
round(nvl((carr.qtime-carr.minallowabletw)/nullif((carr.maxallowabletw-carr.minallowabletw), 0), 0), 3) as qTime1,
case when carr.maxallowabletw > 0 then case when carr.maxallowabletw-carr.qtime < 3 then 'Y' else 'N' end else 'N' end as gonow, carr.wpriority, carr.custdevice, carr.lot_age, carr.lot_priority from 
(select substr(d.stage, 0, 3) as layer, a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, case when d.total_qty is null then 0 else d.total_qty end as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType, d.sch_seq, nvl(d.qtime, 0) as qtime, nvl(to_number(f.minallowabletw), 0) as minallowabletw, nvl(to_number(f.maxallowabletw), 0) as maxallowabletw, nvl(g.priority, 20) as wPriority, d.custdevice, d.lot_age, case when d.priority > 70 then d.priority else 0 end as lot_priority, d.enddate from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                left join LOT_INFO d on d.lotid=c.lot_id
                                left join {1} f on f.CUSTOMER=d.customername and f.TOSTAGE=d.stage and f.PROCESSGROUP=d.pkgfullname
                                left join workgroup_set g on g.workgroup='{4}' and g.stage=d.stage
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=1 and d.state not in ('HOLD')
                                    and a.location_type in ('ERACK','STK') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '{3}') and c.lot_id is not null ) carr 
                                    left join rack rack on rack.""erackID""=carr.locate {2}
                                    order by lot_priority desc, layer desc, eotd(carr.lot_id), gonow desc, qtime1 desc, wpriority desc, sch_seq", _adsTable, _qTimeTable, _strWhere, _carrierType, _workgroup);
}
else
{
strSQL = string.Format(@"select layer, carr.carrier_id, carr.carrier_state, carr.locate, carr.portno, carr.enable, carr.location_type,carr.metal_ring, carr.quantity, carr.total_qty,
                                    carr.carrier_type, carr.command_type, carr.tag_type, carr.lot_id, carr.carrier_type as carrierType, carr.sch_seq, carr.qtime, carr.minallowabletw, carr.maxallowabletw, 
round(nvl((carr.qtime-carr.minallowabletw)/nullif((carr.maxallowabletw-carr.minallowabletw), 0), 0), 3) as qTime1,
case when carr.maxallowabletw > 0 then case when carr.maxallowabletw-carr.qtime < 3 then 'Y' else 'N' end else 'N' end as gonow, carr.custdevice, carr.lot_age, carr.lot_priority from 
(select substr(d.stage, 0, 3) as layer, a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, case when d.total_qty is null then 0 else d.total_qty end as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType, d.sch_seq, nvl(d.qtime, 0) as qtime, nvl(to_number(f.minallowabletw), 0) as minallowabletw, nvl(to_number(f.maxallowabletw), 0) as maxallowabletw, d.custdevice, d.lot_age, case when d.priority > 70 then d.priority else 0 end as lot_priority, d.enddate from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                left join LOT_INFO d on d.lotid=c.lot_id
                                left join {1} f on f.CUSTOMER=d.customername and f.TOSTAGE=d.stage and f.PROCESSGROUP=d.pkgfullname
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=1 and d.state not in ('HOLD')
                                    and a.location_type in ('ERACK','STK') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '{3}') and c.lot_id is not null ) carr 
                                    left join rack rack on rack.""erackID""=carr.locate {2}
                                    order by lot_priority desc, layer desc, eotd(carr.lot_id), gonow desc, qtime1 desc, sch_seq", _adsTable, _qTimeTable, _strWhere, _carrierType);
}
#endif
            }
            else
            {
                strSQL = @"select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, 0 as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=1 
                                    and a.location_type in ('ERACK','STK') and c.associate_state not in ('Initiated', 'Associated With Lot', 'Error', 'Associated With None') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '" + _carrierType.Trim() + "') and a.locate in (select paramvalue from RTD_DEFAULT_SET where parameter='EmptyERack' and paramtype='" + _carrierType.Trim() + "') and c.associate_state = 'Unknown' and c.lot_id is null";
            }

            return strSQL;
        }
        public string QueryQtimeOfOnlineCarrier(string _table, string _lotid)
        {
            string strSQL = "";
            string strWhere = "";

            if (!_lotid.Equals(""))
                strWhere = string.Format(@"and d.lotid = '{0}'", _lotid);

            strSQL = string.Format(@"select a.lot_id, c.stage, c.recipe, d.custdevice as recipe2, d.stage as stage1, b.location_type, d.qtime qtime1, to_number(replace(replace(nvl(to_char(c.qtime), 'NA'), ',', ''),'NA',0)) as qtime, d.pkgfullname as pkgfullname1, nvl(c.pkgfullname, 'NA') as pkgfullname from carrier_lot_associate a 
left join carrier_transfer b on b.carrier_id=a.carrier_id
left join {0} c on c.lotid=a.lot_id 
left join lot_info d on d.lotid=a.lot_id 
where b.location_type in ('ERACK','MR','STOCKER') and c.pkgfullname is not null {1}", _table, strWhere);

            return strSQL;
        }
        public string UpdateQtimeToLotInfo(string _lotid, float _qtime, string _pkgfullname)
        {
            //update lot_info set qtime = 0.222 where lotid =
            //string strSQL = string.Format(@"update lot_info set qtime={0}, pkgfullname='{1}' where lotid = '{2}'", _qtime, _pkgfullname, _lotid);

            string strSet = "set {0}";
            string tmpSet = "";
            string strWhere = "";
            string strSQL = "";

            if (!_lotid.Equals(""))
                strWhere = string.Format(@"where lotid = '{0}'", _lotid);
            else
                return strSQL;

            if (!_qtime.Equals(""))
                tmpSet = string.Format(@"qtime = {0}", _qtime);

            if (!_pkgfullname.Equals(""))
            {
                if (tmpSet.Equals(""))
                {
                    tmpSet = string.Format(@"pkgfullname = '{0}'", _pkgfullname);
                }
                else
                {
                    tmpSet = string.Format(@"{0},  pkgfullname = '{1}'", tmpSet, _pkgfullname);
                }
            }

            if (!tmpSet.Equals(""))
                strSet = string.Format(@"set {0}", tmpSet);
            else
                return strSQL;

            strSQL = string.Format(@"update lot_info {0} {1}", strSet, strWhere);

            return strSQL;
        }
        public string UpdateStageToLotInfo(string _lotid, string _stage, string _pkgfullname)
        {
            //update lot_info set qtime = 0.222 where lotid =
            string strSet = "set {0}";
            string tmpSet = "";
            string strWhere = "";
            string strSQL = "";

            if (!_lotid.Equals(""))
                strWhere = string.Format(@"where lotid = '{0}'", _lotid);
            else
                return strSQL;

            if (!_stage.Equals(""))
                tmpSet = string.Format(@"stage = '{0}'", _stage);

            if (!_pkgfullname.Equals(""))
            {
                if (tmpSet.Equals(""))
                {
                    tmpSet = string.Format(@"pkgfullname = '{0}'", _pkgfullname);
                }
                else
                {
                    tmpSet = string.Format(@"{0},  pkgfullname = '{1}'", tmpSet, _pkgfullname);
                }
            }

            if (!tmpSet.Equals(""))
                strSet = string.Format(@"set {0}", tmpSet);
            else
                return strSQL;

            strSQL = string.Format(@"update lot_info {0} {1}", strSet, strWhere);

            return strSQL;
        }
        public string QueryeRackDisplayBylotid(string _table, string _lotid)
        {
            string strSQL = "";
            string strWhere = "";

            if (!_lotid.Equals(""))
                strWhere = string.Format(@"and lotid = '{0}'", _lotid);
            else
                return strSQL;

            strSQL = string.Format(@"select * from {0} {1}", _table, strWhere);

            return strSQL;
        }
        public string UpdateEotdToLotInfo(string _lotid, string _eotd)
        {
            //update lot_info set qtime = 0.222 where lotid =
            string strSet = "set {0}";
            string tmpSet = "";
            string strWhere = "";
            string strSQL = "";

            if (!_lotid.Equals(""))
                strWhere = string.Format(@"where lotid = '{0}'", _lotid);
            else
                return strSQL;

            if (!_eotd.Equals(""))
                tmpSet = string.Format(@"enddate = '{0}'", _eotd);

            if (!tmpSet.Equals(""))
                strSet = string.Format(@"set {0}", tmpSet);
            else
                return strSQL;

            strSQL = string.Format(@"update lot_info {0} {1}", strSet, strWhere);

            return strSQL;
        }
        public string QueryCarrierByCarrierId(string _carrierId)
        {
            string strSQL = string.Format("select * from carrier_transfer a  where a.carrier_id = '{0}'", _carrierId);
            return strSQL;
        }
        public string UpdateCarrierToUAT(string _carrierId, bool _isuat)
        {
            string strSQL = "";
            if (_isuat)
                strSQL = string.Format("update carrier_transfer set uat=1 where carrier_id = '{0}'", _carrierId);
            else
                strSQL = string.Format("update carrier_transfer set uat=0 where carrier_id = '{0}'", _carrierId);

            return strSQL;
        }

        public string GetLastStageOutErack(string _lotid, string _table, string _eqpworkgroup)
        {
            string strSQL = "";
            string _adstable = "semi_int.rtd_ewlb_ads_vw@semi_int";
            string _mdstable = "rtd_ewlb_ew_mds_tw_rule_tbl_vw";

            _adstable = _table.Equals("") ? _adstable : _table;

            if (!_lotid.Equals(""))
                strSQL = string.Format(@"select a.lotid, a.stage, a.customername, b.fromstage, c.out_erack, c.f_erack, c.uselaststage from lot_info a
left join {0} a1 on a1.lotid=a.lotid
left join {1} b on b.customer=a.customername and b.tostage=a1.stage and b.processgroup=a.pkgfullname
left join workgroup_set c on c.stage=b.fromstage and c.workgroup='{2}'
where a.lotid ='{3}'", _adstable, _mdstable, _eqpworkgroup, _lotid);

            return strSQL;
        }

        public string GetPROCLotInfo()
        {
            string strSQL = "";

            strSQL = string.Format(@"select a.lotid from lot_info a
left join carrier_lot_associate b on b.lot_id=a.lotid
left join carrier_transfer c on c.carrier_id=b.carrier_id
left join workinprocess_sch d on (d.lotid=a.lotid or d.carrierid=c.carrier_id)
where a.rtd_state='PROC' and (d.cmd_current_state not in ('Running','Init') or d.cmd_current_state is null)");

            return strSQL;
        }
        public string SetResetPreTransfer(string _Workgroup, string _Stage, Boolean _set)
        {
            string tmpSet = "";
            string strSQL = "";
            string tmpWhere = "";

            if (!_Workgroup.Equals(""))
            {

                if (_Stage.Equals(""))
                    tmpWhere = string.Format("where workgroup = '{0}'", _Workgroup);
                else if (!_Stage.Equals(""))
                {
                    tmpWhere = string.Format("where workgroup = '{0}' and stage = '{1}'", _Workgroup, _Stage);
                }

                if (_set)
                    tmpSet = string.Format("set PreTransfer = 1");
                else
                    tmpSet = string.Format("set PreTransfer = 0");

                strSQL = string.Format(@"update workgroup_set {0} {1}
                                            ", tmpSet, tmpWhere);
            }

            return strSQL;
        }

        public string GetMidwayPoint(string _table, string _workgroup, string _lotid)
        {
            string strSQL = "";
            string strWhere = "";
            string _adstable = "";

            _adstable = _adstable.Equals("") ? "semi_int.rtd_ewlb_ads_vw@semi_int" : _table;

            strWhere = string.Format(@"where b.location_type in ('ERACK','STK') and z.midway_point is not null and z.enabled=1 and a.lot_id = '{0}'", _lotid);

            strSQL = string.Format(@"select distinct midway_point from carrier_lot_associate a 
left join carrier_transfer b on b.carrier_id=a.carrier_id
left join {0} c on c.lotid=a.lot_id
left join rtd_midways z on z.workgroup='{1}' and z.stage=c.stage and z.current_locate in (
select ""erackID"" as locate from rack where ""erackID""=b.locate union select ""groupID"" as locate from rack where ""erackID""=b.locate) {2}", _adstable, _workgroup, strWhere);

            return strSQL;
        }

        public string QueryMidwaysSet(string _workgroup, string _stage, string _locate)
        {
            string strSQL = "";
            string _columns = "";
            string _cdt = "";
            string _where = "";

            if (!_workgroup.Equals(""))
            {
                if (_cdt.Equals(""))
                    _cdt = string.Format("workgroup='{0}'", _workgroup);
                else
                    _cdt = string.Format("{0} and workgroup='{1}'", _cdt, _workgroup);
            }

            if (!_stage.Equals(""))
            {
                if (_cdt.Equals(""))
                    _cdt = string.Format("stage='{0}'", _stage);
                else
                    _cdt = string.Format("{0} and stage='{1}'", _cdt, _stage);
            }

            if (!_locate.Equals(""))
            {
                if (_cdt.Equals(""))
                    _cdt = string.Format("locate='{0}'", _workgroup);
                else
                    _cdt = string.Format("{0} and locate='{1}'", _cdt, _locate);
            }

            _columns = "workgroup, stage, current_locate, midway_point, create_dt, enabled";

            if (!_cdt.Equals(""))
                _where = string.Format(@"where {0}", _cdt);

            strSQL = string.Format(@"select {0} from rtd_midways {1}", _columns, _where);

            return strSQL;
        }
        public string QueryDataByLot(string _table, string _lotID)
        {
            string strSQL = "";
            string strWhere = "";

            strWhere = string.Format(@"where lotid = '{0}'", _lotID);

            strSQL = string.Format(@"select * from {0} {1}", _table, strWhere);

            return strSQL;
        }
        public string QueryPortInfobyPortID(string _portID)
        {
            string strSQL = "";
            string strWhere = "";

            strWhere = string.Format(@"where port_id = '{0}'", _portID);

            strSQL = string.Format(@"select * from eqp_port_set {0}", strWhere);

            return strSQL;
        }

        public string ChangePortInfobyPortID(string _portID, string _attribute, string _value, string _userid)
        {
            string strSQL = "";
            string strSet = "";
            string strWhere = "";

            if (!_attribute.Equals(""))
            {

                strWhere = string.Format(@"where port_id = '{0}'", _portID);
                switch (_attribute)
                {
                    case "workgroup":
                        strSet = string.Format(@"set {0}='{1}', lastModify_Wg='{2}', lastModify_dt=sysdate", _attribute, _value, _userid);
                        break;
                    case "port_type":
                        strSet = string.Format(@"set {0}='{1}', lastModify_pt='{2}', lastModify_dt=sysdate", _attribute, _value, _userid);
                        break;
                    case "carrier_type":
                        strSet = string.Format(@"set {0}='{1}', lastModify_ct='{2}', lastModify_dt=sysdate", _attribute, _value, _userid);
                        break;
                    case "current_carriertype":
                        strSet = string.Format(@"set {0}='{1}', lastModify_dt=sysdate", _attribute, _value);
                        break;
                    default:
                        break;
                }

                if (!strSet.Equals(""))
                    strSQL = string.Format(@"update eqp_port_set {0} {1}", strSet, strWhere);
            }

            return strSQL;
        }
        public string QueryCarrierInfoByCarrier(string CarrierID)
        {
            string strSQL = string.Format(@"select * from CARRIER_TRANSFER a
                                            left join CARRIER_TYPE_SET b on b.type_key=a.type_key
                                            where a.carrier_id = '{0}'", CarrierID);
            return strSQL;
        }
        public string UpdateFVCStatus(string _equipID, int _status)
        {
            ///fvcstatus: 0. out of service, 1. start loading, 2. equipment processing, 3. start unloading
            string strSQL = "";
            string _strSet = "";
            string _strWhere = "";

            _strSet = String.Format("fvcstatus={0}, lastModify_dt=sysdate", _status);
            _strWhere = String.Format("where equipid = '{0}'", _equipID);

            strSQL = string.Format(@"update EQP_STATUS a set {0} {1}", _strSet, _strWhere);
            return strSQL;
        }
        public string GetAvailableCarrierForFVC(string _adsTable, string _qTimeTable, string _carrierType, List<string> _agvs, bool isFull)
        {
            string _strWhere = "";
            string strSQL = "";
            string _locates = "";
            string _tmp = "";

            try
            {
                foreach (string tmp in _agvs)
                {
                    _tmp = string.Format("(carr.locate in ('{0}') and carr.portno in ({1}))", tmp.Split(':')[0].ToString().Trim(), tmp.Split(':')[1].ToString().Trim());

                    if (_locates.Equals(""))
                        _locates = string.Format("{0}", _tmp);
                    else
                        _locates = string.Format("{0} or {1}", _locates, _tmp);

                    _tmp = "";
                }

                if (isFull)
                {
                    if (!_locates.Trim().Equals(""))
                    {
                        _strWhere = string.Format("where {0} ", _locates);
                    }
                    else
                    {
                        return strSQL;
                    }

#if DEBUG
                    _adsTable = "rtd_ewlb_ads_vw";
                    _qTimeTable = "rtd_ewlb_ew_mds_tw_rule_tbl_vw";

                    strSQL = string.Format(@"select carr.carrier_id, carr.carrier_state, carr.locate, carr.portno, carr.enable, carr.location_type,carr.metal_ring, carr.quantity, carr.total_qty,
                                    carr.carrier_type, carr.command_type, carr.tag_type, carr.lot_id, carr.carrier_type as carrierType, carr.sch_seq, carr.qtime, carr.minallowabletw, carr.maxallowabletw, 
Round((carr.qtime-carr.minallowabletw)/nullif((carr.maxallowabletw-carr.minallowabletw), 0), 3) as qTime1,
case when carr.maxallowabletw > 0 then case when carr.maxallowabletw-carr.qtime < 3 then 'Y' else 'N' end else 'N' end as gonow from 
(select distinct a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, case when d.total_qty is null then 0 else d.total_qty end as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType, d.sch_seq, nvl(d.qtime, 0) as qtime, nvl(to_number(f.minallowabletw), 0) as minallowabletw, nvl(to_number(f.maxallowabletw), 0) as maxallowabletw from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                left join LOT_INFO d on d.lotid=c.lot_id
                                left join {1} f on f.CUSTOMER=d.customername and f.TOSTAGE=d.stage and f.PROCESSGROUP=d.pkgfullname
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=0 and (d.state not in ('HOLD') or d.state is null)
                                    and a.location_type in ('ERACK','STK') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '{3}') ) carr 
                                    left join rack rack on rack.""erackID""=carr.locate {2}
                                    order by carr.lot_id asc, qtime desc, sch_seq", _adsTable, _qTimeTable, _strWhere, _carrierType);
#else
                    strSQL = string.Format(@"select carr.carrier_id, carr.carrier_state, carr.locate, carr.portno, carr.enable, carr.location_type,carr.metal_ring, carr.quantity, carr.total_qty,
                                    carr.carrier_type, carr.command_type, carr.tag_type, carr.lot_id, carr.carrier_type as carrierType, carr.sch_seq, carr.qtime, carr.minallowabletw, carr.maxallowabletw, 
Round((carr.qtime-carr.minallowabletw)/nullif((carr.maxallowabletw-carr.minallowabletw), 0), 3) as qTime1,
case when carr.maxallowabletw > 0 then case when carr.maxallowabletw-carr.qtime < 3 then 'Y' else 'N' end else 'N' end as gonow from 
(select distinct a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, case when d.total_qty is null then 0 else d.total_qty end as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType, d.sch_seq, nvl(d.qtime, 0) as qtime, nvl(to_number(f.minallowabletw), 0) as minallowabletw, nvl(to_number(f.maxallowabletw), 0) as maxallowabletw from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                left join LOT_INFO d on d.lotid=c.lot_id
                                left join {1} f on f.CUSTOMER=d.customername and f.TOSTAGE=d.stage and f.PROCESSGROUP=d.pkgfullname
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=0 and (d.state not in ('HOLD') or d.state is null)
                                    and a.location_type in ('ERACK','STK') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '{3}') ) carr 
                                    left join rack rack on rack.""erackID""=carr.locate {2}
                                    order by  carr.lot_id asc, qtime desc, sch_seq", _adsTable, _qTimeTable, _strWhere, _carrierType);
#endif
                }
                else
                {
                    strSQL = @"select a.carrier_id, a.carrier_state, a.locate, a.portno, a.enable, a.location_type,a.metal_ring, c.quantity, 0 as total_qty,
                                    b.carrier_type, b.command_type, c.tag_type, c.lot_id, c.carrier_type as carrierType from CARRIER_TRANSFER a
                                left join CARRIER_TYPE_SET b on b.type_key = a.type_key
                                left join CARRIER_LOT_ASSOCIATE c on c.carrier_id = a.carrier_id
                                where a.enable=1 and a.carrier_state ='ONLINE' and a.state not in ('HOLD','SYSHOLD') and a.reserve = 0 and a.uat=0
                                    and a.location_type in ('ERACK','STK') and c.associate_state not in ('Initiated', 'Associated With Lot', 'Error', 'Associated With None') and b.carrier_type in ( select carrier_type_key from port_type_asso where carrier_type = '" + _carrierType.Trim() + "') and a.locate in (select paramvalue from RTD_DEFAULT_SET where parameter='EmptyERack' and paramtype='" + _carrierType.Trim() + "') and c.associate_state = 'Unknown' ";
                }
            }
            catch (Exception ex) { }

            return strSQL;
        }
        public string QueryLocateBySector(string _sector)
        {
            string strSQL = string.Format(@"select ""erackID"", sector from rack where instr(sector, '{0}')>0", _sector);
            return strSQL;
        }
        public string QueryFurneceEQP()
        {
            string strSQL = string.Format(@"select a.workgroup, b.equipid from workgroup_set a 
left join eqp_status b on b.workgroup=a.workgroup
where a.isfurnace = 1 and b.fvcstatus not in (1,2) and b.equipid is not null");

            return strSQL;
        }
        public string QueryFurneceOutErack(string _equipid)
        {
            string strSQL = string.Format(@"select a.workgroup, c.carrier_type, a.equipid, b.in_erack, b.out_erack, b.dummy_locate, b.minmumQty, b.maximumQty, 
b.sidewarehouse from eqp_status a 
left join workgroup_set b on b.workgroup=a.workgroup
left join eqp_port_set c on c.equipid=a.equipid and c.port_seq=1
where b.isfurnace = 1 and a.equipid = '{0}'", _equipid);

            return strSQL;
        }
        public string GetAllOfSysHoldCarrier()
        {
            string strSQL = "";

            strSQL = string.Format(@"select a.carrier_id, type_key, carrier_state, locate, portno, enable, location_type, state, lastModify_dt from carrier_transfer a
left join carrier_lot_associate b on b.carrier_id = a.carrier_id
 where state = 'SYSHOLD' and b.lot_id is not null");

            return strSQL;
        }
        public string UpdateTableEQUIP_MATRIX(string EqpId, string StageCode, bool _turnOn)
        {
            string strSQL = "";
            string strSet = "";

            if (_turnOn)
                strSet = "set enable = 1";
            else
                strSet = "set enable = 0";

            strSQL = string.Format("update PROMIS_STAGE_EQUIP_MATRIX {0} where eqpid = '{1}' and stage = '{2}'", strSet, EqpId, StageCode);
            //string strSQL = string.Format("select eqpid from PROMIS_STAGE_EQUIP_MATRIX where eqpid = '{0}' and stage = '{1}'", EqpId, StageCode);

            return strSQL;
        }
        public string UpdateEffectiveSlot(string _lstSlot, string _workgroup)
        {
            string strSQL = "";
            string strSet = "";

            if (!_lstSlot.Equals(""))
                strSet = string.Format("set effectiveslot = '{0}'", _lstSlot);
            else
                return strSQL;

            strSQL = string.Format("update workgroup_set {0} where workgroup = '{1}'", strSet, _workgroup);
            //string strSQL = string.Format("select eqpid from PROMIS_STAGE_EQUIP_MATRIX where eqpid = '{0}' and stage = '{1}'", EqpId, StageCode);

            return strSQL;
        }
        public string QueryEQUIP_MATRIX(string EqpId, string Stage)
        {
            string strSQL = string.Format("select * from PROMIS_STAGE_EQUIP_MATRIX where eqpid = '{0}' and stage = '{1}'", EqpId, Stage);
            return strSQL;
        }
        public string CheckCarrierNumberforVFC(string _erackID, string _lstPortNo)
        {
            string strSQL = "";
            strSQL = string.Format("select * from carrier_transfer where enable=1 and locate = '{0}' and portno in ({1})", _erackID, _lstPortNo);
            return strSQL;
        }
        public string UpdateCarrierTypeForEQPort(string _portID, string _carrierType)
        {
            string strSQL = "";
            string strSet = "";

            if (!_carrierType.Equals(""))
                strSet = string.Format("set carrier_type = '{0}'", _carrierType);
            else
                return strSQL;

            strSQL = string.Format("update eqp_port_set {0} where port_id = '{1}'", strSet, _portID);
            //string strSQL = string.Format("select eqpid from PROMIS_STAGE_EQUIP_MATRIX where eqpid = '{0}' and stage = '{1}'", EqpId, StageCode);

            return strSQL;
        }
        public string QueryCarrierType()
        {
            string strSQL = "";
            strSQL = string.Format("select distinct carrier_type, carrier_typedesc from carrier_type_set");
            return strSQL;
        }
        public string UpdateRecipeToLotInfo(string _lotid, string _recipe)
        {
            //update lot_info set custdevice = '' where lotid =
            string strSet = "set {0}";
            string tmpSet = "";
            string strWhere = "";
            string strSQL = "";

            if (!_lotid.Equals(""))
                strWhere = string.Format(@"where lotid = '{0}'", _lotid);
            else
                return strSQL;

            if (!_recipe.Equals(""))
                tmpSet = string.Format(@"custdevice = '{0}'", _recipe);

            if (!tmpSet.Equals(""))
                strSet = string.Format(@"set {0}", tmpSet);
            else
                return strSQL;

            strSQL = string.Format(@"update lot_info {0} {1}", strSet, strWhere);

            return strSQL;
        }

        public string QueryRecipeSetting(string _equipid)
        {
            string strSQL = "";
            strSQL = string.Format(@"select * from (
select eqpid, recipe_group_1 as recipeID, 'recipe_g1' as recipe_group from ewlb_coat_dev_critical_recipe_list_vw where eqpid = '{0}'
union select eqpid, recipe_group_2 as recipeID, 'recipe_g2' as recipe_group from ewlb_coat_dev_critical_recipe_list_vw where eqpid = '{0}'
union select eqpid, recipe_group_3 as recipeID, 'recipe_g3' as recipe_group from ewlb_coat_dev_critical_recipe_list_vw where eqpid = '{0}'
union select eqpid, recipe_group_4 as recipeID, 'recipe_g4' as recipe_group from ewlb_coat_dev_critical_recipe_list_vw where eqpid = '{0}'
) recipeSet where recipeSet.recipeID is not null", _equipid);
            return strSQL;
        }

        public string QueryCurrentEquipRecipe(string _equipid)
        {
            string strSQL = "";
            strSQL = string.Format(@"select * from ewlb_coat_dev_running_recipe_vw where eqpid='{0}'", _equipid);
            return strSQL;
        }
        public string QueryCarrierOnRack(string _workgroup, string _equip)
        {
            string strSQL = string.Format(@"select d.lot_id, d.carrier_id from (select b.lot_id, a.carrier_id from carrier_transfer a left join carrier_lot_associate b on b.carrier_id = a.carrier_id where a.carrier_state = 'ONLINE' and a.location_type in ('ERACK') and a.locate in (select ""erackID"" from rack where ""groupID"" in (select in_erack from workgroup_set where workgroup = '{0}')) union select b.lot_id, a.carrier_id from carrier_transfer a left join carrier_lot_associate b on b.carrier_id = a.carrier_id where a.carrier_state = 'ONLINE' and a.location_type in ('STK') and a.locate like (select ""erackID"" || '%' from rack where ""erackID"" in (select in_erack from workgroup_set where workgroup = '{0}'))) d left join lot_info c on c.lotid = d.lot_id where instr(c.equiplist, '{1}') <= 0", _workgroup, _equip);

            return strSQL;
        }
        public string UnbindLot(CarrierLotAssociate _carrierLotAsso)
        {
            string strSQL = string.Format(@"update carrier_lot_associate set associate_state='Unknown', change_state_time=sysdate, lot_id='', 
                                         quantity=0, change_station='SyncEwlbCarrier', change_station_type='Sync', update_by='RTD', update_time=to_date('{0}', 'yyyy/MM/dd HH24:mi:ss'), new_bind=0,
                                         last_associate_state='{1}', last_lot_id='{2}'
                                         where carrier_id='{3}'", _carrierLotAsso.UpdateTime, _carrierLotAsso.AssociateState, _carrierLotAsso.LotID, _carrierLotAsso.CarrierID);
            return strSQL;
        }
        public string QueryLCASInfoByLotID(string _lotID)
        {
            string strSQL = string.Format(@"SELECT * from carrier_lot_associate where lot_id = '{0}'", _lotID);

            return strSQL;
        }
        public string QueryTransferListForSideWH(string _lotTable)
        {
            string strSQL = "";
            //actlinfo_lotid_vw
            //rtd_ewlb_ew_mds_tw_rule_tbl_vw , Q-Time table
            //order condition: gonow desc, qtime1 desc, priority asc, lot_age desc
            //Round((carr.qtime-carr.minallowabletw)/nullif((carr.maxallowabletw-carr.minallowabletw), 0), 3) as qTime1
            //Round((g.qtime - g.MINALLOWABLETW) / nullif((g.MAXALLOWABLETW - g.MINALLOWABLETW), 0) , 3) as qtime1
            //select nvl(3-2/nullif(1-0,0),0) from dual;
            //nvl((g.qtime-g.MINALLOWABLETW)/nullif((g.MAXALLOWABLETW-g.MINALLOWABLETW),0), 0) as qtime1

            strSQL = string.Format(@"select layer, g.carrier_id, g.command_type as carrierType, g.locate, g.lot_id, g.lotstage, g.stg1, g.stg2, g.stage, g.in_erack, g.workgroup, g.wgpriority, g.isFurnace, g.priority as lot_priority, g.lot_age, g.qtime, g.minallowabletw, g.maxallowabletw, round(nvl((g.qtime-g.MINALLOWABLETW)/nullif((g.MAXALLOWABLETW-g.MINALLOWABLETW),0), 0), 3) as qtime1,
case when g.maxallowabletw-g.qtime<=3 then 'Y' else 'N' end gonow, g.Sidewarehouse from (
select distinct substr(c1.stage, 0, 3) as layer, a.carrier_id, a.locate, b1.command_type, b.lot_id, c.stage as lotstage, e.stage stg1,e1.stage stg2, case when nvl(e.stage, 'NA') <> 'NA' then e.stage else e1.stage end stage, 
case when nvl(e.stage, 'NA') <> 'NA' then e.in_erack else e1.in_erack end in_erack, d.workgroup,
case when nvl(e.SideWarehouse, 'NA') <> 'NA' then e.SideWarehouse else e1.SideWarehouse end SideWarehouse,
case when nvl(e.preparecarrierForSideWH, 0) <> 0 then e.preparecarrierForSideWH else e1.preparecarrierForSideWH end preparecarrierForSideWH,
c1.priority, c1.lot_age, nvl(c1.qtime, 0) as qtime, nvl(f.MINALLOWABLETW, 0) MINALLOWABLETW, nvl(f.MAXALLOWABLETW, 0 ) MAXALLOWABLETW,
e1.priority as wgpriority, 
e1.isFurnace, c1.enddate, c1.sch_seq 
from carrier_transfer a
left join carrier_lot_associate b on b.carrier_id=a.carrier_id
left join carrier_type_set b1 on b1.type_key=a.type_key
left join lot_info c1 on c1.lotid=b.lot_id
left join {0} c on c.lotid=b.lot_id
left join (select distinct m.stage, eqp.workgroup, eqp.manualmode, eqp.fvcstatus from promis_stage_equip_matrix m 
left join eqp_status eqp on eqp.equipid=m.eqpid) d on d.stage=c.stage
left join workgroup_set e on e.workgroup=d.workgroup and e.stage=c.stage and e.swsideWH=1
left join workgroup_set e1 on e1.workgroup=d.workgroup and e1.stage='DEFAULT' and e1.swsideWH=1
left join rtd_ewlb_ew_mds_tw_rule_tbl_vw f on f.CUSTOMER=c.customername and f.TOSTAGE=c.stage and f.PROCESSGROUP=c.pkgfullname
where a.enable=1 and a.carrier_state='ONLINE' and a.locate is not null and a.uat=0
and a.location_type in ('ERACK','STK') 
and a.reserve=0 and a.state not in ('HOLD', 'SYSHOLD')
and d.manualmode=0
and d.fvcstatus=0
and c.lotid is not null and c.state in ('WAIT')
and (e.SideWarehouse is not null or e1.SideWarehouse is not null)) g
where g.locate not in (select ""erackID"" from rack where ""erackID"" = g.SideWarehouse
union select ""erackID"" from rack where ""groupID"" = g.SideWarehouse
union select ""erackID"" from rack where inStr(sector, g.SideWarehouse)>0)
order by g.workgroup, g.wgpriority, layer desc, eotd(g.lot_id) asc, gonow desc, qtime1 desc, priority desc, g.sch_seq asc", _lotTable);
//20241028 eotd(g.lot_id) change to g.enddate //20241104 roll back
//20241104 pretransfer condition lot_age desc change to sch_seq asc

            return strSQL;
        }
        public string QueryTransferListUIForSideWH(string _lotTable)
        {
            string strSQL = "";
            //actlinfo_lotid_vw
            //rtd_ewlb_ew_mds_tw_rule_tbl_vw , Q-Time table
            //order condition: gonow desc, qtime1 desc, priority asc, lot_age desc
            //Round((carr.qtime-carr.minallowabletw)/nullif((carr.maxallowabletw-carr.minallowabletw), 0), 3) as qTime1
            //Round((g.qtime - g.MINALLOWABLETW) / nullif((g.MAXALLOWABLETW - g.MINALLOWABLETW), 0) , 3) as qtime1
            //select nvl(3-2/nullif(1-0,0),0) from dual;
            //nvl((g.qtime-g.MINALLOWABLETW)/nullif((g.MAXALLOWABLETW-g.MINALLOWABLETW),0), 0) as qtime1

            strSQL = string.Format(@"select layer, g.carrier_id, g.command_type as carrierType, g.locate, g.lot_id, g.stg1, g.stg2, g.stage, g.in_erack, g.workgroup, g.priority as lot_priority, g.lot_age, g.qtime, g.minallowabletw, g.maxallowabletw, round(nvl((g.qtime-g.MINALLOWABLETW)/nullif((g.MAXALLOWABLETW-g.MINALLOWABLETW),0), 0), 3) as qtime1,
case when g.maxallowabletw-g.qtime<=3 then 'Y' else 'N' end gonow, g.Sidewarehouse from (
select distinct substr(c1.stage, 0, 3) as layer, a.carrier_id, a.locate, b1.command_type, b.lot_id, e.stage stg1,e1.stage stg2, case when nvl(e.stage, 'NA') <> 'NA' then e.stage else e1.stage end stage, 
case when nvl(e.stage, 'NA') <> 'NA' then e.in_erack else e1.in_erack end in_erack, d.workgroup,
case when nvl(e.SideWarehouse, 'NA') <> 'NA' then e.SideWarehouse else e1.SideWarehouse end SideWarehouse,
case when nvl(e.preparecarrierForSideWH, 0) <> 0 then e.preparecarrierForSideWH else e1.preparecarrierForSideWH end preparecarrierForSideWH,
c1.priority, c1.lot_age, nvl(c1.qtime, 0) as qtime, nvl(f.MINALLOWABLETW, 0) MINALLOWABLETW, nvl(f.MAXALLOWABLETW, 0 ) MAXALLOWABLETW, 
c1.enddate, c1.sch_seq 
from carrier_transfer a
left join carrier_lot_associate b on b.carrier_id=a.carrier_id
left join carrier_type_set b1 on b1.type_key=a.type_key
left join lot_info c1 on c1.lotid=b.lot_id
left join {0} c on c.lotid=b.lot_id
left join (select distinct m.stage, eqp.workgroup from promis_stage_equip_matrix m 
left join eqp_status eqp on eqp.equipid=m.eqpid) d on d.stage=c.stage
left join workgroup_set e on e.workgroup=d.workgroup and e.stage=c.stage
left join workgroup_set e1 on e1.workgroup=d.workgroup and e1.stage='DEFAULT'
left join rtd_ewlb_ew_mds_tw_rule_tbl_vw f on f.CUSTOMER=c.customername and f.TOSTAGE=c.stage and f.PROCESSGROUP=c.pkgfullname
where a.enable=1 and a.carrier_state='ONLINE' and a.locate is not null and a.uat=0
and a.location_type in ('ERACK','STK') 
and a.reserve=0 and a.state not in ('HOLD', 'SYSHOLD')
and c.lotid is not null and c.state in ('WAIT')
and (e.swsideWH=1 or e1.swsideWH=1) and (e.SideWarehouse is not null or e1.SideWarehouse is not null)) g
where g.locate not in (select ""erackID"" from rack where ""erackID"" = g.SideWarehouse
union select ""erackID"" from rack where ""groupID"" = g.SideWarehouse
union select ""erackID"" from rack where inStr(sector, g.SideWarehouse)>0)
order by layer desc, eotd(g.lot_id) asc, gonow desc, qtime1 desc, priority desc, g.sch_seq asc", _lotTable);
            ///20241028 eotd(g.lot_id) change to g.enddate  //20241104 rollback
            //20241104 pretransfer condition lot_age desc change to sch_seq asc

            return strSQL;
        }
        public string CalculateLoadportQtyByStage(string _workgroup, string _stage, string _lotstage)
        {
            string strSQL = "";
            string strStage = "";

            if (_stage.Equals("DEFAULT"))
                strStage = _lotstage;
            else
                strStage = _stage;

            strSQL = string.Format(@"select d.stage, count(port_id) as totalportqty from (
select a.stage, c.port_id from promis_stage_equip_matrix a
left join eqp_status b on b.equipid=a.eqpid
left join eqp_port_set c on c.equipid=a.eqpid
where a.stage='{0}' and b.workgroup='{1}') d group by d.stage", strStage, _workgroup);

            return strSQL;
        }
        public string CalculateProcessQtyByStage(string _workgroup, string _stage, string _lotstage)
        {
            string strSQL = "";
            string strStage = "";

            if (_stage.Equals("DEFAULT"))
                strStage = _lotstage;
            else
                strStage = _stage;

            strSQL = string.Format(@"select stage, count(target) as processQty from (
select a.stage, c.port_id as target from promis_stage_equip_matrix a
left join eqp_status b on b.equipid=a.eqpid
left join eqp_port_set c on c.equipid=a.eqpid
where a.stage='{0}' and b.workgroup='{1}'
union 
select '{0}' as stage, b.equipid as target from promis_stage_equip_matrix a
left join eqp_status b on b.equipid=a.eqpid
left join eqp_port_set c on c.equipid=a.eqpid
where a.stage='{0}' and b.workgroup='{1}'
) d group by d.stage", strStage, _workgroup);

            return strSQL;
        }
        public string CheckLocateofSideWh(string _locate, string _sideWh)
        {
            string strSQL = string.Format(@"select * from (
select ""erackID"" from rack where ""groupID"" = '{0}'
 union select ""erackID"" from rack where ""erackID"" = '{0}'
 union select ""erackID"" from rack where ""groupID"" like '%{0}|%' or ""groupID"" like '%|{0}%'
 union select ""erackID"" from rack where sector like '%{0}%')
 where ""erackID"" = '{1}'", _sideWh, _locate);
            return strSQL;
        }
        public string CleanLotInfo(int _iDays)
        {
            string strSQL = string.Format(@"update lot_info set rtd_state = 'DELETED' where to_char(lastModify_dt, 'yyyy/MM/dd HH') < to_char(sysdate-{0}, 'yyyy/MM/dd HH') and rtd_state not in ('DELETED','COMPLETED')", _iDays);

            return strSQL;
        }
        public string GetEqpInfoByWorkgroupStage(string _workgroup, string _stage, string _lotstage)
        {
            string strSQL = "";
            string strStage = "";

            if(_stage.Equals("DEFAULT"))
                strStage = string.Format("and b.stage = '{0}'", _lotstage);
            else
                strStage = string.Format("and b.stage = '{0}'", _stage);

            strSQL = string.Format(@"select distinct * from eqp_status a 
left join promis_stage_equip_matrix b on b.eqpid=a.equipid
where a.workgroup = '{0}' {1}", _workgroup, strStage);

            return strSQL;
        }
        public string GetCarrierByLocate(string _locate)
        {
            string strSQL = string.Format(@"select * from CARRIER_TRANSFER where locate = '{0}'", _locate);
            return strSQL;
        }
        public string ResetFurnaceState(string _equipid)
        {
            string strSQL = string.Format(@"update eqp_status set fvcstatus=0 where equipid = '{0}'", _equipid);
            return strSQL;
        }
        public string ResetWorkgroupforFurnace(string _equipid)
        {
            string strSQL = string.Format(@"update workgroup_set set modify_dt=sysdate, lastmodify_dt=sysdate, effectiveslot='' where workgroup in 
(select workgroup from eqp_status where equipid = '{0}')", _equipid);
            return strSQL;
        }
        public string QueryIslockPortId()
        {
            string strSQL = string.Format(@"select * from eqp_port_set where isLock = 1 and port_state not in (1)");
            return strSQL;
        }
        public string QueryEqpStatusNotSame(string _table)
        {
            string strSQL = string.Format(@"select a.equipid, a.machine_state, a.curr_status, a.down_state, b.machine_state as machine_state_rts, b.curr_status as curr_status_rts, b.down_state as down_state_rts, a.modify_dt, a.lastModify_dt from eqp_status a
left join {0} b on b.equipid=a.equipid
where a.machine_state <> b.machine_state or a.curr_status <> b.curr_status or a.down_state <> b.down_state", _table);
            return strSQL;
        }
        public string UpdateEquipMachineStatus(string _machine, string _current, string _down, string _equipid)
        {
            string strSQL = "";
            strSQL = string.Format(@"update eqp_status set machine_state = '{0}', curr_status = '{1}', down_state = '{2}', lastmodify_dt = sysdate where equipid = '{3}'", _machine.Replace(" ", ""), _current.Replace(" ", ""), _down.Replace(" ", ""), _equipid);
            return strSQL;
        }
        public string QueryStageCtrlListByPortNo(string _portID)
        {
            string strSQL = "";
            strSQL = string.Format(@"select portid, stage from eqp_port_detail where portid = '{0}' and enabled = 1", _portID);
            return strSQL;
        }
        public string QueryRTDAlarmAct(string _portID)
        {
            string strSQL = "";
            strSQL = string.Format(@"select a.alarmcode, a.equipid, a.portid, a.actiontype, a.retrytime, case when c.failedtime is null then 0 else c.failedtime end as failedtime  from rtd_alarm_act a
left join eqp_port_set b on b.port_id = a.portid
left join port_alarm_statis c on c.portid=b.port_id and c.alarmcode=a.alarmcode
where a.enabled =1 and a.portid = '{0}'", _portID);
            return strSQL;
        }
        public string QueryRTDAlarmStatisByCode(string _portID, string _alarmCode)
        {
            string strSQL = "";
            strSQL = string.Format(@"select a.alarmcode, a.equipid, a.portid, a.actiontype, a.retrytime, case when c.failedtime is null then -1 else c.failedtime end as failedtime  from rtd_alarm_act a
left join eqp_port_set b on b.port_id = a.portid
left join port_alarm_statis c on c.portid=b.port_id and c.alarmcode=a.alarmcode
where a.enabled =1 and a.portid = '{0}' and a.alarmcode = '{1}'", _portID, _alarmCode);
            return strSQL;
        }
        public string CleanPortAlarmStatis(string _portID)
        {
            string strSQL = "";
            strSQL = string.Format(@"delete port_alarm_statis where portid = '{0}'", _portID);
            return strSQL;
        }
        public string UpdateRetryTime(string _portID, string _alarmCode, int _failedTime)
        {
            string strSQL = "";
            strSQL = string.Format(@"update port_alarm_statis set failedtime = {0} where portid = '{1}' and alarmcode='{2}'", _failedTime, _portID, _alarmCode);
            return strSQL;
        }
        public string InsertPortAlarmStatis(string _portID, string _alarmCode, int _failedTime)
        {
            string strSQL = "";
            strSQL = string.Format(@"insert into port_alarm_statis (portid, alarmcode, failedtime) values ('{0}','{1}','{2}')", _portID, _alarmCode, _failedTime);
            return strSQL;
        }
        public string QueryPortAlarmStatis(string _portID, string _alarmCode)
        {
            string strSQL = "";
            strSQL = string.Format(@"select * from port_alarm_statis where portid = '{0}' and alarmcode='{1}' ", _portID, _alarmCode);
            return strSQL;
        }
        public string QueryRTDAlarms()
        {

            string strSQL = string.Format(@"select * from rtd_alarm where ""new""=1 order by ""last_updated"" desc");

            return strSQL;
        }
        public string QueryExistRTDAlarms(string _args)
        {
            //unitID, code, subcode, commandID
            string[] Args = null;
            string strSQL = "";

            if (!_args.Equals(""))
            {
                Args = _args.Split(",");

                strSQL = string.Format(@"select * from rtd_alarm where ""new"" = 1 and ""unitID"" = '{0}' and ""code"" = {1} and (""subCode"" is null or ""subCode"" = '{2}') and ""commandID"" = '{3}' order by ""createdAt"" desc", Args[0], Args[1], Args[2], Args[3]);
            }
            else
                strSQL = string.Format(@"select * from rtd_alarm where ""commandID"" = '{0}'", "");

            return strSQL;
        }
        public string UpdateRTDAlarms(bool _reset, string _args, string _detail)
        {
            //unitID, code, subcode, commandID
            string[] Args = null;
            string tmpCdt = @"""last_updated"" = sysdate";
            string strSQL = "";

            if (!_args.Equals(""))
            {
                Args = _args.Split(",");
            }

            if (_reset)
            {
                tmpCdt = tmpCdt + @",""new""=0";
            }
            else
            {
                tmpCdt = tmpCdt + string.Format(@",""eventTrigger""='{0}'", Args[4]);
            }

            if (!_detail.Equals(""))
            {
                tmpCdt = tmpCdt + string.Format(@",""detail""='{0}'", _detail);
            }

            strSQL = string.Format(@"update rtd_alarm set {0} where ""new""=1 and ""unitID"" = '{1}' and ""code"" = {2} and (""subCode"" is null or ""subCode"" = '{3}') and ""commandID"" = '{4}'", tmpCdt, Args[0], Args[1], Args[2], Args[3]);

            return strSQL;
        }
        public string GetDispatchingPriority(string _carrierID)
        {

            string strSQL = string.Format(@"select distinct b.lotid, b.stage, c.priority from carrier_lot_associate a
left join lot_info b on b.lotid=a.lot_id
left join workgroup_set c on c.stage=b.stage
where a.carrier_id='{0}'", _carrierID);

            return strSQL;
        }
        public string GetCarrierTypeByPort(string _portID)
        {

            string strSQL = string.Format(@"select distinct a.equipid, a.port_id, b.carrier_type, b.carrier_type_key, c.command_type from eqp_port_set a
left join port_type_asso b on b.carrier_type=a.carrier_type
left join carrier_type_set c on c.type_key=b.carrier_type_key
where a.port_id='{0}'", _portID);

            return strSQL;
        }
        public string SetResetCheckLookupTable(string _Workgroup, string _Stage, Boolean _set)
        {
            string tmpSet = "";
            string strSQL = "";
            string tmpWhere = "";

            if (!_Workgroup.Equals(""))
            {

                if (_Stage.Equals(""))
                    tmpWhere = string.Format("where workgroup = '{0}'", _Workgroup);
                else if (!_Stage.Equals(""))
                {
                    tmpWhere = string.Format("where workgroup = '{0}' and stage = '{1}'", _Workgroup, _Stage);
                }

                if (_set)
                    tmpSet = string.Format("set PreTransfer = 1");
                else
                    tmpSet = string.Format("set PreTransfer = 0");

                strSQL = string.Format(@"update workgroup_set {0} {1}
                                            ", tmpSet, tmpWhere);
            }

            return strSQL;
        }
        public string SetResetParams(string _Workgroup, string _Stage, string _paramsName, Boolean _set)
        {
            string tmpSet = "";
            string strSQL = "";
            string tmpWhere = "";

            if (!_Workgroup.Equals(""))
            {

                if (_Stage.Equals(""))
                    tmpWhere = string.Format("where workgroup = '{0}'", _Workgroup);
                else if (!_Stage.Equals(""))
                {
                    tmpWhere = string.Format("where workgroup = '{0}' and stage = '{1}'", _Workgroup, _Stage);
                }

                if (_set)
                    tmpSet = string.Format("set {0} = 1", _paramsName);
                else
                    tmpSet = string.Format("set {0} = 0", _paramsName);

                strSQL = string.Format(@"update workgroup_set {0} {1}
                                            ", tmpSet, tmpWhere);
            }

            return strSQL;
        }
        public string GetHisWorkinprocessSchByCommand(string command)
        {
            //string _table = "workinprocess_sch";

            string strSQL = string.Format(@"select * from workinprocess_sch_his where cmd_Id = '{0}' order by lastModify_dt desc", command);
            return strSQL;
        }
        public string GetSetofLookupTable(string _workgroup, string _stage)
        {
            //string _table = "workinprocess_sch";
            string _where = "";

            _where = string.Format(@"where workgroup = '{0}' and stage = '{1}'", _workgroup, _stage);

            string strSQL = string.Format(@"select * from workgroup_set {0}", _where);
            return strSQL;
        }
        public string QueryRTDServer(string _server)
        {
            string strSQL = "";
            string strSet = "";
            string strWhere = "";

            if (_server.Equals(""))
                strWhere = string.Format(@"where parameter = 'RTDServer' and paramtype = 'MasterServer'", _server);
            else
                strWhere = string.Format(@"where parameter = 'RTDServer' and paramtype = 'MasterServer' and paramvalue = '{0}'", _server);
            //select * from rtd_default_set where parameter = 'RTDServer' and paramtype = 'MasterServer';
            //select * from rtd_default_set where parameter = 'mcsstate';
            //strWhere = string.Format(@"where parameter = 'RTDServer' and paramtype = 'MasterServer' and paramvalue = '{0}", _server);

            strSQL = string.Format(@"select * from rtd_default_set {0}", strWhere);

            return strSQL;
        }
        public string InsertRTDServer(string _server)
        {
            string strSQL = "";
            string strSet = "";

            strSQL = string.Format(@"insert into rtd_default_set (parameter, paramtype, paramvalue, modifyby, lastmodify_dt, description)
values('RTDServer', 'MasterServer', '{0}', '{1}', sysdate, '{2}')", _server, "RTD", "Defined currentlly RTD Server");

            return strSQL;
        }
        public string UadateRTDServer(string _server)
        {
            string strSQL = "";
            string strSet = "";
            string strWhere = "";


            strSet = string.Format(@"set paramvalue = '{0}'", _server);
            strWhere = string.Format(@"where parameter = 'RTDServer' and paramtype = 'MasterServer'");

            strSQL = string.Format(@"update rtd_default_set {0} {1}", strSet, strWhere);

            return strSQL;
        }
        public string QueryResponseTime(string _server)
        {
            string strSQL = "";
            string strSet = "";
            string strWhere = "";

            //select to_char(lastmodify_dt, 'yyyy/MM/dd HH24:mi:ss') as responseTime from rtd_default_set where parameter = 'ResponseTime' and paramvalue = 'Server1';
            strWhere = string.Format(@"where parameter = 'ResponseTime' and paramvalue = '{0}'", _server);

            strSQL = string.Format(@"select to_char(lastmodify_dt, 'yyyy/MM/dd HH24:mi:ss') as responseTime from rtd_default_set {0}", strWhere);

            return strSQL;
        }
        public string InsertResponseTime(string _server)
        {
            string strSQL = "";
            string strSet = "";

            strSQL = string.Format(@"insert into rtd_default_set (parameter, paramtype, paramvalue, modifyby, lastmodify_dt, description)
values('ResponseTime', 'ResponseTime', '{0}', 'RTD', sysdate, 'RTD server response time')", _server);

            return strSQL;
        }
        public string UadateResponseTime(string _server)
        {
            string strSQL = "";
            string strSet = "";
            string strWhere = "";


            strSet = string.Format(@"set lastmodify_dt = sysdate");
            strWhere = string.Format(@"where parameter = 'ResponseTime' and paramvalue = '{0}'", _server);

            strSQL = string.Format(@"update rtd_default_set {0} {1}", strSet, strWhere);

            return strSQL;
        }
        public string UadateHoldTimes(string _lotid)
        {
            string strSQL = "";
            string strSet = "";
            string strWhere = "";


            strSet = string.Format(@"set hold_times = hold_times + 1");
            strWhere = string.Format(@"where lotid = '{0}'", _lotid);

            strSQL = string.Format(@"update lot_info {0} {1}", strSet, strWhere);

            return strSQL;
        }
        public string CheckRcpConstraint(string _table, string _equip, string _lotid)
        {
            //231030V1.0 增加Lookup Table for Check the equip can do this product logic
            string strSQL = string.Format(@"select rcpconstraint_list from {0} where lotid='{1}' and rcpconstraint_list like '%{2}%'", _table, _lotid, _equip);

            return strSQL;
        }
        public string WriteNewLocationForCarrier(CarrierLocationUpdate _carrierLocation, string _lotid)
        {
            string strSQL = string.Format(@"insert into CarrierLocationHistory (CarrierID, Zone, Location, LocationType, CreatedAt, LotID)
values ('{0}','{1}','{2}','{3}',sysdate, '{4}')", _carrierLocation.CarrierID, _carrierLocation.Zone, _carrierLocation.Location, _carrierLocation.LocationType, _lotid);

            return strSQL;
        }
        public string SetWorkgroupSetByWorkgroupStage(string _Workgroup, string _Stage, string _params, bool _Sw)
        {
            string tmpSet = "";
            string strSQL = "";
            int iPreTransfer = -1;

            if (!_Workgroup.Equals(""))
            {
                strSQL = string.Format("where workgroup = '{0}'", _Workgroup);

                if (!_Stage.Equals(""))
                    strSQL = string.Format("{0} and stage = '{1}'", strSQL, _Stage);

                if (_Sw)
                {
                    tmpSet = string.Format("set {0} = 1", _params);
                }
                else
                {
                    tmpSet = string.Format("set {0} = 0", _params);
                }

                if (!tmpSet.Equals(""))
                    tmpSet = string.Format("{0},Modify_dt = {1},lastModify_dt = {1}", tmpSet, "sysdate");
                else
                    tmpSet = string.Format("lastModify_dt = {0}", tmpSet, "sysdate");
            }
            else
            {
                strSQL = string.Format("where workgroup = 'None'");
                tmpSet = string.Format("lastModify_dt = {0}", tmpSet, "sysdate");
            }

            strSQL = string.Format(@"update workgroup_set {0} {1}
                                            ", tmpSet, strSQL);

            return strSQL;
        }
        public string QueryCommandsTypeByCarrierID(string _carrierID)
        {
            string strSQL = string.Format(@"select distinct a.carrier_id, b.command_type from carrier_transfer a
left join carrier_type_set b on b.type_key=a.type_key
where carrier_id = '{0}'", _carrierID);

            return strSQL;
        }
        public string QueryMaxPriorityByCmdID(string _commandID)
        {
            string strSQL = string.Format(@"select cmd_id, cmd_type,equipid, carrierid, carriertype,source,dest,priority from workinprocess_sch where replace=1 and cmd_id  ='{0}' order by priority desc", _commandID);

            return strSQL;
        }
        public string QueryStageControl()
        {
            string strSQL = string.Format(@"select * from eqp_port_detail");

            return strSQL;
        }
        public string CloneWorkgroupsForWorkgroupSetByWorkgroup(string _newWorkgroup, string _Workgroup)
        {
            string strSQL = string.Format(@"insert into workgroup_set (workgroup,in_erack,out_erack,create_dt,modify_dt,lastmodify_dt,pretransfer,usefailerack,f_erack,stage,qtime_low,qtime_high,checkeqplookuptable,priority,noqtimecarriertype,uselaststage,stagecontroller,rcpconstraint) 
select '{0}',in_erack,out_erack,create_dt,modify_dt,lastmodify_dt,pretransfer,usefailerack,f_erack,stage,qtime_low,qtime_high,checkeqplookuptable,priority,noqtimecarriertype,uselaststage,stagecontroller,rcpconstraint from workgroup_set
where workgroup='{1}'", _newWorkgroup, _Workgroup);
            return strSQL;
        }
        public string CloneStageForWorkgroupSetByWorkgroupStage(string _newStage, string _workgroup, string _stage)
        {
            string strSQL = string.Format(@"insert into workgroup_set (workgroup,in_erack,out_erack,create_dt,modify_dt,lastmodify_dt,pretransfer,usefailerack,f_erack,stage,qtime_low,qtime_high,checkeqplookuptable,priority,noqtimecarriertype,uselaststage,stagecontroller,rcpconstraint) 
select workgroup,in_erack,out_erack,create_dt,modify_dt,lastmodify_dt,pretransfer,usefailerack,f_erack,'{0}',qtime_low,qtime_high,checkeqplookuptable,priority,noqtimecarriertype,uselaststage,stagecontroller,rcpconstraint from workgroup_set
where workgroup='{1}' and stage='{2}'", _newStage, _workgroup, _stage);
            return strSQL;
        }
        public string SetParameterWorkgroupSetByWorkgroupStage(string _Workgroup, string _Stage, string _paramsName, object _paramsValue)
        {
            string tmpSet = "";
            string strSQL = "";
            string tmpType = _paramsValue.GetType().ToString();

            if (!_Workgroup.Equals(""))
            {
                strSQL = string.Format("where workgroup = '{0}'", _Workgroup);

                if (!_Stage.Equals(""))
                    strSQL = string.Format("{0} and stage = '{1}'", strSQL, _Stage);

                if (!_paramsValue.Equals(""))
                {
                    tmpSet = string.Format("set {0}={1}", _paramsName, _paramsValue.GetType().ToString().Equals("System.Int32") ? string.Format("{0}", _paramsValue) : string.Format("'{0}'", _paramsValue));
                }

                if (!tmpSet.Equals(""))
                    tmpSet = string.Format("{0},Modify_dt = {1},lastModify_dt = {1}", tmpSet, "sysdate");
                else
                    tmpSet = string.Format("lastModify_dt = {0}", tmpSet, "sysdate");
            }
            else
            {
                strSQL = string.Format("where workgroup = 'None'");
                tmpSet = string.Format("lastModify_dt = {0}", tmpSet, "sysdate");
            }

            strSQL = string.Format(@"update workgroup_set {0} {1}
                                            ", tmpSet, strSQL);

            return strSQL;
        }
        public string QueryAllMidways(QueryMidways _cond)
        {
            string strSQL = "";
            string tmpSQL = "select * from rtd_midways";
            string strWhere = "";
            string tmpWhere = "";

            if (!_cond.Workgroup.Equals(""))
                tmpWhere = string.Format("where workgroup='{0}'", _cond.Workgroup);
            else
            {
                strSQL = tmpSQL;
                return strSQL;
            }

            if (!_cond.Stage.Equals(""))
            {
                tmpWhere = string.Format("{0} and stage='{1}'", tmpWhere, _cond.Stage);
            }
            else
                tmpWhere = tmpWhere;

            if (!_cond.Midway.Equals(""))
            {
                tmpWhere = string.Format("{0} and midway_point='{1}'", tmpWhere, _cond.Midway);
            }
            else
                tmpWhere = tmpWhere;

            if (!_cond.CurrentLocate.Equals(""))
            {
                tmpWhere = string.Format("{0} and current_locate='{1}'", tmpWhere, _cond.CurrentLocate);
            }
            else
                tmpWhere = tmpWhere;

            strSQL = string.Format(@"{0} {1}", tmpSQL, tmpWhere);

            return strSQL;
        }
        public string QueryMidways(QueryMidways _cond)
        {
            string strSQL = "";
            string tmpSQL = "select * from rtd_midways";
            string strWhere = "";
            string tmpWhere = "";

            if(!_cond.Workgroup.Equals(""))
                tmpWhere = string.Format("where workgroup='{0}' and delmark = 0", _cond.Workgroup);
            else
            {
                strSQL = tmpSQL;
                return strSQL;
            }

            if (!_cond.Stage.Equals(""))
            {
                tmpWhere = string.Format("{0} and stage='{1}'", tmpWhere, _cond.Stage);
            }
            else
                tmpWhere = tmpWhere;

            if (!_cond.Midway.Equals(""))
            {
                tmpWhere = string.Format("{0} and midway_point='{1}'", tmpWhere, _cond.Midway);
            }
            else
                tmpWhere = tmpWhere;

            if (!_cond.CurrentLocate.Equals(""))
            {
                tmpWhere = string.Format("{0} and current_locate='{1}'", tmpWhere, _cond.CurrentLocate);
            }
            else
                tmpWhere = tmpWhere;

            strSQL = string.Format(@"{0} {1}", tmpSQL, tmpWhere);

            return strSQL;
        }
        public string InsertMidways(InsertMidways _cond)
        {
            string strSQL = "";
            string tmpSQL = "";

            tmpSQL = @"insert into rtd_midways (workgroup, stage, midway_point, current_locate, create_dt, enabled, idx)
values ('{0}','{1}','{2}','{3}',sysdate, 0, '{4}')";

            strSQL = string.Format(tmpSQL, _cond.Workgroup, _cond.Stage, _cond.Midway, _cond.CurrentLocate, _cond.Idx);

            return strSQL;
        }
        public string TurnOnMidways(string _idx, bool turnOn)
        {
            string strSQL = "";
            string tmpSQL = "";
            string strSet = "";
            string strWhere = "";

            if(!_idx.Equals(""))
            {
                strWhere = string.Format("where idx = '{0}'", _idx);

                strSet = string.Format("set {0}, modify_dt=sysdate", turnOn.Equals(true) ? "enabled=1" : "enabled=0");

                tmpSQL = @"update rtd_midways {0} {1}";

                strSQL = string.Format(tmpSQL, strSet, strWhere);
            }

            return strSQL;
        }
        public string ResetMidways(string _idx, bool _del)
        {
            string strSQL = "";
            string tmpSQL = "";
            string strSet = "";
            string strWhere = "";

            if (!_idx.Equals(""))
            {
                strWhere = string.Format("where idx = '{0}'", _idx);

                strSet = string.Format("set {0}, enabled=0, modify_dt=sysdate", _del.Equals(true) ? "delmark=1" : "delmark=0");

                tmpSQL = @"update rtd_midways {0} {1}";

                strSQL = string.Format(tmpSQL, strSet, strWhere);
            }

            return strSQL;
        }
        public string QueryStageControlAllList(StageControlList _cond)
        {
            string strSQL = "";
            string tmpSQL = "select * from eqp_port_detail";
            string tmpWhere = "";

            if (!_cond.EQUIPID.Equals(""))
                tmpWhere = string.Format("where EQUIPID='{0}'", _cond.EQUIPID);
            else
            {
                strSQL = tmpSQL;
                return strSQL;
            }

            if (!_cond.PORTID.Equals(""))
            {
                tmpWhere = string.Format("{0} and PORTID='{1}'", tmpWhere, _cond.PORTID);
            }

            if (!_cond.STAGE.Equals(""))
            {
                tmpWhere = string.Format("{0} and STAGE='{1}'", tmpWhere, _cond.STAGE);
            }

            strSQL = string.Format(@"{0} {1}", tmpSQL, tmpWhere);

            return strSQL;
        }
        public string QueryStageControlList(StageControlList _cond)
        {
            string strSQL = "";
            string tmpSQL = "select * from eqp_port_detail";
            string tmpWhere = "";

            if (!_cond.EQUIPID.Equals(""))
                tmpWhere = string.Format("where EQUIPID='{0}' and delmark = 0", _cond.EQUIPID);
            else
            {
                strSQL = tmpSQL;
                return strSQL;
            }

            if (!_cond.PORTID.Equals(""))
            {
                tmpWhere = string.Format("{0} and PORTID='{1}'", tmpWhere, _cond.PORTID);
            }

            if (!_cond.STAGE.Equals(""))
            {
                tmpWhere = string.Format("{0} and STAGE='{1}'", tmpWhere, _cond.STAGE);
            }

            strSQL = string.Format(@"{0} {1}", tmpSQL, tmpWhere);

            return strSQL;
        }
        public string InsertStageControl(StageControlList _cond)
        {
            string strSQL = "";
            string tmpSQL = "";

            tmpSQL = @"insert into eqp_port_detail (equipid, portid, stage, create_dt, enabled)
values ('{0}','{1}','{2}',sysdate, 0)";

            strSQL = string.Format(tmpSQL, _cond.EQUIPID, _cond.PORTID, _cond.STAGE);

            return strSQL;
        }
        public string TurnOnStageControl(TurnOnStageControl _cond, bool turnOn)
        {
            string strSQL = "";
            string tmpSQL = "";
            string strSet = "";
            string tmpWhere = "";
            string strWhere = "";

            if (!_cond.EQUIPID.Equals(""))
            {
                tmpWhere = string.Format("where equipid = '{0}'", _cond.EQUIPID);

                if (!_cond.PORTID.Equals(""))
                {
                    if (!tmpWhere.Equals(""))
                    {
                        tmpWhere = string.Format("{0} and portid = '{1}'", tmpWhere, _cond.PORTID);
                    }
                    else
                        tmpWhere = "";
                }
                else
                    tmpWhere = "";

                if (!_cond.STAGE.Equals(""))
                {
                    if (!tmpWhere.Equals(""))
                    {
                        tmpWhere = string.Format("{0} and stage = '{1}'", tmpWhere, _cond.STAGE);
                    }
                }
            }

            if (!tmpWhere.Equals(""))
            {
                strSet = string.Format("set {0}, modify_dt=sysdate", turnOn.Equals(true) ? "enabled=1" : "enabled=0");

                tmpSQL = @"update eqp_port_detail {0} {1}";

                strSQL = string.Format(tmpSQL, strSet, tmpWhere);
            }

            return strSQL;
        }
        public string ResetStageControl(StageControlList _cond, bool _del)
        {
            string strSQL = "";
            string tmpSQL = "";
            string strSet = "";
            string strWhere = "";

            if (!_cond.Equals(""))
            {
                strWhere = string.Format("where idx = '{0}'", _cond);

                strSet = string.Format("set {0}, enabled=0, modify_dt=sysdate", _del.Equals(true) ? "delmark=1" : "delmark=0");

                tmpSQL = @"update eqp_port_detail {0} {1}";

                strSQL = string.Format(tmpSQL, strSet, strWhere);
            }

            return strSQL;
        }
        public string DeleteStageControl(StageControlList _cond, bool _remove)
        {
            string strSQL = "";
            string tmpSQL = "";
            string strSet = "";
            string tmpWhere = "";
            string strWhere = "";

            if (!_cond.EQUIPID.Equals(""))
            {
                tmpWhere = string.Format("where equipid = '{0}'", _cond.EQUIPID);

                if (!_cond.PORTID.Equals(""))
                {
                    if (!tmpWhere.Equals(""))
                    {
                        tmpWhere = string.Format("{0} and portid = '{1}'", tmpWhere, _cond.PORTID);
                    }
                    else
                        tmpWhere = "";
                }
                else
                    tmpWhere = "";

                if (!_cond.STAGE.Equals(""))
                {
                    if (!tmpWhere.Equals(""))
                    {
                        tmpWhere = string.Format("{0} and stage = '{1}'", tmpWhere, _cond.STAGE);
                    }
                }
            }

            if (!tmpWhere.Equals(""))
            {
                tmpSQL = @"update eqp_port_detail set enabled=0, modify_dt=sysdate, lastmodify_dt=sysdate{0} {1}";

                strSQL = string.Format(tmpSQL, _remove.Equals(true) ? ",delmark=1" : ",delmark=0", tmpWhere);
            }

            return strSQL;
        }
        public string CarrierTransferDTUpdate(string _carrierid, string _updateType)
        {
            string strSQL = "";
            string tmpSQL = "";
            string strSet = "";
            string tmpWhere = "";
            string strWhere = "";

            if(!_carrierid.Equals(""))
            {
                tmpWhere = string.Format("where carrier_id = '{0}'", _carrierid);
            }

            if (_updateType.ToLower().Equals("infoupdate"))
            {
                strSet = string.Format("set info_update_dt = sysdate");
            }
            else if (_updateType.ToLower().Equals("locateupdate"))
            {
                strSet = string.Format("set loc_update_dt = sysdate");
            }

            if (!tmpWhere.Equals(""))
            {
                tmpSQL = @"update carrier_transfer {0} {1}";

                strSQL = string.Format(tmpSQL, strSet, tmpWhere);
            }

            return strSQL;
        }
        public string QueryAllLotOnERack()
        {
            string strSQL = "";

            strSQL = @"select a.carrier_id, c.lotid, c.enddate, a.carrier_state, a.locate, a.portno, a.location_type, a.quantity from carrier_transfer a
left join carrier_lot_associate b on b.carrier_id=a.carrier_id
left join lot_info c on c.lotid=b.lot_id
where a.carrier_state='ONLINE' and lotid is not null";

            return strSQL;
        }
        public string GetNewEotdByLot(string _lotid)
        {
            string strSQL = "";

            strSQL = string.Format("select eotd('{0}') as eotd from dual", _lotid);

            return strSQL;
        }
        public string GetDataFromTableByLot(string _table, string _colname, string _lotid)
        {
            string strSQL;
            string strWhere = "";

            if(!_lotid.Equals(""))
            {
                strWhere = string.Format("where {0}='{1}'", _colname, _lotid);
            }

            strSQL = string.Format("select * from {0} {1}", _table, strWhere);

            return strSQL;
        }
        public string UpdateTurnRatioToLotInfo(string _lotid, string _turnratio)
        {
            //update lot_info set qtime = 0.222 where lotid =
            string strSet = "set {0}";
            string tmpSet = "";
            string strWhere = "";
            string strSQL = "";

            if (!_lotid.Equals(""))
                strWhere = string.Format(@"where lotid = '{0}'", _lotid);
            else
                return strSQL;

            if (!_turnratio.Equals(""))
                tmpSet = string.Format(@"turnratio2 = '{0}'", _turnratio);

            if (!tmpSet.Equals(""))
                strSet = string.Format(@"set {0}", tmpSet);
            else
                return strSQL;

            strSQL = string.Format(@"update lot_info {0} {1}", strSet, strWhere);

            return strSQL;
        }
    }
}
