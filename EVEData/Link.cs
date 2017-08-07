namespace SMT.EVEData
{
    /// <summary>
    /// A link between 2 systems
    /// </summary>
    public class Link
    {
        public string From { get; set; }
        public string To { get; set; }

        /// <summary>
        /// Is this link one between constelations
        /// </summary>
        public bool ConstelationLink { get; set; }

        public Link()
        {
        }

        public Link(string f, string t, bool c)
        {
            From = f;
            To = t;
            ConstelationLink = c;
        }
    }
}