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

        public override string ToString()
        {
            return Name;
        }

        public System()
        {
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
        }
    }
}