using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models.Corporation
{
    public class StarbaseInfo
    {
        [JsonProperty("allow_alliance_members")]
        public bool AllowAllianceMembers { get; set; }

        [JsonProperty("allow_corporation_members")]
        public bool AllowCorporationMembers { get; set; }

        [JsonProperty("anchor")]
        public string Anchor { get; set; }

        [JsonProperty("attack_if_at_war")]
        public bool AttackIfAtWar { get; set; }

        [JsonProperty("attack_if_other_security_status_dropping")]
        public bool AttackIfOtherSecurityStatusDropping { get; set; }

        [JsonProperty("attack_security_status_threshold")]
        public float AttackSecurityStatusThreshold { get; set; }

        [JsonProperty("attack_standing_threshold")]
        public float AttackStandingThreshold { get; set; }

        [JsonProperty("fuel_bay_take")]
        public string FuelBayTake { get; set; }

        [JsonProperty("fuel_bay_view")]
        public string FuelBayView { get; set; }

        [JsonProperty("fuels")]
        public List<Fuel> Fuels { get; set; } = new List<Fuel>();

        [JsonProperty("offline")]
        public string Offline { get; set; }

        [JsonProperty("online")]
        public string Online { get; set; }

        [JsonProperty("unanchor")]
        public string Unanchor { get; set; }

        [JsonProperty("use_alliance_standings")]
        public bool UseAllianceStandings { get; set; }
    }

    public class Fuel
    {
        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
}
