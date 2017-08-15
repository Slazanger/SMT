using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMT.EVEData
{
    public class Alliance
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public Alliance()
        {
            ID = string.Empty;
            Name = string.Empty;
        }
    }


}
