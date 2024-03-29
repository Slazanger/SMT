//-----------------------------------------------------------------------
// EVE Anom Data
//-----------------------------------------------------------------------

using EVEDataUtils;

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
        public HashSet<string> UpdateFromPaste(string pastedText)
        {
            HashSet<string> signaturesPresent = new HashSet<string>();

            string[] pastelines = pastedText.Split('\n');
            foreach (string line in pastelines)
            {
                // split on tabs
                string[] words = line.Split('\t');

                if (words.Length != 6)
                    continue;

                // only care about "Cosmic Signature"
                if (!CosmicSignatureTags.Contains(words[1]))
                    continue;

                string sigID = words[0];
                string sigType = words[2];
                string sigName = words[3];

                if (string.IsNullOrEmpty(sigType))
                    sigType = "Unknown";

                signaturesPresent.Add(sigID);

                if (Anoms.ContainsKey(sigID))
                {
                    // update an existing signature
                    Anom an = Anoms[sigID];

                    if (an.Type == "Unknown")
                        an.Type = sigType;

                    if (!string.IsNullOrEmpty(sigName))
                        an.Name = sigName;
                }
                else
                {
                    // add a new signature
                    Anom an = new Anom();
                    an.Signature = sigID;
                    an.Type = sigType;

                    if (!string.IsNullOrEmpty(sigName))
                        an.Name = sigName;

                    Anoms.Add(sigID, an);
                }
            }

            // find existing signatures that are missing from the paste
            var signaturesMissing = Anoms.Where(kvp => !signaturesPresent.Contains(kvp.Value.Signature))
                .Select(kvp => kvp.Key)
                .ToHashSet();

            return signaturesMissing;
        }
    }
}