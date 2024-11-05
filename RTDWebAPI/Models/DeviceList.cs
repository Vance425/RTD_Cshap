using System;
using System.Collections.Generic;

namespace RTDWebAPI.Models
{
    public class DeviceList
    {
        public string DeviceID { get; set; }
        public string DeviceType { get; set; }
        public List<DeviceSlotList> PortInfoList { get; set; }
    }
}
