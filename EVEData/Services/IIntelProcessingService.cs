//-----------------------------------------------------------------------
// Intel Processing Service Interface
//-----------------------------------------------------------------------

#nullable enable
using System;
using System.Collections.Generic;
using SMT.EVEData;
using EVEDataUtils;

namespace SMT.EVEData.Services
{
    /// <summary>
    /// Service for processing intel and game log data from file monitoring
    /// </summary>
    public interface IIntelProcessingService
    {
        /// <summary>
        /// Intel data list (thread-safe queue)
        /// </summary>
        FixedQueue<IntelData> IntelDataList { get; }

        /// <summary>
        /// Game log data list (thread-safe queue)
        /// </summary>
        FixedQueue<GameLogData> GameLogList { get; }

        /// <summary>
        /// Intel channel filters (channels to monitor)
        /// </summary>
        List<string> IntelFilters { get; }

        /// <summary>
        /// Intel alert filters (triggers for intel alerts)
        /// </summary>
        List<string> IntelAlertFilters { get; }

        /// <summary>
        /// Intel clear filters (markers for clearing intel)
        /// </summary>
        List<string> IntelClearFilters { get; }

        /// <summary>
        /// Intel ignore filters (markers to ignore)
        /// </summary>
        List<string> IntelIgnoreFilters { get; }

        /// <summary>
        /// Intel Updated Event Handler
        /// </summary>
        event EveManager.IntelUpdatedEventHandler? IntelUpdatedEvent;

        /// <summary>
        /// GameLog Added Event Handler
        /// </summary>
        event EveManager.GameLogAddedEventHandler? GameLogAddedEvent;

        /// <summary>
        /// Ship Decloak Event Handler
        /// </summary>
        event EveManager.ShipDecloakedEventHandler? ShipDecloakedEvent;

        /// <summary>
        /// Combat Event Handler
        /// </summary>
        event EveManager.CombatEventHandler? CombatEvent;

        /// <summary>
        /// Process intel file lines from file monitoring service
        /// </summary>
        void ProcessIntelFileLines(string filePath, string channelName, bool isLocalChat, List<string> newLines);

        /// <summary>
        /// Process game log file lines from file monitoring service
        /// </summary>
        void ProcessGameLogFileLines(string filePath, string characterName, List<string> newLines);

        /// <summary>
        /// Load intel filters from disk
        /// </summary>
        void LoadFiltersFromDisk(string saveDataRootFolder);

        /// <summary>
        /// Save intel filters to disk
        /// </summary>
        void SaveFiltersToDisk(string saveDataRootFolder);

        /// <summary>
        /// Initialize the service with systems collection for matching
        /// </summary>
        void Initialize(Func<List<System>> getSystems);
    }
}

