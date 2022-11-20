using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMT.EVEData
{
    public  class FactionWarfareSystemInfo
    {
        public enum State
        {
            None = 0,
            Rearguard = 1,
            CommandLineOperation = 2,
            Frontline = 3
        };

        public int OwnerID { get; set; }
        public string OwnerName { get; set; }

        public int OccupierID { get; set; }
        public string OccupierName { get; set; }
        public State SystemState { get; set; }

        public int SystemID { get; set; }
        public int LinkSystemID { get; set; }
        public string SystemName { get; set; }
        public int VictoryPoints { get; set; }
        public int VictoryPointsThreshold { get; set; }
        

        public static string OwnerIDToName(int id)
        {
            switch (id)
            {
                case 500001: return "Caldari State";
                case 500002: return "Minmatar Republic";
                case 500003: return "Amarr Empire";
                case 500004: return "Gallente Federation";
            }
            return "Unknown";
        }
    }
}
