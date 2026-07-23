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
            ID = -1;
            Name = "????";
            Ticker = "????";
            CapitalSystemID = -1;
        }

        /// <summary>
        /// Gets or sets the ID of Alliance
        /// </summary>
        public long ID { get; set; }

        /// <summary>
        /// Gets or sets the Resolved Name of Alliance
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Ticker of Alliance
        /// </summary>
        public string Ticker { get; set; }

        /// <summary>
        /// The capital system for the alliance, this is used to determine the location of the alliance headquarters and jump bridge locations
        /// </summary>
        public int CapitalSystemID { get; set; }
    }
}