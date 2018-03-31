//-----------------------------------------------------------------------
// EVE AnomManager
//-----------------------------------------------------------------------

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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace SMT.EVEData
{
    /// <summary>
    /// EVE Character
    /// </summary>
    public class Character
    {
        /// <summary>
        /// The name of the system this character is currently in 
        /// </summary>
        private string location;

        /// <summary>
        /// Does the route need updating
        /// </summary>
        private bool routeNeedsUpdate = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Character" /> class
        /// </summary>
        public Character()
        {
            Standings = new Dictionary<string, float>();
            CorporationID = string.Empty;
            AllianceID = string.Empty;
            FleetInfo = new Fleet();
            Waypoints = new ObservableCollection<string>();
            ActiveRoute = new ObservableCollection<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Character" /> class.
        /// </summary>
        /// <param name="name">Name of Character</param>
        /// <param name="lcf">Local Chat File Location</param>
        /// <param name="location">Current Location of Character</param>
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

        /// <summary>
        /// Gets or sets the Name of the Character
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the ID used by ESI for this character
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the ID of the Corp this character is in
        /// </summary>
        public string CorporationID { get; set; }

        /// <summary>
        /// Gets or sets the ID of the Alliance this character is in
        /// </summary>
        public string AllianceID { get; set; }

        /// <summary>
        /// Gets or sets the The location of the local file bound to this session's "Local" chat channel
        /// </summary>
        public string LocalChatFile { get; set; }

        /// <summary>
        /// Gets or sets the location of the character
        /// </summary>
        public string Location
        {
            get
            {
                return location;
            }

            set
            {
                if (location == value)
                {
                    return;
                }

                location = value;
                routeNeedsUpdate = true;
            }
        }

        /// <summary>
        /// Gets or sets if this character is linked with ESI
        /// </summary>
        public bool ESILinked { get; set; }

        /// <summary>
        /// Gets or sets the ESI auth code
        /// </summary>
        public string ESIAuthCode { get; set; }

        /// <summary>
        /// Gets or sets the ESI access token
        /// </summary>
        [XmlIgnoreAttribute]
        public string ESIAccessToken { get; set; }

        /// <summary>
        /// Gets or sets the ESI access token expiry time
        /// </summary>
        public DateTime ESIAccessTokenExpiry { get; set; }

        /// <summary>
        /// Gets or sets the ESI refresh Token
        /// </summary>
        public string ESIRefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the character standings dictionary
        /// </summary>
        [XmlIgnoreAttribute]
        public Dictionary<string, float> Standings { get; set; }

        /// <summary>
        /// Gets or sets the current fleet info for this character
        /// </summary>
        [XmlIgnoreAttribute]
        public Fleet FleetInfo { get; set; }

        /// <summary>
        /// Gets or sets the current list of Waypoints
        /// </summary>
        [XmlIgnoreAttribute]
        public ObservableCollection<string> Waypoints { get; set; }

        /// <summary>
        /// Gets or sets the current active route
        /// </summary>
        [XmlIgnoreAttribute]
        public ObservableCollection<string> ActiveRoute { get; set; }

        /// <summary>
        /// To String
        /// </summary>
        public override string ToString()
        {
            string toStr = Name;
            if (ESILinked)
            {
                toStr += " (ESI)";
            }

            return toStr;
        }

        /// <summary>
        /// Update the Character info
        /// </summary>
        public void Update()
        {
            TimeSpan ts = ESIAccessTokenExpiry - DateTime.Now;
            if (ts.Minutes < 0)
            {
                RefreshAccessToken();
                UpdateInfoFromESI();
            }

            UpdatePositionFromESI();

            //// TODO : 
            ////UpdateFleetIDFromESI();
            ////UpdateFleetInfoFromESI();
            ////UpdateFleetMembersFromESI();

            if (routeNeedsUpdate)
            {
                routeNeedsUpdate = false;
                UpdateActiveRoute();
            }
        }

        /// <summary>
        /// Add Destination to the route
        /// </summary>
        /// <param name="systemID">System to set destination to</param>
        /// <param name="clear">Clear all waypoints before setting?</param>
        public void AddDestination(string systemID, bool clear)
        {
            if (clear)
            {
                Waypoints.Clear();
                ActiveRoute.Clear();
            }

            Waypoints.Add(EveManager.Instance.SystemIDToName[systemID]);

            routeNeedsUpdate = true;

            string url = @"https://esi.tech.ccp.is/v2/ui/autopilot/waypoint/?";

            var httpData = HttpUtility.ParseQueryString(string.Empty);

            httpData["add_to_beginning"] = "false";
            httpData["clear_other_waypoints"] = clear ? "true" : "false";
            httpData["datasource"] = "tranquility";
            httpData["destination_id"] = systemID;
            string httpDataStr = httpData.ToString();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + httpDataStr);
            request.Method = WebRequestMethods.Http.Post;
            request.Timeout = 20000;
            request.Proxy = null;
            request.ContentType = "application/json";
            request.ContentLength = 0;

            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + ESIAccessToken;

            request.BeginGetResponse(new AsyncCallback(AddDestinationCallback), request);
        }

        /// <summary>
        /// Update the active route for the character
        /// </summary>
        private void UpdateActiveRoute()
        {
            if (Waypoints.Count == 0)
            {
                return;
            }

            string start = string.Empty;
            string end = Location;

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                if (Location == Waypoints[0])
                {
                    Waypoints.RemoveAt(0);
                }

                ActiveRoute.Clear();
            }), DispatcherPriority.ApplicationIdle);

            // loop through all the waypoints and query ESI for the route
            for (int i = 0; i < Waypoints.Count; i++)
            {
                start = end;
                end = Waypoints[i];

                EVEData.System startSys = EveManager.Instance.GetEveSystem(start);
                EVEData.System endSys = EveManager.Instance.GetEveSystem(end);

                UriBuilder urlBuilder = new UriBuilder(@"https://esi.tech.ccp.is/v1/route/" + startSys.ID + "/" + endSys.ID + "/");

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
                                string[] systems = obj.ToObject<string[]>();

                                for (int j = 1; j < systems.Length; j++)
                                {
                                    string sysName = EveManager.Instance.SystemIDToName[systems[j]];

                                    Application.Current.Dispatcher.Invoke((Action)(() =>
                                    {
                                        ActiveRoute.Add(sysName);
                                    }), DispatcherPriority.ApplicationIdle);
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Refresh the ESI access token
        /// </summary>
        private bool RefreshAccessToken()
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

            string authHeader = EveAppConfig.ClientID + ":" + EveAppConfig.SecretKey;
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

        /// <summary>
        /// Update the characters position from ESI (will override the position read from any log files
        /// </summary>
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

                            Location = EveManager.Instance.SystemIDToName[sysID];
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Update the characters Fleet ID (if Any)
        /// </summary>
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
                                FleetInfo.FleetID = Fleet.NoFleet;
                            }
                            else
                            {
                                FleetInfo.FleetID = fleetID;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Update the current Fleet information
        /// </summary>
        private void UpdateFleetInfoFromESI()
        {
            if (FleetInfo.FleetID == Fleet.NoFleet)
            {
                return;
            }

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
                            string motd = obj["motd"].ToString();

                            FleetInfo.FleetMOTD = motd;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Update the fleet members from ESI
        /// </summary>
        private void UpdateFleetMembersFromESI()
        {
            if (FleetInfo.FleetID == Fleet.NoFleet)
            {
                return;
            }

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
                            string shipType = obj["ship_type_id"].ToString();
                            string locationID = obj["solar_system_id"].ToString();
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Update the character info from the ESI data if linked
        /// </summary>
        private void UpdateInfoFromESI()
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
                        catch (Exception)
                        {
                        }
                    }
                    while (page < maxPageCount);
                }
            }
            catch (Exception)
            {
            }

            EveManager.Instance.ResolveAllianceIDs(Standings.Keys.ToList());
        }

        /// <summary>
        /// Add Destination async callback
        /// </summary>
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
    }
}