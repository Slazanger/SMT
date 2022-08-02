using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace ESI.NET.Enumerations
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum RoutesFlag
    {
        [EnumMember(Value="shortest")] /**/ Shortest,
        [EnumMember(Value="secure")]   /**/ Secure,
        [EnumMember(Value="insecure")] /**/ Insecure
    }
}
