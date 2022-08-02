using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models.Universe
{
    public class Type
    {
        [JsonProperty("capacity")]
        public float Capacity { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("dogma_attributes")]
        public List<Attribute> DogmaAttributes { get; set; } = new List<Attribute>();

        [JsonProperty("dogma_effects")]
        public List<Effect> DogmaEffects { get; set; } = new List<Effect>();

        [JsonProperty("graphic_id")]
        public int GraphicId { get; set; }

        [JsonProperty("group_id")]
        public int GroupId { get; set; }

        [JsonProperty("icon_id")]
        public int IconId { get; set; }

        [JsonProperty("market_group_id")]
        public int MarketGroupId { get; set; }

        [JsonProperty("mass")]
        public float Mass { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("packaged_volume")]
        public float PackagedVolume { get; set; }

        [JsonProperty("portion_size")]
        public int PortionSize { get; set; }

        [JsonProperty("published")]
        public bool Published { get; set; }

        [JsonProperty("radius")]
        public float Radius { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("volume")]
        public float Volume { get; set; }
    }

    public class Attribute
    {
        [JsonProperty("attribute_id")]
        public int AttributeId { get; set; }

        [JsonProperty("value")]
        public float Value { get; set; }
    }

    public class Effect
    {
        [JsonProperty("effect_id")]
        public int EffectId { get; set; }

        [JsonProperty("is_default")]
        public bool IsDefault { get; set; }
    }
}
