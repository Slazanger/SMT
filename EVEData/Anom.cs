//-----------------------------------------------------------------------
// EVE Anoms
//-----------------------------------------------------------------------

using System;

namespace SMT.EVEData
{
    /// <summary>
    /// An Signature / Anom
    /// </summary>
    public class Anom
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Anom" /> class.
        /// </summary>
        public Anom()
        {
            Name = string.Empty;
            Type = string.Empty;
            TimeFound = DateTime.Now;
        }

        /// <summary>
        /// Type of Signature
        /// </summary>
        public enum SignatureType
        {
            /// <summary>
            /// Unknown signature type
            /// </summary>
            Unknown,

            /// <summary>
            /// A Combat site
            /// </summary>
            Combat,

            /// <summary>
            /// An Ore site
            /// </summary>
            Ore,

            /// <summary>
            /// A Gas site
            /// </summary>
            Gas,

            /// <summary>
            /// A Relic site
            /// </summary>
            Relic,

            /// <summary>
            /// A Data site
            /// </summary>
            Data,

            /// <summary>
            /// A wormhole to another place
            /// </summary>
            WormHole,
        }

        /// <summary>
        /// Gets or sets the name of the signature, eg True Sansha Base XXX
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Signature ID, eg XYZ
        /// </summary>
        public string Signature { get; set; }

        /// <summary>
        /// Gets or sets the time the signature was first found
        /// </summary>
        public DateTime TimeFound { get; set; }

        /// <summary>
        /// Gets or sets the type of signature
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// To String
        /// </summary>
        public override string ToString() => Signature + " " + Type + " " + Name;
    }
}