namespace SMT.EVEData
{
    /// <summary>
    /// A Player owned link between systems
    /// </summary>
    public class JumpBridge
    {
        public JumpBridge()
        {
        }

        public JumpBridge(string f, string fi, string t, string ti, bool friend)
        {
            From = f;
            FromInfo = fi;
            To = t;
            ToInfo = ti;
            Friendly = friend;
        }

        /// <summary>
        /// Starting System
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// Starting System Location (Planet-Moon)
        /// </summary>
        public string FromInfo { get; set; }

        /// <summary>
        /// Ending System
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// Ending System Location (Planet-Moon)
        /// </summary>
        public string ToInfo { get; set; }

        /// <summary>
        /// Is this a friendly or hostile Jumpbridge
        /// </summary>
        public bool Friendly { get; set; }

    }
}