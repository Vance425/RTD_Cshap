using System;

namespace RTDWebAPI.Models
{
    public class SchemaWorkInProcessSch
    {
        public string UUID { get; set; }
        public string Cmd_Id { get; set; }
        public string LotID { get; set; }
        public string Customer { get; set; }
        public int Priority { get; set; }
        public int Replace { get; set; }
        public string Cmd_Type { get; set; }
        public string EquipId { get; set; }
        public string Cmd_State { get; set; }
        public string Cmd_Current_State { get; set; }
        public string CarrierId { get; set; }
        public string CarrierType { get; set; }
        public string Source { get; set; }
        public string Dest { get; set; }
        public int Quantity { get; set; }
        public int Total { get; set; }
        public int IsLastLot { get; set; }
        public string Back { get; set; }
        public DateTime Initial_Dt { get; set; }
        public DateTime WaitingQueue_D { get; set; }
        public DateTime ExecuteQueue_Dt { get; set; }
        public DateTime Completed_Dt { get; set; }
        public DateTime LastModify_Dt { get; set; }
        public DateTime Start_Dt { get; set; }
    }
}