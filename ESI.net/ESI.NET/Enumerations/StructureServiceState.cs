using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace ESI.NET.Enumerations
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum StructureServiceState
    {
        [EnumMember(Value = "online")] Online,
        [EnumMember(Value = "offline")] Offline,
        [EnumMember(Value = "cleamup")] Cleanup
    }
}
