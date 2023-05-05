using System;

namespace RTDWebAPI.Models
{
    public class SchemaEqpPortSet
    {
        public string EquipID { get; set; }
        public string Port_Model { get; set; }
        public string Port_Seq { get; set; }
        public string Port_Type { get; set; }
        public string Port_ID { get; set; }
        public string Carrier_Type { get; set; }
        public string Near_Stocker { get; set; }
        public string Port_State { get; set; }
        public string WorkGroup { get; set; }
        public DateTime Create_Dt { get; set; }
        public DateTime Modify_Dt { get; set; }
        public DateTime LastModify_Dt { get; set; }
    }
}