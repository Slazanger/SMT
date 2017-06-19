using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMT.EVEData
{
    public class ZKillRedisQ
    {
        public class ZKBData
        {
            public string KillID;
            public string SystemID;
            public string KillTime;

        }



        public void Initialise()
        {
        }
        

        public void UpdateThreadFunc()
        {

            bool running = true;

            string RedistURL = @"https://redisq.zkillboard.com/listen.php";


            while(running)
            {

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(RedistURL);
                request.Method = WebRequestMethods.Http.Get;
                request.Timeout = 20000;
                request.Proxy = null;

                WebResponse response = request.GetResponse();

                Stream responseStream = response.GetResponseStream();
                using (StreamReader sr = new StreamReader(responseStream))
                {
                    //Need to return this response 
                    string strContent = sr.ReadToEnd();

                    JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));

                    // JSON feed is now in the format : [{ "system_id": 30035042, "ship_kills": 103, "npc_kills": 103, "pod_kills": 0},
                    while (jsr.Read())
                    {
                        if (jsr.TokenType == JsonToken.StartObject)
                        {
                            JObject obj = JObject.Load(jsr);
                            foreach (JToken killToken in obj["package"].Children() )
                            {
                                string tokenstr = killToken.ToString();
                            }

                            // first node should be a package node :

                        }
                    }
                }


                // wait 10 seconds for the next request
                Thread.Sleep(10000);

            }




        }
    }
}
