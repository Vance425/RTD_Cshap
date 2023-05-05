using System;
using System.Collections.Generic;

namespace RTDWebAPI.Models
{
    public class AEIEQInfo
    {
        public string EqID { get; set; }
        public int EqState { get; set; }
        public int EqAlarmID { get; set; }
        public string EqAlarmText { get; set; }
        public string EqInfo { get; set; }
        public int PortNum { get; set; }
        public List<AEIPortInfo> PortInfoList { get; set; }

    }
}
