using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Industry
{
    public class ObserverInfo
    {
        [JsonProperty("last_updated")]
        public DateTime LastUpdated { get; set; }

        [JsonProperty("character_id")]
        public int CharacterId { get; set; }

        [JsonProperty("recorded_corporation_id")]
        public int RecordedCorporationId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("quantity")]
        public long Quantity { get; set; }
    }
}
