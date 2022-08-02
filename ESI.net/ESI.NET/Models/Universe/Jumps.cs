using Newtonsoft.Json;

namespace ESI.NET.Models.Universe
{
    public class Jumps
    {
        [JsonProperty("system_id")]
        public int SystemId { get; set; }

        [JsonProperty("ship_jumps")]
        public int ShipJumps { get; set; }
    }
}
