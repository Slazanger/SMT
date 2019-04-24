using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMT.EVEData
{
    public class Coalition
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public Color CoalitionColor { get; set;  }
        //public Brush CoalitionBrush { get; set; }
        public List<long> MemberAlliances { get; set; }
    }
}
