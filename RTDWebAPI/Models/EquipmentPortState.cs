using System;
using System.Collections.Generic;

namespace RTDWebAPI.Models
{
    public class EquipmentPortState
    {
        public string EqID { get; set; }
        public List<EquipmentSlotInfo> PortInfoList { get; set; }

    }
}
