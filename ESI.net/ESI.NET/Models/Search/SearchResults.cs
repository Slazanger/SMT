using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models
{
    public class SearchResults
    {
        [JsonProperty("agent")]
        public long[] Agents { get; set; }

        [JsonProperty("alliance")]
        public long[] Alliances { get; set; }

        [JsonProperty("character")]
        public long[] Characters { get; set; }

        [JsonProperty("constellation")]
        public long[] Constellations { get; set; }

        [JsonProperty("corporation")]
        public long[] Corporations { get; set; }

        [JsonProperty("faction")]
        public long[] Factions { get; set; }

        [JsonProperty("inventory_type")]
        public long[] InventoryTypes { get; set; }

        [JsonProperty("region")]
        public long[] Regions { get; set; }

        [JsonProperty("solar_system")]
        public long[] SolarSystems { get; set; }

        [JsonProperty("station")]
        public long[] Stations { get; set; }

        [JsonProperty("structure")]
        public long[] Structures { get; set; }
    }
}
