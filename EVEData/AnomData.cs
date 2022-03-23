//-----------------------------------------------------------------------
// EVE Anom Data
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace SMT.EVEData
{
    /// <summary>
    /// The Per system Anom Data
    /// </summary>
    public class AnomData
    {
        private List<string> CosmicSignatureTags;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnomData" /> class
        /// </summary>
        public AnomData()
        {
            Anoms = new SerializableDictionary<string, Anom>();
            CosmicSignatureTags = new List<string>();

            CosmicSignatureTags.Add("Cosmic Signature");
            CosmicSignatureTags.Add("Kosmische Signatur");
            CosmicSignatureTags.Add("Signature cosmique");
        }

        /// <summary>
        /// Gets or sets the Anom signature to data dictionary
        /// </summary>
        public SerializableDictionary<string, Anom> Anoms { get; set; }

        /// <summary>
        /// Gets or sets the name of the System this AnomData is for
        /// </summary>
        public string SystemName { get; set; }

        /// <summary>
        /// Update the AnomData from the string (usually the clipboard)
        /// </summary>
        /// <param name="pastedText">raw Anom strings</param>
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
                    // only care about "Cosmic Signature"
                    if (CosmicSignatureTags.Contains(words[1]))
                    {
                        validPaste = true;

                        string sigID = words[0];
                        string sigType = words[2];
                        string sigName = words[3];

                        if (string.IsNullOrEmpty(sigType))
                        {
                            sigType = "Unknown";
                        }

                        itemsToKeep.Add(sigID);

                        // valid sig
                        if (Anoms.Keys.Contains(sigID))
                        {
                            // updating an existing one
                            Anom an = Anoms[sigID];
                            if (an.Type == "Unknown")
                            {
                                an.Type = sigType;
                            }

                            if (!string.IsNullOrEmpty(sigName))
                            {
                                an.Name = sigName;
                            }
                        }
                        else
                        {
                            Anom an = new Anom();
                            an.Signature = sigID;
                            an.Type = sigType;

                            if (!string.IsNullOrEmpty(sigName))
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
}