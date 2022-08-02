using Newtonsoft.Json;

namespace ESI.NET.Models.Universe
{
    public class Constellation
    {
        [JsonProperty("constellation_id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("position")]
        public Position Position { get; set; }

        [JsonProperty("region_id")]
        public long RegionId { get; set; }

        [JsonProperty("systems")]
        public long[] Systems { get; set; }
    }
}
