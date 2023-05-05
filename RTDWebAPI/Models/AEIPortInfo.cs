using System;
using System.Collections.Generic;

namespace RTDWebAPI.Models
{
    public class AEIPortInfo
    {
        public string PortID { get; set; }
        public int PortTransferState { get; set; }
        public string CarrierID { get; set; }
        public string LotID { get; set; }
        public int Quantity { get; set; }
        public int PortAlarmID { get; set; }
        public string PortAlarmText { get; set; }
        public int AccessMode { get; set; }
        public int ClampState { get; set; }
    }
}
