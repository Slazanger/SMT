using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;

namespace SMT.EVEData
{
    public class Character
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string LocalChatFile { get; set; }

        public string Location { get; set; }

        public bool ESILinked { get; set; }

        public string ESIAuthCode { get; set; }

        [XmlIgnoreAttribute]
        public string  ESIAccessToken { get; set; }

        public DateTime ESIAccessTokenExpiry { get; set; }

        public string ESIRefreshToken { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public Character()
        {
        }

        public void Update()
        {
            TimeSpan ts = ESIAccessTokenExpiry - DateTime.Now;
            if(ts.Minutes < 0)
            {
                RefreshAccessToken();
            }


            UpdatePositionFromESI();


        }

        public bool RefreshAccessToken()
        {
            if(ESIRefreshToken == "" || ESIRefreshToken == null)
            {
                return false;
            }

            string url = @"https://login.eveonline.com/oauth/token";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Post;
            request.Timeout = 20000;
            request.Proxy = null;

            
            string authHeader = EveManager.CLIENT_ID + ":" + EveManager.SECRET_KEY;
            string authHeader_64 = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(authHeader));

            request.Headers[HttpRequestHeader.Authorization] = authHeader_64;

            var httpData = HttpUtility.ParseQueryString(string.Empty); ;
            httpData["grant_type"] = "refresh_token";
            httpData["refresh_token"] = ESIRefreshToken;

            string httpDataStr = httpData.ToString();
            byte[] data = UTF8Encoding.UTF8.GetBytes(httpDataStr);
            request.ContentLength = data.Length;
            request.ContentType = "application/x-www-form-urlencoded";

            var stream = request.GetRequestStream();
            stream.Write(data, 0, data.Length);


            WebResponse refreshResult =  request.GetResponse();


            Stream responseStream = refreshResult.GetResponseStream();
            using (StreamReader sr = new StreamReader(responseStream))
            {
                //Need to return this response 
                string strContent = sr.ReadToEnd();

                JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));
                while (jsr.Read())
                {
                    if (jsr.TokenType == JsonToken.StartObject)
                    {

                        JObject obj = JObject.Load(jsr);
                        string AccessToken = obj["access_token"].ToString();
                        string TokenType = obj["token_type"].ToString();
                        string ExpiresIn = obj["expires_in"].ToString();
                        string RefreshToken = obj["refresh_token"].ToString();
                        double ExpiryMinutes = double.Parse(ExpiresIn);
                        ExpiryMinutes -= 5.0; // chop down 5 minutes to give us a buffer
                        ESIAccessToken = AccessToken;
                        ESIAccessTokenExpiry = DateTime.Now.AddSeconds(ExpiryMinutes);
                    }
                }
            }


            return true;
        }


        void UpdatePositionFromESI()
        {
            UriBuilder urlBuilder = new UriBuilder(@"https://esi.tech.ccp.is/latest/characters/" + ID + "/location");

            var esiQuery = HttpUtility.ParseQueryString(urlBuilder.Query);
            esiQuery["character_id"] = ID;
            esiQuery["datasource"] = "tranquility";
            esiQuery["token"] = ESIAccessToken;

            urlBuilder.Query = esiQuery.ToString();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlBuilder.ToString());
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;


            try
            {
                HttpWebResponse esiResult = (HttpWebResponse)request.GetResponse();


                if (esiResult.StatusCode != HttpStatusCode.OK)
                {
                    return;
                }

                Stream responseStream = esiResult.GetResponseStream();
                using (StreamReader sr = new StreamReader(responseStream))
                {
                    //Need to return this response 
                    string strContent = sr.ReadToEnd();

                    JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));
                    while (jsr.Read())
                    {
                        if (jsr.TokenType == JsonToken.StartObject)
                        {

                            JObject obj = JObject.Load(jsr);
                            string SysID = obj["solar_system_id"].ToString();

                            Location = EveManager.GetInstance().SystemIDToName[SysID];
                        }
                    }
                }
            }
            catch { }

        }




        public Character(string name, string lcf, string location )
        {
            Name = name;
            LocalChatFile = lcf;
            Location = location;

            ESILinked = false;
            ESIAuthCode = "";
            ESIAccessToken = "";
            ESIRefreshToken = "";
        }

    }
}
