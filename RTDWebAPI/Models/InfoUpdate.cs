using System;

namespace RTDWebAPI.Models
{
    public class InfoUpdate
    {
        public string LotID { get; set; }
        public string Stage { get; set; }
        public string CarrierID { get; set; }
        public string Cust { get; set; }
        public string PartID { get; set; }
        public string LotType { get; set; }
        public string Automotive { get; set; }
        public string State { get; set; }
        public string HoldCode { get; set; }
        public float TurnRatio { get; set; }
        public string EOTD { get; set; }
        public string WaferLot { get; set; }
        public string HoldReas { get; set; }
        public string POTD { get; set; }
        public bool Force { get; set; }
    }
}
