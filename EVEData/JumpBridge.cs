//-----------------------------------------------------------------------
// Jump Bridge
//-----------------------------------------------------------------------

namespace SMT.EVEData
{
    /// <summary>
    /// A Player owned link between systems
    /// </summary>
    public class JumpBridge
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JumpBridge" /> class.
        /// </summary>
        public JumpBridge()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JumpBridge" /> class.
        /// </summary>
        /// <param name="f">From</param>
        /// <param name="t">To</param>
        public JumpBridge(string f, string t)
        {
            From = f;
            To = t;
        }


       


        /// <summary>
        /// Gets or sets the starting System
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// InGame Structure ID
        /// </summary>
        public long FromID { get; set; }

        /// <summary>
        /// Gets or sets the ending System
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// InGame Structure ID
        /// </summary>
        public long ToID { get; set; }

        public override string ToString()
        {
            return $"{From} <==> {To}";
        }
    }
}