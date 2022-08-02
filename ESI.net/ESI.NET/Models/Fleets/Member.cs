using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Fleets
{
    public class Member
    {
        [JsonProperty("character_id")]
        public int CharacterId { get; set; }

        [JsonProperty("ship_type_id")]
        public int ShipTypeId { get; set; }

        [JsonProperty("wing_id")]
        public long WingId { get; set; }

        [JsonProperty("squad_id")]
        public long SquadId { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("role_name")]
        public string RoleName { get; set; }

        [JsonProperty("join_time")]
        public DateTime JoinTime { get; set; }

        [JsonProperty("takes_fleet_warp")]
        public bool TakesFleetWarp { get; set; }

        [JsonProperty("solar_system_id")]
        public int SolarSystemId { get; set; }

        [JsonProperty("station_id")]
        public long StationId { get; set; }
    }
}
