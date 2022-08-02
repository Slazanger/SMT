using System.Runtime.Serialization;

namespace ESI.NET.Enumerations
{
    public enum Contested
    {
        [EnumMember(Value = "captured")]    /**/ Captured,
        [EnumMember(Value = "contested")]   /**/ Contested,
        [EnumMember(Value = "uncontested")] /**/ Uncontested,
        [EnumMember(Value = "vulnerable ")] /**/ Vulnerable
    }
}
