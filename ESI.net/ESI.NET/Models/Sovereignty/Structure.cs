using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Sovereignty
{
    public class Structure
    {
        [JsonProperty("alliance_id")]
        public int AllianceId { get; set; }

        [JsonProperty("solar_system_id")]
        public int SolarSystemId { get; set; }

        [JsonProperty("structure_id")]
        public long Id { get; set; }

        [JsonProperty("structure_type_id")]
        public int TypeId { get; set; }

        [JsonProperty("vulnerability_occupancy_level")]
        public double VulnerabilityOccupancyLevel { get; set; }

        [JsonProperty("vulnerable_end_time")]
        public DateTime VulnerableEndTime { get; set; }

        [JsonProperty("vulnerable_start_time")]
        public DateTime VulnerableStartTime { get; set; }
    }
}
