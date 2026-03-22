//-----------------------------------------------------------------------
// EVE Manager (partial)
//-----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using EVEDataUtils;
using EVEStandard;
using EVEStandard.Enumerations;
using EVEStandard.Models;
using EVEStandard.Models.SSO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SMT.EVEData
{
    // Auth, migration, load/save, characters from disk
    public partial class EveManager
    {
        private void MigrateOldSettings()
        {
            try
            {
                // if we have a storageroot folder; we have already migrated settings
                if(Directory.Exists(EveAppConfig.StorageRoot))
                {
                    return;
                }

                string oldSettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SMT");

                // prior to 1.39 all settings were stored in the My Documents\SMT\ folder
                if(Directory.Exists(oldSettingsFolder))
                {
                    // move the old settings folder to the new location
                    string newSettingsFolder = EveAppConfig.StorageRoot;
                    if(!Directory.Exists(newSettingsFolder))
                    {
                        Directory.CreateDirectory(newSettingsFolder);
                    }
                    // move the old settings to the new location
                    foreach(string file in Directory.GetFiles(oldSettingsFolder))
                    {
                        string fileName = Path.GetFileName(file);
                        string destFile = Path.Combine(newSettingsFolder, fileName);
                        if(!File.Exists(destFile))
                        {
                            File.Move(file, destFile);
                        }
                    }
                    // delete the old settings folder
                    Directory.Delete(oldSettingsFolder, true);
                }
            }
            catch
            {
                // if we fail to migrate the settings, we just ignore it
                // this is a one time migration so we don't need to worry about it again
            }
        }

        private void LoadTranslations()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "Translation.csv");
                if(File.Exists(path))
                {
                    // Read as UTF-8 (required for non-ASCII translations)
                    string[] lines = File.ReadAllLines(path, global::System.Text.Encoding.UTF8);
                    int count = 0;
                    foreach(string line in lines)
                    {
                        if(string.IsNullOrWhiteSpace(line)) continue;

                        string[] parts = line.Split(',');
                        if(parts.Length >= 2)
                        {
                            // Strip UTF-8 BOM if present on first column
                            string en = parts[0].Trim().Replace("\uFEFF", "");
                            string zh = parts[1].Trim();
                            if(!Translations.ContainsKey(en))
                            {
                                Translations.Add(en, zh);
                                count++;
                            }
                        }
                    }
                    global::System.Diagnostics.Debug.WriteLine($"Translation.csv: loaded {count} entries.");
                }
                else
                {
                    global::System.Diagnostics.Debug.WriteLine("Translation.csv: file not found: " + path);
                }
            }
            catch(global::System.Exception ex)
            {
                global::System.Diagnostics.Debug.WriteLine("Translation.csv: read error: " + ex.Message);
            }
        }
        /// <summary>
        /// Pending PKCE code_verifier for the next callback (same flow as old ESI.NET: one random string used to derive both challenge and verifier).
        /// </summary>
        private string _pendingPkceCodeVerifier;

        public string GetESILogonURL(string challengeCode)
        {
            // Match ESI.NET: code_verifier = base64url(UTF8(challengeCode)), code_challenge = base64url(SHA256(UTF8(code_verifier)))
            string codeVerifier = ToBase64UrlString(Encoding.UTF8.GetBytes(challengeCode));
            byte[] hash;
            using(var sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            }
            string codeChallenge = ToBase64UrlString(hash);
            _pendingPkceCodeVerifier = codeVerifier;
            return Sso.AuthorizeToSSOPKCEUri(VersionStr, codeChallenge, ESIScopes);
        }

        private static string ToBase64UrlString(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
        public async void HandleEveAuthSMTUri(Uri uri, string challengeCode)
        {
            var query = HttpUtility.ParseQueryString(uri.Query);
            if(query["code"] == null)
                return;

            string code = query["code"];
            // Use the code_verifier we stored when GetESILogonURL was called (PKCE requires verifier at token exchange, not the raw challenge string)
            string codeVerifier = _pendingPkceCodeVerifier ?? ToBase64UrlString(Encoding.UTF8.GetBytes(challengeCode));
            _pendingPkceCodeVerifier = null;

            AccessTokenDetails tokenDetails;
            try
            {
                tokenDetails = await Sso.VerifyAuthorizationForPKCEAuthAsync(code, codeVerifier);
                if(tokenDetails == null || tokenDetails.ExpiresIn <= 0)
                    return;
            }
            catch
            {
                return;
            }

            CharacterDetails characterDetails;
            try
            {
                characterDetails = await Sso.GetCharacterDetailsAsync(tokenDetails.AccessToken);
                if(characterDetails == null)
                    return;
            }
            catch
            {
                return;
            }

            LocalCharacter esiChar = FindCharacterByName(characterDetails.CharacterName);
            if(esiChar == null)
            {
                esiChar = new LocalCharacter(characterDetails.CharacterName, string.Empty, string.Empty);
                AddCharacter(esiChar);
            }

            esiChar.ESIRefreshToken = tokenDetails.RefreshToken;
            esiChar.ESILinked = true;
            esiChar.ESIAccessToken = tokenDetails.AccessToken;
            esiChar.ESIAccessTokenExpiry = tokenDetails.ExpiresUtc.ToLocalTime();
            esiChar.ID = characterDetails.CharacterId;
            esiChar.ESIScopesStored = characterDetails.Scopes != null ? string.Join(" ", characterDetails.Scopes) : string.Empty;
        }
        public void LoadFromDisk()
        {
            SystemIDToName = new SerializableDictionary<long, string>();

            Regions = Serialization.DeserializeFromDisk<List<MapRegion>>(Path.Combine(DataRootFolder, "MapLayout.dat"));
            Systems = Serialization.DeserializeFromDisk<List<System>>(Path.Combine(DataRootFolder, "Systems.dat"));

            ShipTypes = Serialization.DeserializeFromDisk<SerializableDictionary<string, string>>(Path.Combine(DataRootFolder, "ShipTypes.dat"));

            foreach(System s in Systems)
            {
                SystemIDToName[s.ID] = s.Name;
            }

            CharacterIDToName = new SerializableDictionary<int, string>();
            AllianceIDToName = new SerializableDictionary<int, string>();
            AllianceIDToTicker = new SerializableDictionary<int, string>();

            // patch up any links
            foreach(System s in Systems)
            {
                NameToSystem[s.Name] = s;
                IDToSystem[s.ID] = s;
            }

            // now add the beacons
            string cynoBeaconsFile = Path.Combine(SaveDataRootFolder, "CynoBeacons.txt");
            if(File.Exists(cynoBeaconsFile))
            {
                StreamReader file = new StreamReader(cynoBeaconsFile);

                string line;
                while((line = file.ReadLine()) != null)
                {
                    string system = line.Trim();

                    System s = GetEveSystem(system);
                    if(s != null)
                    {
                        s.HasJumpBeacon = true;
                    }
                }
            }

            Init();
        }
        public void LoadJumpBridgeData()
        {
            JumpBridges = new List<JumpBridge>();

            string dataFilename = Path.Combine(SaveDataRootFolder, "JumpBridges_" + JumpBridge.SaveVersion + ".dat");
            if(!File.Exists(dataFilename))
            {
                return;
            }

            try
            {
                List<JumpBridge> loadList;
                XmlSerializer xms = new XmlSerializer(typeof(List<JumpBridge>));

                FileStream fs = new FileStream(dataFilename, FileMode.Open, FileAccess.Read);
                XmlReader xmlr = XmlReader.Create(fs);

                loadList = (List<JumpBridge>)xms.Deserialize(xmlr);

                foreach(JumpBridge j in loadList)
                {
                    JumpBridges.Add(j);
                }
            }
            catch
            {
            }
        }
        public async Task ResolveAllianceIDs(List<int> IDs)
        {
            if(IDs.Count == 0)
            {
                return;
            }

            // strip out any ID's we already know..
            List<int> UnknownIDs = new List<int>();
            foreach(int l in IDs)
            {
                if((!AllianceIDToName.ContainsKey(l) || !AllianceIDToTicker.ContainsKey(l)) && !UnknownIDs.Contains(l))
                {
                    UnknownIDs.Add(l);
                }
            }

            if(UnknownIDs.Count == 0)
            {
                return;
            }

            try
            {
                var idsLong = UnknownIDs.ConvertAll(i => (long)i);
                var esra = await EveApiClient.Universe.GetNamesAndCategoriesFromIdsAsync(idsLong);
                if(ESIHelpers.ValidateESICall(esra))
                {
                    foreach(UniverseIdsToNames ri in esra.Model)
                    {
                        if(ri.Category == "alliance")
                        {
                            var esraA = await EveApiClient.Alliance.GetAllianceInfoAsync(ri.Id);
                            if(ESIHelpers.ValidateESICall(esraA))
                            {
                                AllianceIDToTicker[(int)ri.Id] = esraA.Model.Ticker;
                                AllianceIDToName[(int)ri.Id] = esraA.Model.Name;
                            }
                            else
                            {
                                AllianceIDToTicker[(int)ri.Id] = "???????????????";
                                AllianceIDToName[(int)ri.Id] = "?????";
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Update the Character ID data for specified list
        /// </summary>
        public async Task ResolveCharacterIDs(List<int> IDs)
        {
            if(IDs.Count == 0)
            {
                return;
            }

            // strip out any ID's we already know..
            List<int> UnknownIDs = new List<int>();
            foreach(int l in IDs)
            {
                if(!CharacterIDToName.ContainsKey(l))
                {
                    UnknownIDs.Add(l);
                }
            }

            if(UnknownIDs.Count == 0)
            {
                return;
            }

            try
            {
                var idsLong = UnknownIDs.ConvertAll(i => (long)i);
                var esra = await EveApiClient.Universe.GetNamesAndCategoriesFromIdsAsync(idsLong);
                if(ESIHelpers.ValidateESICall(esra))
                {
                    foreach(UniverseIdsToNames ri in esra.Model)
                    {
                        if(ri.Category == "character")
                        {
                            CharacterIDToName[(int)ri.Id] = ri.Name;
                        }
                    }
                }
            }
            catch { }
        }
        public void SaveData()
        {
            // save off only the ESI authenticated Characters so create a new copy to serialise from..
            List<LocalCharacter> saveList = new List<LocalCharacter>();

            foreach(LocalCharacter c in GetLocalCharactersCopy())
            {
                if(!string.IsNullOrEmpty(c.ESIRefreshToken))
                {
                    saveList.Add(c);
                }
            }

            XmlSerializer xms = new XmlSerializer(typeof(List<LocalCharacter>));
            string dataFilename = Path.Combine(SaveDataRootFolder, "Characters_" + LocalCharacter.SaveVersion + ".dat");

            using(TextWriter tw = new StreamWriter(dataFilename))
            {
                xms.Serialize(tw, saveList);
            }

            string jbFileName = Path.Combine(SaveDataRootFolder, "JumpBridges_" + JumpBridge.SaveVersion + ".dat");
            Serialization.SerializeToDisk<List<JumpBridge>>(JumpBridges, jbFileName);

            List<string> beaconsToSave = new List<string>();
            foreach(System s in Systems)
            {
                if(s.HasJumpBeacon)
                {
                    beaconsToSave.Add(s.Name);
                }
            }

            // save the intel channels / intel filters
            File.WriteAllLines(Path.Combine(SaveDataRootFolder, "IntelChannels.txt"), IntelFilters);
            File.WriteAllLines(Path.Combine(SaveDataRootFolder, "IntelClearFilters.txt"), IntelClearFilters);
            File.WriteAllLines(Path.Combine(SaveDataRootFolder, "IntelIgnoreFilters.txt"), IntelIgnoreFilters);
            File.WriteAllLines(Path.Combine(SaveDataRootFolder, "IntelAlertFilters.txt"), IntelAlertFilters);
            File.WriteAllLines(Path.Combine(SaveDataRootFolder, "CynoBeacons.txt"), beaconsToSave);
        }
        private void LoadCharacters()
        {
            string dataFilename = Path.Combine(SaveDataRootFolder, "Characters_" + LocalCharacter.SaveVersion + ".dat");
            if(!File.Exists(dataFilename))
            {
                return;
            }

            try
            {
                List<LocalCharacter> loadList;
                XmlSerializer xms = new XmlSerializer(typeof(List<LocalCharacter>));

                FileStream fs = new FileStream(dataFilename, FileMode.Open, FileAccess.Read);
                XmlReader xmlr = XmlReader.Create(fs);

                loadList = (List<LocalCharacter>)xms.Deserialize(xmlr);

                foreach(LocalCharacter c in loadList)
                {
                    c.ESIAccessToken = string.Empty;
                    c.ESIAccessTokenExpiry = DateTime.MinValue;
                    c.LocalChatFile = string.Empty;
                    c.Location = string.Empty;
                    c.Region = string.Empty;

                    AddCharacter(c);
                }
            }
            catch
            {
            }
        }
    }
}
