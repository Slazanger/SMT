//-----------------------------------------------------------------------
// Character Update Background Service
// Handles regular character position and info updates
//-----------------------------------------------------------------------

#nullable enable
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SMT.EVEData.Configuration;

namespace SMT.EVEData.Services
{
    /// <summary>
    /// Background service for updating character data from ESI
    /// Replaces the character update portion of the main background thread
    /// </summary>
    public class CharacterUpdateService : BackgroundService, ICharacterUpdateService
    {
        private readonly ILogger<CharacterUpdateService> _logger;
        private readonly IConfigurationService _configService;
        private readonly IServiceProvider _serviceProvider; // Use service provider to avoid circular dependency
        private bool _isRunning;

        public bool IsRunning => _isRunning;
        public TimeSpan UpdateInterval { get; private set; }

        public CharacterUpdateService(
            ILogger<CharacterUpdateService> logger, 
            IConfigurationService configService,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            
            // Default to 2 second updates (from original CharacterUpdateRate)
            UpdateInterval = TimeSpan.FromSeconds(2);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Character Update Service starting with {UpdateInterval}s interval", UpdateInterval.TotalSeconds);
            _isRunning = true;

            try
            {
               
                // Initial character setup - refresh tokens and do initial position/info updates
                await InitialCharacterSetupAsync();

                // Main update loop
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await UpdateAllCharactersAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during character update cycle");
                    }

                    // Wait for next update cycle
                    await Task.Delay(UpdateInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Character Update Service cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in Character Update Service");
            }
            finally
            {
                _isRunning = false;
                _logger.LogInformation("Character Update Service stopped");
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
            _logger.LogInformation("Forcing immediate character update");
            await UpdateAllCharactersAsync();
        }

        /// <summary>
        /// Initial character setup - refresh tokens and do initial updates
        /// This replaces the initial setup logic from StartBackgroundThread
        /// </summary>
        private async Task InitialCharacterSetupAsync()
        {
            try
            {
                var eveManager = _serviceProvider.GetRequiredService<EveManager>();
                var characters = eveManager.GetLocalCharactersCopy();
                
                if (characters.Count == 0)
                {
                    _logger.LogInformation("No characters loaded yet - skipping initial character setup");
                    return;
                }
                
                _logger.LogInformation("Starting initial character setup for {CharacterCount} characters", characters.Count);

                // Step 1: Refresh all access tokens
                foreach (var character in characters)
                {
                    try
                    {
                        await character.RefreshAccessToken();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to refresh access token for character {Character}", character.Name);
                    }
                }

                // Step 2: Update all positions
                foreach (var character in characters)
                {
                    try
                    {
                        await character.UpdatePositionFromESI();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update position for character {Character}", character.Name);
                    }
                }

                // Step 3: Update all character info
                foreach (var character in characters)
                {
                    try
                    {
                        await character.UpdateInfoFromESI();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update info for character {Character}", character.Name);
                    }
                }

                _logger.LogInformation("Initial character setup completed for {CharacterCount} characters", characters.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during initial character setup");
            }
        }

        /// <summary>
        /// Update all characters - this replaces the character update portion of the main loop
        /// </summary>
        private async Task UpdateAllCharactersAsync()
        {
            var eveManager = _serviceProvider.GetRequiredService<EveManager>();
            var characters = eveManager.GetLocalCharactersCopy();
            
            if (characters.Count == 0)
            {
                return;
            }

            _logger.LogTrace("Updating {CharacterCount} characters", characters.Count);

            for (int i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                try
                {
                    await character.Update();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update character {Character}", character.Name);
                }
            }

            _logger.LogTrace("Character update cycle completed");
        }
    }
}
