using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public double DotlanX { get; set; }
        public double DotLanY { get; set; }
    
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



        public override string ToString()
        {
            return Name;
        }


        public System()
        {

        }

        public System(string name, string id, double x, double y, string region, bool station)
        {
            Name = name;
            ID = id;
            DotlanX = x;
            DotLanY = y;
            Region = region;
            HasNPCStation = station;


            NPCKillsLastHour = -1;
            PodKillsLastHour = -1;
            ShipKillsLastHour = -1;
        }
    }
}
