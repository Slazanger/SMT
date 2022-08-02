using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace ESI.NET.Enumerations
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ContractType
    {
        [EnumMember(Value = "unknown")]  /**/ Unknown,
        [EnumMember(Value = "item_exchange")]  /**/ ItemExchange,
        [EnumMember(Value = "auction")] /**/ Auction,
        [EnumMember(Value = "courier")] /**/ Courier,
        [EnumMember(Value = "loan ")] /**/ Loan
    }
}
