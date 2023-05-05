using System;

namespace RTDWebAPI.Models
{
    public class SchemaLotInfo
    {
        public string LotID { get; set; }
        public int Priority { get; set; }
        public string Carrier_Asso { get; set; }
        public string Equip_Asso { get; set; }
        public string EquipList { get; set; }
        public string State { get; set; }
        public DateTime Create_Dt { get; set; }
        public DateTime LastModify_Dt { get; set; }
    }
}