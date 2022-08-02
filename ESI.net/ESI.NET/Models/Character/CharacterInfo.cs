using Newtonsoft.Json;

namespace ESI.NET.Models.Character
{
    public class CharacterInfo
    {
        [JsonProperty("days_of_activity")]
        public long DaysOfActivity { get; set; }

        [JsonProperty("minutes")]
        public long Minutes { get; set; }

        [JsonProperty("sessions_started")]
        public long SessionsStarted { get; set; }
    }
}