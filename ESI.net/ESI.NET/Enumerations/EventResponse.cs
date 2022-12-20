using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace ESI.NET.Enumerations
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum EventResponse
    {
        [EnumMember(Value = "accepted")]  /**/ Accepted,
        [EnumMember(Value = "declined")]  /**/ Declined,
        [EnumMember(Value = "tentative")] /**/ Tentative
    }
}