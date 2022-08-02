using System;
using System.Runtime.Serialization;

namespace ESI.NET.Enumerations
{
    [Flags]
    public enum SearchCategory
    {
        [EnumMember(Value="agent")]          /**/ Agent = 1,
        [EnumMember(Value="alliance")]       /**/ Alliance = 2,
        [EnumMember(Value="character")]      /**/ Character = 4,
        [EnumMember(Value="constellation")]  /**/ Constellation = 8,
        [EnumMember(Value="corporation")]    /**/ Corporation = 16,
        [EnumMember(Value="faction")]        /**/ Faction = 32,
        [EnumMember(Value="inventory_type")] /**/ InventoryType = 64,
        [EnumMember(Value="region")]         /**/ Region = 128,
        [EnumMember(Value="solar_system")]   /**/ SolarSystem = 256,
        [EnumMember(Value="station")]        /**/ Station = 512,
        [EnumMember(Value="structure")]      /**/ Structure = 1024
    }
}
