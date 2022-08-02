using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models.Industry
{
    public class SolarSystem
    {
        [JsonProperty("solar_system_id")]
        public int SolarSystemId { get; set; }

        [JsonProperty("cost_indices")]
        public List<CostIndice> CostIndices { get; set; } = new List<CostIndice>();
    }

    public class CostIndice
    {
        [JsonProperty("activity")]
        public string Activity { get; set; }

        [JsonProperty("cost_index")]
        public decimal CostIndex { get; set; }
    }
}
