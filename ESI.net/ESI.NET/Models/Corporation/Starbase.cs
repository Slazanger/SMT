using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Corporation
{
    public class Starbase
    {
        [JsonProperty("moon_id")]
        public int MoonId { get; set; }

        [JsonProperty("onlined_since")]
        public DateTime OnlinedSince { get; set; }

        [JsonProperty("reinforced_until")]
        public DateTime ReinforcedUntil { get; set; }

        [JsonProperty("starbase_id")]
        public long StarbaseId { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("system_id")]
        public int SystemId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("unanchor_at")]
        public DateTime UnanchorAt { get; set; }
    }
}
