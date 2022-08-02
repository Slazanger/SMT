using Newtonsoft.Json;

namespace ESI.NET.Models.Fleets
{
    public class Settings
    {
        [JsonProperty("motd")]
        public string Motd { get; set; }

        [JsonProperty("is_free_move")]
        public bool IsFreeMove { get; set; }

        [JsonProperty("is_registered")]
        public bool IsRegistered { get; set; }

        [JsonProperty("is_voice_enabled")]
        public bool IsVoiceEnabled { get; set; }
    }
}
