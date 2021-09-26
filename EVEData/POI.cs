using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMT.EVEData
{
    /// <summary>
    /// A Point of Interest in the game
    /// </summary>
    public class POI
    {
        public string System { get; set; } 
        public string Type { get; set; }
        public string ShortDesc { get; set; }
        public string LongDesc { get; set; }
    }
}
