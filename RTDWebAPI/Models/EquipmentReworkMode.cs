using System;

namespace RTDWebAPI.Models
{
    public class EquipmentReworkMode
    {
        public string EquipID { get; set; }
        private bool _rework = false;
        public bool ReworkMode { get { return _rework; } set { _rework = value; } }
    }
}
