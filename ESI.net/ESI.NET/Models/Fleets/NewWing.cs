using Newtonsoft.Json;

namespace ESI.NET.Models.Fleets
{
    public class NewWing
    {
        [JsonProperty("wing_id")]
        public long WingId { get; set; }
    }
}
