//-----------------------------------------------------------------------
// ZKillboard ReDisQ feed
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SMT.EVEData
{
    /// <summary>
    /// The ZKillboard RedisQ representation
    /// </summary>
    public class ZKillRedisQ
    {
        private BackgroundWorker backgroundWorker;

        public string VerString = "ABC123";


        /// <summary>
        /// Gets or sets the Stream of the last few kills from ZKillBoard
        /// </summary>
        public ObservableCollection<ZKBDataSimple> KillStream { get; set; }

        /// <summary>
        ///
        /// </summary>
        public bool PauseUpdate { get; set; }


        /// <summary>
        /// Initialise the ZKB feed system
        /// </summary>
        public void Initialise()
        {
            KillStream = new ObservableCollection<ZKBDataSimple>();

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.WorkerReportsProgress = false;
            backgroundWorker.DoWork += zkb_DoWork;
            backgroundWorker.RunWorkerCompleted += zkb_DoWorkComplete;

            DispatcherTimer dp = new DispatcherTimer();
            dp.Interval = TimeSpan.FromSeconds(10);
            dp.Tick += Dp_Tick;
            dp.Start();
        }


        private void Dp_Tick(object sender, EventArgs e)
        {
            if (!backgroundWorker.IsBusy && !PauseUpdate)
            {
                backgroundWorker.RunWorkerAsync();
            }
        }

        private async void zkb_DoWork(object sender, DoWorkEventArgs e)
        {
            string redistURL = @"https://redisq.zkillboard.com/listen.php";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(redistURL);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 60000;
            request.UserAgent = VerString;
            request.KeepAlive = true;
            request.Proxy = null;
            HttpWebResponse response;

            try
            {
                response = request.GetResponse() as HttpWebResponse;
            }
            catch (Exception ex)
            {
                e.Result = -1;
                return;
            }

            Stream responseStream = response.GetResponseStream();

            using (StreamReader sr = new StreamReader(responseStream))
            {
                try
                {
                    // Need to return this response
                    string strContent = sr.ReadToEnd();

                    ZKBData.ZkbData z = ZKBData.ZkbData.FromJson(strContent);
                    if (z.Package != null)
                    {
                        ZKBDataSimple zs = new ZKBDataSimple();
                        zs.KillID = long.Parse(z.Package.KillId.ToString());
                        zs.VictimAllianceID = long.Parse(z.Package.Killmail.Victim.AllianceId.ToString());
                        zs.VictimCharacterID = long.Parse(z.Package.Killmail.Victim.CharacterId.ToString());
                        zs.VictimCorpID = long.Parse(z.Package.Killmail.Victim.CharacterId.ToString());
                        zs.SystemName = EveManager.Instance.GetEveSystemNameFromID(z.Package.Killmail.SolarSystemId);
                        if (zs.SystemName == string.Empty)
                        {
                            zs.SystemName = z.Package.Killmail.SolarSystemId.ToString();
                        }

                        zs.KillTime = z.Package.Killmail.KillmailTime;
                        string shipID = z.Package.Killmail.Victim.ShipTypeId.ToString();
                        if (EveManager.Instance.ShipTypes.Keys.Contains(shipID))
                        {
                            zs.ShipType = EveManager.Instance.ShipTypes[shipID];
                        }
                        else
                        {
                            zs.ShipType = "Unknown (" + shipID + ")";
                        }

                        zs.VictimAllianceName = EveManager.Instance.GetAllianceName(zs.VictimAllianceID);

                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            KillStream.Insert(0, zs);
                        }));
                    }
                }
                catch
                {
                    e.Result = -1;
                    return;
                }
            }

            e.Result = 0;
        }



        private void zkb_DoWorkComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            List<long> AllianceIDs = new List<long>();

            for (int i = KillStream.Count - 1; i >= 0; i--)
            {
                if (KillStream[i].VictimAllianceName == string.Empty)
                {
                    if (!EveManager.Instance.AllianceIDToTicker.Keys.Contains(KillStream[i].VictimAllianceID) && !AllianceIDs.Contains(KillStream[i].VictimAllianceID) && KillStream[i].VictimAllianceID != 0)
                    {
                        AllianceIDs.Add(KillStream[i].VictimAllianceID);
                    }
                    else
                    {
                        KillStream[i].VictimAllianceName = EveManager.Instance.GetAllianceName(KillStream[i].VictimAllianceID);
                    }
                }

                if (KillStream[i].KillTime + TimeSpan.FromMinutes(30) < DateTimeOffset.Now)
                {

                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        KillStream.RemoveAt(i);
                    }));

                }
            }
            if (AllianceIDs.Count > 0)
            {
                EveManager.Instance.ResolveAllianceIDs(AllianceIDs);
            }

        }


        public void ShutDown()
        {
            backgroundWorker.CancelAsync();
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
            public long VictimAllianceID { get; set; }

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
            public long VictimCharacterID { get; set; }

            /// <summary>
            /// Gets or sets the Victim's corp ID
            /// </summary>
            public long VictimCorpID { get; set; }

            public override string ToString()
            {
                string allianceTicker = EVEData.EveManager.Instance.GetAllianceTicker(VictimAllianceID);
                if (allianceTicker == string.Empty)
                {
                    allianceTicker = VictimAllianceID.ToString();
                }

                return string.Format("System: {0}, Alliance: {1}, Ship {2}", SystemName, allianceTicker, ShipType);
            }

            protected void OnPropertyChanged(string name)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }
    }
}