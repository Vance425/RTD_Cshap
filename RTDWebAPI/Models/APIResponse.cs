using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace RTDWebAPI.Models
{
    public class APIResponse
    {
        public APIResponse(string json)
        {
            JObject jObject = JObject.Parse(json);

            Success = (bool)jObject["Success"];
            State = (string)jObject["State"];
            ErrorCode = (int)jObject["ErrorCode"];
            Message = (string)jObject["Message"];

            if(jObject["Data"] is not null)
                Data = JObject.Parse(jObject["Data"].ToString());
        }

        public bool Success { get; set; }
        public string State { get; set; }
        public int ErrorCode { get; set; }
        public string Message { get; set; }
        public JObject Data { get; set; }

    }

    public class AoiKeyValue
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}