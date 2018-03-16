//-----------------------------------------------------------------------
// Fleet
//-----------------------------------------------------------------------
using System.Collections.ObjectModel;

namespace SMT.EVEData
{
    /// <summary>
    /// Represents a fleet
    /// </summary>
    public class Fleet
    {
        public const string NoFleet = "Not in Fleet";

        /// <summary>
        /// Initializes a new instance of the <see cref="Fleet" /> class.
        /// </summary>
        public Fleet()
        {
            FleetID = NoFleet;
            CharacterID = string.Empty;
            FleetMOTD = string.Empty;
            Members = new ObservableCollection<FleetMember>();
        }

        /// <summary>
        /// Gets or sets the fleet id used in all ESI Fleet operations
        /// </summary>
        public string FleetID { get; set; }

        /// <summary>
        /// Gets or sets the ID of the character the character this fleet info belongs to
        /// </summary>
        public string CharacterID { get; set; }

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
        public struct FleetMember
        {
            /// <summary>
            /// Fleet Member Character name
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Fleet member Location
            /// </summary>
            public string Location { get; set; }
            public override string ToString() => Name;
        }
    }
}
