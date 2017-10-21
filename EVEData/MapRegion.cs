using System.Collections.Generic;
using System.Linq;

namespace SMT.EVEData
{
    public class MapRegion
    {
        /// <summary>
        ///  English Name of this region
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// "Name" on Dotlan, used in URL's etc
        /// </summary>
        public string DotLanRef { get; set; }

        public SerializableDictionary<string, MapSystem> MapSystems { get; set; }



        public MapRegion()
        {
            MapSystems = new SerializableDictionary<string, MapSystem>();
        }

        public MapRegion(string name)
        {
            Name = name;
            DotLanRef = name.Replace(" ", "_");

            MapSystems = new SerializableDictionary<string, MapSystem>();
        }


        /// <summary>
        /// Is the System on this region map : note as we're using the dotlan layout we have out of region systems on the map for navigability reasons
        /// </summary>
        public bool IsSystemOnMap(string name)
        {
            // to catch out of region systems on the current map, ie region boundaries or strange intra-region settings
            foreach (MapSystem sys in MapSystems.Values.ToList())
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