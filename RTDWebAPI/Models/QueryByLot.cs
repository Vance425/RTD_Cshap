using System;

namespace RTDWebAPI.Models
{
    public class QueryByLot
    {
        public string LotId { get; set; }
        private bool _rework = false;
        public bool Rework { get { return _rework; } set { _rework = value; } }
    }
}
