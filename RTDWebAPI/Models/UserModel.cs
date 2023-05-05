using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTDWebAPI.Models
{
    public class AvailableQualifiedTesterMachine
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string LotId { get; set; }
        public string Mode { get; set; }
    }
}
