using System.Collections.Generic;
using System.Xml.Serialization;

namespace SMT.EVEData
{
    public class System
    {
        public string Name { get; set; }

        public string Region { get; set; }

        /// <summary>
        /// Eve's internal ID for this System
        /// </summary>
        public string ID { get; set; }

        public string ConstellationID { get; set; }

        public double ActualX { get; set; }

        public double ActualY { get; set; }

        public double ActualZ { get; set; }

        public double Security { get; set; }

        public bool HasNPCStation { get; set; }

        [XmlIgnoreAttribute]
        public int NPCKillsLastHour { get; set; }

        [XmlIgnoreAttribute]
        public int PodKillsLastHour { get; set; }

        [XmlIgnoreAttribute]
        public int ShipKillsLastHour { get; set; }

        [XmlIgnoreAttribute]
        public int JumpsLastHour { get; set; }

        [XmlIgnoreAttribute]
        public string SOVAlliance { get; set; }

        [XmlIgnoreAttribute]
        public string SOVCorp { get; set; }

        [XmlIgnoreAttribute]
        public string SOVFaction { get; set; }


        public List<string> Jumps { get; set; }


        public override string ToString() => Name;

        public System()
        {
            Jumps = new List<string>();
        }

        public System(string name, string id, string region, bool station)
        {
            Name = name;
            ID = id;
            Region = region;
            HasNPCStation = station;

            NPCKillsLastHour = -1;
            PodKillsLastHour = -1;
            ShipKillsLastHour = -1;
            JumpsLastHour = -1;

            Jumps = new List<string>();
        }
    }
}