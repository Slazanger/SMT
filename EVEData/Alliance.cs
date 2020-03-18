//-----------------------------------------------------------------------
// EVE Alliance
//-----------------------------------------------------------------------

namespace SMT.EVEData
{
    /// <summary>
    /// A simple container for Alliance items
    /// </summary>
    public class Alliance
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Alliance"/> class.
        /// </summary>
        public Alliance()
        {
            ID = string.Empty;
            Name = string.Empty;
        }

        /// <summary>
        /// Gets or sets the ID of Alliance
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the Resolved Name of Alliance
        /// </summary>
        public string Name { get; set; }
    }
}