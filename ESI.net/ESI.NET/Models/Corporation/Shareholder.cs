using Newtonsoft.Json;

namespace ESI.NET.Models.Corporation
{
    public class Shareholder
    {
        [JsonProperty("share_count")]
        public long ShareCount { get; set; }

        [JsonProperty("shareholder_id")]
        public int ShareholderId { get; set; }

        [JsonProperty("shareholder_type")]
        public string ShareholderType { get; set; }
    }
}
