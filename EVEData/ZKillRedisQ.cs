//-----------------------------------------------------------------------
// ZKillboard R2Z2 feed
//-----------------------------------------------------------------------
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Net;
using EVEDataUtils;

namespace SMT.EVEData
{
    /// <summary>
    /// The ZKillboard R2Z2 feed representation
    /// </summary>
    public class ZKillRedisQ : IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly object lifecycleLock = new object();
        private readonly object allianceResolutionLock = new object();
        private readonly HashSet<int> pendingAllianceResolutions = new HashSet<int>();
        private CancellationTokenSource cancellationSource;
        private Task pollingTask = Task.CompletedTask;
        private long currentSequence = 0;
        private volatile bool pauseUpdate;
        private int killExpireTimeMinutes = 30;
        private bool disposed;

        public ZKillRedisQ()
        {
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SMT/" + EveAppConfig.SMT_VERSION + EveAppConfig.SMT_USERAGENT_DETAILS);
        }

        /// <summary>
        /// Gets or sets the Stream of the last few kills from ZKillBoard
        /// </summary>
        public ObservableCollection<ZKBDataSimple> KillStream { get; } = new ObservableCollection<ZKBDataSimple>();

        /// <summary>
        /// Kills Added Event Handler
        /// </summary>
        public delegate void KillsAddedHandler();

        /// <summary>
        /// Kills Added Events
        /// </summary>
        public event KillsAddedHandler KillsAddedEvent;

        public int KillExpireTimeMinutes
        {
            get => Volatile.Read(ref killExpireTimeMinutes);
            set => Volatile.Write(ref killExpireTimeMinutes, Math.Max(5, value));
        }

        /// <summary>
        ///
        /// </summary>
        public bool PauseUpdate
        {
            get => pauseUpdate;
            set => pauseUpdate = value;
        }

        public Task Completion
        {
            get
            {
                lock(lifecycleLock)
                {
                    return pollingTask;
                }
            }
        }

        /// <summary>
        /// Initialise the ZKB feed system
        /// </summary>
        public void Initialise()
        {
            lock(lifecycleLock)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                if(!pollingTask.IsCompleted)
                {
                    return;
                }

                cancellationSource?.Dispose();
                cancellationSource = new CancellationTokenSource();
                currentSequence = 0;
                RunOnUIThread(() => KillStream.Clear());
                pollingTask = Task.Run(() => PollLoopAsync(cancellationSource.Token));
            }
        }

        public void ShutDown()
        {
            lock(lifecycleLock)
            {
                cancellationSource?.Cancel();
            }
        }

        public void Dispose()
        {
            lock(lifecycleLock)
            {
                if(disposed)
                {
                    return;
                }

                disposed = true;
                cancellationSource?.Cancel();
                cancellationSource?.Dispose();
                httpClient.Dispose();
            }
        }

        private async Task PollLoopAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                if(PauseUpdate)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                try
                {
                    if(currentSequence == 0 && !await LoadCurrentSequenceAsync(cancellationToken).ConfigureAwait(false))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(6), cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    TimeSpan delay = await PollCurrentSequenceAsync(cancellationToken).ConfigureAwait(false);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch(Exception exception)
                {
                    AppLog.Error("ZKill feed", exception);
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<bool> LoadCurrentSequenceAsync(CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await httpClient.GetAsync(
                "https://r2z2.zkillboard.com/ephemeral/sequence.json",
                cancellationToken).ConfigureAwait(false);

            if(!response.IsSuccessStatusCode)
            {
                return false;
            }

            string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            ZKBData.SequenceData sequenceData = ZKBData.SequenceData.FromJson(content);
            if(sequenceData == null || sequenceData.Sequence <= 0)
            {
                return false;
            }

            currentSequence = sequenceData.Sequence;
            AppLog.Info("ZKill feed", "Connected to the live feed.");
            return true;
        }

        private async Task<TimeSpan> PollCurrentSequenceAsync(CancellationToken cancellationToken)
        {
            string requestUrl = $"https://r2z2.zkillboard.com/ephemeral/{currentSequence}.json";
            using HttpResponseMessage response = await httpClient.GetAsync(requestUrl, cancellationToken).ConfigureAwait(false);

            if(response.StatusCode == HttpStatusCode.NotFound)
            {
                ExpireOldKills();
                return TimeSpan.FromSeconds(6);
            }

            if(response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
            }

            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            ZKBData.R2Z2Data data = ZKBData.R2Z2Data.FromJson(content);

            if(data?.Esi?.Victim != null)
            {
                ZKBDataSimple kill = CreateKill(data);
                RunOnUIThread(() =>
                {
                    KillStream.Insert(0, kill);
                    ExpireOldKillsCore();
                    NotifyKillsChanged();
                });

                QueueAllianceResolution(kill.VictimAllianceID, kill.VictimAllianceName);
            }
            else
            {
                ExpireOldKills();
            }

            currentSequence++;
            return TimeSpan.FromMilliseconds(150);
        }

        private static ZKBDataSimple CreateKill(ZKBData.R2Z2Data data)
        {
            string shipID = data.Esi.Victim.ShipTypeId.ToString();
            string shipType = EveManager.Instance.ShipTypes.TryGetValue(shipID, out string knownShipType)
                ? knownShipType
                : "Unknown (" + shipID + ")";

            return new ZKBDataSimple
            {
                KillID = data.KillmailId,
                VictimAllianceID = data.Esi.Victim.AllianceId,
                VictimCharacterID = data.Esi.Victim.CharacterId,
                VictimCorpID = data.Esi.Victim.CorporationId,
                SystemName = EveManager.Instance.GetEveSystemNameFromID((int)data.Esi.SolarSystemId),
                KillTime = data.Esi.KillmailTime.ToLocalTime(),
                ShipType = shipType,
                VictimAllianceName = EveManager.Instance.GetAllianceName(data.Esi.Victim.AllianceId),
            };
        }

        private void QueueAllianceResolution(int allianceID, string allianceName)
        {
            if(allianceID == 0 || !string.IsNullOrEmpty(allianceName))
            {
                return;
            }

            lock(allianceResolutionLock)
            {
                if(!pendingAllianceResolutions.Add(allianceID))
                {
                    return;
                }
            }

            _ = ResolveAllianceNameAsync(allianceID);
        }

        private async Task ResolveAllianceNameAsync(int allianceID)
        {
            try
            {
                await EveManager.Instance.ResolveAllianceIDs(new List<int> { allianceID }).ConfigureAwait(false);
                string allianceName = EveManager.Instance.GetAllianceName(allianceID);
                RunOnUIThread(() =>
                {
                    foreach(ZKBDataSimple kill in KillStream.Where(kill => kill.VictimAllianceID == allianceID))
                    {
                        kill.VictimAllianceName = allianceName;
                    }
                });
            }
            catch(Exception exception)
            {
                AppLog.Error("Resolve zKill alliance", exception);
            }
            finally
            {
                lock(allianceResolutionLock)
                {
                    pendingAllianceResolutions.Remove(allianceID);
                }
            }
        }

        private void ExpireOldKills()
        {
            RunOnUIThread(() =>
            {
                if(ExpireOldKillsCore())
                {
                    NotifyKillsChanged();
                }
            });
        }

        private void NotifyKillsChanged()
        {
            try
            {
                KillsAddedEvent?.Invoke();
            }
            catch(Exception exception)
            {
                AppLog.Error("Update zKill UI", exception);
            }
        }

        private bool ExpireOldKillsCore()
        {
            bool changed = false;
            DateTimeOffset cutoff = DateTimeOffset.Now - TimeSpan.FromMinutes(KillExpireTimeMinutes);
            for(int index = KillStream.Count - 1; index >= 0; index--)
            {
                if(KillStream[index].KillTime < cutoff)
                {
                    KillStream.RemoveAt(index);
                    changed = true;
                }
            }

            return changed;
        }

        private static void RunOnUIThread(Action action)
        {
            if(EveManager.UIThreadInvoker != null)
            {
                EveManager.UIThreadInvoker(action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// A simple class with the Kill Highlights
        /// </summary>
        public class ZKBDataSimple : INotifyPropertyChanged
        {
            private string m_victimAllianceName;

            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            /// Gets or sets the ZKillboard Kill ID
            /// </summary>
            public long KillID { get; set; }

            /// <summary>
            /// Gets or sets the time of the kill
            /// </summary>
            public DateTimeOffset KillTime { get; set; }

            /// <summary>
            /// Gets or sets the Ship Lost in this kill
            /// </summary>
            public string ShipType { get; set; }

            /// <summary>
            /// Gets or sets the System ID the kill was in
            /// </summary>
            public string SystemName { get; set; }

            /// <summary>
            /// Gets or sets the Victims Alliance ID
            /// </summary>
            public int VictimAllianceID { get; set; }

            /// <summary>
            /// Gets or sets the Victims Alliance Name
            /// </summary>
            public string VictimAllianceName
            {
                get
                {
                    return m_victimAllianceName;
                }
                set
                {
                    m_victimAllianceName = value;
                    OnPropertyChanged("VictimAllianceName");
                }
            }

            /// <summary>
            /// Gets or sets the character ID of the victim
            /// </summary>
            public int VictimCharacterID { get; set; }

            /// <summary>
            /// Gets or sets the Victim's corp ID
            /// </summary>
            public int VictimCorpID { get; set; }

            public override string ToString()
            {
                string allianceTicker = EVEData.EveManager.Instance.GetAllianceTicker(VictimAllianceID);
                if(allianceTicker == string.Empty)
                {
                    allianceTicker = VictimAllianceID.ToString();
                }

                return string.Format("System: {0}, Alliance: {1}, Ship {2}", SystemName, allianceTicker, ShipType);
            }

            protected void OnPropertyChanged(string name)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if(handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }
    }
}
