//-----------------------------------------------------------------------
// Thera Connection
//-----------------------------------------------------------------------

namespace EVEData
{
    /// <summary>
    /// Represents a Connection into Thera (sourced from Eve-Scout)
    /// </summary>
    public class TheraConnection
    {
        /// <summary>
        ///Initializes a new instance of the <see cref="TheraConnection" /> class.
        /// </summary>
        /// <param name="sys">System</param>
        /// <param name="region">Region</param>
        /// <param name="inID">In Signature ID</param>
        /// <param name="outID">Out Signature ID</param>
        /// <param name="eol">End of Life Status</param>
        public TheraConnection(string sys, string region, string inID, string outID, string eol)
        {
            Region = region;
            System = sys;
            InSignatureID = inID;
            OutSignatureID = outID;
            EstimatedEOL = eol;
        }

        /// <summary>
        /// Gets or sets the Estimated End of Life status
        /// </summary>
        public string EstimatedEOL { get; set; }

        /// <summary>
        /// Gets or sets the signature ID from the specified system into Thera
        /// </summary>
        public string InSignatureID { get; set; }

        /// <summary>
        /// Gets or sets the signature ID from Thera to the specified system
        /// </summary>
        public string OutSignatureID { get; set; }

        /// <summary>
        /// Gets or sets the region that this system is in
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets the system with the connection to Thera
        /// </summary>
        public string System { get; set; }
    }
}