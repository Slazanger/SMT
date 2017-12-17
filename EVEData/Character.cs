using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Serialization;

namespace SMT.EVEData
{
    public class Character
    {
        public string Name { get; set; }

        public string ID { get; set; }

        public string CorporationID { get; set; }

        public string AllianceID { get; set; }

        public string LocalChatFile { get; set; }

        public string Location { get; set; }

        public bool ESILinked { get; set; }

        public string ESIAuthCode { get; set; }

        [XmlIgnoreAttribute]
        public string ESIAccessToken { get; set; }

        public DateTime ESIAccessTokenExpiry { get; set; }

        public string ESIRefreshToken { get; set; }

        public override string ToString()
        {
            string toStr = Name;
            if(ESILinked)
            {
                toStr += " (ESI)";
            }
            return toStr;
        }

        [XmlIgnoreAttribute]
        public Dictionary<string, float> Standings;



        public Character()
        {
            Standings = new Dictionary<string, float>();
            CorporationID = string.Empty;
            AllianceID = string.Empty;
        }

        public void Update()
        {
            TimeSpan ts = ESIAccessTokenExpiry - DateTime.Now;
            if (ts.Minutes < 0)
            {
                RefreshAccessToken();
                UpdateInfoFromESI();
            }

            UpdatePositionFromESI();
        }

        public bool RefreshAccessToken()
        {
            if (ESIRefreshToken == string.Empty || ESIRefreshToken == null)
            {
                return false;
            }

            string url = @"https://login.eveonline.com/oauth/token";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Post;
            request.Timeout = 20000;
            request.Proxy = null;

            string authHeader = EveAppConfig.CLIENT_ID + ":" + EveAppConfig.SECRET_KEY;
            string authHeader_64 = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(authHeader));

            request.Headers[HttpRequestHeader.Authorization] = authHeader_64;

            var httpData = HttpUtility.ParseQueryString(string.Empty);
            httpData["grant_type"] = "refresh_token";
            httpData["refresh_token"] = ESIRefreshToken;

            string httpDataStr = httpData.ToString();
            byte[] data = UTF8Encoding.UTF8.GetBytes(httpDataStr);
            request.ContentLength = data.Length;
            request.ContentType = "application/x-www-form-urlencoded";

            var stream = request.GetRequestStream();
            stream.Write(data, 0, data.Length);

            WebResponse refreshResult = request.GetResponse();

            Stream responseStream = refreshResult.GetResponseStream();
            using (StreamReader sr = new StreamReader(responseStream))
            {
                // Need to return this response
                string strContent = sr.ReadToEnd();

                JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));
                while (jsr.Read())
                {
                    if (jsr.TokenType == JsonToken.StartObject)
                    {
                        JObject obj = JObject.Load(jsr);
                        string accessToken = obj["access_token"].ToString();
                        string tokenType = obj["token_type"].ToString();
                        string expiresIn = obj["expires_in"].ToString();
                        string refreshToken = obj["refresh_token"].ToString();
                        double expiryMinutes = double.Parse(expiresIn);
                        expiryMinutes -= 5.0; // chop down 5 minutes to give us a buffer
                        ESIAccessToken = accessToken;
                        ESIAccessTokenExpiry = DateTime.Now.AddSeconds(expiryMinutes);
                    }
                }
            }

            return true;
        }

        private void UpdatePositionFromESI()
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
                    // Need to return this response
                    string strContent = sr.ReadToEnd();

                    JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));
                    while (jsr.Read())
                    {
                        if (jsr.TokenType == JsonToken.StartObject)
                        {
                            JObject obj = JObject.Load(jsr);
                            string sysID = obj["solar_system_id"].ToString();

                            Location = EveManager.GetInstance().SystemIDToName[sysID];
                        }
                    }
                }
            }
            catch { }
        }

        public void UpdateInfoFromESI()
        {
            if (string.IsNullOrEmpty(ID) || !ESILinked)
            {
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(CorporationID))
                {
                    string url = @"https://esi.tech.ccp.is/latest/characters/" + ID + "/?datasource=tranquility";

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = WebRequestMethods.Http.Get;
                    request.Timeout = 20000;
                    request.Proxy = null;

                    HttpWebResponse esiResult = (HttpWebResponse)request.GetResponse();

                    if (esiResult.StatusCode != HttpStatusCode.OK)
                    {
                        return;
                    }

                    Stream responseStream = esiResult.GetResponseStream();
                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        // Need to return this response
                        string strContent = sr.ReadToEnd();

                        JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));
                        while (jsr.Read())
                        {
                            if (jsr.TokenType == JsonToken.StartObject)
                            {
                                JObject obj = JObject.Load(jsr);
                                string corpID = obj["corporation_id"].ToString();
                                CorporationID = corpID;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(AllianceID) && !string.IsNullOrEmpty(CorporationID))
                {
                    string url = @"https://esi.tech.ccp.is/latest/corporations/" + CorporationID + "/?datasource=tranquility";


                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = WebRequestMethods.Http.Get;
                    request.Timeout = 20000;
                    request.Proxy = null;

                    HttpWebResponse esiResult = (HttpWebResponse)request.GetResponse();

                    if (esiResult.StatusCode != HttpStatusCode.OK)
                    {
                        return;
                    }

                    Stream responseStream = esiResult.GetResponseStream();
                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        // Need to return this response
                        string strContent = sr.ReadToEnd();

                        JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));
                        while (jsr.Read())
                        {
                            if (jsr.TokenType == JsonToken.StartObject)
                            {
                                JObject obj = JObject.Load(jsr);
                                string allianceID = obj["alliance_id"].ToString();
                                AllianceID = allianceID;
                            }
                        }
                    }
                }



                if (!string.IsNullOrEmpty(AllianceID))
                {
                    int page = 0;
                    int maxPageCount = 1;
                    do
                    {
                        page++;
                        //

                        //string url = @"https://esi.tech.ccp.is/latest/alliances/" + AllianceID + "/contacts/?datasource=tranquility&page=" + page;

                        UriBuilder urlBuilder = new UriBuilder(@"https://esi.tech.ccp.is/latest/alliances/" + AllianceID + "/contacts/");
                        var esiQuery = HttpUtility.ParseQueryString(urlBuilder.Query);
                        esiQuery["page"] = page.ToString();
                        esiQuery["datasource"] = "tranquility";
                        esiQuery["token"] = ESIAccessToken;


                        urlBuilder.Query = esiQuery.ToString();

                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlBuilder.ToString());
                        request.Method = WebRequestMethods.Http.Get;
                        request.ContentType = "application/json";
                        request.Timeout = 20000;
                        request.Proxy = null;

                        try
                        {
                            HttpWebResponse esiResult = (HttpWebResponse)request.GetResponse();
                            maxPageCount = int.Parse(esiResult.Headers["X-Pages"].ToString());


                            if (esiResult.StatusCode != HttpStatusCode.OK)
                            {
                                return;
                            }

                            Stream responseStream = esiResult.GetResponseStream();
                            using (StreamReader sr = new StreamReader(responseStream))
                            {
                                // Need to return this response
                                string strContent = sr.ReadToEnd();

                                JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));
                                while (jsr.Read())
                                {
                                    if (jsr.TokenType == JsonToken.StartObject)
                                    {
                                        JObject obj = JObject.Load(jsr);
                                        string contactID = obj["contact_id"].ToString();
                                        string contactType = obj["contact_type"].ToString();
                                        float standing = float.Parse(obj["standing"].ToString());

                                        Standings[contactID] = standing;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        
                        }



                    } while (page < maxPageCount);




                }

                if (!string.IsNullOrEmpty(AllianceID) && !string.IsNullOrEmpty(CorporationID))
                {

                }
            }
            catch (Exception ex)
            {

            }
        }

        public void AddDestination(string SystemID, bool Clear)
        {
            string url = @"https://esi.tech.ccp.is/latest/ui/autopilot/waypoint/?";

            var httpData = HttpUtility.ParseQueryString(string.Empty);

            httpData["add_to_beginning"] = "false";
            httpData["clear_other_waypoints"] = Clear ? "true" : "false";
            httpData["datasource"] = "tranquility";
            httpData["destination_id"] = SystemID;
            string httpDataStr = httpData.ToString();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + httpDataStr);
            request.Method = WebRequestMethods.Http.Post;
            request.Timeout = 20000;
            request.Proxy = null;
            request.ContentType = "application/json";
            request.ContentLength = 0;

            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + ESIAccessToken;

            //WebResponse setDestoResult = request.GetResponse();

            request.BeginGetResponse(new AsyncCallback(AddDestinationCallback), request);



        }
        private void AddDestinationCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult))
                {
                    Stream responseStream = response.GetResponseStream();
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        int test = 0;
                        test++;
                    }
                }
            }
            catch
            {
            }
        }


        public Character(string name, string lcf, string location)
        {
            Name = name;
            LocalChatFile = lcf;
            Location = location;

            ESILinked = false;
            ESIAuthCode = string.Empty;
            ESIAccessToken = string.Empty;
            ESIRefreshToken = string.Empty;

            Standings = new Dictionary<string, float>();
        }
    }
}