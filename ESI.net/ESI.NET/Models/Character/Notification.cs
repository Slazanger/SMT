using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Character
{
    public class Notification
    {
        [JsonProperty("is_read")]
        public bool IsRead { get; set; }

        [JsonProperty("notification_id")]
        public long NotificationId { get; set; }

        [JsonProperty("sender_id")]
        public int SenderId { get; set; }

        [JsonProperty("sender_type")]
        public string SenderType { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
