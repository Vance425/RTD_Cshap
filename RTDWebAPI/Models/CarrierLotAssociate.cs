using System;

namespace RTDWebAPI.Models
{
    public class CarrierLotAssociate
    {
        public string CarrierID { get; set; }
        public string TagType { get; set; }
        public string CarrierType { get; set; }
        public string AssociateState { get; set; }
        public string ChangeStateTime { get; set; }
        public string LotID { get; set; }
        public string Quantity { get; set; }
        public string ChangeStation { get; set; }
        public string ChangeStationType { get; set; }
        public string UpdateBy { get; set; }
        public string UpdateTime { get; set; }
        public string CreateBy { get; set; }
        public string NewBind { get; set; }
    }
}