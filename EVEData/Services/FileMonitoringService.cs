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

            await SetupIntelWatcherAsync(eveLogFolder);
            await SetupGameLogWatcherAsync(eveLogFolder);

            _isMonitoring = true;
            _logger.LogInformation("File monitoring started successfully");
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

            // Start the file cache trigger (the "super hack" from original code)
            await StartFileCacheTriggerAsync(stoppingToken);
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

            if (!Directory.Exists(chatlogFolder))
            {
                _logger.LogWarning("Chat log folder does not exist: {ChatlogFolder}", chatlogFolder);
                return;
            }

            _intelFileWatcher = new FileSystemWatcher(chatlogFolder)
            {
                Filter = "*.txt",
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _intelFileWatcher.Changed += OnIntelFileChanged;
            
            _logger.LogInformation("Intel file watcher setup for: {ChatlogFolder}", chatlogFolder);
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

            _logger.LogInformation("Starting file cache trigger for {FolderCount} folders", existingFolders.Count);

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

                        // Only process files from the last day
                        if (file.CreationTime > DateTime.Now.AddDays(-1) && shouldReadFile)
                        {
                            await TouchFileAsync(file.FullName);
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
            // Check intel filters
            foreach (var intelFilter in _intelFilters)
            {
                if (file.Name.Contains(intelFilter, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Local chat files
            if (file.Name.Contains("Local_"))
            {
                return true;
            }

            // Game logs
            if (folder.Contains("Gamelogs"))
            {
                return true;
            }

            return false;
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
                
                var channelParts = fileName.Split("_");
                var channelName = string.Join("_", channelParts, 0, channelParts.Length - 3);

                var isLocalChat = changedFile.Contains("Local_");
                var shouldProcess = isLocalChat || _intelFilters.Any(filter => 
                    changedFile.Contains(filter, StringComparison.OrdinalIgnoreCase));

                if (!shouldProcess)
                {
                    return;
                }

                var newLines = ReadNewLinesFromFile(changedFile, _intelFileReadPos);
                if (newLines.Any())
                {
                    var eventArgs = new IntelFileChangedEventArgs(changedFile, fileName, channelName, isLocalChat, newLines);
                    IntelFileChanged?.Invoke(this, eventArgs);
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

                var fileReadFrom = readPositions.ContainsKey(filePath) ? readPositions[filePath] : 0;
                
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
