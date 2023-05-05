using System;

namespace RTDWebAPI.Models
{
    public class SetDatabase
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Pwd { get; set; }
        public string ConnectionString { get; set; }
        public string ProviderName { get; set; }
    }
}
