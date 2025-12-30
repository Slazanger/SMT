//-----------------------------------------------------------------------
// ZKill Feed Background Service
// Modern replacement for ZKillRedisQ using BackgroundService
//-----------------------------------------------------------------------

#nullable enable
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SMT.EVEData.Configuration;
using ESI.NET;
using Newtonsoft.Json;

namespace SMT.EVEData.Services
{
    /// <summary>
    /// Modern background service for ZKillboard RedisQ feed
    /// Replaces the legacy ZKillRedisQ BackgroundWorker implementation
    /// </summary>
    public class ZKillFeedService : BackgroundService, IZKillFeedService
    {
        private readonly ILogger<ZKillFeedService> _logger;
        private readonly IConfigurationService _configService;
        private readonly IServiceProvider _serviceProvider;
        private readonly HttpClient _httpClient;
        private bool _isRunning;

        private readonly string _queueId;
        private readonly List<ZKBDataSimple> _killStream = new();
        private readonly object _killStreamLock = new();

        public bool IsRunning => _isRunning;
        public int KillExpireTimeMinutes { get; set; } = 60; // Default 60 minutes
        public bool PauseUpdate { get; set; } = false;

        public List<ZKBDataSimple> KillStream 
        { 
            get 
            { 
                lock (_killStreamLock) 
                { 
                    return new List<ZKBDataSimple>(_killStream); 
                } 
            } 
        }

        public event EventHandler? KillsAddedEvent;

        public ZKillFeedService(
            ILogger<ZKillFeedService> logger,
            IConfigurationService configService,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            
            // Create HTTP client - will be disposed when service stops
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            // Set User-Agent header (must be valid HTTP header format)
            var userAgent = _configService.GetUserAgent();
            if (!string.IsNullOrWhiteSpace(userAgent))
            {
                // Ensure User-Agent is properly formatted (no leading colons or spaces)
                userAgent = userAgent.Trim();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            }

            // Generate unique queue ID
            _queueId = "SMT_" + EVEDataUtils.Misc.RandomString(35);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ZKill Feed Service starting with queue ID: {QueueId}", _queueId);
            _isRunning = true;

            try
            {
                // Wait for application to initialize
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

                // Start cleanup timer for expired kills
                _ = Task.Run(() => CleanupExpiredKillsAsync(stoppingToken), stoppingToken);

                // Track last alliance resolution check
                DateTime lastAllianceCheck = DateTime.Now;
                
                // Main polling loop
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        if (!PauseUpdate)
                        {
                            await FetchAndProcessKillDataAsync();
                        }
                        
                        // Periodically check for unresolved alliance names (like original zkb_DoWorkComplete)
                        // Check every 5 seconds
                        if (DateTime.Now - lastAllianceCheck > TimeSpan.FromSeconds(5))
                        {
                            await CheckAndResolveAllianceNamesAsync();
                            lastAllianceCheck = DateTime.Now;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during ZKill feed update");
                        
                        // Wait longer on errors to avoid hammering the API
                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    }

                    // Standard polling interval (1 second like original)
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ZKill Feed Service cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in ZKill Feed Service");
            }
            finally
            {
                _isRunning = false;
                _httpClient?.Dispose();
                _logger.LogInformation("ZKill Feed Service stopped");
            }
        }

        public new async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);
        }

        public async Task StopAsync()
        {
            await base.StopAsync(CancellationToken.None);
        }

        public async Task ForceUpdateAsync()
        {
            _logger.LogInformation("Forcing immediate ZKill feed update");
            await FetchAndProcessKillDataAsync();
        }

        /// <summary>
        /// Fetch and process kill data from ZKillboard RedisQ
        /// This replaces the legacy zkb_DoWork method
        /// </summary>
        private async Task FetchAndProcessKillDataAsync()
        {
            try
            {
                string redistUrl = $"https://zkillredisq.stream/listen.php?queueID={_queueId}";
                
                _logger.LogTrace("Fetching kill data from: {Url}", redistUrl);

                var response = await _httpClient.GetAsync(redistUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ZKill API returned error: {StatusCode}", response.StatusCode);
                    
                    // Wait longer on API errors (original had 500 second wait on error)
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5)); // 5 minutes for rate limiting
                    }
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogTrace("No kill data received");
                    await Task.Delay(TimeSpan.FromSeconds(10)); // Wait 10s when no data (from original)
                    return;
                }

                await ProcessKillDataAsync(content);
            }
            catch (TaskCanceledException)
            {
                _logger.LogTrace("ZKill request cancelled/timed out");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching kill data from ZKillboard");
            }
        }

        /// <summary>
        /// Process kill data JSON and convert to ZKBDataSimple
        /// This replaces the logic from zkb_DoWork and zkb_DoWorkComplete
        /// </summary>
        private async Task ProcessKillDataAsync(string jsonContent)
        {
            try
            {
                var zkbData = ZKBData.ZkbData.FromJson(jsonContent);
                if (zkbData?.Package == null)
                {
                    _logger.LogTrace("No valid kill package in response");
                    return;
                }

                var killSimple = new ZKBDataSimple();
                killSimple.KillID = long.Parse(zkbData.Package.KillId.ToString());

                string killHash = zkbData.Package.Zkb.Hash;

                // Get kill details from ESI
                var eveManager = _serviceProvider.GetRequiredService<EveManager>();
                
                _logger.LogTrace("Resolving kill details for kill ID: {KillId}", killSimple.KillID);

                var killMailResponse = await eveManager.ESIClient.Killmails.Information(killHash, (int)killSimple.KillID);
                
                if (!ESIHelpers.ValidateESICall(killMailResponse))
                {
                    _logger.LogWarning("Failed to get kill details from ESI for kill ID: {KillId}", killSimple.KillID);
                    return;
                }

                // Populate kill data
                killSimple.VictimAllianceID = killMailResponse.Data.Victim.AllianceId;
                killSimple.VictimCharacterID = killMailResponse.Data.Victim.CharacterId;
                killSimple.VictimCorpID = killMailResponse.Data.Victim.CorporationId;
                killSimple.SystemName = eveManager.GetEveSystemNameFromID(killMailResponse.Data.SolarSystemId);
                killSimple.KillTime = killMailResponse.Data.KillmailTime.ToLocalTime();

                string shipId = killMailResponse.Data.Victim.ShipTypeId.ToString();
                if (eveManager.ShipTypes.ContainsKey(shipId))
                {
                    killSimple.ShipType = eveManager.ShipTypes[shipId];
                }
                else
                {
                    killSimple.ShipType = $"Unknown ({shipId})";
                }

                killSimple.VictimAllianceName = eveManager.GetAllianceName(killSimple.VictimAllianceID);

                // Resolve alliance IDs if needed (from original zkb_DoWorkComplete logic)
                List<int> allianceIdsToResolve = new List<int>();
                if (string.IsNullOrEmpty(killSimple.VictimAllianceName) && killSimple.VictimAllianceID != 0)
                {
                    if (!eveManager.HasAllianceID(killSimple.VictimAllianceID))
                    {
                        allianceIdsToResolve.Add(killSimple.VictimAllianceID);
                    }
                    else
                    {
                        killSimple.VictimAllianceName = eveManager.GetAllianceName(killSimple.VictimAllianceID);
                    }
                }

                // Add to kill stream (thread-safe)
                lock (_killStreamLock)
                {
                    _killStream.Insert(0, killSimple);
                    
                    // Keep only recent kills (limit to reasonable size)
                    while (_killStream.Count > 1000) // Keep more kills than original
                    {
                        _killStream.RemoveAt(_killStream.Count - 1);
                    }
                }

                // Resolve alliance IDs if needed (fire and forget - don't block kill processing)
                if (allianceIdsToResolve.Count > 0)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            eveManager.ResolveAllianceIDs(allianceIdsToResolve);
                            
                            // Update alliance names for all kills with these alliance IDs
                            // This ensures we update all kills, not just the current one
                            bool updatedAny = false;
                            lock (_killStreamLock)
                            {
                                foreach (var allianceId in allianceIdsToResolve)
                                {
                                    var allianceName = eveManager.GetAllianceName(allianceId);
                                    foreach (var kill in _killStream.Where(k => k.VictimAllianceID == allianceId && string.IsNullOrEmpty(k.VictimAllianceName)))
                                    {
                                        kill.VictimAllianceName = allianceName;
                                        updatedAny = true;
                                    }
                                }
                            }
                            
                            // Notify UI if any alliance names were updated
                            if (updatedAny)
                            {
                                KillsAddedEvent?.Invoke(this, EventArgs.Empty);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to resolve alliance IDs for kill {KillId}", killSimple.KillID);
                        }
                    });
                }

                _logger.LogDebug("Added kill: {ShipType} in {System} at {KillTime}", 
                    killSimple.ShipType, killSimple.SystemName, killSimple.KillTime);

                // Notify subscribers
                KillsAddedEvent?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing kill data");
            }
        }

        /// <summary>
        /// Check for kills with unresolved alliance names and resolve them
        /// Similar to the original zkb_DoWorkComplete logic
        /// </summary>
        private async Task CheckAndResolveAllianceNamesAsync()
        {
            try
            {
                var eveManager = _serviceProvider.GetRequiredService<EveManager>();
                List<int> allianceIdsToResolve = new List<int>();
                
                lock (_killStreamLock)
                {
                    // Find all kills with empty alliance names that need resolution
                    foreach (var kill in _killStream)
                    {
                        if (string.IsNullOrEmpty(kill.VictimAllianceName) && kill.VictimAllianceID != 0)
                        {
                            if (!eveManager.HasAllianceID(kill.VictimAllianceID) && 
                                !allianceIdsToResolve.Contains(kill.VictimAllianceID))
                            {
                                allianceIdsToResolve.Add(kill.VictimAllianceID);
                            }
                            else if (eveManager.HasAllianceID(kill.VictimAllianceID))
                            {
                                // Alliance is already resolved, just update the name
                                kill.VictimAllianceName = eveManager.GetAllianceName(kill.VictimAllianceID);
                            }
                        }
                    }
                }
                
                // Resolve any missing alliance IDs
                if (allianceIdsToResolve.Count > 0)
                {
                    eveManager.ResolveAllianceIDs(allianceIdsToResolve);
                    
                    // Update all kills with the newly resolved alliance names
                    bool updatedAny = false;
                    lock (_killStreamLock)
                    {
                        foreach (var allianceId in allianceIdsToResolve)
                        {
                            var allianceName = eveManager.GetAllianceName(allianceId);
                            foreach (var kill in _killStream.Where(k => k.VictimAllianceID == allianceId && string.IsNullOrEmpty(k.VictimAllianceName)))
                            {
                                kill.VictimAllianceName = allianceName;
                                updatedAny = true;
                            }
                        }
                    }
                    
                    // Notify UI if any alliance names were updated
                    if (updatedAny)
                    {
                        KillsAddedEvent?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking and resolving alliance names");
            }
        }

        /// <summary>
        /// Cleanup expired kills based on KillExpireTimeMinutes
        /// </summary>
        private async Task CleanupExpiredKillsAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cutoffTime = DateTime.Now.AddMinutes(-KillExpireTimeMinutes);
                    
                    bool updatedKillList = false;
                    lock (_killStreamLock)
                    {
                        int initialCount = _killStream.Count;
                        _killStream.RemoveAll(k => k.KillTime < cutoffTime);
                        int removedCount = initialCount - _killStream.Count;
                        
                        if (removedCount > 0)
                        {
                            updatedKillList = true;
                            _logger.LogTrace("Removed {RemovedCount} expired kills", removedCount);
                        }
                    }
                    
                    // Notify if kills were removed (from original zkb_DoWorkComplete)
                    if (updatedKillList)
                    {
                        KillsAddedEvent?.Invoke(this, EventArgs.Empty);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during kill cleanup");
                }

                // Check for expired kills every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}