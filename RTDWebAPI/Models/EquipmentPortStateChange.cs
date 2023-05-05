using System;

namespace RTDWebAPI.Models
{
    public class EquipmentPortStateChange
    {
        public bool PortChange { get; set; }
        public string PortID { get; set; }
        public string PortSeq { get; set; }
        public string EquipID { get; set; }
        public int PortStateCode { get; set; }
        public string AlarmCode { get; set; }
    }
}
