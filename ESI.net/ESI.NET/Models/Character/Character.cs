using Newtonsoft.Json;

namespace ESI.NET.Models.Character
{
    public class Character
    {
        [JsonProperty("character_id")]
        public long Id { get; set; }

        [JsonProperty("character_name")]
        public string Name { get; set; }
    }
}
