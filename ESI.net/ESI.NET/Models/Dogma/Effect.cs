using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models.Dogma
{
    public class Effect
    {
        [JsonProperty("effect_id")]
        public int EffectId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("icon_id")]
        public int IconId { get; set; }

        [JsonProperty("effect_category")]
        public int EffectCategory { get; set; }

        [JsonProperty("pre_expression")]
        public int PreExpression { get; set; }

        [JsonProperty("post_expression")]
        public int PostExpression { get; set; }

        [JsonProperty("is_offensive")]
        public bool IsOffensive { get; set; }

        [JsonProperty("is_assistance")]
        public bool IsAssistance { get; set; }

        [JsonProperty("disallow_auto_repeat")]
        public bool DisallowAutoRepeat { get; set; }

        [JsonProperty("published")]
        public bool Published { get; set; }

        [JsonProperty("is_warp_safe")]
        public bool IsWarpSafe { get; set; }

        [JsonProperty("range_chance")]
        public bool RangeChance { get; set; }

        [JsonProperty("electronic_chance")]
        public bool ElectronicChance { get; set; }

        [JsonProperty("duration_attribute_id")]
        public int DurationAttributeId { get; set; }

        [JsonProperty("tracking_speed_attribute_id")]
        public int TrackingSpeedAttributeId { get; set; }

        [JsonProperty("discharge_attribute_id")]
        public int DischargeAttributeId { get; set; }

        [JsonProperty("range_attribute_id")]
        public int RangeAttributeId { get; set; }

        [JsonProperty("falloff_attribute_id")]
        public int FalloffAttributeId { get; set; }

        [JsonProperty("modifiers")]
        public List<Modifier> Modifiers { get; set; } = new List<Modifier>();

        /// <summary>
        /// Only populated when used in DynamicItem; all other properties except EffectId will be empty
        /// </summary>
        [JsonProperty("is_default")]
        public bool IsDefault { get; set; }
    }

    public class Modifier
    {
        [JsonProperty("func")]
        public string Func { get; set; }

        [JsonProperty("domain")]
        public string Domain { get; set; }

        [JsonProperty("modified_attribute_id")]
        public int ModifiedAttributeId { get; set; }

        [JsonProperty("modifying_attribute_id")]
        public int ModifyingAttributeId { get; set; }

        [JsonProperty("effect_id")]
        public int EffectId { get; set; }

        [JsonProperty("operator")]
        public int Operator { get; set; }
    }
}
