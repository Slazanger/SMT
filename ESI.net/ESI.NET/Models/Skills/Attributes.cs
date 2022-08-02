using Newtonsoft.Json;

namespace ESI.NET.Models.Skills
{
    public class Attributes
    {
        [JsonProperty("charisma")]
        public int Charisma { get; set; }

        [JsonProperty("intelligence")]
        public int Intelligence { get; set; }

        [JsonProperty("memory")]
        public int Memory { get; set; }

        [JsonProperty("perception")]
        public int Perception { get; set; }

        [JsonProperty("willpower")]
        public int Willpower { get; set; }

        [JsonProperty("bonus_remaps")]
        public int BonusRemaps { get; set; }

        [JsonProperty("last_remap_date")]
        public string LastRemapDate { get; set; }

        [JsonProperty("accrued_remap_cooldown_date")]
        public string AccruedRemapCooldownDate { get; set; }
    }
}
