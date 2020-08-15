//-----------------------------------------------------------------------
// Intel Data
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SMT.EVEData
{
    /// <summary>
    /// Intel Data, Represents a single line of intel data
    /// </summary>
    public class IntelData
    {
        /// <summary>
        ///  Initializes a new instance of the <see cref="IntelData" /> class
        /// </summary>
        /// <param name="intelText">the raw line of text from the log file</param>
        public IntelData(string intelText, string intelChannel)
        {
            RawIntelString = intelText;
            // text will be in the format ﻿[ 2017.05.01 18:24:28 ] Charname > blah, blah blah
            int start = intelText.IndexOf('>') + 1;
            IntelString = intelText.Substring(start);
            IntelTime = DateTime.Now;
            Systems = new List<string>();
            ClearNotification = false;
            IntelChannel = $"({intelChannel})";
        }

        public bool ClearNotification { get; set; }


        public string IntelChannel { get; set; }

        /// <summary>
        /// Gets or sets the intel substring (minus time stamp and character name)
        /// </summary>
        public string IntelString { get; set; }

        /// <summary>
        /// Gets or sets the time we parsed the intel (note this is not in eve time)
        /// </summary>
        public DateTime IntelTime { get; set; }

        /// <summary>
        /// Gets or sets the raw line of text (incase we need to do anything else with it)
        /// </summary>
        public string RawIntelString { get; set; }

        /// <summary>
        /// Gets or sets the list of systems we matched when parsing this string
        /// </summary>
        public List<string> Systems { get; set; }

        // public override string ToString() => "[" + IntelTime.ToString("HH:mm") + "] " + IntelString;
    }
}