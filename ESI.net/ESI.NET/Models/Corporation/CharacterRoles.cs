using Newtonsoft.Json;

namespace ESI.NET.Models.Corporation
{
    public class CharacterRoles
    {
        [JsonProperty("character_id")]
        public int CharacterId { get; set; }

        [JsonProperty("grantable_roles")]
        public string[] GrantableRoles { get; set; }

        [JsonProperty("grantable_roles_at_base")]
        public string[] GrantableRolesAtBase { get; set; }

        [JsonProperty("grantable_roles_at_hq")]
        public string[] GrantableRolesAtHq { get; set; }

        [JsonProperty("grantable_roles_at_other")]
        public string[] GrantableRolesAtOther { get; set; }

        [JsonProperty("roles")]
        public string[] Roles { get; set; }

        [JsonProperty("roles_at_base")]
        public string[] RolesAtBase { get; set; }

        [JsonProperty("roles_at_hq")]
        public string[] RolesAtHq { get; set; }

        [JsonProperty("roles_at_other")]
        public string[] RolesAtOther { get; set; }
    }
}
