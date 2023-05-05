using System;

namespace RTDWebAPI.Models
{
    public class EventQueue
    {
        public string EventName { get; set; }
        public object EventObject { get; set; }
    }
}
