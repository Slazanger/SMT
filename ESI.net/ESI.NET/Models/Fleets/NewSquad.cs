using Newtonsoft.Json;

namespace ESI.NET.Models.Fleets
{
    public class NewSquad
    {
        [JsonProperty("squad_id")]
        public long SquadId { get; set; }
    }
}
