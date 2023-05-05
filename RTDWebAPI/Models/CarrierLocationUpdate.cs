using System;

namespace RTDWebAPI.Models
{
    public class CarrierLocationUpdate
    {
        public string CarrierID { get; set; }
        public string Zone { get; set; }
        public string Location { get; set; }
        public string LocationType { get; set; }
        public string TransferState { get; set; }
        public string AlarmCode { get; set; }
        public string CreatedAt { get; set; }
        public string LastStateTime { get; set; }
    }
}