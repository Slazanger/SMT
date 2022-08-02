using Newtonsoft.Json;

namespace ESI.NET.Models.Corporation
{
    public class MemberTitles
    {
        [JsonProperty("character_id")]
        public int CharacterId { get; set; }

        [JsonProperty("titles")]
        public int[] Titles { get; set; }

    }
}
