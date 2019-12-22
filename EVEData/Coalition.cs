using System.Collections.Generic;
using System.Windows.Media;

namespace SMT.EVEData
{
    public class Coalition
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public Color CoalitionColor { get; set; }
        //public Brush CoalitionBrush { get; set; }
        public List<long> MemberAlliances { get; set; }
    }
}
