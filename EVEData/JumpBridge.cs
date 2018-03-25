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
        /// <param name="fi">From Info</param>
        /// <param name="t">To</param>
        /// <param name="ti">ToInfo</param>
        /// <param name="friend">Is Friendly?</param>
        public JumpBridge(string f, string fi, string t, string ti, bool friend)
        {
            From = f;
            FromInfo = fi;
            To = t;
            ToInfo = ti;
            Friendly = friend;
        }

        /// <summary>
        /// Gets or sets the starting System
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// Gets or sets the starting system location info(Planet-Moon)
        /// </summary>
        public string FromInfo { get; set; }

        /// <summary>
        /// Gets or sets the ending System
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// Gets or sets the ending system Location info usually (Planet-Moon)
        /// </summary>
        public string ToInfo { get; set; }

        /// <summary>
        /// Gets or sets if this is a friendly or hostile Jump bridge
        /// </summary>
        public bool Friendly { get; set; }
    }
}