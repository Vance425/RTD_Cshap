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
    public class FurnaceController : BasicController
    {
        private readonly IFunctionService _functionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly DBTool _dbTool;
        private readonly ConcurrentQueue<EventQueue> _eventQueue;
        private readonly List<DBTool> _lstDBSession;

        public FurnaceController(List<DBTool> lstDBSession, IConfiguration configuration, ILogger logger, IFunctionService functionService, ConcurrentQueue<EventQueue> eventQueue)
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

        [HttpPost("ManualTriggerFurnace")]
        public APIResult ManualTriggerFurnace([FromBody] FurnaceIn value)
        {
            APIResult foo = new();
            IBaseDataService _BaseDataService = new BaseDataService();
            EventQueue _eventQ = new EventQueue();
            string funcName = "ManualTriggerFurnace";
            string tmpMsg = "";

            _eventQ.EventName = funcName;
            string _EquipID = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";

            try
            {
                _EquipID = value.EquipID;
                if (_EquipID.Equals(""))
                {
                    foo.Success = false;
                    foo.State = "NG";
                    foo.Message = "_Equip Id can not be empty.";
                    return foo;
                }

                foo = _functionService.SentBatchEvent(_dbTool, _configuration, _logger, true, _EquipID);

                if (foo.Success)
                    _logger.Info(string.Format("Trigger Furnace [{0}] Success. UserID [{1}]", value.EquipID, value.UserID));
                else
                {
                    _logger.Info(string.Format("Trigger Furnace [{0}] Failed. UserID [{1}]", value.EquipID, value.UserID));

                    if(foo.State is null)
                    {
                        foo.Success = false;
                        foo.State = "NG";
                        foo.Message = "AEI no response.";
                        return foo;
                    }
                }
            }
            catch(Exception ex)
            {
                foo.Success = false;
                foo.State = "NG";
                foo.Message = String.Format("Unknow issue. [{0}] Exception: {1}", funcName, ex.Message);
                _logger.Debug(foo.Message);
            }

            return foo;
        }


        [HttpPost("ResetFurnaceFlow")]
        public APIResult ResetFurnaceFlow([FromBody] FurnaceIn value)
        {
            APIResult foo = new();
            List<String> lsEquip = new List<String>();
            string funcName = "ResetFurnaceFlow";
            string tmpMsg = "";
            string strResult = "";
            DataTable dt = null;
            DataTable dt2 = null;
            DataRow[] dr = null;
            string sql = "";
            bool isFurnace = false;
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                //// 查詢資料
                dt = _dbTool.GetDataTable(_BaseDataService.SelectTableEQPStatusInfoByEquipID(value.EquipID));
                dr = dt.Select();
                if (dr.Length <= 0)
                {
                    foo.Success = false;
                    foo.State = "NG";
                    foo.Message = string.Format("The Equipment not exist. userid [{0}].", value.UserID);
                    _logger.Info(foo.Message);
                    return foo;
                }
                else
                {
                    dt2 = _dbTool.GetDataTable(_BaseDataService.SelectWorkgroupSet(value.EquipID));
                    
                    //SelectWorkgroupSet
                    if (dt2.Rows.Count > 0)
                    {
                        isFurnace = dt2.Rows[0]["IsFurnace"] is null ? false : dt2.Rows[0]["IsFurnace"].ToString().Equals("1") ? true: false;

                        if (isFurnace)
                        {
                            sql = _BaseDataService.ResetFurnaceState(value.EquipID);
                            _dbTool.SQLExec(sql, out tmpMsg, true);

                            sql = _BaseDataService.ResetWorkgroupforFurnace(value.EquipID);
                            _dbTool.SQLExec(sql, out tmpMsg, true);

                            if (tmpMsg.Equals(""))
                            {
                                foo.Success = true;
                                foo.State = "OK";
                                foo.Message = string.Format("Reset Furnace success. userid [{0}].", value.UserID);
                                _logger.Info(foo.Message);
                                return foo;
                            }
                        }
                        else
                        {
                            foo.Success = false;
                            foo.State = "NG";
                            foo.Message = string.Format("Reset Furnace Failed, sThe equipment not of Furnace. userid [{0}].", value.UserID);
                            _logger.Info(foo.Message);
                            return foo;
                        }
                    }
                    else
                    {
                        foo.Success = false;
                        foo.State = "NG";
                        foo.Message = string.Format("Reset Furnace Failed, The equipment no Workgroup setting. userid [{0}].", value.UserID);
                        _logger.Info(foo.Message);
                        return foo;
                    }
                }
            }
            catch (Exception ex)
            {
                foo.Success = false;
                foo.State = "NG";
                foo.Message = string.Format("Reset Furnace Failed. [Exception][{0}].", ex.Message);
                _logger.Info(foo.Message);
                return foo;
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

    }
}
