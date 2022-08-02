using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Status
{
    public class Status
    {
        [JsonProperty("players")]
        public int Players { get; set; }

        [JsonProperty("server_version")]
        public string ServerVersion { get; set; }

        [JsonProperty("start_time")]
        public DateTime StartTime { get; set; }

        [JsonProperty("vip")]
        public bool VIP { get; set; }
    }
}
