using Newtonsoft.Json;

namespace ESI.NET.Models.Location
{
    public class Ship
    {
        [JsonProperty("ship_type_id")]
        public int ShipTypeId { get; set; }

        [JsonProperty("ship_item_id")]
        public long ShipItemId { get; set; }

        [JsonProperty("ship_name")]
        public string ShipName { get; set; }
    }
}
