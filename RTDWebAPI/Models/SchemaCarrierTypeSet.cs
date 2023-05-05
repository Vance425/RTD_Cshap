using System;

namespace RTDWebAPI.Models
{
    public class SchemaCarrierTypeSet
    {
        public string Type_Key { get; set; }
        public string Carrier_Type { get; set; }
        public string Command_Type { get; set; }
        public DateTime Create_Dt { get; set; }
        public DateTime Modify_Dt { get; set; }
        public DateTime LastModify_Dt { get; set; }
    }
}