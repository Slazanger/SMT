using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ESI.NET.Models.Universe
{
    public class SolarSystem
    {
        [JsonProperty("star_id")]
        public int StarId { get; set; }

        [JsonProperty("system_id")]
        public int SystemId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("position")]
        public Position Position { get; set; }

        [JsonProperty("security_status")]
        public decimal SecurityStatus { get; set; }

        [JsonProperty("security_class")]
        public string SecurityClass { get; set; }

        [JsonProperty("constellation_id")]
        public int ConstellationId { get; set; }

        [JsonProperty("planets")]
        public List<Planet> Planets { get; set; } = new List<Planet>();

        [JsonProperty("stargates")]
        public int[] Stargates { get; set; }

        [JsonProperty("stations")]
        public int[] Stations { get; set; }
    }
}
