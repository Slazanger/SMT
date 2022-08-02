using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models.Insurance
{
    public class Insurance
    {
        [JsonProperty("type_id")]
        public int TypeID { get; set; }

        [JsonProperty("levels")]
        public List<Levels> Levels { get; set; } = new List<Levels>();
    }

    public class Levels
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("cost")]
        public double Cost { get; set; }

        [JsonProperty("payout")]
        public double Payout { get; set; }
    }
}
