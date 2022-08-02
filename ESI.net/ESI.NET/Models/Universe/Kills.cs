using Newtonsoft.Json;

namespace ESI.NET.Models.Universe
{
    public class Kills
    {
        [JsonProperty("system_id")]
        public int SystemId { get; set; }

        [JsonProperty("ship_kills")]
        public int ShipKills { get; set; }

        [JsonProperty("npc_kills")]
        public int NpcKills { get; set; }

        [JsonProperty("pod_kills")]
        public int PodKills { get; set; }
    }
}
