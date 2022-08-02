using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models.Contacts
{
    public class Contact
    {
        [JsonProperty("standing")]
        public decimal Standing { get; set; }

        [JsonProperty("contact_type")]
        public string ContactType { get; set; }

        [JsonProperty("contact_id")]
        public int ContactId { get; set; }

        [JsonProperty("is_watched")]
        public bool IsWatched { get; set; }

        [JsonProperty("is_blocked")]
        public bool IsBlocked { get; set; }

        [JsonProperty("label_ids")]
        public List<long> LabelIds { get; set; } = new List<long>(); 

    }
}