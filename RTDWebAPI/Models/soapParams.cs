using System;

namespace RTDWebAPI.Models
{
    public class soapParams
    {
        public objHead Head { get; set; }
        public objBody Body { get; set; }
    }
    public class objHead
    {
        public string MethodCode { get; set; }
        public objSecurity Security { get; set; }
    }
    public class objBody
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string LotId { get; set; }
    }
    public class objSecurity
    {
        public string Token { get; set; }
    }
}