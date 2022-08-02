using Newtonsoft.Json;

namespace ESI.NET.Models.Universe
{
    public class Ancestry
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("bloodline_id")]
        public int BloodlineId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("short_description")]
        public string ShortDescription { get; set; }

        [JsonProperty("icon_id")]
        public int IconId { get; set; }

    }
}
