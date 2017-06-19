using System.Collections.Generic;
using System.Linq;



namespace SMT.EVEData
{
    public class RegionData
    {
        /// <summary>
        ///  English Name of this region
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// "Name" on Dotlan, used in URL's etc
        /// </summary>
        public string DotLanRef { get; set; }


        /// <summary>
        /// Systems in this Region
        /// </summary>
        public SerializableDictionary<string, System> Systems;

        /// <summary>
        ///  Jumps/Links between systems
        /// </summary>
        public List<Link> Jumps;


        public RegionData()
        {
            Systems = new SerializableDictionary<string, System>();
        }

        public RegionData(string name)
        {
            Name = name;
            DotLanRef = name.Replace(" ", "_");

            Systems = new SerializableDictionary<string, System>();
            Jumps = new List<Link>();
        }


        /// <summary>
        /// Is the system within this region
        /// </summary>
        public bool DoesSystemExist(string name)
        {
            foreach(System sys in Systems.Values.ToList())
            {
                if(sys.Name == name && Name == sys.Region )
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Is the System on this region map : note as we're using the dotlan layout we have out of region systems on the map for navigability reasons
        /// </summary>
        public bool IsSystemOnMap(string name)
        {
            // to catch out of region systems on the current map, ie region boundaries or strange intra-region settings
            foreach (System sys in Systems.Values.ToList())
            {
                if (sys.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return Name;
        }


    }
}
