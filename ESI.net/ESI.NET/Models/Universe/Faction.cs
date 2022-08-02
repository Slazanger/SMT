using Newtonsoft.Json;

namespace ESI.NET.Models.Universe
{
    public class Faction
    {
        [JsonProperty("faction_id")]
        public int FactionId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("solar_system_id")]
        public int SolarSystemId { get; set; }

        [JsonProperty("corporation_id")]
        public int CorporationId { get; set; }

        [JsonProperty("militia_corporation_id")]
        public int MilitiaCorporationId { get; set; }

        [JsonProperty("size_factor")]
        public decimal SizeFactor { get; set; }

        [JsonProperty("station_count")]
        public int StationCount { get; set; }

        [JsonProperty("station_system_count")]
        public int StationSystemCount { get; set; }

        [JsonProperty("is_unique")]
        public bool IsUnique { get; set; }
    }
}