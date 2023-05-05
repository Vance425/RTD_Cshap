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
    public class SendMCSCommand : BasicController
    {
        private readonly IFunctionService _functionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly List<DBTool> _lstDBSession;
        private readonly DBTool _dbTool;
        private readonly ConcurrentQueue<EventQueue> _eventQueue;

        public SendMCSCommand(List<DBTool> lstDBSession, IConfiguration configuration, ILogger logger, IFunctionService functionService, ConcurrentQueue<EventQueue> eventQueue)
        {
            _dbTool = (DBTool)lstDBSession[0];
            _logger = logger;
            _configuration = configuration;
            _functionService = functionService;
            _eventQueue = eventQueue;
        }

        [HttpPost]
        public APIResult Post([FromBody] FuncNo value)
        {
            APIResult foo = new APIResult();
            IBaseDataService _BaseDataService = new BaseDataService();

            string tmpMsg = "";
            bool bResult = false;
            List<string> args = new();
            DataTable dt = null;
            DataTable dtTemp = null;

            List<string> lstMsg = new();

            string funcName = "";
            EventQueue _eventQ = new EventQueue();

            try
            {
                args = new();

                switch (value.FunctionNo)
                {
                    case "1":
                        try { 
                            args.Add(value.KeyCode);//("EOTD");
                            foo = _functionService.SentCommandtoMCSByModel(_configuration, _logger, "GetDeviceInfo", args);

                            string _portID = "";
                            string _locate = "";
                            int _slotno = 0;
                            string _carrierID = "";
                            string _sql = "";
                            string _carrier = "";

                            CarrierLocationUpdate oclu = new CarrierLocationUpdate();

                            foreach (var key in foo.Data)
                            {
                                _portID = key.Key;
                                _locate = _portID.Split("_LP")[0].ToString();
                                _slotno = int.Parse(_portID.Split("_LP")[1].ToString());
                                _carrierID = key.Value.ToString().Trim();

                                _carrier = "";

                                if(_carrierID.Equals(""))
                                {
                                    //carrierID is empty
                                    _sql = _BaseDataService.GetCarrierByLocate(_locate, _slotno);
                                    dt = _dbTool.GetDataTable(_sql);

                                    if (dt.Rows.Count > 0)
                                    {
                                        _carrier = dt.Rows[0]["carrier_id"].ToString();

                                        oclu = new CarrierLocationUpdate();
                                        oclu.CarrierID = _carrier;
                                        oclu.Location = "";
                                        oclu.LocationType = "";
                                        //do locate update date
                                        funcName = "CarrierLocationUpdate";
                                        _eventQ.EventName = funcName;

                                        _eventQ.EventObject = oclu;
                                        _eventQueue.Enqueue(_eventQ);
                                    }
                                }
                                else
                                {
                                    //carrierID is not empty
                                    _sql = _BaseDataService.SelectTableCarrierTransferByCarrier(_carrierID);
                                    dt = _dbTool.GetDataTable(_sql);

                                    if (dt.Rows.Count > 0)
                                    {
                                        _carrier = dt.Rows[0]["carrier_id"].ToString();

                                        oclu = new CarrierLocationUpdate();
                                        //do locate update date
                                        funcName = "CarrierLocationUpdate";
                                        _eventQ.EventName = funcName;

                                        _eventQ.EventObject = oclu;
                                        _eventQueue.Enqueue(_eventQ);
                                    }
                                }
                            }
                        }
                        catch (Exception ex) { }
                        break;
                    case "2":
                        try {
                            DataTable dtAvaileCarrier = null;
                            string _equip = value.KeyCode;
                            string ineRack = "Test";
                            string _carrierType = "";

                            List<string> _lstTemp = new();
                            string _sector = "";
                            string _lstCarrierID = "";

                            //QueryLocateBySector

                            dtTemp = _dbTool.GetDataTable(_BaseDataService.QueryLocateBySector(ineRack));
                            if (dtTemp.Rows.Count > 0)
                            {
                                int _start = 0;
                                int _Len = 0;
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
                                        _secTemp = _sector.Substring(_start + 1, _Len - _start);
                                    else
                                    {
                                        _secTemp = _sector.Substring(_start + 1);

                                        if (_secTemp.IndexOf("#") > 0)
                                        {
                                            _secTemp = _secTemp.Substring(0, _secTemp.IndexOf("#"));
                                        }
                                    }

                                    _sector = _secTemp;

                                    _lstTemp.Add(string.Format("{0}:{1}", drSector["erackID"].ToString(), _sector));
                                }
                            }

                            dtAvaileCarrier = _functionService.GetAvailableCarrierForFVC(_dbTool, _configuration, _carrierType, _lstTemp, true, "PROD");

                            if (dtAvaileCarrier.Rows.Count > 0)
                            {
                                foreach (DataRow draCarrier in dtAvaileCarrier.Rows)
                                {
                                    try {
                                        if (_lstCarrierID.Equals(""))
                                        {
                                            _lstCarrierID = string.Format("{0}", draCarrier["carrier_id"].ToString());
                                        }
                                        else
                                        {
                                            _lstCarrierID = string.Format("{0},{1}", _lstCarrierID, draCarrier["carrier_id"].ToString());
                                        }
                                    }
                                    catch(Exception ex) { }
                                }
                            }

                            //_lstCarrierID = "12CA0011,12CA0022,12CA0033";
                            int _totalQty = dtAvaileCarrier.Rows.Count;
                            args.Add(_equip);//("Equip") 
                            args.Add(_lstCarrierID);//("_lstCarrierID") 

                            //_totalQty = _lstCarrierID.Split(',').Length;
                            args.Add(_totalQty.ToString());//("_totalQty") 
                            foo = _functionService.SentCommandtoMCSByModel(_configuration, _logger, "Batch", args);

                        }
                        catch (Exception ex) { }
                        break;
                    case "3":
                        try
                        {
                            string _sector = "";
                            List<string> _lstTemp = new();
                            string _secTemp = "";
                            string _inErack = "";
                            string _erack = "";

                            _inErack = value.KeyCode;
                            dt = _dbTool.GetDataTable(_BaseDataService.QueryLocateBySector(_inErack));
                            if (dt.Rows.Count > 0)
                            {
                                int _start = 0;
                                int _Len = 0;
                                int _exist = 0;
                                foreach (DataRow drSector in dt.Rows)
                                {
                                    _erack = drSector["erackID"].ToString();
                                    _sector = drSector["sector"].ToString();
                                    Console.WriteLine(_sector);
                                    _sector = _sector.Replace("\",\"", "\"#\"");
                                    Console.WriteLine(_sector);
                                    _sector = _sector.Replace("\"", "").Replace("{", "").Replace("}", "");
                                    Console.WriteLine(_sector);

                                    _start = _sector.IndexOf(_inErack) + _inErack.Length + 1;
                                    Console.WriteLine(_start);
                                    _Len = _sector.IndexOf("#", _start);

                                    Console.WriteLine(_Len);

                                    if (_Len > 0)
                                        _secTemp = _sector.Substring(_start + 1, _Len - _start);
                                    else
                                    {
                                        _secTemp = _sector.Substring(_start + 1);

                                        if(_secTemp.IndexOf("#") > 0)
                                        {
                                            _secTemp = _secTemp.Substring(0, _secTemp.IndexOf("#"));
                                        }
                                    }

                                    Console.WriteLine(_secTemp);

                                    _sector = _secTemp;

                                    _lstTemp.Add(string.Format("{0}:{1}", drSector["erackID"].ToString(), _sector));
                                }
                            }

                            dt = _functionService.GetAvailableCarrierForFVC(_dbTool, _configuration, "", _lstTemp, true, "PROD");

                            tmpMsg = string.Format("{0} [{1}][{2}]", "Time", "Items", "Message");
                            lstMsg.Add(tmpMsg);

                            tmpMsg = string.Format("{0} [{1}][{2}]", "Time2", "Items", "Message2");
                            lstMsg.Add(tmpMsg);

                            tmpMsg = string.Format("{0} [{1}][{2}]", "Time3", "Items", "Message3");
                            lstMsg.Add(tmpMsg);

                            tmpMsg = string.Format("{0} [{1}][{2}]", "Time4", "Items", "Message4");
                            lstMsg.Add(tmpMsg);

                        }
                        catch (Exception ex) { }
                        break;
                    case "4":
                        try
                        {
                            string _equip = value.KeyCode;
                            foo = _functionService.SentBatchEvent(_dbTool, _configuration, _logger, true, _equip);
                        }
                        catch(Exception ex) { }
                        //SentBatchEvent(DBTool _dbTool, IConfiguration _configuration, ILogger _logger, string _equip)
                        break;
                    default:
                        break;
                }

                //bResult = _functionService.SentTransferCommandtoToMCS(_dbTool, _configuration, _logger, value, out tmpMsg);
            }
            catch(Exception ex) { }

            _logger.Info(string.Format("Info:{0}",tmpMsg));
            _logger.Warn(string.Format("Warning:{0}", tmpMsg));
            _logger.Error(string.Format("Error:{0}", tmpMsg));
            _logger.Debug(string.Format("Debug:{0}", tmpMsg));

            _logger.Debug(string.Format("Debug:{0}", lstMsg.ToString()));

            //string sql = "select * from gyro_lot_carrier_associate";
            //DataSet ds = dbPool.GetDataSet(sql);

            return foo;
        }
        public class FuncNo
        {
            public string FunctionNo { get; set; }
            public string KeyCode { get; set; }
        }
    }
}
