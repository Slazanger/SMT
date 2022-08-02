using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models.Fleets
{
    public class Wing
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("squads")]
        public List<Squad> Squads { get; set; } = new List<Squad>();
    }

    public class Squad
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }
    }
}
