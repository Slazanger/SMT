using Newtonsoft.Json;

namespace ESI.NET.Models.Character
{
    public class Orbital
    {
        [JsonProperty("strike_characters_killed")]
        public long StrikeCharactersKilled { get; set; }

        [JsonProperty("strike_damage_to_players_armor_amount")]
        public long StrikeDamageToPlayersArmorAmount { get; set; }

        [JsonProperty("strike_damage_to_players_shield_amount")]
        public long StrikeDamageToPlayersShieldAmount { get; set; }
    }
}
