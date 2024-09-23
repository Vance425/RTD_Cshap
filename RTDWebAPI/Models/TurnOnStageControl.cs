using System;

namespace RTDWebAPI.Models
{
    public class TurnOnStageControl
    {
        public string EQUIPID { get; set; }
        public string PORTID { get; set; }
        public string STAGE { get; set; }
        public string Enabled { get; set; }
        public string UserID { get; set; }
    }
}
