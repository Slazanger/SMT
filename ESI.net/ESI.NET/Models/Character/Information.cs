using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Character
{
    public class Information
    {
        [JsonProperty("alliance_id")]
        public long AllianceId { get; set; }

        [JsonProperty("ancestry_id")]
        public long AncestryId { get; set; }

        [JsonProperty("birthday")]
        public DateTime Birthday { get; set; }

        [JsonProperty("bloodline_id")]
        public long BloodlineId { get; set; }

        [JsonProperty("corporation_id")]
        public long CorporationId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("faction_id")]
        public long FactionId { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("race_id")]
        public long RaceId { get; set; }

        [JsonProperty("security_status")]
        public decimal SecurityStatus { get; set; }
    }
}
