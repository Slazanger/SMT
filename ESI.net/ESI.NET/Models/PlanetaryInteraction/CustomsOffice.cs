using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.PlanetaryInteraction
{
    public class CustomsOffice
    {
        [JsonProperty("alliance_tax_rate")]
        public decimal AllianceTaxRate { get; set; }

        [JsonProperty("allow_access_with_standings")]
        public bool AllowAccessWithStandings { get; set; }

        [JsonProperty("allow_alliance_access")]
        public bool AllowAllianceAccess { get; set; }

        [JsonProperty("bad_standing_tax_rate")]
        public decimal BadStandingTaxRate { get; set; }

        [JsonProperty("corporation_tax_rate")]
        public string CorporationTaxRate { get; set; }

        [JsonProperty("excellent_standing_tax_rate")]
        public long ExcellentStandingTaxRate { get; set; }

        [JsonProperty("good_standing_tax_rate")]
        public int GoodStandingTaxRate { get; set; }

        [JsonProperty("neutral_standing_tax_rate")]
        public decimal NeutralStandingTaxRate { get; set; }

        [JsonProperty("office_id")]
        public long Id { get; set; }

        [JsonProperty("reinforce_exit_end")]
        public int ReinforceExitEnd { get; set; }

        [JsonProperty("reinforce_exit_start")]
        public int ReinforceExitStart { get; set; }

        [JsonProperty("standing_level")]
        public string StandingLevel { get; set; }

        [JsonProperty("system_id")]
        public long SystemId { get; set; }

        [JsonProperty("terrible_standing_tax_rate")]
        public decimal TerribleDtandingRate { get; set; }
    }
}
