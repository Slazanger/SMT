using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models.Fittings
{
    public class Fitting
    {
        [JsonProperty("fitting_id")]
        public int FittingId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("ship_type_id")]
        public int ShipTypeId { get; set; }

        [JsonProperty("items")]
        public List<Item> Items { get; set; } = new List<Item>();
    }

    public class Item
    {
        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("flag")]
        public string Flag { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
}
