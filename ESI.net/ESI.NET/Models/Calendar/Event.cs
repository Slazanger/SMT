using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Calendar
{
    public class Event
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("event_id")]
        public int EventId { get; set; }

        [JsonProperty("importance")]
        public int Importance { get; set; }

        [JsonProperty("owner_id")]
        public int OwnerId { get; set; }

        [JsonProperty("owner_name")]
        public string OwnerName { get; set; }

        [JsonProperty("owner_type")]
        public string OwnerType { get; set; }

        [JsonProperty("response")]
        public string Response { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }
}
