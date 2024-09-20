using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nancy.Json;
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
            string resultMsg = "";
            string _paramsName = "";

            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                _paramsName = "SetWorkgroupSetByWorkgroupStage";

                _userID = value.UserID.Equals("") ? "-----" : value.UserID;

                dt = _dbTool.GetDataTable(_BaseDataService.QueryWorkgroupSet(value.Workgroup));

                if (dt.Rows.Count > 0)
                {
                    Boolean enableFunction = false;

                    if (!value.Workgroup.Equals(""))
                        tmpKey = string.Format("workgroup='{0}'", value.Workgroup);

                    if (!value.Stage.Equals(""))
                        tmpKey = string.Format("{0} and stage='{1}'", tmpKey, value.Stage);

                    if (!value.Parameter.Equals(""))
                        _paramsName = string.Format("{0}", value.Parameter.Trim());

                    if (!tmpKey.Equals(""))
                    {
                        sql = _BaseDataService.SetWorkgroupSetByWorkgroupStage(value.Workgroup, value.Stage, value.Parameter, true);
                        dtTemp = _dbTool.GetDataTable(sql);

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
        public class ClassWorkgroupSetting
        {
            public string Workgroup { get; set; }
            public string Stage { get; set; }
            public string Parameter { get; set; }
            public bool SwState { get; set; }
            public string UserID { get; set; }
        }
    }
}
