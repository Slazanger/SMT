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
    // Intel and game log watchers
    public partial class EveManager
    {
        public void SetupIntelWatcher()
        {
            IntelDataList = new FixedQueue<IntelData>();
            IntelDataList.SetSizeLimit(250);

            IntelFilters = new List<string>();

            string intelFileFilter = Path.Combine(SaveDataRootFolder, "IntelChannels.txt");

            if(File.Exists(intelFileFilter))
            {
                StreamReader file = new StreamReader(intelFileFilter);
                string line;
                while((line = file.ReadLine()) != null)
                {
                    line = line.Trim();
                    if(!string.IsNullOrEmpty(line))
                    {
                        IntelFilters.Add(line);
                    }
                }
            }
            else
            {
                IntelFilters.Add("Int");
            }

            IntelClearFilters = new List<string>();
            string intelClearFileFilter = Path.Combine(SaveDataRootFolder, "IntelClearFilters.txt");

            if(File.Exists(intelClearFileFilter))
            {
                StreamReader file = new StreamReader(intelClearFileFilter);
                string line;
                while((line = file.ReadLine()) != null)
                {
                    line = line.Trim();
                    if(!string.IsNullOrEmpty(line))
                    {
                        IntelClearFilters.Add(line);
                    }
                }
            }
            else
            {
                // default
                IntelClearFilters.Add("Clr");
                IntelClearFilters.Add("Clear");
            }

            IntelIgnoreFilters = new List<string>();
            string intelIgnoreFileFilter = Path.Combine(SaveDataRootFolder, "IntelIgnoreFilters.txt");

            if(File.Exists(intelIgnoreFileFilter))
            {
                StreamReader file = new StreamReader(intelIgnoreFileFilter);
                string line;
                while((line = file.ReadLine()) != null)
                {
                    line = line.Trim();
                    if(!string.IsNullOrEmpty(line))
                    {
                        IntelIgnoreFilters.Add(line);
                    }
                }
            }
            else
            {
                // default
                IntelIgnoreFilters.Add("Status");
            }

            IntelAlertFilters = new List<string>();
            string intelAlertFileFilter = Path.Combine(SaveDataRootFolder, "IntelAlertFilters.txt");

            if(File.Exists(intelAlertFileFilter))
            {
                StreamReader file = new StreamReader(intelAlertFileFilter);
                string line;
                while((line = file.ReadLine()) != null)
                {
                    line = line.Trim();
                    if(!string.IsNullOrEmpty(line))
                    {
                        IntelAlertFilters.Add(line);
                    }
                }
            }
            else
            {
                // default, alert on nothing
                IntelAlertFilters.Add("");
            }

            intelFileReadPos = new Dictionary<string, int>();

            if(string.IsNullOrEmpty(EVELogFolder) || !Directory.Exists(EVELogFolder))
            {
                string[] logFolderLoc = { Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE", "Logs" };
                EVELogFolder = Path.Combine(logFolderLoc);
            }

            string chatlogFolder = Path.Combine(EVELogFolder, "Chatlogs");

            if(Directory.Exists(chatlogFolder))
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

            if(string.IsNullOrEmpty(EVELogFolder) || !Directory.Exists(EVELogFolder))
            {
                string[] logFolderLoc = { Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE", "Logs" };
                EVELogFolder = Path.Combine(logFolderLoc);
            }

            string gameLogFolder = Path.Combine(EVELogFolder, "Gamelogs");

            if(Directory.Exists(gameLogFolder))
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
            // by opening and closing them it updates the sytem meta files which
            // causes the file watcher to operate correctly otherwise this data
            // doesnt get updated until something other than the eve client reads these files

            List<string> logFolders = new List<string>();
            string chatLogFolder = Path.Combine(EVELogFolder, "Chatlogs");
            string gameLogFolder = Path.Combine(EVELogFolder, "Gamelogs");

            logFolders.Add(chatLogFolder);
            logFolders.Add(gameLogFolder);

            new Thread(() =>
            {
                LogFileCacheTrigger(logFolders);
            }).Start();

            // END SUPERHACK
            // -----------------------------------------------------------------
        }

        private void LogFileCacheTrigger(List<string> eveLogFolders)
        {
            Thread.CurrentThread.IsBackground = false;

            foreach(string dir in eveLogFolders)
            {
                if(!Directory.Exists(dir))
                {
                    return;
                }
            }

            // loop forever
            while(WatcherThreadShouldTerminate == false)
            {
                foreach(string folder in eveLogFolders)
                {
                    DirectoryInfo di = new DirectoryInfo(folder);
                    FileInfo[] files = di.GetFiles("*.txt");
                    foreach(FileInfo file in files)
                    {
                        bool readFile = false;
                        foreach(string intelFilterStr in IntelFilters)
                        {
                            if(file.Name.Contains(intelFilterStr, StringComparison.OrdinalIgnoreCase))
                            {
                                readFile = true;
                                break;
                            }
                        }

                        // local files
                        if(file.Name.Contains("Local_"))
                        {
                            readFile = true;
                        }

                        // gamelogs
                        if(folder.Contains("Gamelogs"))
                        {
                            readFile = true;
                        }

                        // only read files from the last day
                        if(file.CreationTime > DateTime.Now.AddDays(-1) && readFile)
                        {
                            FileStream ifs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            ifs.Seek(0, SeekOrigin.End);
                            ifs.Close();
                        }
                    }

                    Thread.Sleep(1500);
                }
            }
        }

        public void ShuddownIntelWatcher()
        {
            if(intelFileWatcher != null)
            {
                intelFileWatcher.Changed -= IntelFileWatcher_Changed;
            }
            WatcherThreadShouldTerminate = true;
        }

        public void ShuddownGameLogWatcher()
        {
            if(gameLogFileWatcher != null)
            {
                gameLogFileWatcher.Changed -= GameLogFileWatcher_Changed;
            }
            WatcherThreadShouldTerminate = true;
        }
        /// <summary>
        /// Intel File watcher changed handler
        /// </summary>
        private void IntelFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            string changedFile = e.FullPath;

            string[] channelParts = e.Name.Split("_");
            string channelName = string.Join("_", channelParts, 0, channelParts.Length - 3);

            bool processFile = false;
            bool localChat = false;

            // check if the changed file path contains the name of a channel we're looking for
            foreach(string intelFilterStr in IntelFilters)
            {
                if(changedFile.Contains(intelFilterStr, StringComparison.OrdinalIgnoreCase))
                {
                    processFile = true;
                    break;
                }
            }

            if(changedFile.Contains("Local_"))
            {
                localChat = true;
                processFile = true;
            }

            if(processFile)
            {
                try
                {
                    Encoding fe = Misc.GetEncoding(changedFile);
                    FileStream ifs = new FileStream(changedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    StreamReader file = new StreamReader(ifs, fe);

                    int fileReadFrom = 0;

                    // have we seen this file before
                    if(intelFileReadPos.ContainsKey(changedFile))
                    {
                        fileReadFrom = intelFileReadPos[changedFile];
                    }
                    else
                    {
                        if(localChat)
                        {
                            string system = string.Empty;
                            string characterName = string.Empty;

                            // read the iniital block
                            while(!file.EndOfStream)
                            {
                                string l = file.ReadLine();
                                fileReadFrom++;

                                // explicitly skip just "local"
                                if(l.Contains("Channel Name:    Local"))
                                {
                                    // now can read the next line
                                    l = file.ReadLine(); // should be the "Listener : <CharName>"
                                    fileReadFrom++;

                                    characterName = l.Split(':')[1].Trim();

                                    bool addChar = true;
                                    foreach(EVEData.LocalCharacter c in GetLocalCharactersCopy())
                                    {
                                        if(characterName == c.Name)
                                        {
                                            c.Location = system;
                                            c.LocalChatFile = changedFile;

                                            System s = GetEveSystem(system);
                                            if(s != null)
                                            {
                                                c.Region = s.Region;
                                            }
                                            else
                                            {
                                                c.Region = "";
                                            }

                                            addChar = false;
                                        }
                                    }

                                    if(addChar)
                                    {
                                        AddCharacter(new EVEData.LocalCharacter(characterName, changedFile, system));
                                    }

                                    break;
                                }
                            }
                        }

                        while(file.ReadLine() != null)
                        {
                            fileReadFrom++;
                        }

                        fileReadFrom--;
                        file.BaseStream.Seek(0, SeekOrigin.Begin);
                    }

                    for(int i = 0; i < fileReadFrom; i++)
                    {
                        file.ReadLine();
                    }

                    string line = file.ReadLine();

                    while(line != null)
                    {                    // trim any items off the front
                        if(line.Contains('[') && line.Contains(']'))
                        {
                            line = line.Substring(line.IndexOf("["));
                        }

                        if(line == "")
                        {
                            line = file.ReadLine();
                            continue;
                        }

                        fileReadFrom++;

                        if(localChat)
                        {
                            if(line.StartsWith("[") && line.Contains("EVE System > Channel changed to Local"))
                            {
                                string system = line.Split(':').Last().Trim();

                                foreach(EVEData.LocalCharacter c in GetLocalCharactersCopy())
                                {
                                    if(c.LocalChatFile == changedFile)
                                    {
                                        c.Location = system;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // check if it is in the intel list already (ie if you have multiple clients running)
                            bool addToIntel = true;

                            int start = line.IndexOf('>') + 1;
                            string newIntelString = line.Substring(start);

                            if(newIntelString != null)
                            {
                                foreach(EVEData.IntelData idl in IntelDataList)
                                {
                                    if(idl.IntelString == newIntelString && (DateTime.Now - idl.IntelTime).Seconds < 5)
                                    {
                                        addToIntel = false;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                addToIntel = false;
                            }

                            if(line.Contains("Channel MOTD:"))
                            {
                                addToIntel = false;
                            }

                            foreach(String ignoreMarker in IntelIgnoreFilters)
                            {
                                if(line.IndexOf(ignoreMarker, StringComparison.OrdinalIgnoreCase) != -1)
                                {
                                    addToIntel = false;
                                    break;
                                }
                            }

                            if(addToIntel)
                            {
                                EVEData.IntelData id = new EVEData.IntelData(line, channelName);

                                foreach(string s in id.IntelString.Split(' '))
                                {
                                    if(s == "" || s.Length < 3)
                                    {
                                        continue;
                                    }

                                    foreach(String clearMarker in IntelClearFilters)
                                    {
                                        if(clearMarker.IndexOf(s, StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            id.ClearNotification = true;
                                        }
                                    }

                                    foreach(System sys in Systems)
                                    {
                                        if(sys.Name.IndexOf(s, StringComparison.OrdinalIgnoreCase) == 0 || s.IndexOf(sys.Name, StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            id.Systems.Add(sys.Name);
                                        }
                                    }
                                }

                                IntelDataList.Enqueue(id);

                                if(IntelUpdatedEvent != null)
                                {
                                    IntelUpdatedEvent(IntelDataList);
                                }
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
            string changedFile = e.FullPath;
            string characterName = string.Empty;

            try
            {
                Encoding fe = EVEDataUtils.Misc.GetEncoding(changedFile);
                FileStream ifs = new FileStream(changedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                StreamReader file = new StreamReader(ifs, fe);

                int fileReadFrom = 0;

                // have we seen this file before
                if(gameFileReadPos.ContainsKey(changedFile))
                {
                    fileReadFrom = gameFileReadPos[changedFile];
                }
                else
                {
                    // read the iniital block
                    while(!file.EndOfStream)
                    {
                        string l = file.ReadLine();
                        fileReadFrom++;

                        // explicitly skip just "local"
                        if(l.Contains("Gamelog"))
                        {
                            // now can read the next line
                            l = file.ReadLine(); // should be the "Listener : <CharName>"

                            // something wrong with the log file; clear
                            if(!l.Contains("Listener"))
                            {
                                if(gameFileReadPos.ContainsKey(changedFile))
                                {
                                    gameFileReadPos.Remove(changedFile);
                                }

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

                    while(!file.EndOfStream)
                    {
                        string l = file.ReadLine();
                        fileReadFrom++;
                    }

                    // back one line
                    fileReadFrom--;

                    file.BaseStream.Seek(0, SeekOrigin.Begin);
                }

                characterName = gamelogFileCharacterMap[changedFile];

                for(int i = 0; i < fileReadFrom; i++)
                {
                    file.ReadLine();
                }

                string line = file.ReadLine();

                while(line != null)
                {                    // trim any items off the front
                    if(line == "" || !line.StartsWith("["))
                    {
                        line = file.ReadLine();
                        fileReadFrom++;
                        continue;
                    }

                    fileReadFrom++;

                    int typeStartPos = line.IndexOf("(") + 1;
                    int typeEndPos = line.IndexOf(")");

                    // file corrupt
                    if(typeStartPos < 1 || typeEndPos < 1)
                    {
                        continue;
                    }

                    string type = line.Substring(typeStartPos, typeEndPos - typeStartPos);

                    line = line.Substring(typeEndPos + 1);

                    // strip the formatting from the log
                    line = Regex.Replace(line, "<.*?>", String.Empty);

                    GameLogData gd = new GameLogData()
                    {
                        Character = characterName,
                        Text = line,
                        Severity = type,
                        Time = DateTime.Now,
                    };

                    GameLogList.Enqueue(gd);
                    if(GameLogAddedEvent != null)
                    {
                        GameLogAddedEvent(GameLogList);
                    }

                    foreach(LocalCharacter lc in GetLocalCharactersCopy())
                    {
                        if(lc.Name == characterName)
                        {
                            if(type == "combat")
                            {
                                if(CombatEvent != null)
                                {
                                    lc.GameLogWarningText = line;
                                    CombatEvent(characterName, line);
                                }
                            }

                            if(
                                line.Contains("cloak deactivates due to a pulse from a Mobile Observatory") ||
                                line.Contains("Your cloak deactivates due to proximity to") ||
                                line.Contains("Your cloak deactivates due to a pulse from a Dazh Liminality Locus")
                                )
                            {
                                if(ShipDecloakedEvent != null)
                                {
                                    ShipDecloakedEvent(characterName, line);
                                    lc.GameLogWarningText = line;
                                }
                            }
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
}
