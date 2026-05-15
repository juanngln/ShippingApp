using System;

namespace ShippingAppPallet
{
    [Serializable]
    public class ShipmentPlanRow
    {
        public string BPCode { get; set; }
        public string Item { get; set; }
        public string Description { get; set; }
        public decimal? QTY { get; set; }
        public string SO { get; set; }
        public string Position { get; set; }
        public int? Sequence { get; set; }
        public decimal? SalesPrice { get; set; }
    }

    [Serializable]
    public class ItemInfo
    {
        public string Description { get; set; }
        public decimal? SalesPrice { get; set; }
    }
}
