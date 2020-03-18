using System.Collections.Generic;
using System.Windows.Media;

namespace SMT.EVEData
{
    public class Coalition
    {
        public Color CoalitionColor { get; set; }
        public string ID { get; set; }

        //public Brush CoalitionBrush { get; set; }
        public List<long> MemberAlliances { get; set; }

        public string Name { get; set; }
    }
}