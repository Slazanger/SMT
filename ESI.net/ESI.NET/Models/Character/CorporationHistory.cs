using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Character
{
    public class CorporationHistory
    {
        [JsonProperty("corporation_id")]
        public int CorporationId { get; set; }

        [JsonProperty("is_deleted")]
        public bool IsDeleted { get; set; }

        [JsonProperty("record_id")]
        public int RecordId { get; set; }

        [JsonProperty("start_date")]
        public DateTime StartDate { get; set; }
    }
}
