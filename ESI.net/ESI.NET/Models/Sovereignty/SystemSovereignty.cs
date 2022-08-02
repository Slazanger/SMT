using Newtonsoft.Json;

namespace ESI.NET.Models.Sovereignty
{
    public class SystemSovereignty
    {
        [JsonProperty("alliance_id")]
        public int AllianceId { get; set; }

        [JsonProperty("corporation_id")]
        public int CorporationId { get; set; }

        [JsonProperty("faction_id")]
        public int FactionId { get; set; }

        [JsonProperty("system_id")]
        public int SystemId { get; set; }
    }
}
