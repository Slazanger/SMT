using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace ESI.NET.Enumerations
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ResolvedInfoCategory
    {
        [EnumMember(Value = "alliance")] Alliance,
        [EnumMember(Value = "character")] Character,
        [EnumMember(Value = "constellation")] Constellation,
        [EnumMember(Value = "corporation")] Corporation,
        [EnumMember(Value = "inventory_type")] InventoryType,
        [EnumMember(Value = "region")] Region,
        [EnumMember(Value = "solar_system")] SolarSystem,
        [EnumMember(Value = "station")] Station,
        [EnumMember(Value = "faction")] Faction,
        [EnumMember(Value = "structure")] Structure

    }
}
