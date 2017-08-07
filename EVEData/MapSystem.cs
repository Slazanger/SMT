using System.Xml.Serialization;

namespace SMT.EVEData
{

    /// <summary>
    /// This is a representation of a System on a map.. usually these would be in the same region, however 
    /// these will be duplicated in the case of the inter-region link systems and in regions where it makes
    /// sense to draw out of region systems within the current region for layout purposes
    /// </summary>
    public class MapSystem
    {
        public string Name { get; set; }

        public string Region { get; set; }

        public bool OutOfRegion { get; set; }


        public double LayoutX { get; set; }

        public double LayoutY { get; set; }

        /// <summary>
        ///  the main data store for the actual eve system data
        /// </summary>
        [XmlIgnoreAttribute]
        public System ActualSystem;


        public override string ToString()
        {
            return Name;
        }

    }
}
