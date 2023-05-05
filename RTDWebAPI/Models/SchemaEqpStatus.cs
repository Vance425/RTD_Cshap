using System;

namespace RTDWebAPI.Models
{
    public class SchemaEqpStatus
    {
        public string EQUIPID { get; set; }
        public string EQUIP_DEPT { get; set; }
        public string EQUIP_TYPE { get; set; }
        public string EQUIP_TYPEID { get; set; }
        public string MACHINE_STATE { get; set; }
        public string CURR_STATUS { get; set; }
        public string DOWN_STATE { get; set; }
        public string PORT_MODEL { get; set; }
        public int PORT_NUMBER { get; set; }
        public string NEAR_STOCKER { get; set; }
        public string WORKGROUP { get; set; }
        public DateTime Create_Dt { get; set; }
        public DateTime Modify_Dt { get; set; }
        public DateTime LastModify_Dt { get; set; }
    }
}