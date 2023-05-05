using System;

namespace RTDWebAPI.Models
{
    public class TransferList
    {
        public string Source { get; set; }
        public string Dest { get; set; }
        public string LotID { get; set; }
        public int Total { get; set; }
        public string CarrierID { get; set; }
        public int Quantity { get; set; }
        public int IsLastLot { get; set; }
        public string CarrierType { get; set; }
        public string CommandType { get; set; }
    }
}