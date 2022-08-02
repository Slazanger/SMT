using Newtonsoft.Json;

namespace ESI.NET.Models.Universe
{
    public class Structure
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("solar_system_id")]
        public int SolarSystemId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("position")]
        public Position Position { get; set; }
    }
}
