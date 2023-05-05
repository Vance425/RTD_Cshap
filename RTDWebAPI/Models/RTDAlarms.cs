using System;

namespace RTDWebAPI.Models
{
    public class RTDAlarms
    {
        public string UnitType { get; set; }
        public string UnitID { get; set; }
        public string Level { get; set; }
        public int Code { get; set; }
        public string Cause { get; set; }
        public string SubCode { get; set; }
        public string Detail { get; set; }
        public string CommandID { get; set; }
        public string Params { get; set; }
        public string Description { get; set; }
        public int New { get; set; }
        public string CreateAt { get; set; }
        public string lastUpdated { get; set; }
        public string EventTrigger { get; set; }
    }
}