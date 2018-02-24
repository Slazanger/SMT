using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

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
            Name = string.Empty;
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
            foreach (string line in pastelines)
            {
                // split on tabs
                string[] words = line.Split('\t');
                if (words.Length == 6)
                {
                    // filter out "Cosmic Anomaly"
                    if (words[1] == "Cosmic Signature")
                    {
                        validPaste = true;

                        string sigID = words[0];
                        string sigType = words[2];
                        string sigName = words[3];

                        itemsToKeep.Add(sigID);

                        // valid sig
                        if (Anoms.Keys.Contains(sigID))
                        {
                            // updating an existing one
                            Anom an = Anoms[sigID];
                            if (an.Type == AnomType.Unknown)
                            {
                                an.Type = Anom.GetTypeFromString(sigType);
                            }

                            if (sigName != string.Empty)
                            {
                                an.Name = sigName;
                            }
                        }
                        else
                        {
                            Anom an = new Anom();
                            an.Signature = sigID;

                            if (sigType != string.Empty)
                            {
                                an.Type = Anom.GetTypeFromString(sigType);
                            }

                            if (sigName != string.Empty)
                            {
                                an.Name = sigName;
                            }

                            Anoms.Add(sigID, an);
                        }
                    }
                }
            }

            // if we had a valid paste dump any items we didnt reference, brute force scan and remove.. come back to this later..
            if (validPaste)
            {
                List<string> toRemove = new List<string>();
                foreach (string an in Anoms.Keys.ToList())
                {
                    if (!itemsToKeep.Contains(an))
                    {
                        toRemove.Add(an);
                    }
                }

                foreach (string s in toRemove)
                {
                    Anoms.Remove(s);
                }
            }
        }
    }

    public class AnomManager : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public SerializableDictionary<string, AnomData> Systems { get; set; }


        private AnomData m_ActiveSystem;
        public AnomData ActiveSystem
        {
            get
            {
                return m_ActiveSystem;
            }
            set
            {
                m_ActiveSystem = value;
                OnPropertyChanged("ActiveSystem");
            }
        }

        public AnomManager()
        {
            Systems = new SerializableDictionary<string, AnomData>();
            ActiveSystem = null;
        }

        public AnomData GetSystemAnomData(string sysName)
        {
            AnomData ret;
            if (Systems.Keys.Contains(sysName))
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