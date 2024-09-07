//-----------------------------------------------------------------------
// Map System
//-----------------------------------------------------------------------

using System.Numerics;
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
        public enum TextPosition
        {
            Top, Left, Right, Bottom
        }



        /// <summary>
        ///  Gets or sets the  actual eve system
        /// </summary>
        [XmlIgnoreAttribute]
        public System ActualSystem { get; set; }

        /// <summary>
        /// Gets or sets the list of points defining the cell around this system
        /// </summary>
        public List<Vector2> CellPoints { get; set; }

        /// <summary>
        /// A property to get the coordinate for the layout as a Vector2
        /// </summary>
        public Vector2 Layout { get; set; }

        /// <summary>
        /// Gets or sets the Name of the system
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the 
        /// </summary>
        public TextPosition TextPos { get; set; } = TextPosition.Bottom;

        /// <summary>
        /// Gets or sets if this system is considered out of region for layout purposes
        /// </summary>
        public bool OutOfRegion { get; set; }

        /// <summary>
        /// Gets or sets the region this system belongs to
        /// </summary>
        public string Region { get; set; }

        public override string ToString() => Name;
    }
}