using System.Text.RegularExpressions;
using Utils;

namespace EVEData;

// Intel and game log watchers
public partial class EveManager
{

    /// <summary>
    /// Setup the intel file watcher
    /// </summary>
    public void SetupIntelWatcher()
    {
        IntelDataList = new FixedQueue<IntelData>();
        IntelDataList.SetSizeLimit(250);

        IntelFilters = new List<string>();

        var intelFileFilter = Path.Combine(SaveDataRootFolder, "IntelChannels.txt");

        if (File.Exists(intelFileFilter))
        {
            var file = new StreamReader(intelFileFilter);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                line = line.Trim();
                if (!string.IsNullOrEmpty(line)) IntelFilters.Add(line);
            }
        }
        else
        {
            IntelFilters.Add("Int");
        }

        IntelClearFilters = new List<string>();
        var intelClearFileFilter = Path.Combine(SaveDataRootFolder, "IntelClearFilters.txt");

        if (File.Exists(intelClearFileFilter))
        {
            var file = new StreamReader(intelClearFileFilter);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                line = line.Trim();
                if (!string.IsNullOrEmpty(line)) IntelClearFilters.Add(line);
            }
        }
        else
        {
            // default
            IntelClearFilters.Add("Clr");
            IntelClearFilters.Add("Clear");
        }

        IntelIgnoreFilters = new List<string>();
        var intelIgnoreFileFilter = Path.Combine(SaveDataRootFolder, "IntelIgnoreFilters.txt");

        if (File.Exists(intelIgnoreFileFilter))
        {
            var file = new StreamReader(intelIgnoreFileFilter);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                line = line.Trim();
                if (!string.IsNullOrEmpty(line)) IntelIgnoreFilters.Add(line);
            }
        }
        else
        {
            // default
            IntelIgnoreFilters.Add("Status");
        }

        IntelAlertFilters = new List<string>();
        var intelAlertFileFilter = Path.Combine(SaveDataRootFolder, "IntelAlertFilters.txt");

        if (File.Exists(intelAlertFileFilter))
        {
            var file = new StreamReader(intelAlertFileFilter);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                line = line.Trim();
                if (!string.IsNullOrEmpty(line)) IntelAlertFilters.Add(line);
            }
        }
        else
        {
            // default, alert on nothing
            IntelAlertFilters.Add("");
        }

        intelFileReadPos = new Dictionary<string, int>();

        if (string.IsNullOrEmpty(EVELogFolder) || !Directory.Exists(EVELogFolder))
        {
            string[] logFolderLoc = { Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE", "Logs" };
            EVELogFolder = Path.Combine(logFolderLoc);
        }

        var chatlogFolder = Path.Combine(EVELogFolder, "Chatlogs");

        if (Directory.Exists(chatlogFolder))
        {
            intelFileWatcher = new FileSystemWatcher(chatlogFolder)
            {
                Filter = "*.txt",
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            intelFileWatcher.Changed += IntelFileWatcher_Changed;
        }
    }

    /// <summary>
    /// Setup the game log0 watcher
    /// </summary>
    public void SetupGameLogWatcher()
    {
        gameFileReadPos = new Dictionary<string, int>();
        gamelogFileCharacterMap = new Dictionary<string, string>();

        GameLogList = new FixedQueue<GameLogData>();
        GameLogList.SetSizeLimit(50);

        if (string.IsNullOrEmpty(EVELogFolder) || !Directory.Exists(EVELogFolder))
        {
            string[] logFolderLoc = { Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE", "Logs" };
            EVELogFolder = Path.Combine(logFolderLoc);
        }

        var gameLogFolder = Path.Combine(EVELogFolder, "Gamelogs");

        if (Directory.Exists(gameLogFolder))
        {
            gameLogFileWatcher = new FileSystemWatcher(gameLogFolder)
            {
                Filter = "*.txt",
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            gameLogFileWatcher.Changed += GameLogFileWatcher_Changed;
        }
    }

    public void SetupLogFileTriggers()
    {
        // -----------------------------------------------------------------
        // SUPER HACK WARNING....
        //
        // Start up a thread which just reads the text files in the eve log folder
        // by opening and closing them it updates the system meta files which
        // causes the file watcher to operate correctly otherwise this data
        // doesn't get updated until something other than the eve client reads
        // these files

        var logFolders = new List<string>();
        var chatLogFolder = Path.Combine(EVELogFolder, "Chatlogs");
        var gameLogFolder = Path.Combine(EVELogFolder, "Gamelogs");

        logFolders.Add(chatLogFolder);
        logFolders.Add(gameLogFolder);

        new Thread(() => { LogFileCacheTrigger(logFolders); }).Start();

        // END SUPERHACK
        // -----------------------------------------------------------------
    }


    /// <summary>
    /// Log file cache trigger - loop through the eve log folders and open
    /// and close any text files to update the system metadata which is
    /// required for the file watchers to work correctly
    /// </summary>
    /// <param name="eveLogFolders"></param>
    private void LogFileCacheTrigger(List<string> eveLogFolders)
    {
        Thread.CurrentThread.IsBackground = false;

        foreach (var dir in eveLogFolders)
            if (!Directory.Exists(dir))
                return;

        // loop forever
        while (WatcherThreadShouldTerminate == false)
            foreach (var folder in eveLogFolders)
            {
                var di = new DirectoryInfo(folder);
                var files = di.GetFiles("*.txt");
                foreach (var file in files)
                {
                    var readFile = false;
                    foreach (var intelFilterStr in IntelFilters)
                        if (file.Name.Contains(intelFilterStr, StringComparison.OrdinalIgnoreCase))
                        {
                            readFile = true;
                            break;
                        }

                    // local files
                    if (file.Name.Contains("Local_")) readFile = true;

                    // gamelogs
                    if (folder.Contains("Gamelogs")) readFile = true;

                    // only read files from the last day
                    if (file.CreationTime > DateTime.Now.AddDays(-1) && readFile)
                    {
                        var ifs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        ifs.Seek(0, SeekOrigin.End);
                        ifs.Close();
                    }
                }

                Thread.Sleep(1500);
            }
    }


    /// <summary>
    /// Shutdown the intel file watcher 
    /// </summary>
    public void ShuddownIntelWatcher()
    {
        if (intelFileWatcher != null) intelFileWatcher.Changed -= IntelFileWatcher_Changed;
        WatcherThreadShouldTerminate = true;
    }


    /// <summary>
    /// Shutdown the game log file watcher
    /// </summary>    
    public void ShuddownGameLogWatcher()
    {
        if (gameLogFileWatcher != null) gameLogFileWatcher.Changed -= GameLogFileWatcher_Changed;
        WatcherThreadShouldTerminate = true;
    }

    /// <summary>
    /// Intel File watcher changed handler
    /// </summary>
    private void IntelFileWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        var changedFile = e.FullPath;

        var channelParts = e.Name.Split("_");
        var channelName = string.Join("_", channelParts, 0, channelParts.Length - 3);

        var processFile = false;
        var localChat = false;

        // check if the changed file path contains the name of a channel we're looking for
        foreach (var intelFilterStr in IntelFilters)
            if (changedFile.Contains(intelFilterStr, StringComparison.OrdinalIgnoreCase))
            {
                processFile = true;
                break;
            }

        if (changedFile.Contains("Local_"))
        {
            localChat = true;
            processFile = true;
        }

        if (processFile)
        {
            try
            {
                var fe = Misc.GetEncoding(changedFile);
                var ifs = new FileStream(changedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                var file = new StreamReader(ifs, fe);

                var fileReadFrom = 0;

                // have we seen this file before
                if (intelFileReadPos.ContainsKey(changedFile))
                {
                    fileReadFrom = intelFileReadPos[changedFile];
                }
                else
                {
                    if (localChat)
                    {
                        var system = string.Empty;
                        var characterName = string.Empty;

                        // read the iniital block
                        while (!file.EndOfStream)
                        {
                            var l = file.ReadLine();
                            fileReadFrom++;

                            // explicitly skip just "local"
                            if (l.Contains("Channel Name:    Local"))
                            {
                                // now can read the next line
                                l = file.ReadLine(); // should be the "Listener : <CharName>"
                                fileReadFrom++;

                                characterName = l.Split(':')[1].Trim();

                                var addChar = true;
                                foreach (var c in GetLocalCharactersCopy())
                                    if (characterName == c.Name)
                                    {
                                        c.Location = system;
                                        c.LocalChatFile = changedFile;

                                        var s = GetEveSystem(system);
                                        if (s != null)
                                            c.Region = s.Region;
                                        else
                                            c.Region = "";

                                        addChar = false;
                                    }

                                if (addChar) AddCharacter(new LocalCharacter(characterName, changedFile, system));

                                break;
                            }
                        }
                    }

                    while (file.ReadLine() != null) fileReadFrom++;

                    fileReadFrom--;
                    file.BaseStream.Seek(0, SeekOrigin.Begin);
                }

                for (var i = 0; i < fileReadFrom; i++) file.ReadLine();

                var line = file.ReadLine();

                while (line != null)
                {
                    // trim any items off the front
                    if (line.Contains('[') && line.Contains(']')) line = line.Substring(line.IndexOf("["));

                    if (line == "")
                    {
                        line = file.ReadLine();
                        continue;
                    }

                    fileReadFrom++;

                    if (localChat)
                    {
                        if (line.StartsWith("[") && line.Contains("EVE System > Channel changed to Local"))
                        {
                            var system = line.Split(':').Last().Trim();

                            foreach (var c in GetLocalCharactersCopy())
                                if (c.LocalChatFile == changedFile)
                                    c.Location = system;
                        }
                    }
                    else
                    {
                        // check if it is in the intel list already (ie if you have multiple clients running)
                        var addToIntel = true;

                        var start = line.IndexOf('>') + 1;
                        var newIntelString = line.Substring(start);

                        if (newIntelString != null)
                        {
                            foreach (var idl in IntelDataList)
                                if (idl.IntelString == newIntelString && (DateTime.Now - idl.IntelTime).Seconds < 5)
                                {
                                    addToIntel = false;
                                    break;
                                }
                        }
                        else
                        {
                            addToIntel = false;
                        }

                        if (line.Contains("Channel MOTD:")) addToIntel = false;

                        foreach (var ignoreMarker in IntelIgnoreFilters)
                            if (line.IndexOf(ignoreMarker, StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                addToIntel = false;
                                break;
                            }

                        if (addToIntel)
                        {
                            var id = new IntelData(line, channelName);

                            foreach (var s in id.IntelString.Split(' '))
                            {
                                if (s == "" || s.Length < 3) continue;

                                foreach (var clearMarker in IntelClearFilters)
                                    if (clearMarker.IndexOf(s, StringComparison.OrdinalIgnoreCase) == 0)
                                        id.ClearNotification = true;

                                foreach (var sys in Systems)
                                    if (sys.Name.IndexOf(s, StringComparison.OrdinalIgnoreCase) == 0 ||
                                        s.IndexOf(sys.Name, StringComparison.OrdinalIgnoreCase) == 0)
                                        id.Systems.Add(sys.Name);
                            }

                            IntelDataList.Enqueue(id);

                            if (IntelUpdatedEvent != null) IntelUpdatedEvent(IntelDataList);
                        }
                    }

                    line = file.ReadLine();
                }

                ifs.Close();

                intelFileReadPos[changedFile] = fileReadFrom;
            }
            catch
            {
            }
        }
        else
        {
        }
    }

    /// <summary>
    /// GameLog File watcher changed handler
    /// </summary>
    private void GameLogFileWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        var changedFile = e.FullPath;
        var characterName = string.Empty;

        try
        {
            var fe = Misc.GetEncoding(changedFile);
            var ifs = new FileStream(changedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var file = new StreamReader(ifs, fe);

            var fileReadFrom = 0;

            // have we seen this file before
            if (gameFileReadPos.ContainsKey(changedFile))
            {
                fileReadFrom = gameFileReadPos[changedFile];
            }
            else
            {
                // read the iniital block
                while (!file.EndOfStream)
                {
                    var l = file.ReadLine();
                    fileReadFrom++;

                    // explicitly skip just "local"
                    if (l.Contains("Gamelog"))
                    {
                        // now can read the next line
                        l = file.ReadLine(); // should be the "Listener : <CharName>"

                        // something wrong with the log file; clear
                        if (!l.Contains("Listener"))
                        {
                            if (gameFileReadPos.ContainsKey(changedFile)) gameFileReadPos.Remove(changedFile);

                            return;
                        }

                        fileReadFrom++;

                        gamelogFileCharacterMap[changedFile] = l.Split(':')[1].Trim();

                        // session started
                        l = file.ReadLine();
                        fileReadFrom++;

                        // header end
                        l = file.ReadLine();
                        fileReadFrom++;

                        // as its new; skip the entire file -1
                        break;
                    }
                }

                while (!file.EndOfStream)
                {
                    var l = file.ReadLine();
                    fileReadFrom++;
                }

                // back one line
                fileReadFrom--;

                file.BaseStream.Seek(0, SeekOrigin.Begin);
            }

            characterName = gamelogFileCharacterMap[changedFile];

            for (var i = 0; i < fileReadFrom; i++) file.ReadLine();

            var line = file.ReadLine();

            while (line != null)
            {
                // trim any items off the front
                if (line == "" || !line.StartsWith("["))
                {
                    line = file.ReadLine();
                    fileReadFrom++;
                    continue;
                }

                fileReadFrom++;

                var typeStartPos = line.IndexOf("(") + 1;
                var typeEndPos = line.IndexOf(")");

                // file corrupt
                if (typeStartPos < 1 || typeEndPos < 1) continue;

                var type = line.Substring(typeStartPos, typeEndPos - typeStartPos);

                line = line.Substring(typeEndPos + 1);

                // strip the formatting from the log
                line = Regex.Replace(line, "<.*?>", string.Empty);

                var gd = new GameLogData
                {
                    Character = characterName,
                    Text = line,
                    Severity = type,
                    Time = DateTime.Now
                };

                GameLogList.Enqueue(gd);
                if (GameLogAddedEvent != null) GameLogAddedEvent(GameLogList);

                foreach (var lc in GetLocalCharactersCopy())
                    if (lc.Name == characterName)
                    {
                        if (type == "combat")
                            if (CombatEvent != null)
                            {
                                lc.GameLogWarningText = line;
                                CombatEvent(characterName, line);
                            }

                        if (
                            line.Contains("cloak deactivates due to a pulse from a Mobile Observatory") ||
                            line.Contains("Your cloak deactivates due to proximity to") ||
                            line.Contains("Your cloak deactivates due to a pulse from a Dazh Liminality Locus")
                        )
                            if (ShipDecloakedEvent != null)
                            {
                                ShipDecloakedEvent(characterName, line);
                                lc.GameLogWarningText = line;
                            }
                    }

                line = file.ReadLine();
                gameFileReadPos[changedFile] = fileReadFrom;
            }

            ifs.Close();

            gameFileReadPos[changedFile] = fileReadFrom;
        }
        catch
        {
        }
    }
}