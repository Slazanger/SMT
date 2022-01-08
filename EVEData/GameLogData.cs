using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMT.EVEData
{
    public class GameLogData
    {
        public string Character { get; set; }
        public DateTime Time { get; set; }

        public string Severity { get; set; }

        public string Text { get; set; }

        public string RawText { get; set; }
    }
}
