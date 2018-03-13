using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace SMT.EVEData
{
    public class ZKillRedisQ
    {
        public class ZKBDataSimple
        {
            public string KillID { get; set; }
            public string VictimCharacterID { get; set; }
            public string VictimCorpID { get; set; }
            public string VictimAllianceID { get; set; }
            public string SystemID { get; set; }
            public DateTimeOffset KillTime { get; set; }

            public override string ToString()
            {
                return "KillID:" + KillID + " SystemID:" + SystemID + " Victim:" + VictimCharacterID ;
            }
        }

        public ObservableCollection<ZKBDataSimple> KillStream { get; set;}

        public void Initialise()
        {
            KillStream = new ObservableCollection<ZKBDataSimple>();

            new Thread(() =>
            {
                UpdateThreadFunc();
            }).Start();
    }

        public void UpdateThreadFunc()
        {
            bool running = true;

            string redistURL = @"https://redisq.zkillboard.com/listen.php";

            int cleanupCounter = 0;

            while (running)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(redistURL);
                request.Method = WebRequestMethods.Http.Get;
                request.Timeout = 20000;
                request.Proxy = null;

                WebResponse response = request.GetResponse();

                Stream responseStream = response.GetResponseStream();
                using (StreamReader sr = new StreamReader(responseStream))
                {
                    // Need to return this response
                    string strContent = sr.ReadToEnd();

//                    JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));

                    try
                    {
                        ZKBData.ZkbData z = ZKBData.ZkbData.FromJson(strContent);
                        if(z.Package != null)
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
                                KillStream.Add(zs);
                            }), DispatcherPriority.ApplicationIdle);
                        }
                        else
                        {
                            Thread.Sleep(5000);
                        }

                        cleanupCounter++;

                        // now clean up the list

                        if(cleanupCounter > 100)
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

                            }), DispatcherPriority.ApplicationIdle);

                            cleanupCounter = 0;
                        }


                    }
                    catch
                    {
                    }


                }

                // wait 1 seconds for the next request
                Thread.Sleep(100);
            }
        }
    }
}