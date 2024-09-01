using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RTDWebAPI.Commons.Method.Database;
using RTDWebAPI.Commons.Method.WSClient;
using RTDWebAPI.Interface;
using RTDWebAPI.Models;
using RTDWebAPI.Service;
using ServiceStack.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace RTDWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FunctionTest : BasicController
    {
        private readonly IFunctionService _functionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly DBTool _dbTool;

        public FunctionTest(IConfiguration configuration, ILogger logger, IFunctionService functionService, DBTool dbTool)
        {
            _logger = logger;
            _configuration = configuration;
            _functionService = functionService;
            _dbTool = dbTool;
        }

        [HttpPost("CallRTDAlarmSet")]
        public Boolean CallRTDAlarmSet([FromBody] classAlarmCode value)
        {
            string funcName = "CallRTDAlarmSet";
            string tmpMsg = "";
            string strResult = "";
            bool bResult = false;
            DataTable dt = null;
            DataRow[] dr = null;
            string sql = "";
            IBaseDataService _BaseDataService = new BaseDataService();

            JCETWebServicesClient jcetWebServiceClient = new JCETWebServicesClient();
            JCETWebServicesClient.ResultMsg resultMsg = new JCETWebServicesClient.ResultMsg();
            string _args = "";
            string _floor = "";
            string _id = "";
            string _portid = "";

            try
            {

                jcetWebServiceClient = new JCETWebServicesClient();

                resultMsg = new JCETWebServicesClient.ResultMsg();
                resultMsg = jcetWebServiceClient.UIAppClient("workstations", _configuration, _args);
                string result3 = resultMsg.retMessage;
                string tmpStr = "";

                //JObject oToken = JObject.Parse(result3);
                JArray array = JArray.Parse(result3);

                foreach (JObject objx in array.Children<JObject>())
                {
                    List<string[]> ls = new();
                    foreach (JProperty singleProp in objx.Properties())
                    {
                        if (singleProp.Name.Contains("floor"))
                        {
                            string[] val = new string[2];
                            val[0] = singleProp.Name;
                            val[1] = singleProp.Value.ToString();
                            ls.Add(val);
                        }

                        if (singleProp.Name.Contains("stations"))
                        {
                            string tmpp = @"[
    {
      ""alarm"": false,
      ""bufConstrain"": false,
      ""carrierID"": ""Unknown"",
      ""enabled"": true,
      ""equipmentID"": ""CTLG-01"",
      ""equipmentState"": ""PM"",
      ""from"": """",
      ""id"": ""CTLG-01_LP1"",
      ""msg"": """",
      ""openDoorAssist"": false,
      ""portID"": ""CTLG-01_LP1"",
      ""preDispatch"": 0,
      ""return"": ""ERT03|ERT04"",
      ""stage"": """",
      ""state"": ""Unknown"",
      ""type"": ""LotIn&LotOut"",
      ""validInput"": true,
      ""zoneID"": ""zone2""
    }]";
                            //var objWorkstation = JsonSerializer.Deserialize<classStation>(singleProp.Value.ToString());

                            //string json = @"{""key1"":""value1"",""key2"":""value2""}";

                            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(singleProp.Value.ToString());

                            tmpStr = singleProp.Value.ToString();

                            JArray array2 = JArray.Parse(tmpStr);

                            for(int i = 0; i < array2.Count; i++)
                            {
                                try {
                                    var varObj = array2[i];
                                    var itemProperties = varObj.Children<JProperty>();

                                    //Newtonsoft.Json反序列化
                                    string json = @"{ 'Name':'C#','Age':'3000','ID':'1','Sex':'女'}";
                                    classWorkstation descJsonStu = JsonConvert.DeserializeObject<classWorkstation>(tmpp);//反序列化
                                }
                                catch(Exception ex) { }
                            }

                            foreach (JObject obj2 in array2.Children<JObject>())
                            {
                                //var itemProperties = obj2.Children<JProperty>();

                                //var myElement = itemProperties.FirstOrDefault(x => x.Name == "id");

                                //var myElementValue = myElement.Value; ////This is a JValue type

                                //foreach (JProperty obj2Prop in obj2.Properties())
                                //    if (obj2Prop.Name.Contains("id"))
                                ///{
                                //    _id = obj2Prop.Name;
                                //    _portid = "";
                                //}
                                //else
                                //{
                                //    Console.WriteLine("");
                                //}
                                List<string[]> ls2 = new();
                                //foreach (JProperty singleProp2 in obj2.Properties())
                                //{
                                    ///if (singleProp2.Name.Contains("id"))
                                    //{
                                        //string[] val2 = new string[2];
                                        //val2[0] = singleProp2.Name;
                                        //val2[1] = singleProp2.Value.ToString();

//                                    }
  //                              }
                            }

                            Console.WriteLine(String.Join("-", array2.Where(i => (int)i > 1).Select(i => i.ToString())));
                        }
                    }
                }

                //Console.WriteLine(oToken.);

                /*
                _logger.Debug(string.Format("[UnloadCarrierType] PORT_ID [{0}]", "3AOI29-R_LP2"));
                string tmpCarrier = _functionService.GetCarrierByPortId(_dbTool, "3AOI29-R_LP2");
                _logger.Debug(string.Format("[UnloadCarrierType] tmpSQL [{0}]", tmpCarrier));
                sql = _BaseDataService.SelectTableCarrierAssociateByCarrierID(tmpCarrier);
                _logger.Debug(string.Format("[UnloadCarrierType] SQL [{0}]", sql));
                dt = _dbTool.GetDataTable(sql);

                string carrierfeature = "";
                string carrierType = "";

                carrierfeature = value.CommandID.Substring(0, 4);
                if (carrierfeature.Equals("EW12") || carrierfeature.Equals("EW13"))
                    carrierType = "HD";
                else if (carrierfeature.Equals("EWLB"))
                    carrierType = "HD";  //Foup to HD
                if (carrierfeature.Equals("12SL") || carrierfeature.Equals("13SL") || carrierfeature.Equals("25SL"))
                    carrierType = "Foup";
                else if (value.CommandID.Substring(0, 3).Equals("EFA"))
                    carrierType = "Foup";
                else
                    carrierType = "";

                return true;
                */
                /*
                string equipid = "EQP123";
                string conditions = "";
                sql = _BaseDataService.QueryReserveStateByEquipid(equipid);
                conditions = "EQP234,2023/03/30 09:00:00,2023/03/30 10:59:59,400973";
                sql = _BaseDataService.InsertEquipReserve(conditions);
                _dbTool.SQLExec(sql, out tmpMsg, true);
                conditions = "SETTIME,EQP234,400973,2023/03/30 09:00:00,2023/03/30 09:59:59";
                sql = _BaseDataService.UpdateEquipReserve(conditions);

                sql = _BaseDataService.SelectAvailableCarrierByCarrierType("Foup", false);

                dt = _dbTool.GetDataTable(_BaseDataService.QueryRackByGroupID("ERT01"));


                if(dt.Rows.Count > 0)
                {
                    bResult = false;
                }

                if(_functionService.AutoHoldForDispatchIssue(_dbTool, _configuration, _logger, out tmpMsg))
                {
                    bResult = true;
                    return bResult;
                }

                string[] tmpString = new string[] {value.CommandID, "", ""};
                if (_functionService.CallRTDAlarm(_dbTool, int.Parse(value.AlarmCode), tmpString))
                {
                    bResult = true;
                }
                */
            }
            catch (Exception ex)
            {
                bResult = false;
            }
            finally
            {
                //Do Nothing
            }

            return bResult;
        }
        public class classAlarmCode
        {
            public string AlarmCode { get; set; }
            public string CommandID { get; set; }
        }
        public class classStation
        { 
            public List<classWorkstation> lstStation { get; set; }
        }
        public class classWorkstation
        {
            public bool alarm { get; set; }
            public bool bufconstrain { get; set; }
            public string carrierID { get; set; }
            public bool enabled { get; set; }
            public string equipmentID { get; set; }
            public string equipmentState { get; set; }
            public string from { get; set; }
            public string id { get; set; }
            public string msg { get; set; }
            public bool openDoorAssist { get; set; }
            public string portID { get; set; }
            public string return2 { get; set; }
            public string stage { get; set; }
            public string state { get; set; }
            public string type { get; set; }
            public bool validInput { get; set; }
            public string zoneID { get; set; }
        }
    }
}
