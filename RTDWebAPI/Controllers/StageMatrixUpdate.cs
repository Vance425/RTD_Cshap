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

namespace RTDWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]

    public class StageMatrixUpdate : BasicController
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
        private readonly DBTool _dbTool;
        private readonly ConcurrentQueue<EventQueue> _eventQueue;
        private readonly List<DBTool> _lstDBSession;

        public StageMatrixUpdate(ILogger logger, IConfiguration configuration, List<DBTool> lstDBSession, ConcurrentQueue<EventQueue> eventQueue)
        {
            _logger = logger;
            _configuration = configuration;
            //_dbTool = (DBTool) lstDBSession[1];
            _eventQueue = eventQueue;
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

        [HttpPost]
        public APIResult Post([FromBody] LstStageMatrix value)
        {
            APIResult foo;
            IBaseDataService _BaseDataService = new BaseDataService();
            IFunctionService _functionService = new FunctionService();

            string funcName = "StageMatrixUpdate";
            string tmpMsg = "";
            EventQueue _eventQ = new EventQueue();
            _eventQ.EventName = funcName;
            DataTable dt = null;
            DataTable dtTemp = null;
            DataRow[] dr = null;
            string sql = "";
            string eqState = "";
            int _enable = 0;

            foo = new APIResult();
            foo.Success = false;
            foo.State = "OK";
            foo.Message = "";

            try
            {
                var jsonStringName = new JavaScriptSerializer();
                var jsonStringResult = jsonStringName.Serialize(value);
                _logger.Info(string.Format("Function:{0}, Received:[{1}]", funcName, jsonStringResult));

                foreach(StageMatrix stageMatrix in value.ListStageMatrix)
                {
                    sql = string.Format(_BaseDataService.QueryEQUIP_MATRIX(stageMatrix.EquipID, stageMatrix.Stage));
                    dt = _dbTool.GetDataTable(sql);

                    if(dt.Rows.Count > 0)
                    {
                        _enable = int.Parse(dt.Rows[0]["enable"].ToString());
                        if (!_enable.Equals(stageMatrix.enableBTN))
                        {
                            tmpMsg = string.Format("{0}, {1}, {2}", stageMatrix.EquipID, stageMatrix.Stage, stageMatrix.enableBTN.ToString());

                            if(stageMatrix.enableBTN.Equals(1))
                                _dbTool.SQLExec(_BaseDataService.UpdateTableEQUIP_MATRIX(stageMatrix.EquipID, stageMatrix.Stage, true), out tmpMsg, true);
                            if (stageMatrix.enableBTN.Equals(0))
                                _dbTool.SQLExec(_BaseDataService.UpdateTableEQUIP_MATRIX(stageMatrix.EquipID, stageMatrix.Stage, false), out tmpMsg, true);

                            Console.WriteLine(tmpMsg);
                        }
                    }
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
        public class StageMatrix
        {
            public string Stage { get; set; }
            public string EquipID { get; set; }
            public int enableBTN { get; set; }
        }
        public class LstStageMatrix
        {
            public List<StageMatrix> ListStageMatrix { get; set; }
        }
    }
}
