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
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace SMT.EVEData
{
    /// <summary>
    /// The ZKillboard RedisQ representation
    /// </summary>
    public class ZKillRedisQ
    {
        private bool updateThreadRunning = true;
        private Thread updateThread;

        ~ZKillRedisQ()
        {
            updateThreadRunning = false;
        }

        /// <summary>
        ///
        /// </summary>
        public bool PauseUpdate { get; set; }

        /// <summary>
        /// Gets or sets the Stream of the last few kills from ZKillBoard
        /// </summary>
        public ObservableCollection<ZKBDataSimple> KillStream { get; set; }

        /// <summary>
        /// Initialise the ZKB feed system
        /// </summary>
        public void Initialise()
        {
            KillStream = new ObservableCollection<ZKBDataSimple>();

            ThreadStart ts = new ThreadStart(UpdateThreadFunc);
            updateThread = new Thread(ts);
            updateThread.Name = "ZkillRedisQ Update";
            updateThread.Start();
        }

        public void ShutDown()
        {
            updateThreadRunning = false;
            if (!updateThread.Join(10000))
            {
                updateThread.Abort();
            }
        }

        /// <summary>
        /// The main update function
        /// </summary>
        private void UpdateThreadFunc()
        {
            string redistURL = @"https://redisq.zkillboard.com/listen.php";

            int cleanupCounter = 0;

            while (updateThreadRunning)
            {
                if (PauseUpdate)
                {
                    if (KillStream.Count > 0)
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            KillStream.Clear();
                        }), DispatcherPriority.ContextIdle, null);
                    }

                    Thread.Sleep(5000);
                    continue;
                }

                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(redistURL);
                request.Method = WebRequestMethods.Http.Get;
                request.Timeout = 60000;
                request.UserAgent = "SMT/0.60";
                request.KeepAlive = true;
                request.Proxy = null;
                HttpWebResponse response;

                try
                {
                    response = request.GetResponse() as HttpWebResponse;
                }
                catch (Exception)
                {
                    Thread.Sleep(60000);
                    continue;
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
                           }), DispatcherPriority.ContextIdle, null);
                        }
                        else
                        {
                            // nothing was returned by the request; rather than spam the server just wait 5 seconds
                            Thread.Sleep(5000);
                        }

                        cleanupCounter++;

                        // now clean up the list
                        if (cleanupCounter > 5)
                        {
                            List<long> AllianceIDs = new List<long>();

                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
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
                                        KillStream.RemoveAt(i);
                                    }
                                }
                                if (AllianceIDs.Count > 0)
                                {
                                    EveManager.Instance.ResolveAllianceIDs(AllianceIDs);
                                }
                            }), DispatcherPriority.ContextIdle, null);

                            cleanupCounter = 0;
                        }
                    }
                    catch
                    {
                        // wait 10 seconds
                        for (int i = 0; i < 10; i++)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }

                // wait for the next request
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// A simple class with the Kill Highlights
        /// </summary>
        public class ZKBDataSimple : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            /// Gets or sets the ZKillboard Kill ID
            /// </summary>
            public long KillID { get; set; }

            /// <summary>
            /// Gets or sets the character ID of the victim
            /// </summary>
            public long VictimCharacterID { get; set; }

            /// <summary>
            /// Gets or sets the Victim's corp ID
            /// </summary>
            public long VictimCorpID { get; set; }

            /// <summary>
            /// Gets or sets the Victims Alliance ID
            /// </summary>
            public long VictimAllianceID { get; set; }

            private string m_victimAllianceName;

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
            /// Gets or sets the System ID the kill was in
            /// </summary>
            public string SystemName { get; set; }

            /// <summary>
            /// Gets or sets the Ship Lost in this kill
            /// </summary>
            public string ShipType { get; set; }

            /// <summary>
            /// Gets or sets the time of the kill
            /// </summary>
            public DateTimeOffset KillTime { get; set; }

            protected void OnPropertyChanged(string name)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }

            public override string ToString()
            {
                string allianceTicker = EVEData.EveManager.Instance.GetAllianceTicker(VictimAllianceID);
                if (allianceTicker == string.Empty)
                {
                    allianceTicker = VictimAllianceID.ToString();
                }

                return string.Format("System: {0}, Alliance: {1}, Ship {2}", SystemName, allianceTicker, ShipType);
            }
        }
    }
}