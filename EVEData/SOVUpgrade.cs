//-----------------------------------------------------------------------
// SOVUpgrade
//-----------------------------------------------------------------------

namespace SMT.EVEData
{
    /// <summary>
    /// Represents a Sovereignty Upgrade type
    /// </summary>
    public enum SOVUpgradeType
    {
        // Modern Equinox Upgrades
        CynosuralNavigation,
        CynosuralSuppression,
        AdvancedLogisticsNetwork,
        SupercapitalConstructionFacilities,

        // Legacy IHub Upgrades
        OreProspecting1,
        OreProspecting2,
        OreProspecting3,
        OreProspecting4,
        OreProspecting5,

        CombatSites1,
        CombatSites2,
        CombatSites3,
        CombatSites4,
        CombatSites5,

        Wormhole1,
        Wormhole2,
        Wormhole3,
        Wormhole4,
        Wormhole5,

        MiniProfession1,
        MiniProfession2,
        MiniProfession3,
        MiniProfession4,
        MiniProfession5,

        Entrapment1,
        Entrapment2,
        Entrapment3,
        Entrapment4,
        Entrapment5
    }

    /// <summary>
    /// Represents a Sovereignty Upgrade installed in a system
    /// </summary>
    public class SOVUpgrade
    {
        public SOVUpgradeType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int TypeID { get; set; }

        public SOVUpgrade(SOVUpgradeType type, string name, string description, int typeID)
        {
            Type = type;
            Name = name;
            Description = description;
            TypeID = typeID;
        }

        /// <summary>
        /// Gets the display name for the upgrade
        /// </summary>
        public string DisplayName
        {
            get
            {
                return Type switch
                {
                    SOVUpgradeType.CynosuralNavigation => "Cynosural Navigation",
                    SOVUpgradeType.CynosuralSuppression => "Cynosural Suppression",
                    SOVUpgradeType.AdvancedLogisticsNetwork => "Advanced Logistics Network",
                    SOVUpgradeType.SupercapitalConstructionFacilities => "Supercapital Construction",

                    SOVUpgradeType.OreProspecting1 => "Ore Prospecting I",
                    SOVUpgradeType.OreProspecting2 => "Ore Prospecting II",
                    SOVUpgradeType.OreProspecting3 => "Ore Prospecting III",
                    SOVUpgradeType.OreProspecting4 => "Ore Prospecting IV",
                    SOVUpgradeType.OreProspecting5 => "Ore Prospecting V",

                    SOVUpgradeType.CombatSites1 => "Combat Sites I",
                    SOVUpgradeType.CombatSites2 => "Combat Sites II",
                    SOVUpgradeType.CombatSites3 => "Combat Sites III",
                    SOVUpgradeType.CombatSites4 => "Combat Sites IV",
                    SOVUpgradeType.CombatSites5 => "Combat Sites V",

                    SOVUpgradeType.Wormhole1 => "Wormhole I",
                    SOVUpgradeType.Wormhole2 => "Wormhole II",
                    SOVUpgradeType.Wormhole3 => "Wormhole III",
                    SOVUpgradeType.Wormhole4 => "Wormhole IV",
                    SOVUpgradeType.Wormhole5 => "Wormhole V",

                    SOVUpgradeType.MiniProfession1 => "Mini-Profession I",
                    SOVUpgradeType.MiniProfession2 => "Mini-Profession II",
                    SOVUpgradeType.MiniProfession3 => "Mini-Profession III",
                    SOVUpgradeType.MiniProfession4 => "Mini-Profession IV",
                    SOVUpgradeType.MiniProfession5 => "Mini-Profession V",

                    SOVUpgradeType.Entrapment1 => "Entrapment I",
                    SOVUpgradeType.Entrapment2 => "Entrapment II",
                    SOVUpgradeType.Entrapment3 => "Entrapment III",
                    SOVUpgradeType.Entrapment4 => "Entrapment IV",
                    SOVUpgradeType.Entrapment5 => "Entrapment V",

                    _ => Name ?? Type.ToString()
                };
            }
        }

        /// <summary>
        /// Gets a short category name for grouping
        /// </summary>
        public string Category
        {
            get
            {
                return Type switch
                {
                    SOVUpgradeType.CynosuralNavigation or SOVUpgradeType.CynosuralSuppression or
                    SOVUpgradeType.AdvancedLogisticsNetwork or SOVUpgradeType.SupercapitalConstructionFacilities
                        => "Strategic",

                    SOVUpgradeType.OreProspecting1 or SOVUpgradeType.OreProspecting2 or SOVUpgradeType.OreProspecting3 or
                    SOVUpgradeType.OreProspecting4 or SOVUpgradeType.OreProspecting5 or
                    SOVUpgradeType.MiniProfession1 or SOVUpgradeType.MiniProfession2 or SOVUpgradeType.MiniProfession3 or
                    SOVUpgradeType.MiniProfession4 or SOVUpgradeType.MiniProfession5
                        => "Industrial",

                    SOVUpgradeType.CombatSites1 or SOVUpgradeType.CombatSites2 or SOVUpgradeType.CombatSites3 or
                    SOVUpgradeType.CombatSites4 or SOVUpgradeType.CombatSites5 or
                    SOVUpgradeType.Wormhole1 or SOVUpgradeType.Wormhole2 or SOVUpgradeType.Wormhole3 or
                    SOVUpgradeType.Wormhole4 or SOVUpgradeType.Wormhole5 or
                    SOVUpgradeType.Entrapment1 or SOVUpgradeType.Entrapment2 or SOVUpgradeType.Entrapment3 or
                    SOVUpgradeType.Entrapment4 or SOVUpgradeType.Entrapment5
                        => "Military",

                    _ => "Other"
                };
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
