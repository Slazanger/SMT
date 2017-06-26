using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMT.EVEData
{

    public enum AnomType
    {
        Unknown,
        Combat,
        Ore,
        Gas,
        Relic,
        Data,
        WormHole,
    }

    public class Anom
    {
        public Anom()
        {
            Name = "";
            Type = AnomType.Unknown;

            TimeFound = DateTime.Now;
        }

        public string Signature { get; set; }
        public AnomType Type { get; set; }
        public string Name { get; set; }

        public DateTime TimeFound { get; set; }


        public override string ToString()
        {
            return Signature + " " + Type + " " + Name;
        }


        public static AnomType GetTypeFromString(string text)
        {
            if (text == "Combat Site")
            {
                return AnomType.Combat;
            }
            else if (text == "Ore Site")
            {
                return AnomType.Ore;
            }
            else if (text == "Gas Site")
            {
                return AnomType.Gas;
            }
            else if (text == "Relic Site")
            {
                return AnomType.Relic;
            }
            else if (text == "Data Site")
            {
                return AnomType.Data;
            }
            else if (text == "Worm Hole")
            {
                return AnomType.WormHole;
            }
            else
            {
                return AnomType.Unknown;
            }
        }

    }

    public class AnomData
    {
        public string SystemName { get; set; } 

        public SerializableDictionary<string, Anom> Anoms { get; set; }



        public AnomData()
        {
            Anoms = new SerializableDictionary<string, Anom>();
        }


        public void UpdateFromPaste(string pastedText)
        {

            string[] pastelines = pastedText.Split('\n');
            foreach (string Line in pastelines)
            {
                // split on tabs
                string[] words = Line.Split('\t');
                if(words.Length == 6)
                {
                    if(words[1] == "Cosmic Signature" || words[1] == "Cosmic Anomaly")
                    {
                        string SigID = words[0];
                        string SigType = words[2];
                        string SigName = words[3];

                        // valid sig
                        if(Anoms.Keys.Contains(SigID))
                        {
                            // updating an existing one
                            Anom an = Anoms[SigID];
                            if (an.Type == AnomType.Unknown)
                            {
                                an.Type = Anom.GetTypeFromString(SigType);
                            }

                            if(SigName != "")
                            {
                                an.Name = SigName;
                            }

                        }
                        else
                        {
                            Anom an = new Anom();
                            an.Signature = SigID;                           
                           
                            if(SigType != "")
                            {
                                an.Type = Anom.GetTypeFromString(SigType);
                            }
                            if(SigName != "")
                            {
                                an.Name = SigName;
                            }
                            Anoms.Add(SigID, an);
                        }
                    }
                }

                foreach( string Word in words)
                {

                }
            }
        }
    }

    public class AnomManager
    {
        public SerializableDictionary<string, AnomData> Systems { get; set; }


        public AnomManager()
        {
            Systems = new SerializableDictionary<string, AnomData>();
        }

        public AnomData GetSystemAnomData(string sysName)
        {
            AnomData ret;
            if(Systems.Keys.Contains(sysName))
            {
                ret = Systems[sysName];
            }
            else
            {
                ret = new AnomData();
                ret.SystemName = sysName;
                Systems.Add(sysName, ret);
            }

            return ret;
        }

    }


}
