using System;
using System.Collections.Generic;

namespace SMT.EVEData
{
    public class IntelData
    {
        /// <summary>
        /// Time we parsed the intel (note this is not in eve time)
        /// </summary>
        public DateTime IntelTime;

        /// <summary>
        /// The intel substring (minus time stamp and character name)
        /// </summary>
        public string IntelString;

        /// <summary>
        /// The raw line of text (incase we need to do anything else with it)
        /// </summary>
        public string RawIntelString;

        /// <summary>
        /// The list of systems we matched when parsing this string
        /// </summary>
        public List<string> Systems;

        public IntelData(string intelText)
        {
            RawIntelString = intelText;

            // text will be in the format ﻿[ 2017.05.01 18:24:28 ] Charname > blah, blah blah
            IntelString = intelText.Split('>')[1];
            IntelString.Trim();
            IntelTime = DateTime.Now;
            Systems = new List<string>();
        }

        public override string ToString()
        {
            return "[" + IntelTime.ToString("HH:mm") + "] " + IntelString;
        }
    }
}
