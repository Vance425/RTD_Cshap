using System;

namespace RTDWebAPI.Models
{
    public class EquipmentStatusChange
    {
        public string EquipID { get; set; }
        public string PortNum { get; set; }
        public bool StatusChange { get; set; }
        public int StatusCode { get; set; }
        public string AlarmCode { get; set; }
    }
}
