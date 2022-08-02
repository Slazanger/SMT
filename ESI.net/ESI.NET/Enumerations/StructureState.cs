using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace ESI.NET.Enumerations
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum StructureState
    {
        [EnumMember(Value = "anchor_vulnerable")] AnchorVulnerable,
        [EnumMember(Value = "anchoring")] Anchoring,
        [EnumMember(Value = "armor_reinforce")] ArmorReinforce,
        [EnumMember(Value = "armor_vulnerable")] ArmorVulnerable,
        [EnumMember(Value = "deploy_vulnerable")] DeployVulnerable,
        [EnumMember(Value = "fitting_invulnerable")] FittingInvulnerable,
        [EnumMember(Value = "hull_reinforce")] HullReinforce,
        [EnumMember(Value = "hull_vulnerable")] HullVulnerable,
        [EnumMember(Value = "online_deprecated")] OnlineDeprecated,
        [EnumMember(Value = "onlining_vulnerable")] OnliningVulnerable,
        [EnumMember(Value = "shield_vulnerable")] ShieldVulnerable,
        [EnumMember(Value = "unanchored")] Unanchored,
        [EnumMember(Value = "unknown")] Unknown
    }
}
