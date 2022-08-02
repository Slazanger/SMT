using Newtonsoft.Json;

namespace ESI.NET.Models.Fleets
{
    public class FleetInfo
    {
        [JsonProperty("fleet_id")]
        public long FleetId { get; set; }

        [JsonProperty("wing_id")]
        public long WingId { get; set; }

        [JsonProperty("squad_id")]
        public long SquadId { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }
    }
}
