using ESI.NET.Enumerations;
using Newtonsoft.Json;

namespace ESI.NET.Models.Calendar
{
    public class Response
    {
        [JsonProperty("character_id")]
        public int CharacterId { get; set; }

        [JsonProperty("event_response")]
        public EventResponse EventResponse { get; set; }
    }
}
