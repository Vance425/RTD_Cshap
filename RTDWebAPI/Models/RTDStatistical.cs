using System;

namespace RTDWebAPI.Models
{
    public class RTDStatistical
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Times { get; set; }
        public string Type { get; set; }
    }
}
