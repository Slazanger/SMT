using System.Runtime.Serialization;

namespace ESI.NET.Enumerations
{
    public enum DataSource
    {
        [EnumMember(Value = "singularity")] /**/ Singularity,
        [EnumMember(Value = "tranquility")] /**/ Tranquility,
        [EnumMember(Value = "serenity")]    /**/ Serenity
    }
}
