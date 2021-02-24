//-----------------------------------------------------------------------
// System
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SMT.EVEData
{
    /// <summary>
    /// Represents the actual eve system, this may be referenced by multiple regions in the case of either border systems or systems that make sense to be drawn in another region
    /// </summary>
    public class System
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="System" /> class.
        /// </summary>
        public System()
        {
            Jumps = new List<string>();
            SHStructures = new List<StructureHunter.Structures>();

            SOVAllianceTCU = 0;
            SOVAllianceIHUB = 0;
            HasJumpBeacon = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="System" /> class.
        /// </summary>
        /// <param name="name">Name of the System</param>
        /// <param name="id">ID of the system</param>
        /// <param name="region">Region this system is in</param>
        /// <param name="station">Does this system contain an NPC station</param>
        public System(string name, long id, string region, bool station, bool iceBelt)
        {
            Name = name;
            ID = id;
            Region = region;
            HasNPCStation = station;
            HasIceBelt = iceBelt;

            // default the ESI stats
            NPCKillsLastHour = -1;
            NPCKillsDeltaLastHour = 0;
            PodKillsLastHour = -1;
            ShipKillsLastHour = -1;
            JumpsLastHour = -1;
            ActiveIncursion = false;

            SOVAllianceTCU = 0;
            SOVAllianceIHUB = 0;

            Jumps = new List<string>();
            SHStructures = new List<StructureHunter.Structures>();
        }

        /// <summary>
        /// Gets or sets the an incursion is active in this system
        /// </summary>
        [XmlIgnoreAttribute]
        public bool ActiveIncursion { get; set; }

        /// <summary>
        /// Gets or sets the X coordinate in real space for this system
        /// </summary>
        public double ActualX { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate in real space for this system
        /// </summary>
        public double ActualY { get; set; }

        /// <summary>
        /// Gets or sets the Z coordinate in real space for this system
        /// </summary>
        public double ActualZ { get; set; }

        /// <summary>
        /// the 2d X coordinate used to render on the universe view
        /// </summary>
        public double UniverseX { get; set; }

        /// <summary>
        /// the 2d Y coordinate used to render on the universe view
        /// </summary>
        public double UniverseY { get; set; }

        [XmlIgnoreAttribute]
        public bool  CustomUniverseLayout { get; set; }

        /// <summary>
        /// Gets or sets EVE's internal Constellation ID
        /// </summary>
        public string ConstellationID { get; set; }

        public bool HasIceBelt { get; set; }

        public bool HasJoveObservatory { get; set; }

        [XmlIgnoreAttribute]
        public bool HasJumpBeacon { get; set; }

        /// <summary>
        /// Gets or sets if this system has an NPC Station
        /// </summary>
        public bool HasNPCStation { get; set; }

        /// <summary>
        /// Gets or sets Eve's internal ID for this System
        /// </summary>
        public long ID { get; set; }

        [XmlIgnoreAttribute]
        public float IHubOccupancyLevel { get; set; }

        [XmlIgnoreAttribute]
        public DateTime IHubVunerabliltyEnd { get; set; }

        [XmlIgnoreAttribute]
        public DateTime IHubVunerabliltyStart { get; set; }

        /// <summary>
        /// Gets or sets the list of Jumps from this system
        /// </summary>
        public List<string> Jumps { get; set; }

        /// <summary>
        /// Gets or sets the number of pods killed in the last hour
        /// </summary>
        [XmlIgnoreAttribute]
        public int JumpsLastHour { get; set; }

        /// <summary>
        /// Gets or sets the Name of the system
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the delta of NPC Kills in the last hour
        /// </summary>
        [XmlIgnoreAttribute]
        public int NPCKillsDeltaLastHour { get; set; }

        /// <summary>
        /// Gets or sets the number of NPC Kills in the last hour
        /// </summary>
        [XmlIgnoreAttribute]
        public int NPCKillsLastHour { get; set; }

        /// <summary>
        /// Gets or sets the Faction of the system if owned by an NPC Corp
        /// </summary>
        [XmlIgnoreAttribute]
        public string NPCSOVFaction { get; set; }

        /// <summary>
        /// Gets or sets the number of pod kills in the last hour
        /// </summary>
        [XmlIgnoreAttribute]
        public int PodKillsLastHour { get; set; }

        public double RadiusAU { get; set; }

        /// <summary>
        /// Gets or sets the Region this system belongs to
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets the number of player ships killed in the last hour
        /// </summary>
        [XmlIgnoreAttribute]
        public int ShipKillsLastHour { get; set; }

        /// <summary>
        /// Gets or sets the an incursion is active in this system
        /// </summary>
        [XmlIgnoreAttribute]
        public List<StructureHunter.Structures> SHStructures { get; set; }

        /// <summary>
        /// Gets or sets the name of the alliance holding sov in this system
        /// </summary>
        [XmlIgnoreAttribute]
        public long SOVAllianceIHUB { get; set; }

        /// <summary>
        /// Gets or sets the name of the alliance holding sov in this system
        /// </summary>
        [XmlIgnoreAttribute]
        public long SOVAllianceTCU { get; set; }

        /// <summary>
        /// Gets or sets the name of the corporation holding sov in this system
        /// </summary>
        [XmlIgnoreAttribute]
        public long SOVCorp { get; set; }

        [XmlIgnoreAttribute]
        public float TCUOccupancyLevel { get; set; }

        [XmlIgnoreAttribute]
        public DateTime TCUVunerabliltyEnd { get; set; }

        [XmlIgnoreAttribute]
        public DateTime TCUVunerabliltyStart { get; set; }

        /// <summary>
        /// Gets or sets the Systems True Security Value
        /// </summary>
        public double TrueSec { get; set; }

        public override string ToString()
        {
            return $"{Name} ({Region})";
        }
    }
}