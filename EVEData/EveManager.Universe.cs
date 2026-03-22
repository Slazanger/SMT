//-----------------------------------------------------------------------
// EVE Manager (partial)
//-----------------------------------------------------------------------

using Utils;


namespace EVEData;

// Universe / map lookups and navigation init
public partial class EveManager
{
    /// <summary>
    /// Does the System Exist ?
    /// </summary>
    /// <param name="name">Name (not ID) of the system</param>
    public bool DoesSystemExist(string name)
    {
        return GetEveSystem(name) != null;
    }

    /// <summary>
    /// Get the alliance name from the alliance ID
    /// </summary>
    /// <param name="id">Alliance ID</param>
    /// <returns>Alliance Name</returns>
    public string GetAllianceName(int id)
    {
        var name = string.Empty;
        if (AllianceIDToName.ContainsKey(id)) name = AllianceIDToName[id];

        return name;
    }

    /// <summary>
    /// Gets the alliance ticker eg "TEST" from the alliance ID
    /// </summary>
    /// <param name="id">Alliance ID</param>
    /// <returns>Alliance Ticker</returns>
    public string GetAllianceTicker(int id)
    {
        var ticker = string.Empty;
        if (AllianceIDToTicker.ContainsKey(id)) ticker = AllianceIDToTicker[id];

        return ticker;
    }

    public string GetCharacterName(int id)
    {
        var name = string.Empty;
        if (CharacterIDToName.ContainsKey(id)) name = CharacterIDToName[id];

        return name;
    }

    /// <summary>
    /// Get a System object from the name, note : for regions which have other region systems in it wont return
    /// them.. eg TR07-s is on the esoteria map, but the object corresponding to the feythabolis map will be returned
    /// </summary>
    /// <param name="name">Name (not ID) of the system</param>
    public System GetEveSystem(string name)
    {
        if (NameToSystem.ContainsKey(name)) return NameToSystem[name];

        return null;
    }

    /// <summary>
    /// Get a System object from the ID
    /// </summary>
    /// <param name="id">ID of the system</param>
    public System GetEveSystemFromID(long id)
    {
        if (IDToSystem.ContainsKey(id)) return IDToSystem[id];

        return null;
    }

    /// <summary>
    /// Get a System name from the ID
    /// </summary>
    /// <param name="id">ID of the system</param>
    public string GetEveSystemNameFromID(long id)
    {
        var s = GetEveSystemFromID(id);
        if (s != null) return s.Name;

        return string.Empty;
    }

    /// <summary>
    /// Calculate the range between the two systems
    /// </summary>
    public decimal GetRangeBetweenSystems(string from, string to)
    {
        var systemFrom = GetEveSystem(from);
        var systemTo = GetEveSystem(to);

        if (systemFrom == null || systemTo == null || from == to) return 0.0M;

        var x = systemFrom.ActualX - systemTo.ActualX;
        var y = systemFrom.ActualY - systemTo.ActualY;
        var z = systemFrom.ActualZ - systemTo.ActualZ;

        var length = DecimalMath.DecimalEx.Sqrt(x * x + y * y + z * z);

        return length;
    }

    /// <summary>
    /// Get the MapRegion from the name
    /// </summary>
    /// <param name="name">Name of the Region</param>
    /// <returns>Region Object</returns>
    public MapRegion GetRegion(string name)
    {
        foreach (var reg in Regions)
            if (reg.Name == name)
                return reg;

        return null;
    }

    /// <summary>
    /// Get the System name from the System ID
    /// </summary>
    /// <param name="id">System ID</param>
    /// <returns>System Name</returns>
    public string GetSystemNameFromSystemID(long id)
    {
        var name = string.Empty;
        if (SystemIDToName.ContainsKey(id)) name = SystemIDToName[id];

        return name;
    }

    /// <summary>
    /// Initializes the Navigation
    /// </summary>
    public void InitNavigation()
    {
        SerializableDictionary<string, List<string>> jumpRangeCache;

        var DataRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        var JRC = Path.Combine(DataRootPath, "JumpRangeCache.dat");

        if (!File.Exists(JRC)) throw new NotImplementedException();
        jumpRangeCache = Serialization.DeserializeFromDisk<SerializableDictionary<string, List<string>>>(JRC);
        Navigation.InitNavigation(NameToSystem.Values.ToList(), JumpBridges, jumpRangeCache);

        InitZarzakhConnections();
    }


    /// <summary>
    /// Add or update a jump bridge, if the from system is already in the list of jump bridges
    /// then it will update the stationID for that system, otherwise it will add a new jump
    /// bridge to the list
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="stationID"></param>
    public void AddUpdateJumpBridge(string from, string to, long stationID)
    {
        // validate
        if (GetEveSystem(from) == null || GetEveSystem(to) == null) return;

        var found = false;

        foreach (var jb in JumpBridges)
        {
            if (jb.From == from)
            {
                found = true;
                jb.FromID = stationID;
            }

            if (jb.To == from)
            {
                found = true;
                jb.ToID = stationID;
            }
        }

        if (!found)
        {
            var njb = new JumpBridge(from, to);
            njb.FromID = stationID;
            JumpBridges.Add(njb);
        }
    }
}