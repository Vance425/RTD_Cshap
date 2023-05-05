using System;

namespace RTDWebAPI.Models
{
    public class AlarmText
    {
        public string AlarmCode { get; set; }
        public string SubCode { get; set; }
        public string Type { get; set; }
        public string AlarmText2 { get; set; }
        public string Description { get; set; }
        public string CreatedAt { get; set; }
        public string LastStateTime { get; set; }
    }
}