using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    //[ApiController]
    //[Route("[controller]")]
    public class GetLotInfo : BasicController
    {

        private readonly ILogger<GetLotInfo> _logger;
        private readonly DBTool _dbTool;

        public GetLotInfo(ILogger<GetLotInfo> logger, List<DBTool> lstDBSession)
        {
            _logger = logger;
            _dbTool = (DBTool)lstDBSession[1];
        }

        [HttpPost]
        public string Post([FromBody] CommandContent value)
        {
            APIResult foo;
            string funcName = "GetLotInfo";
            string tmpMsg = "";
            string strResult = "";
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            try
            {
                ////// 查詢資料
                //dt = _dbTool.GetDataTable(_BaseDataService.SelectTableLotInfo());
                //dr = dt.Select();
                //if (dr.Length <= 0)
                //{

                //}
                //else
                //{
                //    strResult = DataTableToJsonWithJsonNet(dt);
                //}


            }
            catch (Exception ex)
            {
                return strResult;
            }
            finally
            {
                //_logger.LogInformation(string.Format("Info :{0}", value.CarrierID));
                dt.Clear(); dt.Dispose(); dt = null;
                dr = null;
            }

            _logger.LogInformation(string.Format("Info:{0}",tmpMsg));
            _logger.LogWarning(string.Format("Warning:{0}", tmpMsg));
            _logger.LogError(string.Format("Error:{0}", tmpMsg));
            _logger.LogDebug(string.Format("Debug:{0}", tmpMsg));
            _logger.LogCritical(string.Format("Critical:{0}", tmpMsg));

            //string sql = "select * from gyro_lot_carrier_associate";
            //DataSet ds = dbPool.GetDataSet(sql);

            return strResult;
        }
        public string DataTableToJsonWithJsonNet(DataTable table)
        {
            string JsonString = string.Empty;
            JsonString = JsonConvert.SerializeObject(table);
            return JsonString;

        }
    }
}
