using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ESI.NET.Models.Clones
{
    public class Clones
    {
        [JsonProperty("last_clone_jump_date")]
        public DateTime LastCloneJumpDate { get; set; }

        [JsonProperty("home_location")]
        public HomeLocation HomeLocation { get; set; }

        [JsonProperty("last_station_change_date")]
        public DateTime LastStationChangeDate { get; set; }

        [JsonProperty("jump_clones")]
        public List<JumpClone> JumpClones { get; set; } = new List<JumpClone>();
    }

    public class HomeLocation
    {
        [JsonProperty("location_id")]
        public long LocationId { get; set; }

        [JsonProperty("location_type")]
        public string LocationType { get; set; }
    }

    public class JumpClone
    {
        [JsonProperty("jump_clone_id")]
        public int JumpCloneId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("location_id")]
        public long LocationId { get; set; }

        [JsonProperty("location_type")]
        public string LocationType { get; set; }

        [JsonProperty("implants")]
        public int[] Implants { get; set; }
    }
}
