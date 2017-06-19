using System;

namespace SMT.EVEData
{

    /// <summary>
    /// A link between 2 systems
    /// </summary>
    public class Link
    {

        public String From;
        public String To;

        /// <summary>
        /// Is this link one between constelations 
        /// </summary>
        public bool ConstelationLink;

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
