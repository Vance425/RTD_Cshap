using System;

namespace RTDWebAPI.Models
{
    public class JcetAlarm
    {
        public bool eMail { get; set; }

        public bool SMS { get; set; }

        public bool repeat { get; set; }

        public int hours { get; set; }
        public int mints { get; set; }
        public bool action { get; set; }
        public string scenario { get; set; }
        public string EquipID { get; set; }
        public string PortID { get; set; }
        public string Stage { get; set; }
        public string Lot { get; set; }
        public string LastLot { get; set; }
        public string MfgDeviceforlastLot { get; set; }
        public string CustDeviceforlastLot { get; set; }
        public string NextLot { get; set; }
        public string MfgDevicefornextLot { get; set; }
        public string CustDevicefornextLot { get; set; }
        public string lotid { get; set; }
        public string nextlot { get; set; }
        public string stage { get; set; }
        public string partid { get; set; }
        public string nextpart { get; set; }
        public string customername { get; set; }
        public string result { get; set; }
    }
}
