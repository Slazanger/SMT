//-----------------------------------------------------------------------
// Intel Processing Service Implementation
//-----------------------------------------------------------------------

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using SMT.EVEData;
using EVEDataUtils;

namespace SMT.EVEData.Services
{
    /// <summary>
    /// Service for processing intel and game log data from file monitoring
    /// </summary>
    public class IntelProcessingService : IIntelProcessingService
    {
        private readonly ILogger<IntelProcessingService> _logger;
        private Func<List<System>>? _getSystems;

        /// <summary>
        /// Intel data list (thread-safe queue)
        /// </summary>
        public FixedQueue<IntelData> IntelDataList { get; private set; }

        /// <summary>
        /// Game log data list (thread-safe queue)
        /// </summary>
        public FixedQueue<GameLogData> GameLogList { get; private set; }

        /// <summary>
        /// Intel channel filters (channels to monitor)
        /// </summary>
        public List<string> IntelFilters { get; private set; }

        /// <summary>
        /// Intel alert filters (triggers for intel alerts)
        /// </summary>
        public List<string> IntelAlertFilters { get; private set; }

        /// <summary>
        /// Intel clear filters (markers for clearing intel)
        /// </summary>
        public List<string> IntelClearFilters { get; private set; }

        /// <summary>
        /// Intel ignore filters (markers to ignore)
        /// </summary>
        public List<string> IntelIgnoreFilters { get; private set; }

        /// <summary>
        /// Intel Updated Event Handler
        /// </summary>
        public event EveManager.IntelUpdatedEventHandler? IntelUpdatedEvent;

        /// <summary>
        /// GameLog Added Event Handler
        /// </summary>
        public event EveManager.GameLogAddedEventHandler? GameLogAddedEvent;

        /// <summary>
        /// Ship Decloak Event Handler
        /// </summary>
        public event EveManager.ShipDecloakedEventHandler? ShipDecloakedEvent;

        /// <summary>
        /// Combat Event Handler
        /// </summary>
        public event EveManager.CombatEventHandler? CombatEvent;

        public IntelProcessingService(ILogger<IntelProcessingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize collections
            IntelDataList = new FixedQueue<IntelData>();
            IntelDataList.SetSizeLimit(250);
            GameLogList = new FixedQueue<GameLogData>();
            GameLogList.SetSizeLimit(50);

            IntelFilters = new List<string>();
            IntelAlertFilters = new List<string>();
            IntelClearFilters = new List<string>();
            IntelIgnoreFilters = new List<string>();
        }

        /// <summary>
        /// Initialize the service with systems collection for matching
        /// </summary>
        public void Initialize(Func<List<System>> getSystems)
        {
            _getSystems = getSystems ?? throw new ArgumentNullException(nameof(getSystems));
        }

        /// <summary>
        /// Process intel file lines from file monitoring service
        /// </summary>
        public void ProcessIntelFileLines(string filePath, string channelName, bool isLocalChat, List<string> newLines)
        {
            _logger.LogDebug("Processing {Count} intel lines from {File} (Channel: {Channel}, LocalChat: {IsLocal})", 
                newLines.Count, filePath, channelName, isLocalChat);
            
            foreach (var line in newLines)
            {
                ProcessIntelLine(filePath, channelName, isLocalChat, line);
            }
        }

        /// <summary>
        /// Process game log file lines from file monitoring service
        /// </summary>
        public void ProcessGameLogFileLines(string filePath, string characterName, List<string> newLines)
        {
            foreach (var line in newLines)
            {
                ProcessGameLogLine(filePath, characterName, line);
            }
        }

        /// <summary>
        /// Process a single intel line
        /// </summary>
        private void ProcessIntelLine(string filePath, string channelName, bool isLocalChat, string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            // Skip system update lines and other non-intel content
            if (line.Contains("Channel Name:") || line.Contains("Listener:") || line.Contains("Session started"))
                return;

            bool addToIntel = false;

            // Check if we should add this line to intel
            if (isLocalChat)
            {
                // For local chat, check if it's a system change or has intel content
                if (line.Contains("> ") && !line.Contains("EVE System > "))
                {
                    addToIntel = true;
                    _logger.LogDebug("Local chat intel detected: {Line}", line);
                }
            }
            else
            {
                // For intel channels, check against alert filters
                // If no alert filters are configured, accept all intel
                if (IntelAlertFilters.Count == 0)
                {
                    addToIntel = true;
                    _logger.LogDebug("No alert filters configured, accepting all intel: {Line}", line);
                }
                else
                {
                    foreach (string alertFilterStr in IntelAlertFilters)
                    {
                        if (string.IsNullOrEmpty(alertFilterStr))
                        {
                            // Empty string means alert on all
                            addToIntel = true;
                            _logger.LogDebug("Empty alert filter matched, accepting intel: {Line}", line);
                            break;
                        }
                        else if (line.Contains(alertFilterStr, StringComparison.OrdinalIgnoreCase))
                        {
                            addToIntel = true;
                            _logger.LogDebug("Alert filter '{Filter}' matched intel: {Line}", alertFilterStr, line);
                            break;
                        }
                    }
                }
            }

            // Check ignore filters
            foreach (string ignoreMarker in IntelIgnoreFilters)
            {
                if (line.IndexOf(ignoreMarker, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    addToIntel = false;
                    break;
                }
            }

            // Check for duplicate intel (within 5 seconds)
            if (addToIntel)
            {
                int start = line.IndexOf('>') + 1;
                if (start > 0 && start < line.Length)
                {
                    string newIntelString = line.Substring(start);
                    if (!string.IsNullOrEmpty(newIntelString))
                    {
                        foreach (IntelData idl in IntelDataList)
                        {
                            if (idl.IntelString == newIntelString && (DateTime.Now - idl.IntelTime).TotalSeconds < 5)
                            {
                                addToIntel = false;
                                break;
                            }
                        }
                    }
                }
            }

            if (line.Contains("Channel MOTD:"))
            {
                addToIntel = false;
            }

            if (addToIntel)
            {
                var intelData = new IntelData(line, channelName);

                // Process the line to find system names and clear markers
                if (_getSystems != null)
                {
                    try
                    {
                        var systems = _getSystems();
                        if (systems != null && systems.Count > 0)
                        {
                            foreach (string word in intelData.IntelString.Split(' '))
                            {
                                if (string.IsNullOrEmpty(word) || word.Length < 3)
                                    continue;

                                // Check for clear markers
                                foreach (string clearMarker in IntelClearFilters)
                                {
                                    if (clearMarker.IndexOf(word, StringComparison.OrdinalIgnoreCase) == 0)
                                    {
                                        intelData.ClearNotification = true;
                                    }
                                }

                                // Check for system names
                                foreach (System sys in systems)
                                {
                                    if (sys.Name.IndexOf(word, StringComparison.OrdinalIgnoreCase) == 0 || 
                                        word.IndexOf(sys.Name, StringComparison.OrdinalIgnoreCase) == 0)
                                    {
                                        intelData.Systems.Add(sys.Name);
                                    }
                                }
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Systems collection not available yet for intel processing");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error getting systems for intel processing");
                    }
                }
                else
                {
                    _logger.LogDebug("Intel processing service not initialized with systems yet");
                }

                // Add to intel data list (even if systems aren't matched yet)
                IntelDataList.Enqueue(intelData);

                // Raise the event that the UI listens to
                IntelUpdatedEvent?.Invoke(IntelDataList);
                
                _logger.LogInformation("Added intel: {Line} -> Systems: {Systems} (Channel: {Channel}, LocalChat: {IsLocal})", 
                    line, string.Join(", ", intelData.Systems), channelName, isLocalChat);
            }
            else
            {
                _logger.LogDebug("Intel line filtered out: {Line} (Channel: {Channel}, LocalChat: {IsLocal}, AlertFilters: {FilterCount})", 
                    line, channelName, isLocalChat, IntelAlertFilters.Count);
            }
        }

        /// <summary>
        /// Process a single game log line
        /// </summary>
        private void ProcessGameLogLine(string filePath, string characterName, string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            // Skip header lines
            if (line.Contains("Gamelog") || line.Contains("Listener:") || line.Contains("Session started"))
                return;

            try
            {
                // Create game log data entry
                var gameLogData = new GameLogData()
                {
                    Character = characterName,
                    Text = line,
                    RawText = line,
                    Time = DateTime.Now,
                    Severity = "Info"
                };

                // Add to game log list
                GameLogList.Enqueue(gameLogData);

                // Check for specific events
                if (line.Contains("(cloaked)") && line.Contains("decloak"))
                {
                    string pilot = ExtractPilotNameFromGameLog(line);
                    ShipDecloakedEvent?.Invoke(pilot, line);
                }

                if (line.Contains("belonging to") && (line.Contains("under attack") || line.Contains("reinforced")))
                {
                    string pilot = ExtractPilotNameFromGameLog(line);
                    CombatEvent?.Invoke(pilot, line);
                }

                // Raise the game log updated event
                GameLogAddedEvent?.Invoke(GameLogList);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing game log line: {Line}", line);
            }
        }

        /// <summary>
        /// Extract pilot name from game log line
        /// </summary>
        private string ExtractPilotNameFromGameLog(string line)
        {
            // Simple extraction - may need refinement based on actual log format
            try
            {
                if (line.Contains(" > "))
                {
                    var parts = line.Split(" > ");
                    if (parts.Length > 1)
                    {
                        return parts[0].Trim();
                    }
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Load intel filters from disk
        /// </summary>
        public void LoadFiltersFromDisk(string saveDataRootFolder)
        {
            try
            {
                // Clear existing filters before loading
                IntelFilters.Clear();
                IntelAlertFilters.Clear();
                IntelClearFilters.Clear();
                IntelIgnoreFilters.Clear();

                // Load Intel Channels
                string intelFileFilter = Path.Combine(saveDataRootFolder, "IntelChannels.txt");
                if (File.Exists(intelFileFilter))
                {
                    using (StreamReader file = new StreamReader(intelFileFilter))
                    {
                        string? line;
                        while ((line = file.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (!string.IsNullOrEmpty(line))
                            {
                                IntelFilters.Add(line);
                            }
                        }
                    }
                }
                else
                {
                    IntelFilters.Add("Int"); // Default filter
                }

                // Load Intel Clear Filters
                string intelClearFileFilter = Path.Combine(saveDataRootFolder, "IntelClearFilters.txt");
                if (File.Exists(intelClearFileFilter))
                {
                    using (StreamReader file = new StreamReader(intelClearFileFilter))
                    {
                        string? line;
                        while ((line = file.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (!string.IsNullOrEmpty(line))
                            {
                                IntelClearFilters.Add(line);
                            }
                        }
                    }
                }
                else
                {
                    IntelClearFilters.Add("clear");
                    IntelClearFilters.Add("clr");
                }

                // Load Intel Ignore Filters
                string intelIgnoreFileFilter = Path.Combine(saveDataRootFolder, "IntelIgnoreFilters.txt");
                if (File.Exists(intelIgnoreFileFilter))
                {
                    using (StreamReader file = new StreamReader(intelIgnoreFileFilter))
                    {
                        string? line;
                        while ((line = file.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (!string.IsNullOrEmpty(line))
                            {
                                IntelIgnoreFilters.Add(line);
                            }
                        }
                    }
                }

                // Load Intel Alert Filters
                string intelAlertFileFilter = Path.Combine(saveDataRootFolder, "IntelAlertFilters.txt");
                if (File.Exists(intelAlertFileFilter))
                {
                    using (StreamReader file = new StreamReader(intelAlertFileFilter))
                    {
                        string? line;
                        while ((line = file.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (!string.IsNullOrEmpty(line))
                            {
                                IntelAlertFilters.Add(line);
                            }
                        }
                    }
                }
                else
                {
                    // Default: alert on nothing (empty string means alert on all)
                    IntelAlertFilters.Add("");
                }

                // Ensure we have at least default filters
                if (!IntelFilters.Any()) 
                {
                    IntelFilters.Add("Int");
                    _logger.LogWarning("No intel filters found, using default: 'Int'");
                }
                if (!IntelAlertFilters.Any()) 
                {
                    IntelAlertFilters.Add(""); // Empty string means alert on all
                    _logger.LogWarning("No alert filters found, using default: '' (alert on all)");
                }

                _logger.LogInformation("Loaded intel filters: {IntelCount} channels, {AlertCount} alert filters", 
                    IntelFilters.Count, IntelAlertFilters.Count);
                _logger.LogInformation("  Intel Channels: {Channels}", string.Join(", ", IntelFilters));
                _logger.LogInformation("  Alert Filters: {Filters}", string.Join(", ", IntelAlertFilters.Select(f => string.IsNullOrEmpty(f) ? "(empty - all)" : f)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading intel filters from disk");
                // Set defaults if loading fails
                if (!IntelFilters.Any()) IntelFilters.Add("Int");
                if (!IntelAlertFilters.Any()) IntelAlertFilters.Add(""); // Empty string means alert on all
            }
        }

        /// <summary>
        /// Save intel filters to disk
        /// </summary>
        public void SaveFiltersToDisk(string saveDataRootFolder)
        {
            try
            {
                File.WriteAllLines(Path.Combine(saveDataRootFolder, "IntelChannels.txt"), IntelFilters);
                File.WriteAllLines(Path.Combine(saveDataRootFolder, "IntelClearFilters.txt"), IntelClearFilters);
                File.WriteAllLines(Path.Combine(saveDataRootFolder, "IntelIgnoreFilters.txt"), IntelIgnoreFilters);
                File.WriteAllLines(Path.Combine(saveDataRootFolder, "IntelAlertFilters.txt"), IntelAlertFilters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving intel filters to disk");
            }
        }
    }
}

