//-----------------------------------------------------------------------
// SOVUpgradeStorage
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SMT.EVEData
{
    /// <summary>
    /// Stores SOV upgrade configuration for systems
    /// </summary>
    [Serializable]
    public class SOVUpgradeStorage
    {
        /// <summary>
        /// Dictionary mapping system ID to list of upgrade types
        /// </summary>
        public Dictionary<long, List<SOVUpgradeType>> SystemUpgrades { get; set; }

        public SOVUpgradeStorage()
        {
            SystemUpgrades = new Dictionary<long, List<SOVUpgradeType>>();
        }

        /// <summary>
        /// Save SOV upgrades from all systems to storage
        /// </summary>
        public void SaveFromSystems(List<System> systems)
        {
            SystemUpgrades.Clear();

            foreach (var system in systems)
            {
                if (system.SOVUpgrades != null && system.SOVUpgrades.Count > 0)
                {
                    var upgradeTypes = new List<SOVUpgradeType>();
                    foreach (var upgrade in system.SOVUpgrades)
                    {
                        upgradeTypes.Add(upgrade.Type);
                    }
                    SystemUpgrades[system.ID] = upgradeTypes;
                }
            }
        }

        /// <summary>
        /// Load SOV upgrades to all systems from storage
        /// </summary>
        public void LoadToSystems(List<System> systems)
        {
            foreach (var system in systems)
            {
                if (SystemUpgrades.ContainsKey(system.ID))
                {
                    system.SOVUpgrades.Clear();
                    foreach (var upgradeType in SystemUpgrades[system.ID])
                    {
                        var upgrade = CreateUpgradeFromType(upgradeType);
                        system.SOVUpgrades.Add(upgrade);
                    }
                }
            }
        }

        /// <summary>
        /// Create a SOVUpgrade object from a type
        /// </summary>
        private SOVUpgrade CreateUpgradeFromType(SOVUpgradeType type)
        {
            string name = type.ToString();
            string description = GetUpgradeDescription(type);
            int typeID = GetUpgradeTypeID(type);

            return new SOVUpgrade(type, name, description, typeID);
        }

        private string GetUpgradeDescription(SOVUpgradeType type)
        {
            return type switch
            {
                SOVUpgradeType.CynosuralNavigation => "Enables Pharolux Cyno Beacon",
                SOVUpgradeType.CynosuralSuppression => "Enables Tenebrex Cyno Jammer",
                SOVUpgradeType.AdvancedLogisticsNetwork => "Enables Ansiblex Jump Gate",
                SOVUpgradeType.SupercapitalConstructionFacilities => "Enables Supercapital Shipyards",
                SOVUpgradeType.OreProspecting1 or SOVUpgradeType.OreProspecting2 or SOVUpgradeType.OreProspecting3 or
                SOVUpgradeType.OreProspecting4 or SOVUpgradeType.OreProspecting5 => "Increases ore resources",
                SOVUpgradeType.CombatSites1 or SOVUpgradeType.CombatSites2 or SOVUpgradeType.CombatSites3 or
                SOVUpgradeType.CombatSites4 or SOVUpgradeType.CombatSites5 => "Increases combat sites",
                SOVUpgradeType.Wormhole1 or SOVUpgradeType.Wormhole2 or SOVUpgradeType.Wormhole3 or
                SOVUpgradeType.Wormhole4 or SOVUpgradeType.Wormhole5 => "Increases wormhole chance",
                SOVUpgradeType.MiniProfession1 or SOVUpgradeType.MiniProfession2 or SOVUpgradeType.MiniProfession3 or
                SOVUpgradeType.MiniProfession4 or SOVUpgradeType.MiniProfession5 => "Increases mini-profession sites",
                SOVUpgradeType.Entrapment1 or SOVUpgradeType.Entrapment2 or SOVUpgradeType.Entrapment3 or
                SOVUpgradeType.Entrapment4 or SOVUpgradeType.Entrapment5 => "Increases complex chance",
                _ => ""
            };
        }

        private int GetUpgradeTypeID(SOVUpgradeType type)
        {
            return type switch
            {
                SOVUpgradeType.CynosuralNavigation => 81615,
                SOVUpgradeType.CynosuralSuppression => 81619,
                SOVUpgradeType.AdvancedLogisticsNetwork => 81621,
                SOVUpgradeType.SupercapitalConstructionFacilities => 81623,
                _ => 0
            };
        }

        /// <summary>
        /// Save to file
        /// </summary>
        public static void SaveToFile(SOVUpgradeStorage storage, string filename)
        {
            try
            {
                XmlSerializer xms = new XmlSerializer(typeof(SOVUpgradeStorage));
                using (TextWriter tw = new StreamWriter(filename))
                {
                    xms.Serialize(tw, storage);
                }
            }
            catch { }
        }

        /// <summary>
        /// Load from file
        /// </summary>
        public static SOVUpgradeStorage LoadFromFile(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    XmlSerializer xms = new XmlSerializer(typeof(SOVUpgradeStorage));
                    using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        return xms.Deserialize(fs) as SOVUpgradeStorage;
                    }
                }
            }
            catch { }

            return new SOVUpgradeStorage();
        }
    }
}
