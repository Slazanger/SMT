using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models.Mail
{
    public class LabelCounts
    {
        [JsonProperty("total_unread_count")]
        public int TotalUnreadCount { get; set; }

        [JsonProperty("labels")]
        public List<Label> Labels { get; set; } = new List<Label>();
    }

    public class Label
    {
        [JsonProperty("unread_count")]
        public int UnreadCount { get; set; }

        [JsonProperty("label_id")]
        public int LabelId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }
    }
}
