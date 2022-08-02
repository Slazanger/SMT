using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models.Dogma
{
    class DynamicItem
    {
        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("dogma_attributes")]
        public List<Attribute> DogmaAttributes { get; set; } = new List<Attribute>();

        [JsonProperty("dogma_effects")]
        public List<Effect> DogmaEffects { get; set; } = new List<Effect>();

        [JsonProperty("mutator_type_id")]
        public int MutatorTypeId { get; set; }

        [JsonProperty("source_type_id")]
        public int SourceTypeId { get; set; }
    }
}