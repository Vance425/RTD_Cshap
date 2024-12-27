using GyroLibrary;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nancy.Json;
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
using System.Threading.Tasks;

namespace RTDWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    
    public class SettingRTDConfigController : BasicController
    {

        private readonly ILogger _logger;
        private readonly DBTool _dbTool;
        private readonly IConfiguration _configuration;
        private readonly List<DBTool> _lstDBSession;

        public SettingRTDConfigController(ILogger logger, List<DBTool> lstDBSession, IConfiguration configuration)
        {
            _logger = logger;
            //_dbTool = dbTool;
            _configuration = configuration;
            _lstDBSession = lstDBSession;

            for (int idb = 0; idb < _lstDBSession.Count; idb++)
            {
                _dbTool = _lstDBSession[idb];
                if (_dbTool.IsConnected)
                {
                    break;
                }
            }
        }

        [HttpPost("SetEquipmentPortModel")]
        public APIResult SetEquipmentPortModel([FromBody] ClassEquipmentPortModel value)
        {
            APIResult foo;
            string funcName = "SetEquipmentPortModel";
            string tmpMsg = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql;
            DateTime dtLimit;
            IBaseDataService _BaseDataService = new BaseDataService();
            IFunctionService _functionService = new FunctionService();

            try
            {
                if (value.Equipment.Equals(""))
                {
                    //_dbTool.SQLExec(_BaseDataService.UpdateRtdAlarm(value.DateTime), out tmpMsg, true);
                    tmpMsg = "Equipment can not be empty. please check!";
                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                    return foo;
                }

                if (value.PortModel.Equals(""))
                {
                    //_dbTool.SQLExec(_BaseDataService.UpdateRtdAlarm(value.DateTime), out tmpMsg, true);
                    tmpMsg = "PortModel can not be empty. please check!";
                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                    return foo;
                }

                try
                {
                    int portNum = 0;

                    switch (value.PortModel)
                    {
                        case "1IOT1":
                            portNum = 1;
                            break;
                        case "1I1OT1":
                            portNum = 2;
                            break;
                        case "1I1OT2":
                            portNum = 2;
                            break;
                        case "2I2OT1":
                            portNum = 4;
                            break;
                        default:
                            tmpMsg = "[SetEquipmentPortModel] Alarm : PortModel is invalid. please check.";
                            break;
                    }

                    if (tmpMsg.Equals(""))
                    {
                        sql = String.Format(_BaseDataService.UpdateEquipPortModel(value.Equipment, value.PortModel, portNum));
                        _dbTool.SQLExec(sql, out tmpMsg, true);

                        _functionService.AutoGeneratePort(_dbTool, _configuration, _logger, value.Equipment, value.PortModel, out tmpMsg);
                    }
                }
                catch (Exception ex)
                {
                    tmpMsg = "[SetEquipmentPortModel] Exception occurred : {0}";
                    tmpMsg = String.Format(tmpMsg, ex.Message);
                }

                if (tmpMsg.Equals(""))
                {
                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmpMsg
                    };
                }
                else
                {
                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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

        public class ClassEquipmentPortModel
        {
            public string Equipment { get; set; }
            public string PortModel { get; set; }
        }

        [HttpPost("SetEquipmentWorkgroup")]
        public APIResult SetEquipmentWorkgroup([FromBody] ClassEquipmentWorkgroup value)
        {
            APIResult foo;
            string funcName = "SetEquipmentWorkgroup";
            string tmpMsg = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql;
            DateTime dtLimit;
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                if (value.Equipment.Equals(""))
                {
                    //_dbTool.SQLExec(_BaseDataService.UpdateRtdAlarm(value.DateTime), out tmpMsg, true);
                    tmpMsg = "Equipment can not be empty. please check.";

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
                else
                {
                    dt = _dbTool.GetDataTable(_BaseDataService.SelectTableEquipmentPortsInfoByEquipId(value.Equipment));

                    if (dt.Rows.Count <= 0)
                    {
                        tmpMsg = string.Format("Equipment [{0}] not exist.. please check.", value.Equipment);

                        foo = new APIResult()
                        {
                            Success = false,
                            State = "NG",
                            Message = tmpMsg
                        };
                    }
                    else
                    {
                        if (dt.Columns.Contains("Workgroup"))
                        {
                            tmpMsg = "";

                            if (!dt.Rows[0]["Workgroup"].ToString().Equals(value.Workgroup))
                            {
                                sql = String.Format(_BaseDataService.UpdateEquipWorkgroup(value.Equipment, value.Workgroup));
                                _dbTool.SQLExec(sql, out tmpMsg, true);
                            }

                            if (tmpMsg.Equals(""))
                            {
                                dt = _dbTool.GetDataTable(_BaseDataService.SelectTableEQP_Port_SetByEquipId(value.Equipment));

                                bool bChk = false;
                                foreach (DataRow dr1 in dt.Rows)
                                {
                                    if (!dr1["Workgroup"].ToString().Equals(value.Workgroup))
                                    {
                                        bChk = true;
                                    }

                                    if (bChk)
                                        break;
                                }

                                if (bChk)
                                {
                                    tmpMsg = "";
                                    sql = String.Format(_BaseDataService.UpdateEquipWorkgroup(value.Equipment, value.Workgroup));
                                    _dbTool.SQLExec(sql, out tmpMsg, true);
                                }
                            }

                            if (tmpMsg.Equals(""))
                            {
                                foo = new APIResult()
                                {
                                    Success = true,
                                    State = "OK",
                                    Message = tmpMsg
                                };
                            }
                            else
                            {
                                tmpMsg = string.Format("[{0}] Workgroup Issue. Equipment [{1}] Status data issue. please check.", funcName, value.Equipment);
                                foo = new APIResult()
                                {
                                    Success = false,
                                    State = "NG",
                                    Message = tmpMsg
                                };
                            }
                        }
                        else
                        {
                            tmpMsg = string.Format("Equipment [{0}] not exist.. please check.", value.Equipment);

                            foo = new APIResult()
                            {
                                Success = false,
                                State = "NG",
                                Message = tmpMsg
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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
        public class ClassEquipmentWorkgroup
        {
            public string Equipment { get; set; }
            public string Workgroup { get; set; }
        }

        [HttpPost("SetWorkroupSet")]
        public APIResult SetWorkroupSet([FromBody] ClassWorkgroupSet value)
        {
            APIResult foo;
            string funcName = "SetWorkroupSet";
            string tmpMsg = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql;
            DateTime dtLimit;
            IBaseDataService _BaseDataService = new BaseDataService();
            string WorkgroupID = "";

            try
            {
                if (value.Equipment.Equals(""))
                {
                    //_dbTool.SQLExec(_BaseDataService.UpdateRtdAlarm(value.DateTime), out tmpMsg, true);
                    tmpMsg = "Equipment can not be empty. please check.";

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };

                    return foo;
                }
                else
                {
                    dt = _dbTool.GetDataTable(_BaseDataService.SelectTableEquipmentPortsInfoByEquipId(value.Equipment));

                    if (dt.Rows.Count <= 0)
                    {
                        tmpMsg = string.Format("Equipment [{0}] not exist.. please check.", value.Equipment);

                        foo = new APIResult()
                        {
                            Success = false,
                            State = "NG",
                            Message = tmpMsg
                        };

                        return foo;
                    }
                    else
                    {
                        if(!dt.Rows[0]["Workgroup"].ToString().Equals(""))
                            WorkgroupID = dt.Rows[0]["Workgroup"].ToString().Equals("") ? "" : dt.Rows[0]["Workgroup"].ToString();
                    }
                }

                dt = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(WorkgroupID));

                if (dt.Rows.Count > 0)
                {
                    sql = String.Format(_BaseDataService.UpdateWorkgroupSet(WorkgroupID, value.InErack, value.OutErack));
                    _dbTool.SQLExec(sql, out tmpMsg, true);

                    if (tmpMsg.Equals(""))
                    {
                        foo = new APIResult()
                        {
                            Success = true,
                            State = "OK",
                            Message = tmpMsg
                        };
                    }
                    else
                    {
                        tmpMsg = string.Format("[{0}] Workgroup Issue. Workgroup [{1}] update failed. please check.", funcName, WorkgroupID);
                        foo = new APIResult()
                        {
                            Success = false,
                            State = "NG",
                            Message = tmpMsg
                        };
                    }
                }
                else
                {
                    tmpMsg = string.Format("[{0}] Workgroup Error. Workgroup [{1}] not exist.", funcName, WorkgroupID);
                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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
        public class ClassWorkgroupSet
        {
            public string Equipment { get; set; }
            public string InErack { get; set; }
            public string OutErack { get; set; }
        }
        public class ClassEquipment
        {
            public string Equipment { get; set; }
        }

        [HttpPost("CreateWorkroup")]
        public APIResult CreateWorkroup([FromBody] ClassWorkgroupId value)
        {
            APIResult foo;
            string funcName = "CreateWorkroup";
            string tmpMsg = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql;
            DateTime dtLimit;
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                if (value.WorkgroupID.Equals(""))
                {
                    //_dbTool.SQLExec(_BaseDataService.UpdateRtdAlarm(value.DateTime), out tmpMsg, true);
                    tmpMsg = "Workgroup can not be empty. please check.";

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };

                    return foo;
                }

                dt = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(value.WorkgroupID));

                if (dt.Rows.Count <= 0)
                {
                    sql = String.Format(_BaseDataService.CreateWorkgroup(value.WorkgroupID));
                    _dbTool.SQLExec(sql, out tmpMsg, true);

                    if(tmpMsg.Equals(""))
                    {
                        foo = new APIResult()
                        {
                            Success = true,
                            State = "OK",
                            Message = tmpMsg
                        };
                    }
                    else
                    {
                        tmpMsg = string.Format("[{0}] Create Issue. Workgroup [{1}] create failed.", funcName, value.WorkgroupID);
                        foo = new APIResult()
                        {
                            Success = false,
                            State = "NG",
                            Message = tmpMsg
                        };
                    }
                }
                else
                {
                    tmpMsg = string.Format("[{0}] Create Error. Workgroup [{1}] is exist.", funcName, value.WorkgroupID);
                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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

        [HttpPost("DeleteWorkroup")]
        public APIResult DeleteWorkroup([FromBody] ClassWorkgroupId value)
        {
            APIResult foo;
            string funcName = "DeleteWorkroup";
            string tmpMsg = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql;
            DateTime dtLimit;
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                if (value.WorkgroupID.Equals(""))
                {
                    //_dbTool.SQLExec(_BaseDataService.UpdateRtdAlarm(value.DateTime), out tmpMsg, true);
                    tmpMsg = "Workgroup can not be empty. please check.";

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };

                    return foo;
                }

                dt = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(value.WorkgroupID));

                if (dt.Rows.Count > 0)
                {
                    sql = String.Format(_BaseDataService.DeleteWorkgroup(value.WorkgroupID));
                    _dbTool.SQLExec(sql, out tmpMsg, true);

                    if (tmpMsg.Equals(""))
                    {
                        foo = new APIResult()
                        {
                            Success = true,
                            State = "OK",
                            Message = tmpMsg
                        };
                    }
                    else
                    {
                        tmpMsg = string.Format("[{0}] Delete Issue. Workgroup [{1}] delete failed.", funcName, value.WorkgroupID);
                        foo = new APIResult()
                        {
                            Success = false,
                            State = "NG",
                            Message = tmpMsg
                        };
                    }
                }
                else
                {
                    tmpMsg = string.Format("[{0}] Delete Error. Workgroup [{1}] is not exist.", funcName, value.WorkgroupID);
                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                tmpMsg = string.Format("[{0}] Unknow Error. Exception: {1}", funcName, ex.Message);

                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
                    Message = tmpMsg
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
        public class ClassWorkgroupId
        {
            public string WorkgroupID { get; set; }
        }

        [HttpGet("QueryRtdPortModelDef")]
        public ActionResult<String> QueryRtdPortModelDef()
        {

            string funcName = "QueryRtdPortModelDef";
            string tmpMsg = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                //// 查詢資料
                dt = _dbTool.GetDataTable(_BaseDataService.QueryPortModelDef());

                if (dt.Rows.Count > 0)
                {
                    strResult = JsonConvert.SerializeObject(dt);
                }
            }
            catch (Exception ex)
            {
                return strResult;
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return strResult;
        }

        [HttpGet("QueryWorkgroupList")]
        public ActionResult<String> QueryWorkgroupList()
        {

            string funcName = "QueryWorkgroupList";
            string tmpMsg = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                //// 查詢資料
                dt = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSetAndUseState(""));

                if (dt.Rows.Count > 0)
                {
                    strResult = JsonConvert.SerializeObject(dt);
                }
            }
            catch (Exception ex)
            {
                return strResult;
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return strResult;
        }

        [HttpGet("QueryPromisStage")]
        public ActionResult<String> QueryPromisStage()
        {

            string funcName = "QueryPromisStage";
            string tmpMsg = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                //// 查詢資料
                dt = _dbTool.GetDataTable(_BaseDataService.SelectRTDDefaultSet("PromisStage"));

                if (dt.Rows.Count > 0)
                {
                    strResult = JsonConvert.SerializeObject(dt);
                }
            }
            catch (Exception ex)
            {
                return strResult;
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return strResult;
        }

        [HttpGet("QueryEQPType")]
        public ActionResult<String> QueryEQPType()
        {

            string funcName = "QueryEQPType";
            string tmpMsg = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                //// 查詢資料
                dt = _dbTool.GetDataTable(_BaseDataService.QueryEQPType());

                if (dt.Rows.Count > 0)
                {
                    strResult = JsonConvert.SerializeObject(dt);
                }
            }
            catch (Exception ex)
            {
                return strResult;
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return strResult;
        }

        [HttpGet("QueryEQPIDType")]
        public ActionResult<String> QueryEQPIDType()
        {

            string funcName = "QueryEQPIDType";
            string tmpMsg = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                //// 查詢資料
                dt = _dbTool.GetDataTable(_BaseDataService.QueryEQPIDType());

                if (dt.Rows.Count > 0)
                {
                    strResult = JsonConvert.SerializeObject(dt);
                }
            }
            catch (Exception ex)
            {
                return strResult;
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return strResult;
        }

        [HttpPost("InsertPromisStageEquipMatrix")]
        public APIResult InsertPromisStageEquipMatrix([FromBody] ClassPromisStageEquipMatrix value)
        {
            APIResult foo;
            string funcName = "InsertPromisStageEquipMatrix";
            string tmpMsg = "";
            string errMsg = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql;
            DateTime dtLimit;
            IBaseDataService _BaseDataService = new BaseDataService();
            IFunctionService _functionService = new FunctionService();

            try
            {
                if (value.EqpType.Equals(""))
                {
                    //_dbTool.SQLExec(_BaseDataService.UpdateRtdAlarm(value.DateTime), out tmpMsg, true);
                    tmpMsg = "Equipment Type can not be empty. please check.";

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };

                    return foo;
                }

                if (value.Stage.Equals(""))
                {
                    //_dbTool.SQLExec(_BaseDataService.UpdateRtdAlarm(value.DateTime), out tmpMsg, true);
                    tmpMsg = "Promis Stage can not be empty. please check.";

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };

                    return foo;
                }

                if (value.UserId.Equals(""))
                {
                    //_dbTool.SQLExec(_BaseDataService.UpdateRtdAlarm(value.DateTime), out tmpMsg, true);
                    tmpMsg = "UserId can not be empty. please check.";

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };

                    return foo;
                }

                if (_functionService.DoInsertPromisStageEquipMatrix(_dbTool, _logger, value.Stage, value.EqpType, value.Equips, value.UserId, out errMsg))
                {
                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = errMsg
                    };
                }
                else
                {
                    tmpMsg = string.Format("[{0}] Insert Error! Error Message: {1}", funcName, errMsg);
                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                tmpMsg = string.Format("[{0}] Unknow Error. Exception: {1}", funcName, ex.Message);

                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
                    Message = tmpMsg
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
        public class ClassPromisStageEquipMatrix
        {
            public string Equips { get; set; }
            public string EqpType { get; set; }
            public string Stage { get; set; }
            public string UserId { get; set; }
        }

        [HttpGet("GetPromisStageInfo")]
        public string GetPromisStageInfo()
        {
            string funcName = "GetPromisStageInfo";
            string tmpMsg = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                //// 查詢資料
                sql = _BaseDataService.ShowTableEQUIP_MATRIX();
                //_logger.Info(string.Format("sql string: [{0}]", sql));
                dt = _dbTool.GetDataTable(sql);
                dr = dt.Select();
                if (dr.Length <= 0)
                {

                }
                else
                {
                    strResult = JsonConvert.SerializeObject(dt);
                }
            }
            catch (Exception ex)
            {
                return strResult;
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return strResult;
        }

        [HttpPost("DeletePromisStageEquipMatrix")]
        public APIResult DeletePromisStageEquipMatrix([FromBody] ClassPromisStageEquipMatrix value)
        {
            APIResult foo;
            string funcName = "DeletePromisStageEquipMatrix";
            string tmpMsg = "";
            string errMsg = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql;
            DateTime dtLimit;
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                var jsonStringName = new JavaScriptSerializer();
                var jsonStringResult = jsonStringName.Serialize(value);
                _logger.Info(string.Format("Function:{0}, Received:[{1}]", funcName, jsonStringResult));

                if (value.EqpType.Equals("") && value.Stage.Equals("") && value.Equips.Equals(""))
                {
                    tmpMsg = "All conditions must satisfy at least one.";

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };

                    return foo;
                }

                sql = _BaseDataService.DeletePromisStageEquipMatrix(value.Stage, value.EqpType, value.Equips); 
                if (_dbTool.SQLExec(sql, out errMsg, true))
                {
                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = errMsg
                    };

                    _logger.Info(string.Format("Function:{0}, Done.", funcName));
                }
                else
                {
                    tmpMsg = string.Format("[{0}] Delete Error! Error Message: {1}", funcName, errMsg);
                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                tmpMsg = string.Format("[{0}] Unknow Error. Exception: {1}", funcName, ex.Message);

                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
                    Message = tmpMsg
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
        [HttpPost("SetPreTransByWorkgroupStage")]
        public APIResult SetPreTransByWorkgroup([FromBody] ClassWorkgroup value)
        {
            APIResult foo;
            string funcName = "SetPreTransByWorkgroupStage";
            string tmpMsg = "";
            string tmp2Msg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string tmpKey = "";
            string _stage = "";
            string _userID = "";
            string resultMsg = "";

            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                _userID = value.UserID.Equals("") ? "-----" : value.UserID;

                dt = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(value.Workgroup));

                if (dt.Rows.Count > 0)
                {
                    Boolean enablePreTrans = false;

                    if (!value.Stage.Equals(""))
                        tmpKey = string.Format("stage='{0}'", value.Stage);

                    if (!tmpKey.Equals(""))
                    {
                        dr = dt.Select(tmpKey);

                        if (dr.Length > 0)
                        {
                            tmp2Msg = "{" + string.Format(@"'{0}':'{1}', 'UserID':'{2}'", value.Stage, dr[0]["PRETRANSFER"].ToString(), _userID) + "}";
                            //iPreTransMode = dt.Rows[0]["PRETRANSFER"].ToString().Equals("1") ? true : false;
                            enablePreTrans = dr[0]["PRETRANSFER"].ToString().Equals("1") ? true : false;
                            _stage = value.Stage;
                        }
                        else
                        {
                            tmpMsg = string.Format("Stage {0} not exist.", value.Stage);
                        }
                    }
                    else
                    {
                        string tmpStage = "";
                        string lstStage = "";

                        foreach (DataRow drTemp in dt.Rows)
                        {
                            tmpStage = string.Format(@"'{0}':'{1}'", drTemp["stage"].ToString(), drTemp["pretransfer"].ToString());

                            if (lstStage.Equals(""))
                                lstStage = tmpStage;
                            else
                                lstStage = string.Format("{0}, {1}", lstStage, tmpStage);
                        }

                        tmp2Msg = "{" + string.Format(@"{0}, 'UserID':'{1}'", lstStage, _userID) + "}";

                        enablePreTrans = dt.Rows[0]["PRETRANSFER"].ToString().Equals("1") ? true : false;
                        _stage = "";
                    }

                    if (enablePreTrans)
                    {
                        if (value.Set.Equals(0))
                        {
                            _logger.Info(tmp2Msg);
                            //// 更新狀態
                            _dbTool.SQLExec(_BaseDataService.SetResetPreTransfer(value.Workgroup, value.Stage, false), out tmpMsg, true);
                            _logger.Debug(tmpMsg);
                        }
                        else
                            resultMsg = "No Changed";
                    }
                    else
                    {
                        if (value.Set.Equals(1))
                        {
                            _logger.Info(tmp2Msg);
                            //// 更新狀態
                            _dbTool.SQLExec(_BaseDataService.SetResetPreTransfer(value.Workgroup, value.Stage, true), out tmpMsg, true);
                            _logger.Debug(tmpMsg);
                        }
                        else
                            resultMsg = "No Changed";
                    }
                }
                else
                {
                    tmpMsg = string.Format("Workgroup {0} not exist.", value.Workgroup);
                }

                if (tmpMsg.Equals(""))
                {
                    if (resultMsg.Equals(""))
                        tmp2Msg = string.Format("Set pretransfer success. [{0}][{1}] by [{2}]", value.Workgroup, _stage, _userID);
                    else
                        tmp2Msg = string.Format("Set pretransfer success. {0} [{1}][{2}] by [{3}]", resultMsg, value.Workgroup, _stage, _userID);

                    _logger.Info(tmp2Msg);

                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmpMsg
                    };
                }
                else
                {
                    tmp2Msg = string.Format("Set pretransfer failed. [{0}][{1}] by [{2}]", value.Workgroup, _stage, _userID);
                    _logger.Info(tmp2Msg);

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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
        public class ClassWorkgroup
        {
            public string Workgroup { get; set; }
            public string Stage { get; set; }
            public int Set { get; set; }
            public string UserID { get; set; }
        }

        [HttpPost("SetMidwaysByWorkgroupStageLocate")]
        public APIResult SetMidwaysByWorkgroupStageLocate([FromBody] ClassMidways value)
        {
            APIResult foo;
            string funcName = "SetMidwaysByWorkgroupStageLocate";
            string tmpMsg = "";
            string tmp2Msg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string tmpKey = "";
            string _stage = "";
            string _userID = "";
            string resultMsg = "";

            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                _userID = value.UserID.Equals("") ? "-----" : value.UserID;

                dt = _dbTool.GetDataTable(_BaseDataService.QueryMidwaysSet(value.Workgroup, value.Stage, value.Locate));

                if (dt.Rows.Count > 0)
                {
                    Boolean enablePreTrans = false;

                    if (!value.Stage.Equals(""))
                        tmpKey = string.Format("stage='{0}'", value.Stage);

                    if (!tmpKey.Equals(""))
                    {
                        dr = dt.Select(tmpKey);

                        if (dr.Length > 0)
                        {
                            tmp2Msg = "{" + string.Format(@"'{0}':'{1}', 'UserID':'{2}'", value.Stage, dr[0]["PRETRANSFER"].ToString(), _userID) + "}";
                            //iPreTransMode = dt.Rows[0]["PRETRANSFER"].ToString().Equals("1") ? true : false;
                            enablePreTrans = dr[0]["PRETRANSFER"].ToString().Equals("1") ? true : false;
                            _stage = value.Stage;
                        }
                        else
                        {
                            tmpMsg = string.Format("Stage {0} not exist.", value.Stage);
                        }
                    }
                    else
                    {
                        string tmpStage = "";
                        string lstStage = "";

                        foreach (DataRow drTemp in dt.Rows)
                        {
                            tmpStage = string.Format(@"'{0}':'{1}'", drTemp["stage"].ToString(), drTemp["pretransfer"].ToString());

                            if (lstStage.Equals(""))
                                lstStage = tmpStage;
                            else
                                lstStage = string.Format("{0}, {1}", lstStage, tmpStage);
                        }

                        tmp2Msg = "{" + string.Format(@"{0}, 'UserID':'{1}'", lstStage, _userID) + "}";

                        enablePreTrans = dt.Rows[0]["PRETRANSFER"].ToString().Equals("1") ? true : false;
                        _stage = "";
                    }

                    if (enablePreTrans)
                    {
                        if (value.Set.Equals(0))
                        {
                            _logger.Info(tmp2Msg);
                            //// 更新狀態
                            _dbTool.SQLExec(_BaseDataService.SetResetPreTransfer(value.Workgroup, value.Stage, false), out tmpMsg, true);
                            _logger.Debug(tmpMsg);
                        }
                        else
                            resultMsg = "No Changed";
                    }
                    else
                    {
                        if (value.Set.Equals(1))
                        {
                            _logger.Info(tmp2Msg);
                            //// 更新狀態
                            _dbTool.SQLExec(_BaseDataService.SetResetPreTransfer(value.Workgroup, value.Stage, true), out tmpMsg, true);
                            _logger.Debug(tmpMsg);
                        }
                        else
                            resultMsg = "No Changed";
                    }
                }
                else
                {
                    tmpMsg = string.Format("Workgroup {0} not exist.", value.Workgroup);
                }

                if (tmpMsg.Equals(""))
                {
                    if (resultMsg.Equals(""))
                        tmp2Msg = string.Format("Set pretransfer success. [{0}][{1}] by [{2}]", value.Workgroup, _stage, _userID);
                    else
                        tmp2Msg = string.Format("Set pretransfer success. {0} [{1}][{2}] by [{3}]", resultMsg, value.Workgroup, _stage, _userID);

                    _logger.Info(tmp2Msg);

                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmpMsg
                    };
                }
                else
                {
                    tmp2Msg = string.Format("Set pretransfer failed. [{0}][{1}] by [{2}]", value.Workgroup, _stage, _userID);
                    _logger.Info(tmp2Msg);

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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
        public class ClassMidways
        {
            public string Workgroup { get; set; }
            public string Stage { get; set; }
            public string Locate { get; set; }
            public int Set { get; set; }
            public string UserID { get; set; }
        }

        [HttpPost("SaveEquipmentLoadportSet")]
        public APIResult SaveLoadportSet([FromBody] ClassLoadportSet value)
        {
            APIResult foo;
            string funcName = "SaveEquipmentLoadportSet";
            string tmpMsg = "";
            string tmp2Msg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string tmpKey = "";
            string _attribute = "";
            string _attributevalue = "";
            string _userID = "";
            string resultMsg = "";

            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                _userID = value.UserID.Equals("") ? "-----" : value.UserID;

                dt = _dbTool.GetDataTable(_BaseDataService.QueryPortInfobyPortID(value.PortID));

                if (dt.Rows.Count > 0)
                {
                    //--workgroup, 
                    //--port_model
                    //--carrier_type
                    _attribute = value.Attribute.ToLower();
                    switch (value.Attribute.ToLower())
                    {
                        case "workgroup":
                        case "port_type":
                        case "carrier_type":
                            _attributevalue = dt.Rows[0][_attribute.ToUpper()].ToString();

                            if(!_attributevalue.Equals(value.Value))
                            {
                                sql = _BaseDataService.ChangePortInfobyPortID(value.PortID, _attribute, value.Value, _userID);
                                
                                if(!sql.Equals(""))
                                {
                                    _dbTool.SQLExec(sql, out resultMsg, true);
                                }
                            }
                            else
                            {
                                tmpMsg = string.Format("Set Value failed. Port [{0}] Attribute value [{1}] no change by [{2}].", value.PortID, value.Attribute, _userID);
                            }
                            break;
                        default:
                            tmpMsg = string.Format("Set Value failed. [{0}] Attribute [{1}][{2}] by [{3}]", value.PortID, value.Attribute, value.Value, _userID);
                            break;
                    }


                }
                else
                {
                    tmpMsg = string.Format("PortID [{0}] not exist.", value.PortID);
                }

                if (tmpMsg.Equals(""))
                {
                    if (resultMsg.Equals(""))
                        tmp2Msg = string.Format("Set Value success. [{0}] Attribute [{1}][{2}] by [{3}]", value.PortID, value.Attribute, value.Value, _userID);
                    else
                        tmp2Msg = string.Format("Set Value success. [{0}][{1}] Attribute [{2}][{3}] by [{4}]", value.PortID, resultMsg, value.Attribute, value.Value, _userID);

                    _logger.Info(tmp2Msg);

                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmp2Msg
                    };
                }
                else
                {
                    _logger.Info(tmpMsg);

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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
        public class ClassLoadportSet
        {
            public string PortID { get; set; }
            public string Attribute { get; set; }
            public string Value { get; set; }
            public string UserID { get; set; }
        }
        [HttpPost("SetLookupTableByWorkgroup")]
        public APIResult SetLookupTableByWorkgroup([FromBody] ClassWorkgroup value)
        {
            APIResult foo;
            string funcName = "SetLookupTableByWorkgroup";
            string tmpMsg = "";
            string tmp2Msg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string tmpKey = "";
            string _stage = "";
            string _userID = "";
            string resultMsg = "";

            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                _userID = value.UserID.Equals("") ? "-----" : value.UserID;

                dt = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(value.Workgroup));

                if (dt.Rows.Count > 0)
                {
                    Boolean enableFunction = false;

                    if (!value.Stage.Equals(""))
                        tmpKey = string.Format("stage='{0}'", value.Stage);

                    if (!tmpKey.Equals(""))
                    {
                        dr = dt.Select(tmpKey);

                        if (dr.Length > 0)
                        {
                            tmp2Msg = "{" + string.Format(@"'{0}':'{1}', 'UserID':'{2}'", value.Stage, dr[0]["CHECKEQPLOOKUPTABLE"].ToString(), _userID) + "}";
                            //iPreTransMode = dt.Rows[0]["PRETRANSFER"].ToString().Equals("1") ? true : false;
                            enableFunction = dr[0]["CHECKEQPLOOKUPTABLE"].ToString().Equals("1") ? true : false;
                            _stage = value.Stage;
                        }
                        else
                        {
                            tmpMsg = string.Format("Stage {0} not exist.", value.Stage);
                        }
                    }
                    else
                    {
                        string tmpStage = "";
                        string lstStage = "";

                        foreach (DataRow drTemp in dt.Rows)
                        {
                            tmpStage = string.Format(@"'{0}':'{1}'", drTemp["stage"].ToString(), drTemp["CHECKEQPLOOKUPTABLE"].ToString());

                            if (lstStage.Equals(""))
                                lstStage = tmpStage;
                            else
                                lstStage = string.Format("{0}, {1}", lstStage, tmpStage);
                        }

                        tmp2Msg = "{" + string.Format(@"{0}, 'UserID':'{1}'", lstStage, _userID) + "}";

                        enableFunction = dt.Rows[0]["CHECKEQPLOOKUPTABLE"].ToString().Equals("1") ? true : false;
                        _stage = "";
                    }

                    if (enableFunction)
                    {
                        if (value.Set.Equals(0))
                        {
                            _logger.Info(tmp2Msg);
                            //// 更新狀態
                            _dbTool.SQLExec(_BaseDataService.SetResetCheckLookupTable(value.Workgroup, value.Stage, false), out tmpMsg, true);
                            _logger.Debug(tmpMsg);
                        }
                        else
                            resultMsg = "No Changed";
                    }
                    else
                    {
                        if (value.Set.Equals(1))
                        {
                            _logger.Info(tmp2Msg);
                            //// 更新狀態
                            _dbTool.SQLExec(_BaseDataService.SetResetCheckLookupTable(value.Workgroup, value.Stage, true), out tmpMsg, true);
                            _logger.Debug(tmpMsg);
                        }
                        else
                            resultMsg = "No Changed";
                    }
                }
                else
                {
                    tmpMsg = string.Format("Workgroup {0} not exist.", value.Workgroup);
                }

                if (tmpMsg.Equals(""))
                {
                    if (resultMsg.Equals(""))
                        tmp2Msg = string.Format("Set check lookup table success. [{0}][{1}] by [{2}]", value.Workgroup, _stage, _userID);
                    else
                        tmp2Msg = string.Format("Set check lookup table success. {0} [{1}][{2}] by [{3}]", resultMsg, value.Workgroup, _stage, _userID);

                    _logger.Info(tmp2Msg);

                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmpMsg
                    };
                }
                else
                {
                    tmp2Msg = string.Format("Set check lookup table failed. [{0}][{1}] by [{2}]", value.Workgroup, _stage, _userID);
                    _logger.Info(tmp2Msg);

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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
        [HttpPost("SetUseLastStage")]
        public APIResult SetUseLastStage([FromBody] ClassWorkgroup value)
        {
            APIResult foo;
            string funcName = "SetUseLastStage";
            string tmpMsg = "";
            string tmp2Msg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string tmpKey = "";
            string _stage = "";
            string _userID = "";
            string resultMsg = "";
            string _paramsName = "";

            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                _paramsName = "USELASTSTAGE";

                _userID = value.UserID.Equals("") ? "-----" : value.UserID;

                dt = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(value.Workgroup));

                if (dt.Rows.Count > 0)
                {
                    Boolean enableFunction = false;

                    if (!value.Stage.Equals(""))
                        tmpKey = string.Format("stage='{0}'", value.Stage);

                    if (!tmpKey.Equals(""))
                    {
                        dr = dt.Select(tmpKey);

                        if (dr.Length > 0)
                        {
                            tmp2Msg = "{" + string.Format(@"'{0}':'{1}', 'UserID':'{2}'", value.Stage, dr[0]["USELASTSTAGE"].ToString(), _userID) + "}";
                            //iPreTransMode = dt.Rows[0]["PRETRANSFER"].ToString().Equals("1") ? true : false;
                            enableFunction = dr[0]["USELASTSTAGE"].ToString().Equals("1") ? true : false;
                            _stage = value.Stage;
                        }
                        else
                        {
                            tmpMsg = string.Format("Stage {0} not exist.", value.Stage);
                        }
                    }
                    else
                    {
                        string tmpStage = "";
                        string lstStage = "";

                        foreach (DataRow drTemp in dt.Rows)
                        {
                            tmpStage = string.Format(@"'{0}':'{1}'", drTemp["stage"].ToString(), drTemp["USELASTSTAGE"].ToString());

                            if (lstStage.Equals(""))
                                lstStage = tmpStage;
                            else
                                lstStage = string.Format("{0}, {1}", lstStage, tmpStage);
                        }

                        tmp2Msg = "{" + string.Format(@"{0}, 'UserID':'{1}'", lstStage, _userID) + "}";

                        enableFunction = dt.Rows[0]["USELASTSTAGE"].ToString().Equals("1") ? true : false;
                        _stage = "";
                    }

                    if (enableFunction)
                    {
                        if (value.Set.Equals(0))
                        {
                            _logger.Info(tmp2Msg);
                            //// 更新狀態
                            _dbTool.SQLExec(_BaseDataService.SetResetParams(value.Workgroup, value.Stage, _paramsName, false), out tmpMsg, true);
                            _logger.Debug(tmpMsg);
                        }
                        else
                            resultMsg = "No Changed";
                    }
                    else
                    {
                        if (value.Set.Equals(1))
                        {
                            _logger.Info(tmp2Msg);
                            //// 更新狀態
                            _dbTool.SQLExec(_BaseDataService.SetResetParams(value.Workgroup, value.Stage, _paramsName, true), out tmpMsg, true);
                            _logger.Debug(tmpMsg);
                        }
                        else
                            resultMsg = "No Changed";
                    }
                }
                else
                {
                    tmpMsg = string.Format("Workgroup {0} not exist.", value.Workgroup);
                }

                if (tmpMsg.Equals(""))
                {
                    if (resultMsg.Equals(""))
                        tmp2Msg = string.Format("Set check out use last Stage success. [{0}][{1}] by [{2}]", value.Workgroup, _stage, _userID);
                    else
                        tmp2Msg = string.Format("Set check out use last Stage success. {0} [{1}][{2}] by [{3}]", resultMsg, value.Workgroup, _stage, _userID);

                    _logger.Info(tmp2Msg);

                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmpMsg
                    };
                }
                else
                {
                    tmp2Msg = string.Format("Set check out use last Stage failed. [{0}][{1}] by [{2}]", value.Workgroup, _stage, _userID);
                    _logger.Info(tmp2Msg);

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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
        [HttpPost("SetAoiMeasurement")]
        public APIResult SetAoiMeasurement([FromBody] ClassWorkgroup value)
        {
            APIResult foo;
            string funcName = "SetAoiMeasurement";
            string tmpMsg = "";
            string tmp2Msg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string tmpKey = "";
            string _stage = "";
            string _userID = "";
            string resultMsg = "";
            string _paramsName = "";

            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                _paramsName = "AOIMEASUREMENT";

                _userID = value.UserID.Equals("") ? "-----" : value.UserID;

                dt = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(value.Workgroup));

                if (dt.Rows.Count > 0)
                {
                    Boolean enableFunction = false;

                    if (!value.Stage.Equals(""))
                        tmpKey = string.Format("stage='{0}'", value.Stage);

                    if (!tmpKey.Equals(""))
                    {
                        dr = dt.Select(tmpKey);

                        if (dr.Length > 0)
                        {
                            tmp2Msg = "{" + string.Format(@"'{0}':'{1}', 'UserID':'{2}'", value.Stage, dr[0][_paramsName].ToString(), _userID) + "}";
                            //iPreTransMode = dt.Rows[0]["PRETRANSFER"].ToString().Equals("1") ? true : false;
                            enableFunction = dr[0][_paramsName].ToString().Equals("1") ? true : false;
                            _stage = value.Stage;
                        }
                        else
                        {
                            tmpMsg = string.Format("Stage {0} not exist.", value.Stage);
                        }
                    }
                    else
                    {
                        string tmpStage = "";
                        string lstStage = "";

                        foreach (DataRow drTemp in dt.Rows)
                        {
                            tmpStage = string.Format(@"'{0}':'{1}'", drTemp["stage"].ToString(), drTemp[_paramsName].ToString());

                            if (lstStage.Equals(""))
                                lstStage = tmpStage;
                            else
                                lstStage = string.Format("{0}, {1}", lstStage, tmpStage);
                        }

                        tmp2Msg = "{" + string.Format(@"{0}, 'UserID':'{1}'", lstStage, _userID) + "}";

                        enableFunction = dt.Rows[0][_paramsName].ToString().Equals("1") ? true : false;
                        _stage = "";
                    }

                    if (enableFunction)
                    {
                        if (value.Set.Equals(0))
                        {
                            _logger.Info(tmp2Msg);
                            //// 更新狀態
                            _dbTool.SQLExec(_BaseDataService.SetResetParams(value.Workgroup, value.Stage, _paramsName, false), out tmpMsg, true);
                            _logger.Debug(tmpMsg);
                        }
                        else
                            resultMsg = "No Changed";
                    }
                    else
                    {
                        if (value.Set.Equals(1))
                        {
                            _logger.Info(tmp2Msg);
                            //// 更新狀態
                            _dbTool.SQLExec(_BaseDataService.SetResetParams(value.Workgroup, value.Stage, _paramsName, true), out tmpMsg, true);
                            _logger.Debug(tmpMsg);
                        }
                        else
                            resultMsg = "No Changed";
                    }
                }
                else
                {
                    tmpMsg = string.Format("Workgroup {0} not exist.", value.Workgroup);
                }

                if (tmpMsg.Equals(""))
                {
                    if (resultMsg.Equals(""))
                        tmp2Msg = string.Format("Set use fail eRack success. [{0}][{1}] by [{2}]", value.Workgroup, _stage, _userID);
                    else
                        tmp2Msg = string.Format("Set use fail eRack success. {0} [{1}][{2}] by [{3}]", resultMsg, value.Workgroup, _stage, _userID);

                    _logger.Info(tmp2Msg);

                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmpMsg
                    };
                }
                else
                {
                    tmp2Msg = string.Format("Set use fail eRack failed. [{0}][{1}] by [{2}]", value.Workgroup, _stage, _userID);
                    _logger.Info(tmp2Msg);

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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
        [HttpPost("SetWorkgroupSetByWorkgroupStage")]
        public APIResult SetWorkgroupSetByWorkgroupStage([FromBody] ClassWorkgroupSetting value)
        {
            APIResult foo;
            string funcName = "SetWorkgroupSetByWorkgroupStage";
            string tmpMsg = "";
            string tmp2Msg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string tmpKey = "";
            string _stage = "";
            string _userID = "";
            string _tmpValues = "";
            string _paramsName = "";

            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                _paramsName = "SetWorkgroupSetByWorkgroupStage";

                _userID = value.UserID.Equals("") ? "-----" : value.UserID;

                dt = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(value.Workgroup));

                if (dt.Rows.Count > 0)
                {
                    if (!value.Workgroup.Equals(""))
                        tmpKey = string.Format("workgroup='{0}'", value.Workgroup);

                    if (!value.Stage.Equals(""))
                        tmpKey = string.Format("{0} and stage='{1}'", tmpKey, value.Stage);

                    if (!value.Parameter.Equals(""))
                        _paramsName = string.Format("{0}", value.Parameter.Trim());

                    if (!tmpKey.Equals(""))
                    {
                        if(value.Stage.Equals(""))
                            dr = dt.Select(string.Format("stage='{1}'", _paramsName, "DEFAULT"));
                        else
                            dr = dt.Select(string.Format("stage='{1}'", _paramsName, value.Stage));

                        if(dr.Length > 0)
                        {
                            if (!dr[0][_paramsName].ToString().Equals(value.SwState.Equals(true) ? "1" : "0"))
                            {
                                sql = _BaseDataService.SetWorkgroupSetByWorkgroupStage(value.Workgroup, value.Stage, value.Parameter, value.SwState);
                                _dbTool.SQLExec(sql, out tmpMsg, true);

                                if (tmpMsg.Equals(""))
                                {
                                    tmp2Msg = string.Format(@"The function [{0}] been {1}. Workgroup: [{2}], Stage: [{3}], UserID: [{4}]", _paramsName, value.SwState.Equals(true) ? "turn on" : "turn off", value.Workgroup, value.Stage, _userID);

                                }
                                else
                                {
                                    tmp2Msg = string.Format("Function change fail. Workgroup: [{0}], Stage: [{1}], State: {2} [{3}]", value.Workgroup, value.Stage, value.SwState, tmp2Msg);
                                }
                            }
                            else
                            {
                                tmp2Msg = string.Format("No need change state. Workgroup: [{0}], Stage: [{1}], State: {2} [{3}]", value.Workgroup, value.Stage, value.SwState, value.UserID);
                            }
                        }
                    }
                }
                else
                {
                    tmp2Msg = string.Format("Workgroup {0} not exist.", value.Workgroup);
                }

                if (tmpMsg.Equals(""))
                {
                    _logger.Info(tmp2Msg);

                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmp2Msg
                    };
                }
                else
                {
                    _logger.Info(tmp2Msg);

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmp2Msg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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
        public class ClassWorkgroupSetting
        {
            public string Workgroup { get; set; }
            public string Stage { get; set; }
            public string Parameter { get; set; }
            public bool SwState { get; set; }
            public string UserID { get; set; }
        }

        [HttpGet("QueryStageControl")]
        public ActionResult<String> QueryStageControl()
        {

            string funcName = "QueryStageControl";
            string tmpMsg = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                //// 查詢資料
                dt = _dbTool.GetDataTable(_BaseDataService.QueryStageControl());

                if (dt.Rows.Count > 0)
                {
                    strResult = JsonConvert.SerializeObject(dt);
                }
            }
            catch (Exception ex)
            {
                return strResult;
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return strResult;
        }
        [HttpPost("CloneWorkgroupWorkgroupSetByWorkgroup")]
        public APIResult CloneWorkgroupWorkgroupSetByWorkgroup([FromBody] ClassParamsWorkgroupSet value)
        {
            APIResult foo;
            string funcName = "CloneWorkgroupWorkgroupSetByWorkgroup";
            string tmpMsg = "";
            string tmp2Msg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string tmpKey = "";
            string _stage = "";
            string _userID = "";
            string _tmpNewValue = "";
            string _paramsName = "";
            bool _processingState = false;

            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {

                _userID = value.UserID.Equals("") ? "-----" : value.UserID;

                dt = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(value.Workgroup));

                if (dt.Rows.Count > 0)
                {

                    if (!value.newWorkgroup.Equals(""))
                        _tmpNewValue = string.Format("{0}", value.newWorkgroup.Trim());
                    else
                        tmpMsg = string.Format("New workgroup cannot empty! please check.");

                    if (tmpMsg.Equals(""))
                    {
                        sql = _BaseDataService.CloneWorkgroupsForWorkgroupSetByWorkgroup(value.newWorkgroup, value.Workgroup);
                        _dbTool.SQLExec(sql, out tmpMsg, true);

                        if (tmpMsg.Equals(""))
                        {
                            tmpMsg = string.Format(@"Clone workgroup [{0}] Success. userID [{1}]", value.newWorkgroup, _userID);
                            _processingState = true;
                        }
                        else
                        {
                            tmpMsg = string.Format(@"Clone workgroup [{0}] Fail. userID [{1}]", value.newWorkgroup, _userID);
                            _processingState = false;
                        }
                    }
                }
                else
                {
                    tmpMsg = string.Format("Workgroup {0} not exist.", value.Workgroup);
                }

                if (_processingState)
                {
                    _logger.Info(tmpMsg);

                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmpMsg
                    };
                }
                else
                {
                    _logger.Info(tmpMsg);

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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
        [HttpPost("CloneStageForWorkgroupSetByWorkgroupStage")]
        public APIResult CloneStageForWorkgroupSetByWorkgroupStage([FromBody] ClassParamsWorkgroupSet value)
        {
            APIResult foo;
            string funcName = "CloneStageForWorkgroupSetByWorkgroupStage";
            string tmpMsg = "";
            string tmp2Msg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string tmpKey = "";
            string _stage = "";
            string _userID = "";
            string _tmpNewValue = "";
            string _paramsName = "";
            bool _procesingState = false;

            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {

                _userID = value.UserID.Equals("") ? "-----" : value.UserID;

                dt = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(value.Workgroup));

                if (dt.Rows.Count > 0)
                {
                    dtTemp = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(value.Workgroup, value.Stage));

                    if (dtTemp.Rows.Count > 0)
                    {
                        if (tmpMsg.Equals(""))
                        {
                            sql = _BaseDataService.CloneStageForWorkgroupSetByWorkgroupStage(value.newStage, value.Workgroup, value.Stage);
                            _dbTool.SQLExec(sql, out tmpMsg, true);

                            if (tmpMsg.Equals(""))
                            {
                                tmpMsg = string.Format(@"Clone workgroup [{0}] Success. userID [{1}]", value.newWorkgroup, _userID);
                                _procesingState = true;
                            }
                            else
                            {
                                tmpMsg = string.Format(@"Clone workgroup [{0}] Fail. userID [{1}]", value.newWorkgroup, _userID);
                                _procesingState = false;
                            }
                        }
                    }
                }
                else
                {
                    tmpMsg = string.Format("Workgroup {0} not exist.", value.Workgroup);
                }

                if (_procesingState)
                {
                    _logger.Info(tmpMsg);

                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmpMsg
                    };
                }
                else
                {
                    _logger.Info(tmpMsg);

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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
        public class ClassParamsWorkgroupSet
        {
            public string Workgroup { get; set; }
            public string Stage { get; set; }
            public string newWorkgroup { get; set; }
            public string newStage { get; set; }
            public string UserID { get; set; }
        }
        [HttpPost("SetParametersWorkgroupSetByWorkgroupStage")]
        public APIResult SetParametersWorkgroupSetByWorkgroupStage([FromBody] ClassEntityWorkgroupSetting value)
        {
            APIResult foo;
            string funcName = "SetParametersWorkgroupSetByWorkgroupStage";
            string tmpMsg = "";
            string tmp2Msg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string tmpKey = "";
            string _stage = "";
            string _userID = "";
            string _tmpValues = "";
            string _paramsName = "";
            bool _isSwitch = false;
            bool _doLogic = false;

            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                _paramsName = "SetParametersWorkgroupSetByWorkgroupStage";

                _userID = value.UserID.Equals("") ? "-----" : value.UserID;

                dt = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(value.Workgroup));

                if (dt.Rows.Count > 0)
                {
                    if (!value.Workgroup.Equals(""))
                        tmpKey = string.Format("workgroup='{0}'", value.Workgroup);

                    if (!value.Stage.Equals(""))
                        tmpKey = string.Format("{0} and stage='{1}'", tmpKey, value.Stage);

                    if (!value.Parameter.Equals(""))
                        _paramsName = string.Format("{0}", value.Parameter.Trim());

                    if (!tmpKey.Equals(""))
                    {
                        ///有Stage
                        if (value.Stage.Equals(""))
                            dr = dt.Select(string.Format("stage='{1}'", _paramsName, "DEFAULT"));
                        else
                            dr = dt.Select(string.Format("stage='{1}'", _paramsName, value.Stage));

                        if (dr.Length > 0)
                        {
                            if (!value.Parameter.Equals(""))
                            {
                                _isSwitch = false;
                                _doLogic = true;
                            }
                            else
                            {
                                _doLogic = false;
                                tmpMsg = string.Format(@"Parameter invalid. please check parameter.");
                            }

                            if(_doLogic)
                            {

                                if (!value.Parameter.Equals(""))
                                {
                                    bool _multi = false;
                                    string[] _lstParams;

                                    if (value.Values.IndexOf(',') > 0)
                                    { _multi = true; }

                                    if (_multi)
                                    {
                                        _lstParams = value.Values.Split(',');
                                    }
                                    else
                                    {
                                        _lstParams = new string[] { value.Values };
                                    }

                                    foreach (string tmpParam in _lstParams)
                                    {

                                        if (!dr[0][_paramsName].Equals(null))
                                        {
                                            sql = _BaseDataService.SetParameterWorkgroupSetByWorkgroupStage(value.Workgroup, value.Stage, value.Parameter, value.IsNumber.Equals(true) ? int.Parse(tmpParam) : tmpParam);
                                            _dbTool.SQLExec(sql, out tmpMsg, true);

                                            if (tmpMsg.Equals(""))
                                            {
                                                tmp2Msg = string.Format(@"The Key [{0}] been change to {1}. Workgroup: [{2}], Stage: [{3}], UserID: [{4}]", _paramsName, tmpParam, value.Workgroup, value.Stage, _userID);

                                            }
                                            else
                                            {
                                                tmp2Msg = string.Format("Parameter change fail. Workgroup: [{0}], Stage: [{1}], State: {2} [{3}]", value.Workgroup, value.Stage, value.IsNumber, tmp2Msg);
                                            }
                                        }
                                        else
                                        {
                                            tmp2Msg = string.Format("No need change state. Workgroup: [{0}], Stage: [{1}], State: {2} [{3}]", value.Workgroup, value.Stage, value.IsNumber, value.UserID);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        ///無Stage, 直接用workgroup update
                        if (value.Stage.Equals(""))
                            dr = dt.Select(string.Format("stage='{1}'", _paramsName, "DEFAULT"));
                        else
                            dr = dt.Select(string.Format("stage='{1}'", _paramsName, value.Stage));

                        if (dr.Length > 0)
                        {
                            if (!dr[0][_paramsName].ToString().Equals(value.IsNumber.Equals(true) ? "1" : "0"))
                            {
                                sql = _BaseDataService.SetWorkgroupSetByWorkgroupStage(value.Workgroup, value.Stage, value.Parameter, value.IsNumber);
                                _dbTool.SQLExec(sql, out tmpMsg, true);

                                if (tmpMsg.Equals(""))
                                {
                                    tmp2Msg = string.Format(@"The function [{0}] been {1}. Workgroup: [{2}], Stage: [{3}], UserID: [{4}]", _paramsName, value.IsNumber.Equals(true) ? "turn on" : "turn off", value.Workgroup, value.Stage, _userID);

                                }
                                else
                                {
                                    tmp2Msg = string.Format("Function change fail. Workgroup: [{0}], Stage: [{1}], State: {2} [{3}]", value.Workgroup, value.Stage, value.IsNumber, tmp2Msg);
                                }
                            }
                            else
                            {
                                tmp2Msg = string.Format("No need change state. Workgroup: [{0}], Stage: [{1}], State: {2} [{3}]", value.Workgroup, value.Stage, value.IsNumber, value.UserID);
                            }
                        }
                    }
                }
                else
                {
                    tmp2Msg = string.Format("Workgroup {0} not exist.", value.Workgroup);
                }

                if (tmpMsg.Equals(""))
                {
                    _logger.Info(tmp2Msg);

                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmp2Msg
                    };
                }
                else
                {
                    _logger.Info(tmp2Msg);

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmp2Msg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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
        public class ClassEntityWorkgroupSetting
        {
            public string Workgroup { get; set; }
            public string Stage { get; set; }
            public string Parameter { get; set; }
            public bool IsNumber { get; set; }
            public string Values { get; set; }
            public string UserID { get; set; }
        }
        
        [HttpPost("QueryMidways")]
        public ActionResult<String> QueryMidways([FromBody] QueryMidways value)
        {

            string funcName = "QueryMidways";
            string tmpMsg = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                //// 查詢資料
                sql = _BaseDataService.QueryMidways(value);
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    strResult = JsonConvert.SerializeObject(dt);
                }
            }
            catch (Exception ex)
            {
                return strResult;
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return strResult;
        }
        public class ClassConditionMidways
        {
            public string Workgroup { get; set; }
            public string Stage { get; set; }
            public string Midway { get; set; }
            public string UserID { get; set; }
        }

        public class ClassRemoveMidways
        {
            public string Workgroup { get; set; }
            public string Stage { get; set; }
            public string idx { get; set; }
            public string UserID { get; set; }
        }
        [HttpPost("InsertMidways")]
        public APIResult InsertMidways([FromBody] InsertMidways value)
        {
            APIResult foo;
            string funcName = "InsertMidways";
            string tmpMsg = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            InsertMidways _insertMidways;
            string[] _lstCurrentLocate;
            string _failedCurrentLocate = "";
            string _failedCauses = "";
            int _idx = 1;
            string _currentdatetime = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                if(value.CurrentLocate.IndexOf(',') > 0)
                {
                    _lstCurrentLocate = value.CurrentLocate.Split(',');
                }
                else
                {
                    _lstCurrentLocate = new string[] { value.CurrentLocate.ToString() };
                }

                _currentdatetime = DateTime.Now.ToString("yyyyMMddHHmmss");

                foreach (string tmpKey in _lstCurrentLocate)
                {
                    tmpMsg = "";
                    _insertMidways = new InsertMidways();

                    _insertMidways.Workgroup = value.Workgroup;
                    _insertMidways.Stage = value.Stage;
                    _insertMidways.Midway = value.Midway;
                    _insertMidways.CurrentLocate = tmpKey;
                    _insertMidways.Idx = string.Format("{0}{1}", _currentdatetime, _idx.ToString().PadLeft(5, '0'));

                    try {
                        //// 查詢資料
                        _dbTool.SQLExec(_BaseDataService.InsertMidways(_insertMidways), out tmpMsg, true);

                        if(tmpMsg.Equals(""))
                        {
                            //_failedCauses = string.Format("[{0}, {1}]", _insertMidways.CurrentLocate, tmpMsg);
                        }
                        else
                        {
                            _failedCauses = string.Format("{0}[{1},{2}]", _failedCauses, _insertMidways.CurrentLocate, tmpMsg);
                        }
                    }
                    catch(Exception ex) { 
                        if(_failedCurrentLocate.Equals(""))
                        {
                            _failedCurrentLocate = string.Format("[{0},{1}]", tmpKey, ex.Message);
                        }
                        else
                        {
                            _failedCurrentLocate = string.Format("{0}[{1},{2}]", _failedCurrentLocate, tmpKey, ex.Message);
                        }
                    }

                    _idx++;
                }

                //
                if (_failedCurrentLocate.Equals(""))
                {
                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmpMsg
                    };
                }
                else
                {
                    foo = new APIResult()
                    {
                        Success = true,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
                    Message = ex.Message
                };
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return foo;
        }
        [HttpPost("TurnOnMidways")]
        public APIResult TurnOnMidways([FromBody] TurnOnMidways value)
        {
            APIResult foo;
            string funcName = "TurnOnMidways";
            string tmpMsg = "";
            string tmpMsg2 = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            TurnOnMidways _turnOnMidways;
            QueryMidways _queryMidways;
            string[] _lstCurrentLocate;
            string _failedCurrentLocate = "";
            string _failedCauses = "";
            string _idxString = "";
            string _theRecordsState = "";
            bool _break = false;
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                if (value.CurrentLocate.IndexOf(',') > 0)
                {
                    _lstCurrentLocate = value.CurrentLocate.Split(',');
                }
                else
                {
                    _lstCurrentLocate = new string[] { value.CurrentLocate.ToString() };
                }

                _queryMidways = new QueryMidways();
                _queryMidways.Workgroup = value.Workgroup;
                _queryMidways.Stage = value.Stage;
                _queryMidways.Midway = value.Midway;

                foreach (string tmpKey in _lstCurrentLocate)
                {
                    _queryMidways.CurrentLocate = tmpKey;

                    sql = _BaseDataService.QueryMidways(_queryMidways);
                    dt = _dbTool.GetDataTable(sql);

                    if(dt.Rows.Count > 0)
                    {
                        foreach(DataRow drTemp in dt.Rows)
                        {
                            _idxString = drTemp["idx"].ToString();
                            _theRecordsState = drTemp["enabled"].ToString().Equals("1") ? "true" : "false";

                            tmpMsg = "";

                            try
                            {
                                if (!_theRecordsState.Equals(value.State))
                                {

                                    //// 查詢資料
                                    _dbTool.SQLExec(_BaseDataService.TurnOnMidways(_idxString, value.State.ToLower().Equals("true") ? true : false), out tmpMsg, true);

                                    if (tmpMsg.Equals(""))
                                    {
                                        tmpMsg2 = string.Format("The midway [{0}/{1}/{2}/{3}] has been {4}", _queryMidways.Workgroup, _queryMidways.Stage, _queryMidways.Midway, _queryMidways.CurrentLocate, value.State.ToLower().Equals("true") ? "turn on" : "turn off");
                                        _break = false;
                                    }
                                    else
                                    {
                                        tmpMsg2 = string.Format("Change state[{0}] fail. Midway [{1}/{2}/{3}/{4}]. Message: {5}", value.State, _queryMidways.Workgroup, _queryMidways.Stage, _queryMidways.Midway, _queryMidways.CurrentLocate, tmpMsg2);
                                        _break = true;
                                    }

                                    _logger.Info(tmpMsg2);
                                }
                            }
                            catch (Exception ex)
                            {
                                tmpMsg2 = string.Format("Change state[{0}] fail. Midway [{1}/{2}/{3}/{4}]. [Exception]: {5}", value.State, _queryMidways.Workgroup, _queryMidways.Stage, _queryMidways.Midway, _queryMidways.CurrentLocate, tmpMsg2);
                                _logger.Info(tmpMsg2);
                                _break = true;
                            }

                            if (_break.Equals(true))
                                break;
                            else
                                _queryMidways.CurrentLocate = "";
                        }
                    }

                    if (_break.Equals(true))
                        break;
                    else
                        _queryMidways.CurrentLocate = "";
                }

                //
                if (_break.Equals(false))
                {
                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmpMsg2
                    };
                }
                else
                {
                    foo = new APIResult()
                    {
                        Success = true,
                        State = "NG",
                        Message = tmpMsg2
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
                    Message = ex.Message
                };
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return foo;
        }
        [HttpPost("DeleteMidways")]
        public APIResult DeleteMidways([FromBody] ClassRemoveMidways value)
        {
            APIResult foo;
            string funcName = "DeleteMidways";
            string tmpMsg = "";
            string tmpMsg2 = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            string[] _lstRemoveIdx;
            string _strRemoveIdx = "";
            bool _break = false;
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                if (value.idx.IndexOf(',') > 0)
                {
                    _lstRemoveIdx = value.idx.Split(',');
                }
                else
                {
                    if(value.idx.Equals(""))
                        _lstRemoveIdx = new string[] { };
                    else
                        _lstRemoveIdx = new string[] { value.idx.ToString() };
                }

                if (_lstRemoveIdx.Length > 0)
                {
                    foreach (string tmpIdx in _lstRemoveIdx)
                    {
                        if (_strRemoveIdx.Equals(""))
                        {
                            _strRemoveIdx = string.Format("'{0}'", tmpIdx);
                        }
                        else
                        {
                            _strRemoveIdx = string.Format("{0},'{1}'", _strRemoveIdx, tmpIdx);
                        }
                    }

                    try
                    {
                        //// 查詢資料
                        _dbTool.SQLExec(_BaseDataService.ResetMidways(_strRemoveIdx, true), out tmpMsg, true);

                        if (tmpMsg.Equals(""))
                        {
                            //tmpMsg2 = string.Format("The midways  [{0}/{1}/{2}] has been delete.", _turnOnStageControl.EQUIPID, _turnOnStageControl.PORTID, _turnOnStageControl.STAGE);
                            _break = true;
                        }
                        else
                        {
                            //tmpMsg2 = string.Format("delete stage control failed. Stage is [{0}/{1}/{2}]. Message: {3}", _turnOnStageControl.EQUIPID, _turnOnStageControl.PORTID, _turnOnStageControl.STAGE, tmpMsg);
                            _break = false;
                        }

                        _logger.Info(tmpMsg2);
                    }
                    catch (Exception ex)
                    {
                        //tmpMsg2 = string.Format("delete stage control failed. Stage Control [{1}/{2}/{3}]. [Exception]: {4}", _turnOnStageControl.EQUIPID, _turnOnStageControl.PORTID, _turnOnStageControl.STAGE, ex.Message);
                        _logger.Info(tmpMsg2);
                        _break = false;
                    }
                }
                else
                {
                    if (!value.Workgroup.Equals(""))
                    {
                        if (!value.Stage.Equals(""))
                        {
                            ///remove midways by workgroup, stage 

                            try
                            {
                                //// 查詢資料
                                _dbTool.SQLExec(_BaseDataService.ResetMidways(value.Workgroup, value.Stage, true), out tmpMsg, true);

                                if (tmpMsg.Equals(""))
                                {
                                    //tmpMsg2 = string.Format("The midways  [{0}/{1}/{2}] has been delete.", _turnOnStageControl.EQUIPID, _turnOnStageControl.PORTID, _turnOnStageControl.STAGE);
                                    _break = true;
                                }
                                else
                                {
                                    //tmpMsg2 = string.Format("delete stage control failed. Stage is [{0}/{1}/{2}]. Message: {3}", _turnOnStageControl.EQUIPID, _turnOnStageControl.PORTID, _turnOnStageControl.STAGE, tmpMsg);
                                    _break = false;
                                }

                                _logger.Info(tmpMsg2);
                            }
                            catch (Exception ex)
                            {
                                //tmpMsg2 = string.Format("delete stage control failed. Stage Control [{1}/{2}/{3}]. [Exception]: {4}", _turnOnStageControl.EQUIPID, _turnOnStageControl.PORTID, _turnOnStageControl.STAGE, ex.Message);
                                _logger.Info(tmpMsg2);
                                _break = false;
                            }
                        }
                        else
                        {
                            ///did not remove any thing
                        }
                    }
                    else
                    { //did not remove any thing

                    }
                }

                //
                if (_break.Equals(true))
                {
                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmpMsg2
                    };
                }
                else
                {
                    foo = new APIResult()
                    {
                        Success = true,
                        State = "NG",
                        Message = tmpMsg2
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
                    Message = string.Format("[{0}][Exception: {1}]", funcName, ex.Message)
                };
            }

            return foo;
        }

        [HttpPost("QueryStageControlList")]
        public ActionResult<String> QueryStageControlList([FromBody] StageControlList value)
        {

            string funcName = "QueryStageControlList";
            string tmpMsg = "";
            string strResult = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                //// 查詢資料
                sql = _BaseDataService.QueryStageControlList(value);
                dt = _dbTool.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    string[] tmpStr = new string[dt.Rows.Count];
                    string tmpJson = "";
                    JArray jsonArray = new JArray();
                    JObject jsonObject = new JObject();
                    JObject jsonObj2 = new JObject();

                    //foreach (DataRow row in dt.Rows)
                    tmpJson = JsonConvert.SerializeObject(dt);

                    if (tmpJson.TrimStart().StartsWith("["))
                    {
                        jsonArray = JArray.Parse(tmpJson);
                        // Process the array
                    }
                    else
                    {
                        jsonObject = JObject.Parse(tmpJson);
                        // Process the object
                    }
                    string tmpBBB = "";
                    string _stage = "";
                    string _workgroup = "";
                    string _inErack = "";
                    Dictionary<string, string> tmpDict = new Dictionary<string, string>();
                    Dictionary<string, string> tmpArray; 
                    for (int i = 0; i < jsonArray.Count; i++)
                    {
                        
                        tmpJson = jsonArray[i].ToString().Replace("\r\n","").Replace("\"{","").Replace("}\"", "");
                        tmpDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(tmpJson);

                        try {

                            _stage = tmpDict["STAGE"] is null ? "" :tmpDict["STAGE"].ToString();
                            _workgroup = tmpDict["WORKGROUP"] is null ? "" :tmpDict["WORKGROUP"].ToString();

                            sql = _BaseDataService.QueryWorkgroupSet(_workgroup, _stage);
                            dtTemp = _dbTool.GetDataTable(sql);

                            if (dtTemp.Rows.Count > 0)
                            {
                                _inErack = dtTemp.Rows[0]["in_erack"].ToString();
                            }

                            sql = _BaseDataService.QueryRackByGroupID(_inErack);
                            dtTemp = _dbTool.GetDataTable(sql);

                            if (dtTemp.Rows.Count > 0)
                            {
                                foreach (DataRow drRecord in dtTemp.Rows)
                                {
                                    tmpArray = new Dictionary<string, string>();
                                    tmpArray.Add("erackID", drRecord["erackID"].ToString());
                                    tmpArray.Add("groupID", drRecord["groupID"].ToString());
                                    tmpArray.Add("location", drRecord["location"].ToString());
                                    tmpArray.Add("validCarrierType", drRecord["validCarrierType"].ToString());

                                    //row["destination"] = JObject.Parse(string.Format("[{0}]", JsonConvert.SerializeObject(tmpArray.ToString()))).ToString();
                                    tmpJson = tmpJson.Replace("\"DEST\"", string.Format("{0}", MyDictionaryToJson(tmpArray)));

                                    if (tmpBBB.Equals(""))
                                    {
                                        tmpBBB = string.Format("{0}", tmpJson);
                                    }
                                    else
                                    {
                                        tmpBBB = string.Format("{0},{1}", tmpBBB, tmpJson);
                                    }
                                }
                            }
                        }
                        catch(Exception ex) { }
                    }
                    //strResult = JsonConvert.SerializeObject(tmpStr);
                    strResult = string.Format("[{0}]", tmpBBB);
                }
            }
            catch (Exception ex)
            {
                return strResult;
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return strResult;
        }
        string MyDictionaryToJson(Dictionary<string, string> dict)
        {
            var entries = dict.Select(d =>
                string.Format("\"{0}\": \"{1}\"", d.Key, string.Join(",", d.Value)));
            return "{" + string.Join(",", entries) + "}";
        }

        [HttpPost("InsertStageControl")]
        public APIResult InsertStageControl([FromBody] StageControlList value)
        {
            APIResult foo;
            string funcName = "InsertStageControl";
            string tmpMsg = "";
            string tmpMsg2 = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            StageControlList _stageControl;
            string[] _lstCurrentLocate;
            string _failedCurrentLocate = "";
            string _failedCauses = "";
            int _idx = 1;
            string _currentdatetime = "";
            bool _break = false;
            IBaseDataService _BaseDataService = new BaseDataService();
            string _successInsert = "";
            string _successMsg = "";

            try
            {
                if (value.STAGE.IndexOf(',') > 0)
                {
                    _lstCurrentLocate = value.STAGE.Split(',');
                }
                else
                {
                    _lstCurrentLocate = new string[] { value.STAGE.ToString() };
                }

                _currentdatetime = DateTime.Now.ToString("yyyyMMddHHmmss");

                foreach (string tmpKey in _lstCurrentLocate)
                {
                    tmpMsg = "";
                    _stageControl = new StageControlList();

                    _stageControl.EQUIPID = value.EQUIPID;
                    _stageControl.PORTID = value.PORTID;
                    _stageControl.STAGE = tmpKey;

                    try
                    {
                        sql = _BaseDataService.QueryStageControlAllList(_stageControl);
                        dt = _dbTool.GetDataTable(sql);

                        if(dt.Rows.Count > 0)
                        {
                            //// 查詢資料
                            _dbTool.SQLExec(_BaseDataService.DeleteStageControl(_stageControl, false), out tmpMsg, true);

                            if (tmpMsg.Equals(""))
                            {
                                _failedCauses = string.Format("The Stage Control [{0}/{1}/{2}] has been recovery.", _stageControl.EQUIPID, _stageControl.PORTID, _stageControl.STAGE);
                                _break = false;
                            }
                            else
                            {
                                _failedCauses = string.Format("recovery stage control failed. Stage is [{0}/{1}/{2}]. Message: {3}", _stageControl.EQUIPID, _stageControl.PORTID, _stageControl.STAGE, tmpMsg);
                                _break = true;
                            }
                        }
                        else
                        {
                            //// 查詢資料
                            _dbTool.SQLExec(_BaseDataService.InsertStageControl(_stageControl), out tmpMsg, true);

                            if (tmpMsg.Equals(""))
                            {
                                _failedCauses = string.Format("Insert Stage Control. [{0}/{1}/{2}][Success]", _stageControl.EQUIPID, _stageControl.PORTID, _stageControl.STAGE);
                                _successInsert = _successInsert.Equals("") ? string.Format("{0}", _stageControl.STAGE) : string.Format("{0},{1}", _successInsert, _stageControl.STAGE);
                                _break = false;
                            }
                            else
                            {
                                _failedCauses = string.Format("Insert Stage Control failed. [{0}/{1}/{2}][Error][{3}]", _stageControl.EQUIPID, _stageControl.PORTID, _stageControl.STAGE, tmpMsg);
                                _break = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _failedCauses = string.Format("recovery stage control failed. [{0}/{1}/{2}][Exception][{3}]", _stageControl.EQUIPID, _stageControl.PORTID, _stageControl.STAGE, ex.Message);
                        _break = true;
                    }
                }

                //
                if (!_break)
                {
                    if (_successInsert.Equals(""))
                        _successMsg = "No anything been insert!";
                    else
                        _successMsg = string.Format("Insert Stage Control. [{0}/{1}, {2}][Success]", value.EQUIPID, value.PORTID, _successInsert);

                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = _successMsg
                    };
                }
                else
                {
                    foo = new APIResult()
                    {
                        Success = true,
                        State = "NG",
                        Message = _failedCauses
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
                    Message = ex.Message
                };
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return foo;
        }
        [HttpPost("TurnOnStageControl")]
        public APIResult TurnOnStageControl([FromBody] TurnOnStageControl value)
        {
            APIResult foo;
            string funcName = "TurnOnStageControl";
            string tmpMsg = "";
            string tmpMsg2 = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            TurnOnStageControl _turnOnStageControl;
            StageControlList _stageControl;
            string[] _lstCurrentLocate;
            string _failedCurrentLocate = "";
            string _failedCauses = "";
            string _idxString = "";
            string _theRecordsState = "";
            bool _break = false;
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                if (value.STAGE.IndexOf(',') > 0)
                {
                    _lstCurrentLocate = value.STAGE.Split(',');
                }
                else
                {
                    _lstCurrentLocate = new string[] { value.STAGE.ToString() };
                }

                _turnOnStageControl = new TurnOnStageControl();
                _turnOnStageControl.EQUIPID = value.EQUIPID;
                _turnOnStageControl.PORTID = value.PORTID;
                _stageControl = new StageControlList();
                _stageControl.EQUIPID = value.EQUIPID;
                _stageControl.PORTID = value.PORTID;

                foreach (string tmpKey in _lstCurrentLocate)
                {
                    _turnOnStageControl.STAGE = tmpKey;
                    _stageControl.STAGE = tmpKey;

                    sql = _BaseDataService.QueryStageControlList(_stageControl);
                    dt = _dbTool.GetDataTable(sql);

                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow drTemp in dt.Rows)
                        {
                            _theRecordsState = drTemp["enabled"].ToString().Equals("1") ? "true" : "false";

                            tmpMsg = "";

                            try
                            {
                                if (!_theRecordsState.Equals(value.Enabled))
                                {

                                    //// 查詢資料
                                    _dbTool.SQLExec(_BaseDataService.TurnOnStageControl(_turnOnStageControl, value.Enabled.ToLower().Equals("true") ? true : false), out tmpMsg, true);

                                    if (tmpMsg.Equals(""))
                                    {
                                        tmpMsg2 = string.Format("The Stage Control [{0}/{1}/{2}] has been {3}", _turnOnStageControl.EQUIPID, _turnOnStageControl.PORTID, _turnOnStageControl.STAGE, value.Enabled.ToLower().Equals("true") ? "turn on" : "turn off");
                                        _break = false;
                                    }
                                    else
                                    {
                                        tmpMsg2 = string.Format("Change state[{0}] fail. Midway [{1}/{2}/{3}]. Message: {4}", value.Enabled, _turnOnStageControl.EQUIPID, _turnOnStageControl.PORTID, _turnOnStageControl.STAGE, tmpMsg);
                                        _break = true;
                                    }

                                    _logger.Info(tmpMsg2);
                                }
                            }
                            catch (Exception ex)
                            {
                                tmpMsg2 = string.Format("Change state[{0}] fail. Stage Control [{1}/{2}/{3}]. [Exception]: {4}", value.Enabled, _turnOnStageControl.EQUIPID, _turnOnStageControl.PORTID, _turnOnStageControl.STAGE, ex.Message);
                                _logger.Info(tmpMsg2);
                                _break = true;
                            }

                            if (_break.Equals(true))
                                break;
                            else
                                _turnOnStageControl.STAGE = "";
                        }
                    }

                    if (_break.Equals(true))
                        break;
                    else
                        _turnOnStageControl.STAGE = "";
                }

                //
                if (_break.Equals(false))
                {
                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmpMsg2
                    };
                }
                else
                {
                    foo = new APIResult()
                    {
                        Success = true,
                        State = "NG",
                        Message = tmpMsg2
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
                    Message = ex.Message
                };
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return foo;
        }
        [HttpPost("DeleteStageControl")]
        public APIResult DeleteStageControl([FromBody] StageControlList value)
        {
            APIResult foo;
            string funcName = "DeleteStageControl";
            string tmpMsg = "";
            string tmpMsg2 = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            TurnOnStageControl _turnOnStageControl;
            StageControlList _stageControl;
            string[] _lstCurrentLocate;
            string _failedCurrentLocate = "";
            string _failedCauses = "";
            string _idxString = "";
            string _theRecordsState = "";
            bool _break = false;
            string _removesuccess = "";
            string _removefail = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                if (value.STAGE.IndexOf(',') > 0)
                {
                    _lstCurrentLocate = value.STAGE.Split(',');
                }
                else
                {
                    _lstCurrentLocate = new string[] { value.STAGE.ToString() };
                }

                _turnOnStageControl = new TurnOnStageControl();
                _turnOnStageControl.EQUIPID = value.EQUIPID;
                _turnOnStageControl.PORTID = value.PORTID;
                _stageControl = new StageControlList();
                _stageControl.EQUIPID = value.EQUIPID;
                _stageControl.PORTID = value.PORTID;


                foreach (string tmpKey in _lstCurrentLocate)
                {
                    _turnOnStageControl.STAGE = tmpKey;
                    _stageControl.STAGE = tmpKey;

                    sql = _BaseDataService.QueryStageControlList(_stageControl);
                    dt = _dbTool.GetDataTable(sql);

                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow drTemp in dt.Rows)
                        {
                            tmpMsg = "";

                            try
                            {
                                //// 查詢資料
                                _dbTool.SQLExec(_BaseDataService.DeleteStageControl(_stageControl, true), out tmpMsg, true);

                                if (tmpMsg.Equals(""))
                                {
                                    //tmpMsg2 = string.Format("The Stage Control [{0}/{1}/{2}] has been delete.", _turnOnStageControl.EQUIPID, _turnOnStageControl.PORTID, _turnOnStageControl.STAGE);
                                    if(_removesuccess.Equals(""))
                                        _removesuccess = _stageControl.STAGE;
                                    else
                                        _removesuccess = string.Format("{0},{1}", _removesuccess, _stageControl.STAGE);
                                }
                                else
                                {
                                    //tmpMsg2 = string.Format("delete stage control failed. Stage is [{0}/{1}/{2}]. Message: {3}", _turnOnStageControl.EQUIPID, _turnOnStageControl.PORTID, _turnOnStageControl.STAGE, tmpMsg);
                                    if (_removefail.Equals(""))
                                        _removefail = _stageControl.STAGE;
                                    else
                                        _removefail = string.Format("{0},{1}", _removefail, _stageControl.STAGE);
                                }

                                _logger.Info(tmpMsg2);
                            }
                            catch (Exception ex)
                            {
                                tmpMsg2 = string.Format("delete stage control failed. Stage Control [{1}/{2}/{3}]. [Exception]: {4}", _turnOnStageControl.EQUIPID, _turnOnStageControl.PORTID, _turnOnStageControl.STAGE, ex.Message);
                                _logger.Info(tmpMsg2);
                            }

                            if (_break.Equals(true))
                                break;
                            else
                                _turnOnStageControl.STAGE = "";
                        }

                        _break = true;
                    }

                    _turnOnStageControl.STAGE = "";
                }

                //
                if (_break.Equals(true))
                {
                    if (!_removefail.Equals(""))
                    {
                        tmpMsg2 = string.Format("delete stage control failed. Success [{0}] / Failed[{1}]", _removesuccess, _removefail);
                    }

                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmpMsg2
                    };
                }
                else
                {
                    foo = new APIResult()
                    {
                        Success = true,
                        State = "NG",
                        Message = tmpMsg2
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
                    Message = ex.Message
                };
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return foo;
        }
        [HttpPost("CloneStageControlByEquip")]
        private APIResult CloneStageControlByEquip([FromBody] StageControlList value)
        {
            APIResult foo;
            string funcName = "CloneStageControlByEquip";
            string tmpMsg = "";
            string tmp2Msg = "";
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string tmpKey = "";
            string _stage = "";
            string _userID = "";
            string _tmpNewValue = "";
            string _paramsName = "";
            bool _procesingState = false;

            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {

                _userID = value.UserID.Equals("") ? "-----" : value.UserID;

                dt = _dbTool.GetDataTable(_BaseDataService.QueryStageControlList(value));
                /*
                if (dt.Rows.Count > 0)
                {
                    dtTemp = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(value.Workgroup));

                    if (dtTemp.Rows.Count > 0)
                    {
                        if (tmpMsg.Equals(""))
                        {
                            sql = _BaseDataService.CloneStageForWorkgroupSetByWorkgroupStage(value.newStage, value.Workgroup, value.Stage);
                            _dbTool.SQLExec(sql, out tmpMsg, true);

                            if (tmpMsg.Equals(""))
                            {
                                tmpMsg = string.Format(@"Clone workgroup [{0}] Success. userID [{1}]", value.newWorkgroup, _userID);
                                _procesingState = true;
                            }
                            else
                            {
                                tmpMsg = string.Format(@"Clone workgroup [{0}] Fail. userID [{1}]", value.newWorkgroup, _userID);
                                _procesingState = false;
                            }
                        }
                    }
                }
                else
                {
                    tmpMsg = string.Format("Workgroup {0} not exist.", value.EQUIPID);
                }
                */
                if (_procesingState)
                {
                    _logger.Info(tmpMsg);

                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = tmpMsg
                    };
                }
                else
                {
                    _logger.Info(tmpMsg);

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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

        [HttpPost("ResetProrityForWorkroupSet")]
        public APIResult ResetProrityForWorkroupSet([FromBody] ClassStagePriorityWorkgroupSet value)
        {
            APIResult foo;
            string funcName = "ResetProrityForWorkroupSet";
            string tmpMsg = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql;
            DateTime dtLimit;
            IBaseDataService _BaseDataService = new BaseDataService();
            string WorkgroupID = "";
            string StageID = "";
            int Current_Priority = 0;

            try
            {
                foo = new APIResult()
                {
                    Success = false,
                    State = "NG",
                    Message = tmpMsg
                };

                if (value.Workgroup.Equals(""))
                {
                    //_dbTool.SQLExec(_BaseDataService.UpdateRtdAlarm(value.DateTime), out tmpMsg, true);
                    tmpMsg = "Workgroup can not be empty. please check.";

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };

                    return foo;
                }
                else
                {
                    dt = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(value.Workgroup, value.Stage));

                    if (dt.Rows.Count <= 0)
                    {
                        tmpMsg = string.Format("Stage [{0}] not exist.. please check.", value.Stage);

                        foo = new APIResult()
                        {
                            Success = false,
                            State = "NG",
                            Message = tmpMsg
                        };

                        return foo;
                    }
                    else
                    {
                        if (!dt.Rows[0]["Workgroup"].ToString().Equals(""))
                            WorkgroupID = dt.Rows[0]["Workgroup"].ToString().Equals("") ? "" : dt.Rows[0]["Workgroup"].ToString();

                        if (!dt.Rows[0]["Stage"].ToString().Equals(""))
                            StageID = dt.Rows[0]["Stage"].ToString().Equals("") ? "" : dt.Rows[0]["Stage"].ToString();
                    }
                }

                dt = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(WorkgroupID, StageID));

                if (dt.Rows.Count > 0)
                {
                    Current_Priority = int.Parse(dt.Rows[0]["priority"].ToString());

                    if (!value.Priority.Equals(Current_Priority))
                    {
                        sql = String.Format(_BaseDataService.UpdatePriorityForWorkgroupSet(WorkgroupID, StageID, value.Priority));
                        _dbTool.SQLExec(sql, out tmpMsg, true);

                        if (tmpMsg.Equals(""))
                        {
                            foo = new APIResult()
                            {
                                Success = true,
                                State = "OK",
                                Message = tmpMsg
                            };
                        }
                        else
                        {
                            tmpMsg = string.Format("[{0}] Workgroup update Issue. Workgroup [{1}] stage [{2}] Priority [{3}]. please check.", funcName, WorkgroupID, StageID, value.Priority);
                            foo = new APIResult()
                            {
                                Success = false,
                                State = "NG",
                                Message = tmpMsg
                            };
                        }
                    }
                }
                else
                {
                    tmpMsg = string.Format("[{0}] Workgroup Error. Workgroup [{1}] not exist.", funcName, WorkgroupID);
                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = true,
                    State = "NG",
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

            if (!tmpMsg.Equals(""))
                _logger.Debug(tmpMsg);

            return foo;
        }
        public class ClassStagePriorityWorkgroupSet
        {
            public string Workgroup { get; set; }
            public string Stage { get; set; }
            public int Priority { get; set; }
        }

        [HttpPost("MonitorDatabaseRequest")]
        public APIResult MonitorDatabaseRequest([FromBody] ClassMonitorDatabaseRequest value)
        {
            ///1.0.25.0204.1.0.0
            APIResult foo;
            string funcName = "MonitorDatabaseRequest";
            string tmpMsg = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();
            Dictionary<string, object> _object = new Dictionary<string, object>();

            try
            {
                foo = new APIResult()
                {
                    Success = false,
                    State = "NG",
                    Message = tmpMsg
                };

                if(value.TurnOn.Equals(""))
                {
                    tmpMsg = string.Format("Turn On value cannot empty.");

                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };

                    return foo;
                }

                sql = _BaseDataService.SelectRTDDefaultSet("DebugDBRequest");
                dt = _dbTool.GetDataTable(sql);
                if (dt.Rows.Count > 0)
                {
                    _object.Add(ConstParams.Parameter, "DebugDBRequest");
                    _object.Add(ConstParams.Paramtype, "databaseAccess");
                    _object.Add(ConstParams.Paramvalue, value.TurnOn.ToUpper().Equals("TRUE") ? "True": "False");
                    _object.Add(ConstParams.Modifyby, "RTD");
                    _object.Add(ConstParams.Description, "");

                    sql = _BaseDataService.UadateRTDParameterSet(_object);
                    _dbTool.SQLExec(sql, out tmpMsg, true);

                    strResult = "";
                }
                else
                {
                    _object.Add(ConstParams.Parameter, "DebugDBRequest");
                    _object.Add(ConstParams.Paramtype, "databaseAccess");
                    _object.Add(ConstParams.Paramvalue, "False");
                    _object.Add(ConstParams.Modifyby, "RTD");
                    _object.Add(ConstParams.Description, "");

                    sql = _BaseDataService.InsertRTDParameterSet(_object);
                    _dbTool.SQLExec(sql, out tmpMsg, true);

                    strResult = "";
                }

                if(tmpMsg.Equals(""))
                {
                    foo = new APIResult()
                    {
                        Success = true,
                        State = "OK",
                        Message = strResult
                    };
                }
                else
                {
                    foo = new APIResult()
                    {
                        Success = false,
                        State = "NG",
                        Message = tmpMsg
                    };
                }
            }
            catch (Exception ex)
            {
                foo = new APIResult()
                {
                    Success = false,
                    State = "NG",
                    Message = ex.Message
                };
            }
            finally
            {
                if (dt is not null)
                {
                    dt.Clear(); dt.Dispose(); dt = null; dr = null;
                }
            }

            return foo;
        }
        public class ClassMonitorDatabaseRequest
        {
            public string TurnOn { get; set; }
        }
    }
}
