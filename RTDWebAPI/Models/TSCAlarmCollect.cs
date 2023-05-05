using System;

namespace RTDWebAPI.Models
{
    public class TSCAlarmCollect
    {

        public int ALID { get; set; }
        public string ALTX { get; set; }
        public string ALType { get; set; }
        public string ALSV { get; set; }
        public string UnitType { get; set; }
        public string UnitID { get; set; }
        public string Level { get; set; }
        public string SubCode { get; set; }
    }
}