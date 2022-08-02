using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.PlanetaryInteraction
{
    public class Planet
    {
        [JsonProperty("last_update")]
        public DateTime LastUpdate { get; set; }

        [JsonProperty("num_pins")]
        public int NumberOfPins { get; set; }

        [JsonProperty("owner_id")]
        public long OwnerId { get; set; }

        [JsonProperty("planet_id")]
        public long PlanetId { get; set; }

        [JsonProperty("planet_type")]
        public string PlanetType { get; set; }

        [JsonProperty("solar_system_id")]
        public long SolarSystemId { get; set; }

        [JsonProperty("upgrade_level")]
        public int UpgradeLevel { get; set; }
    }
}
