using System;

namespace RTDWebAPI.Models
{
    public class HisCommandStatus
    {
        public string CommandID { get; set; }
        public string CarrierID { get; set; }
        public string LotID { get; set; }
        public string CommandType { get; set; }
        public string Source { get; set; }
        public string Dest { get; set; }
        public string AlarmCode { get; set; }
        public string Reason { get; set; }
        public string CreatedAt { get; set; }
        public string LastStateTime { get; set; }
    }
}