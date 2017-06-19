using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMT.EVEData
{
    public class Character
    {
        public string Name { get; set; }
        public string LocalChatFile { get; set; }

        public string Location { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public Character()
        {
        }

        public Character(string name, string lcf, string location )
        {
            Name = name;
            LocalChatFile = lcf;
            Location = location;
        }

    }
}
