using System;

namespace RTDWebAPI.Models
{
    public class SchemaCarrierTransfer
    {
        public string Carrier_Id { get; set; }
        public string Type_Key { get; set; }
        public string Carrier_State { get; set; }
        public string Locate { get; set; }
        public string PortNo { get; set; }
        public int Enable { get; set; }
        public DateTime Create_Dt { get; set; }
        public DateTime Modify_Dt { get; set; }
        public DateTime LastModify_Dt { get; set; }
    }
}