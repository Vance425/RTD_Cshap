using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTDWebAPI.Models
{
    public class UserModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Department { get; set; }
        public string Role { get; set; }
    }
}
