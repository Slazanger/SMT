using Newtonsoft.Json;

namespace ESI.NET.Models
{
    public class Standing
    {
        [JsonProperty("from_id")]
        public int FromId { get; set; }

        [JsonProperty("from_type")]
        public string FromType { get; set; }

        [JsonProperty("standing")]
        public decimal Value { get; set; }
    }
}
