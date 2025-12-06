//-----------------------------------------------------------------------
// Universe Data Background Service
// Handles SOV campaigns, universe data, server info, connection updates
//-----------------------------------------------------------------------

#nullable enable
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SMT.EVEData.Configuration;

namespace SMT.EVEData.Services
{
    /// <summary>
    /// Background service for updating EVE universe data from ESI
    /// Replaces the SOV campaign and low-frequency update portions of the main background thread
    /// </summary>
    public class UniverseDataService : BackgroundService, IUniverseDataService
    {
        private readonly ILogger<UniverseDataService> _logger;
        private readonly IConfigurationService _configService;
        private readonly IServiceProvider _serviceProvider; // Use service provider to avoid circular dependency
        private bool _isRunning;

        private DateTime _nextSovCampaignUpdate = DateTime.MinValue;
        private DateTime _nextLowFrequencyUpdate = DateTime.MinValue;
        private DateTime _nextDotlanUpdate = DateTime.MinValue;

        public bool IsRunning => _isRunning;
        public TimeSpan SovCampaignUpdateInterval { get; private set; }
        public TimeSpan LowFrequencyUpdateInterval { get; private set; }
        public TimeSpan DotlanUpdateInterval { get; private set; }

        public UniverseDataService(
            ILogger<UniverseDataService> logger, 
            IConfigurationService configService,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            
            // Use intervals from configuration or defaults from original code
            SovCampaignUpdateInterval = TimeSpan.FromSeconds(30); // From original SOVCampaignUpdateRate
            LowFrequencyUpdateInterval = TimeSpan.FromMinutes(20); // From original LowFreqUpdateRate  
            DotlanUpdateInterval = TimeSpan.FromMinutes(30); // Reasonable default for Dotlan updates
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Universe Data Service starting - SOV: {SovInterval}s, LowFreq: {LowFreqInterval}m, Dotlan: {DotlanInterval}m", 
                SovCampaignUpdateInterval.TotalSeconds, LowFrequencyUpdateInterval.TotalMinutes, DotlanUpdateInterval.TotalMinutes);
            
            _isRunning = true;

            try
            {

                // Initialize next update times - schedule first updates sooner for better responsiveness
                _nextSovCampaignUpdate = DateTime.Now + TimeSpan.FromSeconds(5); // First SOV update in 5 seconds
                _nextLowFrequencyUpdate = DateTime.Now + TimeSpan.FromSeconds(5); // First low freq update in 5 seconds
                _nextDotlanUpdate = DateTime.Now + TimeSpan.FromSeconds(5); // First Dotlan update in 5 seconds
                
                _logger.LogInformation("Universe Data Service initialized - Next updates scheduled:");
                _logger.LogInformation("  SOV Campaign: {SovTime}", _nextSovCampaignUpdate);
                _logger.LogInformation("  Low Frequency: {LowFreqTime}", _nextLowFrequencyUpdate);
                _logger.LogInformation("  Dotlan: {DotlanTime}", _nextDotlanUpdate);

                // Main update loop - check what needs updating
                while (!stoppingToken.IsCancellationRequested)
                {
                    var now = DateTime.Now;

                    try
                    {
                        // Check SOV campaign updates
                        if (now >= _nextSovCampaignUpdate)
                        {
                            _logger.LogDebug("Starting SOV campaign update");
                            await UpdateSovCampaignsAsync();
                            _nextSovCampaignUpdate = now + SovCampaignUpdateInterval;
                        }

                        // Check low frequency updates (universe data, server info, connections)
                        if (now >= _nextLowFrequencyUpdate)
                        {
                            _logger.LogInformation("Starting low frequency update (universe data, server info, connections)");
                            await UpdateLowFrequencyDataAsync();
                            _nextLowFrequencyUpdate = now + LowFrequencyUpdateInterval;
                            _logger.LogInformation("Next low frequency update scheduled for: {NextTime}", _nextLowFrequencyUpdate);
                        }

                        // Check Dotlan updates
                        if (now >= _nextDotlanUpdate)
                        {
                            _logger.LogDebug("Starting Dotlan update");
                            await UpdateDotlanDataAsync();
                            _nextDotlanUpdate = now + DotlanUpdateInterval;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during universe data update cycle");
                    }

                    // Sleep for 1 second before checking again (similar to original 100ms but less aggressive)
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Universe Data Service cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in Universe Data Service");
            }
            finally
            {
                _isRunning = false;
                _logger.LogInformation("Universe Data Service stopped");
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

        public async Task ForceUniverseDataUpdateAsync()
        {
            _logger.LogInformation("Forcing immediate universe data update");
            await UpdateLowFrequencyDataAsync();
        }

        public async Task ForceSovCampaignUpdateAsync()
        {
            _logger.LogInformation("Forcing immediate SOV campaign update");
            await UpdateSovCampaignsAsync();
        }

        public async Task ForceDotlanUpdateAsync()
        {
            _logger.LogInformation("Forcing immediate Dotlan update");
            await UpdateDotlanDataAsync();
        }

        /// <summary>
        /// Update SOV campaign data - extracted from original loop
        /// </summary>
        private async Task UpdateSovCampaignsAsync()
        {
            try
            {
                _logger.LogTrace("Updating SOV campaigns");
                var eveManager = _serviceProvider.GetRequiredService<EveManager>();
                eveManager.UpdateSovCampaigns();
                _logger.LogTrace("SOV campaign update completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update SOV campaigns");
            }
        }

        /// <summary>
        /// Update low frequency data (universe data, server info, connections) - extracted from original loop
        /// </summary>
        private async Task UpdateLowFrequencyDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting low frequency data update");
                var eveManager = _serviceProvider.GetRequiredService<EveManager>();

                // Update universe data (kills, jumps, SOV, incursions, etc.)
                _logger.LogInformation("Updating ESI universe data");
                eveManager.UpdateESIUniverseData();

                // Update server info
                _logger.LogInformation("Updating server info");
                eveManager.UpdateServerInfo();

                // Update Thera connections
                _logger.LogInformation("Updating Thera connections");
                eveManager.UpdateTheraConnections();

                // Update Turnur connections  
                _logger.LogInformation("Updating Turnur connections");
                eveManager.UpdateTurnurConnections();

                _logger.LogInformation("Low frequency data update completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update low frequency data");
            }
        }

        /// <summary>
        /// Update Dotlan kill delta information - extracted from original loop
        /// </summary>
        private async Task UpdateDotlanDataAsync()
        {
            try
            {
                _logger.LogTrace("Updating Dotlan data");
                var eveManager = _serviceProvider.GetRequiredService<EveManager>();
                eveManager.UpdateDotlanKillDeltaInfo();
                _logger.LogTrace("Dotlan data update completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update Dotlan data");
            }
        }
    }
}
