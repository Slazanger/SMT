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
            else if (text == "Wormhole")
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
            bool validPaste = false;
            List<string> itemsToKeep = new List<string>();
            string[] pastelines = pastedText.Split('\n');
            foreach (string Line in pastelines)
            {
                // split on tabs
                string[] words = Line.Split('\t');
                if(words.Length == 6)
                {
                    // filter out "Cosmic Anomaly"
                    if (words[1] == "Cosmic Signature")
                    {
                        validPaste = true;

                        string SigID = words[0];
                        string SigType = words[2];
                        string SigName = words[3];

                        itemsToKeep.Add(SigID);

                        // valid sig
                        if (Anoms.Keys.Contains(SigID))
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

                
            }

            // if we had a valid paste dump any items we didnt reference, brute force scan and remove.. come back to this later..
            if(validPaste)
            {
                List<string> toRemove = new List<string>();
                foreach(string an in Anoms.Keys.ToList())
                {
                    if(!itemsToKeep.Contains(an))
                    {
                        toRemove.Add(an);
                    }
                }

                foreach(string s in toRemove)
                {
                    Anoms.Remove(s);
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
