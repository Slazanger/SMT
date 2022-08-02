using Newtonsoft.Json;

namespace ESI.NET.Models.Corporation
{
    public class Title
    {
        [JsonProperty("title_id")]
        public int TitleId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("roles")]
        public string[] Roles { get; set; }

        [JsonProperty("grantable_roles")]
        public string[] GrantableRoles { get; set; }

        [JsonProperty("roles_at_hq")]
        public string[] RolesAtHq { get; set; }

        [JsonProperty("grantable_roles_at_hq")]
        public string[] GrantableRolesAtHq { get; set; }

        [JsonProperty("roles_at_base")]
        public string[] RolesAtBase { get; set; }

        [JsonProperty("grantable_roles_at_base")]
        public string[] GrantableRolesAtBase { get; set; }

        [JsonProperty("roles_at_other")]
        public string[] RolesAtOther { get; set; }

        [JsonProperty("grantable_roles_at_other")]
        public string[] GrantableRolesAtOther { get; set; }
    }
}
