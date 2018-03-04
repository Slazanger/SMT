using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMT.EVEData
{
    public class TheraConnection
    {
        public string System { get; set; }
        public string Region { get; set; }
        public string InSignatureID { get; set; }
        public string OutSignatureID { get; set; }
        public string EstimatedEOL { get; set; }

        public TheraConnection(string sys, string region, string inID, string outID, string eol)
        {
            Region = region;
            System = sys;
            InSignatureID = inID;
            OutSignatureID = outID;
            EstimatedEOL = eol;
        }
    }
}
