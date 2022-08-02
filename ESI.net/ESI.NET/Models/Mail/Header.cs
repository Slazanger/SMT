using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models.Mail
{
    public class Header
    {
        [JsonProperty("mail_id")]
        public long MailId { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("from")]
        public int From { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("labels")]
        public long[] Labels { get; set; }

        [JsonProperty("recipients")]
        public List<Recipient> Recipients { get; set; } = new List<Recipient>();

        [JsonProperty("is_read")]
        public bool IsRead { get; set; }
    }
}
