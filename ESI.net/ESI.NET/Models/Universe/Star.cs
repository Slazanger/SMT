using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ESI.NET.Models.Universe
{
    public class Star
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("solar_system_id")]
        public int SolarSystemId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("age")]
        public long Age { get; set; }

        [JsonProperty("luminosity")]
        public decimal Luminosity { get; set; }

        [JsonProperty("radius")]
        public long Radius { get; set; }

        [JsonProperty("spectral_class")]
        public string SpectralClass { get; set; }

        [JsonProperty("temperature")]
        public int Temperature { get; set; }
    }
}
