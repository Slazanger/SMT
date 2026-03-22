using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using EVEStandard.Models.SSO;
using Utils;

namespace EVEData;

// Auth, migration, load/save, characters from disk
public partial class EveManager
{
    /// <summary>
    /// Migrate old settings from My Documents\SMT\ to the new storage location
    /// (AppData\Roaming\SMT\) if they exist.
    ///
    /// This is a one-time migration that will be skipped on subsequent runs
    /// if the new storage location already exists. If the migration fails for
    /// any reason, we simply ignore it and continue with defaults, as this is
    /// not critical and only affects users who had settings in the old location.
    /// </summary>
    private void MigrateOldSettings()
    {
        try
        {
            // if we have a storageroot folder; we have already migrated settings
            if (Directory.Exists(EveAppConfig.StorageRoot)) return;

            var oldSettingsFolder =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SMT");

            // prior to 1.39 all settings were stored in the My Documents\SMT\ folder
            if (Directory.Exists(oldSettingsFolder))
            {
                // move the old settings folder to the new location
                var newSettingsFolder = EveAppConfig.StorageRoot;
                if (!Directory.Exists(newSettingsFolder)) Directory.CreateDirectory(newSettingsFolder);
                // move the old settings to the new location
                foreach (var file in Directory.GetFiles(oldSettingsFolder))
                {
                    var fileName = Path.GetFileName(file);
                    var destFile = Path.Combine(newSettingsFolder, fileName);
                    if (!File.Exists(destFile)) File.Move(file, destFile);
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

    /// <summary>
    /// Load the English/Chinese translations from the Translation.csv file in the data folder, if it exists.
    /// </summary>
    private void LoadTranslations()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "Translation.csv");
            if (File.Exists(path))
            {
                // Read as UTF-8 (required for non-ASCII translations)
                var lines = File.ReadAllLines(path, Encoding.UTF8);
                var count = 0;
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(',');
                    if (parts.Length >= 2)
                    {
                        // Strip UTF-8 BOM if present on first column
                        var en = parts[0].Trim().Replace("\uFEFF", "");
                        var zh = parts[1].Trim();
                        if (!Translations.ContainsKey(en))
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
        catch (Exception ex)
        {
            global::System.Diagnostics.Debug.WriteLine("Translation.csv: read error: " + ex.Message);
        }
    }

    /// <summary>
    /// Pending PKCE code_verifier for the next callback (same flow as old ESI.NET: one random string used to derive both challenge and verifier).
    /// </summary>
    private string _pendingPkceCodeVerifier;


    /// <summary>
    /// Get the URL to log in to ESI and link a character, using PKCE for enhanced security.
    /// The challengeCode is used to derive both the code_challenge sent to ESI
    /// and the code_verifier stored for later token exchange.
    /// </summary>
    /// <param name="challengeCode"></param>
    /// <returns></returns>
    public string GetESILogonURL(string challengeCode)
    {
        // Match ESI.NET: code_verifier = base64url(UTF8(challengeCode)), code_challenge = base64url(SHA256(UTF8(code_verifier)))
        var codeVerifier = ToBase64UrlString(Encoding.UTF8.GetBytes(challengeCode));
        byte[] hash;
        using (var sha256 = SHA256.Create())
        {
            hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        }

        var codeChallenge = ToBase64UrlString(hash);
        _pendingPkceCodeVerifier = codeVerifier;
        return Sso.AuthorizeToSSOPKCEUri(VersionStr, codeChallenge, ESIScopes);
    }

    /// <summary>
    /// Convert a byte array to a Base64 URL-safe string, without padding,
    /// and with '+' replaced by '-' and '/' replaced by '_', as required by PKCE.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    private static string ToBase64UrlString(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }


    /// <summary>
    /// Handle the callback from ESI after the user has logged in and authorized SMT,
    /// using PKCE to securely exchange the authorization code for tokens.
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="challengeCode"></param>
    public async void HandleEveAuthSMTUri(Uri uri, string challengeCode)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);
        if (query["code"] == null)
            return;

        var code = query["code"];
        // Use the code_verifier we stored when GetESILogonURL was called (PKCE requires verifier at token exchange, not the raw challenge string)
        var codeVerifier = _pendingPkceCodeVerifier ?? ToBase64UrlString(Encoding.UTF8.GetBytes(challengeCode));
        _pendingPkceCodeVerifier = null;

        AccessTokenDetails tokenDetails;
        try
        {
            tokenDetails = await Sso.VerifyAuthorizationForPKCEAuthAsync(code, codeVerifier);
            if (tokenDetails == null || tokenDetails.ExpiresIn <= 0)
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
            if (characterDetails == null)
                return;
        }
        catch
        {
            return;
        }

        var esiChar = FindCharacterByName(characterDetails.CharacterName);
        if (esiChar == null)
        {
            esiChar = new LocalCharacter(characterDetails.CharacterName, string.Empty, string.Empty);
            AddCharacter(esiChar);
        }

        esiChar.ESIRefreshToken = tokenDetails.RefreshToken;
        esiChar.ESILinked = true;
        esiChar.ESIAccessToken = tokenDetails.AccessToken;
        esiChar.ESIAccessTokenExpiry = tokenDetails.ExpiresUtc.ToLocalTime();
        esiChar.ID = characterDetails.CharacterId;
        esiChar.ESIScopesStored =
            characterDetails.Scopes != null ? string.Join(" ", characterDetails.Scopes) : string.Empty;
    }


    /// <summary>
    /// Load the map layout, system data, ship types, and other related information from disk.
    /// </summary>
    public void LoadFromDisk()
    {
        SystemIDToName = new SerializableDictionary<long, string>();

        Regions = Serialization.DeserializeFromDisk<List<MapRegion>>(Path.Combine(DataRootFolder, "MapLayout.dat"));
        Systems = Serialization.DeserializeFromDisk<List<System>>(Path.Combine(DataRootFolder, "Systems.dat"));

        ShipTypes =
            Serialization.DeserializeFromDisk<SerializableDictionary<string, string>>(Path.Combine(DataRootFolder,
                "ShipTypes.dat"));

        foreach (var s in Systems) SystemIDToName[s.ID] = s.Name;

        CharacterIDToName = new SerializableDictionary<int, string>();
        AllianceIDToName = new SerializableDictionary<int, string>();
        AllianceIDToTicker = new SerializableDictionary<int, string>();

        // patch up any links
        foreach (var s in Systems)
        {
            NameToSystem[s.Name] = s;
            IDToSystem[s.ID] = s;
        }

        // now add the beacons
        var cynoBeaconsFile = Path.Combine(SaveDataRootFolder, "CynoBeacons.txt");
        if (File.Exists(cynoBeaconsFile))
        {
            var file = new StreamReader(cynoBeaconsFile);

            string line;
            while ((line = file.ReadLine()) != null)
            {
                var system = line.Trim();

                var s = GetEveSystem(system);
                if (s != null) s.HasJumpBeacon = true;
            }
        }

        Init();
    }


    /// <summary>
    /// Load the Jump Bridge data from disk, if it exists. 
    /// </summary>
    public void LoadJumpBridgeData()
    {
        JumpBridges = new List<JumpBridge>();

        var dataFilename = Path.Combine(SaveDataRootFolder, "JumpBridges_" + JumpBridge.SaveVersion + ".dat");
        if (!File.Exists(dataFilename)) return;

        try
        {
            List<JumpBridge> loadList;
            var xms = new XmlSerializer(typeof(List<JumpBridge>));

            var fs = new FileStream(dataFilename, FileMode.Open, FileAccess.Read);
            var xmlr = XmlReader.Create(fs);

            loadList = (List<JumpBridge>)xms.Deserialize(xmlr);

            foreach (var j in loadList) JumpBridges.Add(j);
        }
        catch
        {
        }
    }


    /// <summary>
    /// Resolve the Alliance ID data for specified list of IDs, by calling ESI to get the names and tickers for any unknown IDs.
    /// </summary>
    /// <param name="IDs"></param>
    /// <returns></returns>
    public async Task ResolveAllianceIDs(List<int> IDs)
    {
        if (IDs.Count == 0) return;

        // strip out any ID's we already know..
        var UnknownIDs = new List<int>();
        foreach (var l in IDs)
            if ((!AllianceIDToName.ContainsKey(l) || !AllianceIDToTicker.ContainsKey(l)) && !UnknownIDs.Contains(l))
                UnknownIDs.Add(l);

        if (UnknownIDs.Count == 0) return;

        try
        {
            var idsLong = UnknownIDs.ConvertAll(i => (long)i);
            var esra = await EveApiClient.Universe.GetNamesAndCategoriesFromIdsAsync(idsLong);
            if (ESIHelpers.ValidateESICall(esra))
                foreach (var ri in esra.Model)
                    if (ri.Category == "alliance")
                    {
                        var esraA = await EveApiClient.Alliance.GetAllianceInfoAsync(ri.Id);
                        if (ESIHelpers.ValidateESICall(esraA))
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
        catch
        {
        }
    }

    /// <summary>
    /// Update the Character ID data for specified list
    /// </summary>
    public async Task ResolveCharacterIDs(List<int> IDs)
    {
        if (IDs.Count == 0) return;

        // strip out any ID's we already know..
        var UnknownIDs = new List<int>();
        foreach (var l in IDs)
            if (!CharacterIDToName.ContainsKey(l))
                UnknownIDs.Add(l);

        if (UnknownIDs.Count == 0) return;

        try
        {
            var idsLong = UnknownIDs.ConvertAll(i => (long)i);
            var esra = await EveApiClient.Universe.GetNamesAndCategoriesFromIdsAsync(idsLong);
            if (ESIHelpers.ValidateESICall(esra))
                foreach (var ri in esra.Model)
                    if (ri.Category == "character")
                        CharacterIDToName[(int)ri.Id] = ri.Name;
        }
        catch
        {
        }
    }


    /// <summary>
    /// Save the ESI authenticated characters, Jump Bridges, and Intel Channel filters to disk.
    /// Only characters with an ESI refresh token are saved, as these are the only ones that
    /// can be re-authenticated on subsequent runs. 
    /// </summary>
    public void SaveData()
    {
        // save off only the ESI authenticated Characters so create a new copy to serialise from..
        var saveList = new List<LocalCharacter>();

        foreach (var c in GetLocalCharactersCopy())
            if (!string.IsNullOrEmpty(c.ESIRefreshToken))
                saveList.Add(c);

        var xms = new XmlSerializer(typeof(List<LocalCharacter>));
        var dataFilename = Path.Combine(SaveDataRootFolder, "Characters_" + LocalCharacter.SaveVersion + ".dat");

        using (TextWriter tw = new StreamWriter(dataFilename))
        {
            xms.Serialize(tw, saveList);
        }

        var jbFileName = Path.Combine(SaveDataRootFolder, "JumpBridges_" + JumpBridge.SaveVersion + ".dat");
        Serialization.SerializeToDisk<List<JumpBridge>>(JumpBridges, jbFileName);

        var beaconsToSave = new List<string>();
        foreach (var s in Systems)
            if (s.HasJumpBeacon)
                beaconsToSave.Add(s.Name);

        // save the intel channels / intel filters
        File.WriteAllLines(Path.Combine(SaveDataRootFolder, "IntelChannels.txt"), IntelFilters);
        File.WriteAllLines(Path.Combine(SaveDataRootFolder, "IntelClearFilters.txt"), IntelClearFilters);
        File.WriteAllLines(Path.Combine(SaveDataRootFolder, "IntelIgnoreFilters.txt"), IntelIgnoreFilters);
        File.WriteAllLines(Path.Combine(SaveDataRootFolder, "IntelAlertFilters.txt"), IntelAlertFilters);
        File.WriteAllLines(Path.Combine(SaveDataRootFolder, "CynoBeacons.txt"), beaconsToSave);
    }


    /// <summary>
    /// Load the ESI authenticated characters from disk, if the file exists. 
    /// </summary>
    private void LoadCharacters()
    {
        var dataFilename = Path.Combine(SaveDataRootFolder, "Characters_" + LocalCharacter.SaveVersion + ".dat");
        if (!File.Exists(dataFilename)) return;

        try
        {
            List<LocalCharacter> loadList;
            var xms = new XmlSerializer(typeof(List<LocalCharacter>));

            var fs = new FileStream(dataFilename, FileMode.Open, FileAccess.Read);
            var xmlr = XmlReader.Create(fs);

            loadList = (List<LocalCharacter>)xms.Deserialize(xmlr);

            foreach (var c in loadList)
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