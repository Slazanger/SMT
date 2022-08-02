using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace ESI.NET.Enumerations
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum FleetRole
    {
        [EnumMember(Value="fleet_commander")] /**/ FleetCommander,
        [EnumMember(Value="wing_commander")]  /**/ WingCommander,
        [EnumMember(Value="squad_commander")] /**/ SquadCommander,
        [EnumMember(Value="squad_member")]    /**/ SquadMember
    }
}
