using Newtonsoft.Json;

namespace ESI.NET.Models.Character
{
    public class Title
    {
        [JsonProperty("title_id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}