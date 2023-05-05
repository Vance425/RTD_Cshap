using System;

namespace RTDWebAPI.Models
{
    public class SchemaWorkGroup
    {
        public string WorkGroup { get; set; }
        public string In_Erack { get; set; }
        public string Out_Erack { get; set; }
        public DateTime Create_Dt { get; set; }
        public DateTime Modify_Dt { get; set; }
        public DateTime LastModify_Dt { get; set; }
    }
}