using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Industry
{
    public class Observer
    {
        [JsonProperty("last_updated")]
        public DateTime LastUpdated { get; set; }

        [JsonProperty("observer_id")]
        public long ObserverId { get; set; }

        [JsonProperty("observer_type")]
        public string ObserverType { get; set; }
    }
}
