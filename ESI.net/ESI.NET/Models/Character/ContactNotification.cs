using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Character
{
    public class ContactNotification
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("notification_id")]
        public long NotificationId { get; set; }

        [JsonProperty("send_date")]
        public DateTime SendDate { get; set; }

        [JsonProperty("sender_character_id")]
        public long SenderCharacterId { get; set; }

        [JsonProperty("standing_level")]
        public decimal StandingLevel { get; set; }
    }
}
