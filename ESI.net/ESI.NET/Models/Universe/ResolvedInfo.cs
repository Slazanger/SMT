using ESI.NET.Enumerations;
using Newtonsoft.Json;

namespace ESI.NET.Models.Universe
{
    public class ResolvedInfo
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("category")]
        public ResolvedInfoCategory Category { get; set; }
    }
}
