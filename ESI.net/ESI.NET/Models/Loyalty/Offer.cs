using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models.Loyalty
{
    public class Offer
    {
        [JsonProperty("offer_id")]
        public int OfferId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("lp_cost")]
        public int LpCost { get; set; }

        [JsonProperty("isk_cost")]
        public long IskCost { get; set; }

        [JsonProperty("ak_cost")]
        public int AkCost { get; set; }

        [JsonProperty("required_items")]
        public List<Item> RequiredItems { get; set; } = new List<Item>();
    }

    public class Item
    {
        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
}
