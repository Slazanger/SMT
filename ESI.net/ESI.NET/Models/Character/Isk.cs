using Newtonsoft.Json;

namespace ESI.NET.Models.Character
{
    public class Isk
    {
        [JsonProperty("in")]
        public long In { get; set; }

        [JsonProperty("out")]
        public long Out { get; set; }
    }
}
