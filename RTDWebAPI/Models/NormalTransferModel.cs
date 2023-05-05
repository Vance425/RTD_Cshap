using System;
using System.Collections.Generic;

namespace RTDWebAPI.Models
{
    public class NormalTransferModel
    {
        public string CommandID { get; set; }
        public string CarrierID { get; set; }
        public string EquipmentID { get; set; }
        public string LotID { get; set; }
        public string PortModel { get; set; }
        public int Priority { get; set; }
        public int Replace { get; set; }
        public List<TransferList> Transfer { get; set; }
    }
}