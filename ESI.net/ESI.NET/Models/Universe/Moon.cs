using Newtonsoft.Json;

namespace ESI.NET.Models.Universe
{
    public class Moon
    {
        [JsonProperty("moon_id")]
        public int MoonId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("position")]
        public Position Position { get; set; }

        [JsonProperty("system_id")]
        public int SystemId { get; set; }
    }
}
