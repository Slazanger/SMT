using Newtonsoft.Json;

namespace ESI.NET.Models.Killmails
{
    public class Killmail
    {
        [JsonProperty("killmail_hash")]
        public string Hash { get; set; }

        [JsonProperty("killmail_id")]
        public int Id { get; set; }
    }
}
