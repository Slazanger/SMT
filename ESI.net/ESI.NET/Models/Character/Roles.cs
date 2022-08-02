using Newtonsoft.Json;

namespace ESI.NET.Models.Character
{
    public class Roles
    {
        [JsonProperty("roles")]
        public string[] MainRoles { get; set; }

        [JsonProperty("roles_at_base")]
        public string[] RolesAtBase { get; set; }

        [JsonProperty("roles_at_hq")]
        public string[] RolesAtHq { get; set; }

        [JsonProperty("roles_at_other")]
        public string[] RolesAtOther { get; set; }

    }
}
