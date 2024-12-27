using System;

namespace RTDWebAPI.Models
{
    public class ClassQueryRTDAlarm
    {
        public int page { get; set; }
        public int recordsLengthOfPage { get; set; }
        public string level { get; set; }
        public string code { get; set; }
        public string sorting_by { get; set; }
        public string sorting_type { get; set; }
        public string start_Dt { get; set; }
        public string last_Update_Dt { get; set; }
    }
}