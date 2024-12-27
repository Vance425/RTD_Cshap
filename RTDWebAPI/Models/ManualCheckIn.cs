using System;

namespace RTDWebAPI.Models
{
    public class ManualCheckIn
    {
        public string CarrierID { get; set; }
        public string LotID { get; set; }
        public string PortID { get; set; }
        public int Quantity { get; set; }
        public int Total { get; set; }
        public string UserID { get; set; }
        public string Pwd { get; set; }
    }
}
