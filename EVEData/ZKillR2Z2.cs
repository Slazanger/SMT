//-----------------------------------------------------------------------
// ZKillboard R2Z2 feed
//-----------------------------------------------------------------------
using System.ComponentModel;
using System.Net;
using Timer = System.Timers.Timer;

namespace SMT.EVEData
{
    /// <summary>
    /// The ZKillboard R2Z2 feed representation
    /// </summary>
    public class ZKillR2Z2
    {
        private BackgroundWorker backgroundWorker;

        private long currentSequence = 0;
        private DateTime nextPollTime = DateTime.MinValue;

        /// <summary>
        /// Gets or sets the Stream of the last few kills from ZKillBoard
        /// </summary>
        public List<ZKBDataSimple> KillStream { get; set; }

        /// <summary>
        /// Kills Added Event Handler
        /// </summary>
        public delegate void KillsAddedHandler();

        /// <summary>
        /// Kills Added Events
        /// </summary>
        public event KillsAddedHandler KillsAddedEvent;

        public int KillExpireTimeMinutes { get; set; }

        /// <summary>
        ///
        /// </summary>
        public bool PauseUpdate { get; set; }

        /// <summary>
        /// Initialise the ZKB feed system
        /// </summary>
        public void Initialise()
        {
            KillStream = new List<ZKBDataSimple>();

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.WorkerReportsProgress = false;
            backgroundWorker.DoWork += zkb_DoWork;
            backgroundWorker.RunWorkerCompleted += zkb_DoWorkComplete;

            Timer dp = new Timer(150);
            dp.Elapsed += Dp_Tick;
            dp.AutoReset = true;
            dp.Enabled = true;
        }

        public void ShutDown()
        {
            backgroundWorker.CancelAsync();
        }

        private void Dp_Tick(object sender, EventArgs e)
        {
            if(!backgroundWorker.IsBusy && !PauseUpdate && DateTime.Now >= nextPollTime)
            {
                backgroundWorker.RunWorkerAsync();
            }
        }

        private void zkb_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                HttpClient hc = new HttpClient();

                string userAgent = "SMT/" + EveAppConfig.SMT_VERSION + EveAppConfig.SMT_USERAGENT_DETAILS;
                hc.DefaultRequestHeaders.Add("User-Agent", userAgent);

                if(currentSequence == 0)
                {
                    string seqUrl = "https://r2z2.zkillboard.com/ephemeral/sequence.json";
                    var seqResponse = hc.GetAsync(seqUrl).Result;
                    if(seqResponse.IsSuccessStatusCode)
                    {
                        string seqContent = seqResponse.Content.ReadAsStringAsync().Result;
                        EVEData.SequenceData seqData = EVEData.SequenceData.FromJson(seqContent);
                        if(seqData != null)
                        {
                            currentSequence = seqData.Sequence;
                        }
                    }
                    if(currentSequence == 0)
                    {
                        nextPollTime = DateTime.Now.AddSeconds(6);
                        e.Result = 0;
                        return;
                    }
                }

                string r2z2Url = $"https://r2z2.zkillboard.com/ephemeral/{currentSequence}.json";
                var response = hc.GetAsync(r2z2Url).Result;

                if(response.StatusCode == HttpStatusCode.NotFound)
                {
                    nextPollTime = DateTime.Now.AddSeconds(6);
                    e.Result = 0;
                    return;
                }
                else if(response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    nextPollTime = DateTime.Now.AddSeconds(60);
                    e.Result = 0;
                    return;
                }
                else if(response.IsSuccessStatusCode)
                {
                    string strContent = response.Content.ReadAsStringAsync().Result;
                    EVEData.R2Z2Data r2z2Data = EVEData.R2Z2Data.FromJson(strContent);

                    if(r2z2Data != null && r2z2Data.Esi != null && r2z2Data.Esi.Victim != null)
                    {
                        ZKBDataSimple zs = new ZKBDataSimple();
                        zs.KillID = r2z2Data.KillmailId;
                        zs.VictimAllianceID = r2z2Data.Esi.Victim.AllianceId;
                        zs.VictimCharacterID = r2z2Data.Esi.Victim.CharacterId;
                        zs.VictimCorpID = r2z2Data.Esi.Victim.CorporationId;
                        zs.SystemName = EveManager.Instance.GetEveSystemNameFromID((int)r2z2Data.Esi.SolarSystemId);
                        zs.KillTime = r2z2Data.Esi.KillmailTime.ToLocalTime();

                        string shipID = r2z2Data.Esi.Victim.ShipTypeId.ToString();
                        if(EveManager.Instance.ShipTypes.ContainsKey(shipID))
                        {
                            zs.ShipType = EveManager.Instance.ShipTypes[shipID];
                        }
                        else
                        {
                            zs.ShipType = "Unknown (" + shipID + ")";
                        }

                        zs.VictimAllianceName = EveManager.Instance.GetAllianceName(zs.VictimAllianceID);

                        KillStream.Insert(0, zs);

                        if(KillsAddedEvent != null)
                        {
                            KillsAddedEvent();
                        }
                    }

                    currentSequence++;
                    e.Result = 0;
                }
                else
                {
                    // Any other error, just back off for a bit
                    nextPollTime = DateTime.Now.AddSeconds(10);
                    e.Result = -1;
                }
            }
            catch
            {
                nextPollTime = DateTime.Now.AddSeconds(10);
                e.Result = -1;
            }
        }

        private async void zkb_DoWorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            bool updatedKillList = false;

            List<int> AllianceIDs = new List<int>();

            for(int i = KillStream.Count - 1; i >= 0; i--)
            {
                if(KillStream[i].VictimAllianceName == string.Empty)
                {
                    if(!EveManager.Instance.AllianceIDToTicker.ContainsKey(KillStream[i].VictimAllianceID) && !AllianceIDs.Contains(KillStream[i].VictimAllianceID) && KillStream[i].VictimAllianceID != 0)
                    {
                        AllianceIDs.Add(KillStream[i].VictimAllianceID);
                    }
                    else
                    {
                        KillStream[i].VictimAllianceName = EveManager.Instance.GetAllianceName(KillStream[i].VictimAllianceID);
                    }
                }

                if(KillStream[i].KillTime + TimeSpan.FromMinutes(KillExpireTimeMinutes) < DateTimeOffset.Now)
                {
                    KillStream.RemoveAt(i);

                    updatedKillList = true;
                }
            }
            if(AllianceIDs.Count > 0)
            {
                await EveManager.Instance.ResolveAllianceIDs(AllianceIDs);
            }

            if(updatedKillList)
            {
                // kills are coming in so fast that this is redundant
                if(KillsAddedEvent != null)
                {
                    KillsAddedEvent();
                }
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