using System;

namespace RTDWebAPI.Models
{
    public class RTDDefaultSet
    {
        public string Parameter { get; set; }
        public string ParamType { get; set; }
        public string ParamValue { get; set; }
        public string ModifyBy { get; set; }
        public DateTime LastModify_DT { get; set; }
        public string Description { get; set; }
    }
}
