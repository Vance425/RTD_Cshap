using System;

namespace RTDWebAPI.Models
{
    public class SchemaCarrierLotAssociate
    {
        public string CarrierId { get; set; }
        public string Tag_Type { get; set; }
        public string Carrier_Type { get; set; }
        public string Associate_State { get; set; }
        public DateTime Change_State_Time { get; set; }
        public string Lot_Id { get; set; }
        public int Quantity { get; set; }
        public string Change_Station { get; set; }
        public string Change_Station_Type { get; set; }
        public DateTime Last_Associate_State { get; set; }
        public string Last_Associate_Station { get; set; }
        public string Last_Associate_Station_Type { get; set; }
        public string Update_By { get; set; }
        public DateTime Update_Time { get; set; }
        public string Description { get; set; }
    }
}