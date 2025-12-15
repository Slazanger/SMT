//-----------------------------------------------------------------------
// File Monitoring Service Implementation
// Replaces file monitoring functionality from EveManager
//-----------------------------------------------------------------------

#nullable enable
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SMT.EVEData.Events;
using EVEDataUtils;

namespace SMT.EVEData.Services
{
    /// <summary>
    /// Modern file monitoring service using BackgroundService
    /// Monitors EVE chat logs and game logs for changes
    /// </summary>
    public class FileMonitoringService : BackgroundService, IFileMonitoringService
    {
        private readonly ILogger<FileMonitoringService> _logger;
        private readonly Dictionary<string, int> _intelFileReadPos = new();
        private readonly Dictionary<string, int> _gameFileReadPos = new();
        private readonly Dictionary<string, string> _gamelogFileCharacterMap = new();
        
        private FileSystemWatcher? _intelFileWatcher;
        private FileSystemWatcher? _gameLogFileWatcher;
        private string? _currentLogFolder;
        private IEnumerable<string> _intelFilters = Enumerable.Empty<string>();
        private bool _isMonitoring;

        public FileMonitoringService(ILogger<FileMonitoringService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region IFileMonitoringService Implementation

        public event EventHandler<IntelFileChangedEventArgs>? IntelFileChanged;
        public event EventHandler<GameLogFileChangedEventArgs>? GameLogFileChanged;

        public bool IsMonitoring => _isMonitoring;
        public string? CurrentLogFolder => _currentLogFolder;

        public async Task StartMonitoringAsync(string eveLogFolder, IEnumerable<string> intelFilters, CancellationToken cancellationToken)
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("File monitoring is already active");
                return;
            }

            _currentLogFolder = eveLogFolder;
            _intelFilters = intelFilters.ToList(); // Create a copy
            
            _logger.LogInformation("Starting file monitoring for EVE logs at: {LogFolder}", eveLogFolder);

            // Initialize read positions to end of files to avoid reading old content on startup
            InitializeReadPositions(eveLogFolder);

            await SetupIntelWatcherAsync(eveLogFolder);
            await SetupGameLogWatcherAsync(eveLogFolder);

            _isMonitoring = true;
            _logger.LogInformation("File monitoring started successfully");
            _logger.LogInformation("_isMonitoring set to true, _currentLogFolder: {Folder}", _currentLogFolder);
            
            // Also start the cache trigger directly here as a backup
            // The BackgroundService should also start it, but this ensures it runs
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000); // Give BackgroundService a chance to start it first
                if (_isMonitoring && !string.IsNullOrEmpty(_currentLogFolder))
                {
                    _logger.LogInformation("Starting file cache trigger directly (backup method)");
                    try
                    {
                        await StartFileCacheTriggerAsync(CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error starting cache trigger directly");
                    }
                }
            });
        }

        public async Task StopMonitoringAsync()
        {
            if (!_isMonitoring)
            {
                return;
            }

            _logger.LogInformation("Stopping file monitoring");

            await CleanupWatchersAsync();
            
            _isMonitoring = false;
            _currentLogFolder = null;
            
            _logger.LogInformation("File monitoring stopped");
        }

        #endregion

        #region BackgroundService Implementation

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FileMonitoringService background task starting");

            // Wait for monitoring to be started before starting the cache trigger
            // The cache trigger needs _currentLogFolder to be set
            int waitCount = 0;
            while (!_isMonitoring && !stoppingToken.IsCancellationRequested)
            {
                waitCount++;
                if (waitCount % 5 == 0) // Log every 5 seconds
                {
                    _logger.LogInformation("Waiting for file monitoring to start... (waited {Seconds} seconds)", waitCount);
                }
                await Task.Delay(1000, stoppingToken);
            }

            if (_isMonitoring && !stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("File monitoring is active, starting file cache trigger");
                // Start the file cache trigger (the "super hack" from original code)
                await StartFileCacheTriggerAsync(stoppingToken);
            }
            else if (!_isMonitoring)
            {
                _logger.LogWarning("File monitoring was never started, cache trigger not running");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("FileMonitoringService background task stopping");
            
            await StopMonitoringAsync();
            await base.StopAsync(cancellationToken);
        }

        #endregion

        #region Private Implementation

        private async Task SetupIntelWatcherAsync(string eveLogFolder)
        {
            var chatlogFolder = Path.Combine(eveLogFolder, "Chatlogs");

            _logger.LogInformation("Setting up intel watcher for folder: {ChatlogFolder}", chatlogFolder);
            _logger.LogInformation("Chatlog folder exists: {Exists}", Directory.Exists(chatlogFolder));

            if (!Directory.Exists(chatlogFolder))
            {
                _logger.LogWarning("Chat log folder does not exist: {ChatlogFolder}", chatlogFolder);
                return;
            }

            try
            {
                _intelFileWatcher = new FileSystemWatcher(chatlogFolder)
                {
                    Filter = "*.txt",
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };

                _intelFileWatcher.Changed += OnIntelFileChanged;
                
                _logger.LogInformation("Intel file watcher setup for: {ChatlogFolder} (Filters: {Filters})", 
                    chatlogFolder, string.Join(", ", _intelFilters));
                _logger.LogInformation("FileSystemWatcher enabled: {Enabled}, Filter: {Filter}, NotifyFilter: {NotifyFilter}", 
                    _intelFileWatcher.EnableRaisingEvents, _intelFileWatcher.Filter, _intelFileWatcher.NotifyFilter);
                
                // List some files in the directory for debugging
                try
                {
                    var files = Directory.GetFiles(chatlogFolder, "*.txt").Take(5);
                    _logger.LogInformation("Sample files in chatlog folder: {Files}", string.Join(", ", files.Select(Path.GetFileName)));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not list files in chatlog folder");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up intel file watcher");
                throw;
            }
            
            await Task.CompletedTask;
        }

        private async Task SetupGameLogWatcherAsync(string eveLogFolder)
        {
            var gameLogFolder = Path.Combine(eveLogFolder, "Gamelogs");

            if (!Directory.Exists(gameLogFolder))
            {
                _logger.LogWarning("Game log folder does not exist: {GameLogFolder}", gameLogFolder);
                return;
            }

            _gameLogFileWatcher = new FileSystemWatcher(gameLogFolder)
            {
                Filter = "*.txt",
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _gameLogFileWatcher.Changed += OnGameLogFileChanged;
            
            _logger.LogInformation("Game log file watcher setup for: {GameLogFolder}", gameLogFolder);
            await Task.CompletedTask;
        }

        private async Task CleanupWatchersAsync()
        {
            if (_intelFileWatcher != null)
            {
                _intelFileWatcher.Changed -= OnIntelFileChanged;
                _intelFileWatcher.Dispose();
                _intelFileWatcher = null;
            }

            if (_gameLogFileWatcher != null)
            {
                _gameLogFileWatcher.Changed -= OnGameLogFileChanged;
                _gameLogFileWatcher.Dispose();
                _gameLogFileWatcher = null;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// The "super hack" from original code - periodically touches files to trigger file system events
        /// This is needed because EVE client doesn't update file metadata properly
        /// </summary>
        private async Task StartFileCacheTriggerAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_currentLogFolder))
            {
                return;
            }

            var logFolders = new List<string>
            {
                Path.Combine(_currentLogFolder, "Chatlogs"),
                Path.Combine(_currentLogFolder, "Gamelogs")
            };

            // Verify folders exist
            var existingFolders = logFolders.Where(Directory.Exists).ToList();
            if (!existingFolders.Any())
            {
                _logger.LogWarning("No EVE log folders found for cache trigger");
                return;
            }

            _logger.LogInformation("Starting file cache trigger for {FolderCount} folders: {Folders}", 
                existingFolders.Count, string.Join(", ", existingFolders));

            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1500));
                
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await ProcessFileCacheTriggerAsync(existingFolders, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("File cache trigger stopped due to cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in file cache trigger");
            }
        }

        private async Task ProcessFileCacheTriggerAsync(List<string> logFolders, CancellationToken cancellationToken)
        {
            foreach (var folder in logFolders)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var directoryInfo = new DirectoryInfo(folder);
                    var files = directoryInfo.GetFiles("*.txt");

                    foreach (var file in files)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        var shouldReadFile = ShouldProcessFile(file, folder);
                        var isRecent = file.LastWriteTime > DateTime.Now.AddMinutes(-5) || file.CreationTime > DateTime.Now.AddDays(-1);

                        // Process recent files or files from the last day
                        if (isRecent && shouldReadFile)
                        {
                            // Read new lines from the file (this is the "super hack" - direct file reading)
                            var isChatlog = folder.Contains("Chatlogs");
                            if (isChatlog)
                            {
                                var newLines = ReadNewLinesFromFile(file.FullName, _intelFileReadPos);
                                if (newLines.Any())
                                {
                                    var fileName = file.Name;
                                    var channelParts = fileName.Split("_");
                                    var channelName = string.Join("_", channelParts, 0, Math.Max(0, channelParts.Length - 3));
                                    var isLocalChat = fileName.Contains("Local_");
                                    
                                    _logger.LogInformation("File cache trigger found {Count} new lines in {FileName} (Channel: {Channel}, LocalChat: {IsLocal})", 
                                        newLines.Count, fileName, channelName, isLocalChat);
                                    var eventArgs = new IntelFileChangedEventArgs(file.FullName, fileName, channelName, isLocalChat, newLines);
                                    IntelFileChanged?.Invoke(this, eventArgs);
                                    _logger.LogInformation("IntelFileChanged event raised for {FileName}", fileName);
                                }
                            }
                            else
                            {
                                // Game log
                                var newLines = ReadNewLinesFromFile(file.FullName, _gameFileReadPos);
                                if (newLines.Any())
                                {
                                    var fileName = file.Name;
                                    var characterName = ExtractCharacterNameFromGameLog(fileName);
                                    
                                    _logger.LogInformation("File cache trigger found {Count} new lines in game log {FileName}", newLines.Count, fileName);
                                    var eventArgs = new GameLogFileChangedEventArgs(file.FullName, fileName, characterName, newLines);
                                    GameLogFileChanged?.Invoke(this, eventArgs);
                                }
                            }
                        }
                        else if (!shouldReadFile && isRecent)
                        {
                            //_logger.LogDebug("Skipping file {FileName} - doesn't match filters (Filters: {Filters})", file.Name, string.Join(", ", _intelFilters));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing files in folder: {Folder}", folder);
                }
            }
        }

        private bool ShouldProcessFile(FileInfo file, string folder)
        {
            // Local chat files - always process
            if (file.Name.Contains("Local_"))
            {
                //_logger.LogDebug("File {FileName} matches Local_ filter", file.Name);
                return true;
            }

            // Game logs - always process
            if (folder.Contains("Gamelogs"))
            {
                //_logger.LogDebug("File {FileName} is a game log", file.Name);
                return true;
            }

            // Check intel filters for chat logs
            foreach (var intelFilter in _intelFilters)
            {
                if (file.Name.Contains(intelFilter, StringComparison.OrdinalIgnoreCase))
                {
                    //_logger.LogDebug("File {FileName} matches intel filter '{Filter}'", file.Name, intelFilter);
                    return true;
                }
            }

            //_logger.LogTrace("File {FileName} does not match any filters (Filters: {Filters})", file.Name, string.Join(", ", _intelFilters));
            return false;
        }

        /// <summary>
        /// Initialize read positions for all existing files to skip old content on startup
        /// </summary>
        private void InitializeReadPositions(string eveLogFolder)
        {
            try
            {
                var chatlogFolder = Path.Combine(eveLogFolder, "Chatlogs");
                var gameLogFolder = Path.Combine(eveLogFolder, "Gamelogs");

                // Initialize intel file read positions
                if (Directory.Exists(chatlogFolder))
                {
                    var intelFiles = Directory.GetFiles(chatlogFolder, "*.txt");
                    foreach (var file in intelFiles)
                    {
                        if (!_intelFileReadPos.ContainsKey(file))
                        {
                            InitializeFileReadPosition(file, _intelFileReadPos);
                        }
                    }
                    _logger.LogInformation("Initialized read positions for {Count} intel files", intelFiles.Length);
                }

                // Initialize game log file read positions
                if (Directory.Exists(gameLogFolder))
                {
                    var gameLogFiles = Directory.GetFiles(gameLogFolder, "*.txt");
                    foreach (var file in gameLogFiles)
                    {
                        if (!_gameFileReadPos.ContainsKey(file))
                        {
                            InitializeFileReadPosition(file, _gameFileReadPos);
                        }
                    }
                    _logger.LogInformation("Initialized read positions for {Count} game log files", gameLogFiles.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error initializing read positions");
            }
        }

        /// <summary>
        /// Initialize read position for a single file to the end (skip all existing content)
        /// </summary>
        private void InitializeFileReadPosition(string filePath, Dictionary<string, int> readPositions)
        {
            try
            {
                var encoding = Misc.GetEncoding(filePath);
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream, encoding);

                int lineCount = 0;
                while (!reader.EndOfStream)
                {
                    reader.ReadLine();
                    lineCount++;
                }
                readPositions[filePath] = lineCount;
                _logger.LogDebug("Initialized read position for {FileName} at line {LineCount} (end of file)", 
                    Path.GetFileName(filePath), lineCount);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Could not initialize read position for file: {FilePath}", filePath);
                // If we can't read the file, set position to 0 so we'll try again later
                readPositions[filePath] = 0;
            }
        }

        private async Task TouchFileAsync(string filePath)
        {
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fileStream.Seek(0, SeekOrigin.End);
                // File automatically closed by using statement
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Could not touch file: {FilePath}", filePath);
                // This is expected for files in use, so we don't log as error
            }
            
            await Task.CompletedTask;
        }

        #endregion

        #region Event Handlers

        private void OnIntelFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                var changedFile = e.FullPath;
                var fileName = e.Name ?? Path.GetFileName(changedFile);
                
                _logger.LogDebug("FileSystemWatcher detected intel file change: {FileName} ({FullPath})", fileName, changedFile);
                
                var channelParts = fileName.Split("_");
                var channelName = string.Join("_", channelParts, 0, channelParts.Length - 3);

                var isLocalChat = changedFile.Contains("Local_");
                var shouldProcess = isLocalChat || _intelFilters.Any(filter => 
                    changedFile.Contains(filter, StringComparison.OrdinalIgnoreCase));

                if (!shouldProcess)
                {
                    _logger.LogDebug("Skipping file {FileName} - doesn't match filters (LocalChat: {IsLocal}, Filters: {Filters})", 
                        fileName, isLocalChat, string.Join(", ", _intelFilters));
                    return;
                }

                _logger.LogDebug("Processing intel file: {FileName}, Channel: {Channel}, LocalChat: {IsLocal}", 
                    fileName, channelName, isLocalChat);

                var newLines = ReadNewLinesFromFile(changedFile, _intelFileReadPos);
                if (newLines.Any())
                {
                    _logger.LogInformation("Found {Count} new lines in {FileName}, raising IntelFileChanged event", 
                        newLines.Count, fileName);
                    var eventArgs = new IntelFileChangedEventArgs(changedFile, fileName, channelName, isLocalChat, newLines);
                    IntelFileChanged?.Invoke(this, eventArgs);
                    _logger.LogDebug("IntelFileChanged event raised with {Count} lines", newLines.Count);
                }
                else
                {
                    _logger.LogDebug("No new lines found in {FileName}", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing intel file change: {FilePath}", e.FullPath);
            }
        }

        private void OnGameLogFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                var changedFile = e.FullPath;
                var fileName = e.Name ?? Path.GetFileName(changedFile);
                
                // Extract character name from filename (this logic may need refinement)
                var characterName = ExtractCharacterNameFromGameLog(fileName);

                var newLines = ReadNewLinesFromFile(changedFile, _gameFileReadPos);
                if (newLines.Any())
                {
                    var eventArgs = new GameLogFileChangedEventArgs(changedFile, fileName, characterName, newLines);
                    GameLogFileChanged?.Invoke(this, eventArgs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing game log file change: {FilePath}", e.FullPath);
            }
        }

        private List<string> ReadNewLinesFromFile(string filePath, Dictionary<string, int> readPositions)
        {
            var newLines = new List<string>();

            try
            {
                var encoding = Misc.GetEncoding(filePath);
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream, encoding);

                // Check if this is the first time we're reading this file
                bool isFirstRead = !readPositions.ContainsKey(filePath);
                
                if (isFirstRead)
                {
                    // On first read, skip to the end of the file to only process new lines going forward
                    // This prevents processing old intel when the app starts
                    int lineCount = 0;
                    while (!reader.EndOfStream)
                    {
                        reader.ReadLine();
                        lineCount++;
                    }
                    readPositions[filePath] = lineCount;
                    _logger.LogDebug("Initialized read position for {FileName} at line {LineCount} (end of file)", 
                        Path.GetFileName(filePath), lineCount);
                    return newLines; // Return empty - we've skipped all existing content
                }

                var fileReadFrom = readPositions[filePath];
                
                // Skip to the position we last read from
                for (int i = 0; i < fileReadFrom && !reader.EndOfStream; i++)
                {
                    reader.ReadLine();
                }

                // Read new lines
                var currentLine = fileReadFrom;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line != null)
                    {
                        newLines.Add(line);
                        currentLine++;
                    }
                }

                // Update the read position
                readPositions[filePath] = currentLine;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read from file: {FilePath}", filePath);
            }

            return newLines;
        }

        private string ExtractCharacterNameFromGameLog(string fileName)
        {
            // This is a simplified implementation - may need refinement based on actual file naming
            // Game log files typically have format like "20231204_123456_charactername.txt"
            var parts = fileName.Replace(".txt", "").Split('_');
            return parts.Length > 2 ? parts[2] : "Unknown";
        }

        #endregion
    }
}
