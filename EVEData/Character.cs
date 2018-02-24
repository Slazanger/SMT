using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Windows;
using System.Windows.Threading;
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

        public bool RouteUpdate { get; set; }

        private string m_Location;
        private bool m_RouteNeedsUpdate = false;
        public string Location
        {
            get
            {
                return m_Location;
            }
            set
            {
                if(m_Location == value)
                {
                    return;
                }
                
                m_Location = value;
                m_RouteNeedsUpdate = true;
            }
        }

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


        [XmlIgnoreAttribute]
        public Fleet FleetInfo;


        public Character()
        {
            Standings = new Dictionary<string, float>();
            CorporationID = string.Empty;
            AllianceID = string.Empty;
            FleetInfo = new Fleet();
            Waypoints = new ObservableCollection<string>();
            ActiveRoute = new ObservableCollection<string>();

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

            // TODO : 
            //UpdateFleetIDFromESI();
            //UpdateFleetInfoFromESI();
            //UpdateFleetMembersFromESI();

            if(m_RouteNeedsUpdate)
            {
                m_RouteNeedsUpdate = false;
                UpdateActiveRoute();
            }

        }

        [XmlIgnoreAttribute]
        public ObservableCollection<String> Waypoints { get; set; }

        [XmlIgnoreAttribute]
        public ObservableCollection<String> ActiveRoute { get; set; }


        void UpdateActiveRoute()
        {
            if (Waypoints.Count == 0)
            {
                return;
            }

            string Start = "";
            string End = Location;


            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                if (Location == Waypoints[0])
                {
                    Waypoints.RemoveAt(0);
                }


                ActiveRoute.Clear();
            }), DispatcherPriority.ApplicationIdle);



            for (int i = 0; i < Waypoints.Count; i++)
            {
                Start = End;
                End = Waypoints[i];

                EVEData.System StartSys = EveManager.GetInstance().GetEveSystem(Start);
                EVEData.System EndSys = EveManager.GetInstance().GetEveSystem(End);


                UriBuilder urlBuilder = new UriBuilder(@"https://esi.tech.ccp.is/v1/route/" + StartSys.ID + "/" + EndSys.ID + "/" );

                var esiQuery = HttpUtility.ParseQueryString(urlBuilder.Query);
                esiQuery["datasource"] = "tranquility";

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
                            if (jsr.TokenType == JsonToken.StartArray)
                            {
                                JArray obj = JArray.Load(jsr);
                                string[] Systems = obj.ToObject<string[]>();
                                
                                for(int j=1; j< Systems.Length; j++)
                                {
                                    string sysName = EveManager.GetInstance().SystemIDToName[Systems[j]];

                                    Application.Current.Dispatcher.Invoke((Action)(() =>
                                    {
                                        ActiveRoute.Add(sysName);
                                    }), DispatcherPriority.ApplicationIdle);

                                }

                            }
                        }
                    }
                }
                catch { }
            }


            RouteUpdate = true;
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
            UriBuilder urlBuilder = new UriBuilder(@"https://esi.tech.ccp.is/v1/characters/" + ID + "/location");

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

        private void UpdateFleetIDFromESI()
        {

            UriBuilder urlBuilder = new UriBuilder(@"https://esi.tech.ccp.is/v1/characters/" + ID + "/fleet");

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
                            string fleetID = obj["fleet_id"].ToString();

                            if (fleetID == "-1")
                            {
                                FleetInfo.FleetID = Fleet.NO_FLEET;
                            }
                            else
                            {
                                FleetInfo.FleetID = fleetID;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void UpdateFleetInfoFromESI()
        {
            if (FleetInfo.FleetID == Fleet.NO_FLEET)
                return;


            UriBuilder urlBuilder = new UriBuilder(@"https://esi.tech.ccp.is/v1/fleets/" + FleetInfo.FleetID + "/");

            var esiQuery = HttpUtility.ParseQueryString(urlBuilder.Query);

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
                            string MOTD = obj["motd"].ToString();

                            FleetInfo.FleetMOTD = MOTD;
                        }
                    }
                }
            }
            catch { }
        }

        private void UpdateFleetMembersFromESI()
        {
            if (FleetInfo.FleetID == Fleet.NO_FLEET)
                return;

            UriBuilder urlBuilder = new UriBuilder(@"https://esi.tech.ccp.is/v1/fleets/" + FleetInfo.FleetID + "/members/");

            var esiQuery = HttpUtility.ParseQueryString(urlBuilder.Query);

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
                            string charID = obj["character_id"].ToString();
                            string ShipType = obj["ship_type_id"].ToString();
                            string locationID = obj["solar_system_id"].ToString();
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
                    string url = @"https://esi.tech.ccp.is/v4/characters/" + ID + "/?datasource=tranquility";

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
                    string url = @"https://esi.tech.ccp.is/v4/corporations/" + CorporationID + "/?datasource=tranquility";


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

                        //string url = @"https://esi.tech.ccp.is/v1/alliances/" + AllianceID + "/contacts/?datasource=tranquility&page=" + page;

                        UriBuilder urlBuilder = new UriBuilder(@"https://esi.tech.ccp.is/v1/alliances/" + AllianceID + "/contacts/");
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
            if(Clear)
            {
                Waypoints.Clear();
                ActiveRoute.Clear();
            }

            Waypoints.Add(EveManager.GetInstance().SystemIDToName[SystemID]);

            m_RouteNeedsUpdate = true;


            string url = @"https://esi.tech.ccp.is/v2/ui/autopilot/waypoint/?";

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
            FleetInfo = new Fleet();

            Waypoints = new ObservableCollection<string>();
            ActiveRoute = new ObservableCollection<string>();

        }
    }
}