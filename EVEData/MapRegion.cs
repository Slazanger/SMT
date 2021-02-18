//-----------------------------------------------------------------------
// Map Region
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;

namespace SMT.EVEData
{
    /// <summary>
    /// Represents a Map of a Region (will have out of region systems on the map)
    /// </summary>
    public class MapRegion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MapRegion" /> class.
        /// </summary>
        public MapRegion()
        {
            MapSystems = new SerializableDictionary<string, MapSystem>();
            RegionLinks = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapRegion" /> class.
        /// </summary>
        /// <param name="name">Name of the Region</param>
        /// <param name="faction">Faction (if any) of the region</param>
        /// <param name="universeViewX">X Location to render this on the universe map</param>
        /// <param name="universeViewY">Y Location to render this on the universe map</param>
        public MapRegion(string name, string id, string faction, double universeViewX, double universeViewY)
        {
            Name = name;
            DotLanRef = name.Replace(" ", "_");
            ID = id;

            Faction = faction;
            UniverseViewX = universeViewX;
            UniverseViewY = universeViewY;

            MapSystems = new SerializableDictionary<string, MapSystem>();
            RegionLinks = new List<string>();

            HasHighSecSystems = false;
            HasLowSecSystems = false;
            HasNullSecSystems = false;
        }

        /// <summary>
        /// Gets or sets the "Name" on Dotlan, used in URL's etc
        /// </summary>
        public string DotLanRef { get; set; }

        /// <summary>
        /// Gets or sets the Regions Faction name
        /// </summary>
        public string Faction { get; set; }

        public bool HasHighSecSystems { get; set; }

        public bool HasLowSecSystems { get; set; }

        public bool HasNullSecSystems { get; set; }

        /// <summary>
        /// Gets or sets the region ID
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of systems on this map
        /// </summary>
        public SerializableDictionary<string, MapSystem> MapSystems { get; set; }

        /// <summary>
        ///  Gets or sets the English name of this region
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the list of links to other Regions
        /// </summary>
        public List<string> RegionLinks { get; set; }

        /// <summary>
        /// Gets or sets the Regions X coord on the universe map
        /// </summary>
        public double RegionX { get; set; }


        /// <summary>
        /// Gets or sets the Regions Y coord on the universe map
        /// </summary>
        public double RegionY { get; set; }

        /// <summary>
        /// Gets or sets the Regions X coord on the universe map
        /// </summary>
        public double UniverseViewX { get; set; }

        /// <summary>
        /// Gets or sets the Regions Y coord on the universe map
        /// </summary>
        public double UniverseViewY { get; set; }

        public List<Point> RegionOutline { get; set; }

        /// <summary>
        /// Is the System on this region map : note as we're using the dotlan layout we have out of region systems on the map for navigability reasons
        /// </summary>
        /// <param name="name">Name of the System to Check</param>
        /// <returns></returns>
        public bool IsSystemOnMap(string name)
        {
            // to catch out of region systems on the current map, ie region boundaries or strange intra-region settings
            foreach (KeyValuePair<string, MapSystem> kvp in MapSystems)
            {
                if (kvp.Value.Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString() => Name;
    }
}