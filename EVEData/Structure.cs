using System;

namespace SMT.EVEData
{
    public class Structure
    {
        public Structure()
        {
        }

        public Structure(long TypeID, long StructureID, string SystemName, string StructureName)
        {
            ID = TypeID;
            Name = StructureName;
            System = SystemName;
            LastUpdate = DateTime.Now;
            State = PowerState.Unknown;

            switch (StructureID)
            {
                case 35832: // Astra
                    Type = StructureType.Astrahus;
                    break;

                case 35834: // Keepstar
                    Type = StructureType.Keepstar;
                    break;

                case 35833: // fortizar
                    Type = StructureType.Fortizar;
                    break;

                case 47512: // faction fortizar
                case 47513: // faction fortizar
                case 47514: // faction fortizar
                case 47515: // faction fortizar
                case 47516: // faction fortizar
                    Type = StructureType.FactionFortizar;
                    break;

                case 35827: // Sotiyo
                    Type = StructureType.Sotiyo;
                    break;

                case 35836: // Tatara
                    Type = StructureType.Tatara;
                    break;

                case 35826: // Azbel
                    Type = StructureType.Azbel;
                    break;

                case 35835: // Athanor
                    Type = StructureType.Athanor;
                    break;

                case 35825: // Raitaru
                    Type = StructureType.Raitaru;
                    break;

                case 35840: // CynoBeacon
                    Type = StructureType.CynoBeacon;
                    break;

                case 37534: // CynoJammer
                    Type = StructureType.CynoJammer;
                    break;

                case 35841: // JumpGate
                    Type = StructureType.JumpGate;
                    break;
            }
        }

        public enum PowerState
        {
            Normal,
            LowPower,
            Shield,
            Armor,
            Unknown,
        }

        public enum StructureType
        {
            // citadel
            Astrahus,

            Fortizar,
            FactionFortizar,
            Keepstar,

            // engineering
            Raitaru,

            Azbel,
            Sotiyo,

            // refineries
            Athanor,

            Tatara,
            NPCStation,

            // Ansiblex
            JumpGate,

            CynoBeacon,
            CynoJammer
        }

        public long ID { get; set; }
        public DateTime LastUpdate { get; set; }
        public string Name { get; set; }
        public PowerState State { get; set; }
        public string System { get; set; }
        public StructureType Type { get; set; }
    }
}