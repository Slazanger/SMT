using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Corporation
{
    public class MemberInfo
    {
        [JsonProperty("base_id")]
        public int BaseId { get; set; }

        [JsonProperty("character_id")]
        public int CharacterId { get; set; }

        [JsonProperty("location_id")]
        public long LocationId { get; set; }

        [JsonProperty("logoff_date")]
        public DateTime LogoffDate { get; set; }

        [JsonProperty("logon_date")]
        public DateTime LogonDate { get; set; }

        [JsonProperty("ship_type_id")]
        public int ShipTypeId { get; set; }

        [JsonProperty("start_date")]
        public DateTime StartDate { get; set; }
    }
}
