using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace ESI.NET.Enumerations
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum MarketOrderType
    {
        [EnumMember(Value="all")]  /**/ All,
        [EnumMember(Value="buy")]  /**/ Buy,
        [EnumMember(Value="sell")] /**/ Sell
    }
}
