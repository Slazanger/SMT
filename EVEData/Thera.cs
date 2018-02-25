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
        public string SignatureID { get; set; }
        public string EstimatedEOL { get; set; }

        public TheraConnection(string sys, string region, string id, string eol)
        {
            Region = region;
            System = sys;
            SignatureID = id;
            EstimatedEOL = eol;
        }
    }
}
