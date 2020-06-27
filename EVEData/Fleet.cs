//-----------------------------------------------------------------------
// Fleet
//-----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SMT.EVEData
{
    /// <summary>
    /// Represents a fleet
    /// </summary>
    public class Fleet
    {
        public const string NoFleet = "Not in Fleet";

        public bool IsFleetBoss { get; set; }

        public DateTime NextFleetMembershipCheck { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Fleet" /> class.
        /// </summary>
        public Fleet()
        {
            FleetID = 0;
            FleetMOTD = string.Empty;
            Members = new ObservableCollection<FleetMember>();
        }

        /// <summary>
        /// Gets or sets the fleet id used in all ESI Fleet operations
        /// </summary>
        public long FleetID { get; set; }

        /// <summary>
        /// Gets or sets the fleet MOTD
        /// </summary>
        public string FleetMOTD { get; set; }

        /// <summary>
        /// Gets or sets the current Fleet Members collection
        /// </summary>
        public ObservableCollection<FleetMember> Members { get; set; }

        /// <summary>
        /// Fleet member info
        /// </summary>
        public class FleetMember
        {
            public bool IsValid { get; set; }

            public string ShipType { get; set; }

            public string Location { get; set; }

            public long CharacterID { get; set; }

            public string Name { get; set; }

            public override string ToString() => Name;
        }
    }
}