//-----------------------------------------------------------------------
// ZKillboard ReDisQ feed
//-----------------------------------------------------------------------
using System;
using System.Collections.ObjectModel;
using System.IO;
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
            updateThread.Join();
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
                if(PauseUpdate)
                {
                    if(KillStream.Count > 0)
                    {
                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            KillStream.Clear();
                        }), DispatcherPriority.Normal);

                    }

                    Thread.Sleep(5000);
                    continue;
                }

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(redistURL);
                request.Method = WebRequestMethods.Http.Get;
                request.Timeout = 10000;
                request.Proxy = null;

                HttpWebResponse response;

                try
                {
                    response = request.GetResponse() as HttpWebResponse;
                }
                catch
                {
                    Thread.Sleep(1000);
                    continue;
                }

                Stream responseStream = response.GetResponseStream();

                using (StreamReader sr = new StreamReader(responseStream))
                {
                    // Need to return this response
                    string strContent = sr.ReadToEnd();

                    try
                    {
                        ZKBData.ZkbData z = ZKBData.ZkbData.FromJson(strContent);
                        if (z.Package != null)
                        {
                            ZKBDataSimple zs = new ZKBDataSimple();
                            zs.KillID = z.Package.KillId.ToString();
                            zs.VictimAllianceID = z.Package.Killmail.Victim.AllianceId.ToString();
                            zs.VictimCharacterID = z.Package.Killmail.Victim.CharacterId.ToString();
                            zs.VictimCorpID = z.Package.Killmail.Victim.CharacterId.ToString();
                            zs.SystemID = z.Package.Killmail.SolarSystemId.ToString();
                            zs.KillTime = z.Package.Killmail.KillmailTime;

                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                KillStream.Insert(0,zs);
                            }), DispatcherPriority.Normal);
                        }
                        else
                        {
                            // nothing was returned by the request; rather than spam the server just wait 5 seconds
                            Thread.Sleep(5000);
                        }

                        cleanupCounter++;

                        // now clean up the list
                        if (cleanupCounter > 10)
                        {
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                for (int i = KillStream.Count - 1; i >= 0; i--)
                                {
                                    if (KillStream[i].KillTime + TimeSpan.FromMinutes(30) < DateTimeOffset.Now)
                                    {
                                        KillStream.RemoveAt(i);
                                    }
                                }
                            }), DispatcherPriority.Normal);

                            cleanupCounter = 0;
                        }
                    }
                    catch
                    {
                    }
                }

                // wait for the next request
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// A simple class with the Kill Highlights
        /// </summary>
        public class ZKBDataSimple
        {
            /// <summary>
            /// Gets or sets the ZKillboard Kill ID
            /// </summary>
            public string KillID { get; set; }

            /// <summary>
            /// Gets or sets the character ID of the victim
            /// </summary>
            public string VictimCharacterID { get; set; }

            /// <summary>
            /// Gets or sets the Victim's corp ID
            /// </summary>
            public string VictimCorpID { get; set; }

            /// <summary>
            /// Gets or sets the Victims Alliance ID
            /// </summary>
            public string VictimAllianceID { get; set; }

            /// <summary>
            /// Gets or sets the System ID the kill was in
            /// </summary>
            public string SystemID { get; set; }

            /// <summary>
            /// Gets or sets the time of the kill
            /// </summary>
            public DateTimeOffset KillTime { get; set; }

            public override string ToString()
            {
                string systemName = EVEData.EveManager.Instance.GetSystemNameFromSystemID(SystemID);
                if(systemName == string.Empty)
                {
                    systemName = SystemID;
                }
                return "KillID:" + KillID + " System:" + systemName + " Victim:" + VictimCharacterID;
            }
        }
    }
}