using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models.Universe
{
    public class IDLookup
    {
        [JsonProperty("agents")]
        public List<ResolvedInfo> Agents { get; set; } = new List<ResolvedInfo>();

        [JsonProperty("alliances")]
        public List<ResolvedInfo> Alliances { get; set; } = new List<ResolvedInfo>();

        [JsonProperty("characters")]
        public List<ResolvedInfo> Characters { get; set; } = new List<ResolvedInfo>();

        [JsonProperty("constellations")]
        public List<ResolvedInfo> Constellations { get; set; } = new List<ResolvedInfo>();

        [JsonProperty("corporations")]
        public List<ResolvedInfo> Corporations { get; set; } = new List<ResolvedInfo>();

        [JsonProperty("factions")]
        public List<ResolvedInfo> Factions { get; set; } = new List<ResolvedInfo>();

        [JsonProperty("inventory_types")]
        public List<ResolvedInfo> InventoryTypes { get; set; } = new List<ResolvedInfo>();

        [JsonProperty("regions")]
        public List<ResolvedInfo> Regions { get; set; } = new List<ResolvedInfo>();

        [JsonProperty("systems")]
        public List<ResolvedInfo> Systems { get; set; } = new List<ResolvedInfo>();

        [JsonProperty("stations")]
        public List<ResolvedInfo> Stations { get; set; } = new List<ResolvedInfo>();

        [JsonProperty("structures")]
        public List<ResolvedInfo> Structures { get; set; } = new List<ResolvedInfo>();

    }
}
