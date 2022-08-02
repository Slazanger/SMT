using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models.Mail
{
    public class Message
    {
        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("from")]
        public int From { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("recipients")]
        public List<Recipient> Recipients { get; set; } = new List<Recipient>();

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("labels")]
        public long[] Labels { get; set; }

        [JsonProperty("read")]
        public bool Read { get; set; }
    }

    public class Recipient
    {
        [JsonProperty("recipient_type")]
        public string RecipientType { get; set; }

        [JsonProperty("recipient_id")]
        public int RecipientId { get; set; }
    }
}
