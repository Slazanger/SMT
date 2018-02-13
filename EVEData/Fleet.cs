using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMT.EVEData
{
    public class Fleet
    {
        public string FleetID { get; set; }
        public string CharacterID { get; set; }
        public string FleetMOTD { get; set; }

        public const string NO_FLEET = "Not in Fleet";

        public struct FleetMember
        {
            public string Name { get; set; }
            public string Location { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        public ObservableCollection<FleetMember> Members;

        public Fleet()
        {
            FleetID = NO_FLEET;
            CharacterID = String.Empty;
            FleetMOTD = String.Empty;
            Members = new ObservableCollection<FleetMember>();
        }
    }
}
