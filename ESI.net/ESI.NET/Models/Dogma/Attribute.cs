using Newtonsoft.Json;

namespace ESI.NET.Models.Dogma
{
    public class Attribute
    {
        [JsonProperty("attribute_id")]
        public int AttributeId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("icon_id")]
        public int IconId { get; set; }

        [JsonProperty("default_value")]
        public double DefaultValue { get; set; }

        [JsonProperty("published")]
        public bool Published { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("unit_id")]
        public int UnitId { get; set; }

        [JsonProperty("stackable")]
        public bool Stackable { get; set; }

        [JsonProperty("high_is_good")]
        public bool HighIsGood { get; set; }

        /// <summary>
        /// Only populated when used in DynamicItem; all other properties except AttributeId will be empty
        /// </summary>
        [JsonProperty("value")]
        public decimal Value { get; set; }
    }
}
