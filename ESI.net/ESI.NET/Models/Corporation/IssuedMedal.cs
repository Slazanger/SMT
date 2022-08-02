using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Corporation
{
    public class IssuedMedal
    {
        [JsonProperty("character_id")]
        public int CharacterId { get; set; }

        [JsonProperty("issued_at")]
        public DateTime IssuedAt { get; set; }

        [JsonProperty("issuer_id")]
        public int IssuerId { get; set; }

        [JsonProperty("medal_id")]
        public int MedalId { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
