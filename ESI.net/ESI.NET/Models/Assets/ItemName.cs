using Newtonsoft.Json;

namespace ESI.NET.Models.Assets
{
    public class ItemName
    {
        [JsonProperty("item_id")]
        public long ItemId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
