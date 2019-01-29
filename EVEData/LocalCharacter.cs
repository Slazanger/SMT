using ESI.NET.Enumerations;
using ESI.NET.Models.SSO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Serialization;


namespace SMT.EVEData
{

    //jumpclones


    public class LocalCharacter : Character
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

        public string Region { get; set; }
        

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

        [XmlIgnoreAttribute]
        public ESI.NET.Models.SSO.AuthorizedCharacterData ESIAuthData { get; set; }

        /// <summary>
        /// Gets or sets the character standings dictionary
        /// </summary>
        [XmlIgnoreAttribute]
        public Dictionary<long, float> Standings { get; set; }

        [XmlIgnoreAttribute]
        public Dictionary<long, long> LabelMap { get; set; }

        [XmlIgnoreAttribute]
        public Dictionary<long, string> LabelNames { get; set; }


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
        /// Gets or sets the character structure dictionary
        /// </summary>
        [XmlIgnoreAttribute]
        public Dictionary<string, List<StructureIDs.StructureIdData>> DockableStructures { get; set; }


    /// <summary>
    /// Initializes a new instance of the <see cref="Character" /> class
    /// </summary>
    public LocalCharacter()
        {
            Standings = new Dictionary<long, float>();

            LabelMap = new Dictionary<long, long>();
            LabelNames = new Dictionary<long, string>();

            CorporationID = 0;
            AllianceID = 0;
            FleetInfo = new Fleet();
            Waypoints = new ObservableCollection<string>();
            ActiveRoute = new ObservableCollection<string>();
            DockableStructures = new Dictionary<string, List<StructureIDs.StructureIdData>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Character" /> class.
        /// </summary>
        /// <param name="name">Name of Character</param>
        /// <param name="lcf">Local Chat File Location</param>
        /// <param name="location">Current Location of Character</param>
        public LocalCharacter(string name, string lcf, string location)
        {
            Name = name;
            LocalChatFile = lcf;
            Location = location;

            ESILinked = false;
            ESIAuthCode = string.Empty;
            ESIAccessToken = string.Empty;
            ESIRefreshToken = string.Empty;

            Standings = new Dictionary<long, float>();

            LabelMap = new Dictionary<long, long>();
            LabelNames = new Dictionary<long, string>();

            FleetInfo = new Fleet();

            Waypoints = new ObservableCollection<string>();
            ActiveRoute = new ObservableCollection<string>();
        }


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
        public async void Update()
        {
            TimeSpan ts = ESIAccessTokenExpiry - DateTime.Now;
            if (ts.Minutes < 0)
            {
                await RefreshAccessToken();
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
        public async void AddDestination(long systemID, bool clear)
        {
            if (clear)
            {
                Waypoints.Clear();
                ActiveRoute.Clear();
            }

            Waypoints.Add(EveManager.Instance.SystemIDToName[systemID]);

            ESI.NET.EsiClient esiClient = EveManager.Instance.ESIClient;
            esiClient.SetCharacterData(ESIAuthData);

            ESI.NET.EsiResponse<string> esr = await esiClient.UserInterface.Waypoint(systemID, false, clear);
            if(EVEData.ESIHelpers.ValidateESICall<string>(esr))
            {
                routeNeedsUpdate = true;
            }
        }

        /// <summary>
        /// Update the active route for the character
        /// </summary>
        private async void UpdateActiveRoute()
        {

            if (Waypoints.Count == 0)
            {
                return;
            }


            ESI.NET.EsiClient esiClient = EveManager.Instance.ESIClient;
            esiClient.SetCharacterData(ESIAuthData);


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


                ESI.NET.EsiResponse<int[]> esr = await esiClient.Routes.Map((int)startSys.ID, (int)endSys.ID);

                if(EVEData.ESIHelpers.ValidateESICall<int[]>(esr))
                {
                    foreach(int j in esr.Data)
                    {
                        string sysName = EveManager.Instance.SystemIDToName[j];

                        Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            ActiveRoute.Add(sysName);
                        }), DispatcherPriority.ApplicationIdle);
                    }
                }

            }

        }

        /// <summary>
        /// Refresh the ESI access token
        /// </summary>
        private async Task RefreshAccessToken()
        {
            if(String.IsNullOrEmpty(ESIRefreshToken) || !ESILinked)
            {
                return;
            }

            SsoToken sst;
            AuthorizedCharacterData acd;
            sst = await EveManager.Instance.ESIClient.SSO.GetToken(GrantType.RefreshToken, ESIRefreshToken);
            if(sst.RefreshToken == null)
            {
                return;
            }

            acd = await EveManager.Instance.ESIClient.SSO.Verify(sst);
   
            if (String.IsNullOrEmpty(acd.Token))
            {
                return;
            }

            ESIAccessToken = acd.Token;
            ESIAccessTokenExpiry = acd.ExpiresOn;
            ESIRefreshToken = acd.RefreshToken;
            ESILinked = true;
            ESIAuthData = acd;

        }

        /// <summary>
        /// Update the characters position from ESI (will override the position read from any log files
        /// </summary>
        private async void UpdatePositionFromESI()
        {
            if(string.IsNullOrEmpty(ESIAccessToken) )
            {
                return;
            }

            ESI.NET.EsiClient esiClient = EveManager.Instance.ESIClient;
            esiClient.SetCharacterData(ESIAuthData);
            ESI.NET.EsiResponse<ESI.NET.Models.Location.Location> location = await esiClient.Location.Location();

            
            Location = EveManager.Instance.SystemIDToName[location.Data.SolarSystemId];
            System s = EVEData.EveManager.Instance.GetEveSystem(Location);
            if (s != null)
            {
                Region = s.Region;
            }
            else
            {
                Region = "";
            } 
        }

        /// <summary>
        /// Update the characters Fleet ID (if Any)
        /// </summary>
        private void UpdateFleetIDFromESI()
        {
            UriBuilder urlBuilder = new UriBuilder(@"https://esi.evetech.net/v1/characters/" + ID + "/fleet");

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

            UriBuilder urlBuilder = new UriBuilder(@"https://esi.evetech.net/v1/fleets/" + FleetInfo.FleetID + "/");

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

            UriBuilder urlBuilder = new UriBuilder(@"https://esi.evetech.net/v1/fleets/" + FleetInfo.FleetID + "/members/");

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
                if (CorporationID == 0)
                {
                    string url = @"https://esi.evetech.net/v4/characters/" + ID + "/?datasource=tranquility";

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
                                long corpID = long.Parse(obj["corporation_id"].ToString());
                                CorporationID = corpID;
                            }
                        }
                    }
                }

                if (AllianceID == 0 && CorporationID != 0)
                {
                    string url = @"https://esi.evetech.net/v4/corporations/" + CorporationID + "/?datasource=tranquility";

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
                                if(obj["alliance_id"] != null)
                                {
                                    AllianceID = long.Parse(obj["alliance_id"].ToString());
                                }
                            }
                        }
                    }
                }

                if(AllianceID != 0)
                {
                    int page = 0;
                    int maxPageCount = 1;
                    do
                    {
                        page++;

                        UriBuilder urlBuilder = new UriBuilder(@"https://esi.evetech.net/v1/alliances/" + AllianceID + "/contacts/");
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
                                        long contactID = long.Parse(obj["contact_id"].ToString());
                                        string contactType = obj["contact_type"].ToString();
                                        long LabelID = 0;

                                        if (obj["label_id"] != null)
                                        {
                                            LabelID = long.Parse(obj["label_id"].ToString());

                                            LabelMap[contactID] = LabelID;
                                        }

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

        public void UpdateStructureInfoForRegion(string Region)
        {
            if (!ESILinked)
                return;

            MapRegion mr = EveManager.Instance.GetRegion(Region);

            // somethings gone wrong
            if(mr == null)
            {
                return;
            }

            // iterate over each structure and search for structres containing the text for each system
            foreach(MapSystem ms in mr.MapSystems.Values.ToList())
            {
                // skip systems we've already checked
                if( DockableStructures.Keys.Contains(ms.Name))
                {
                    continue;
                }

                List<StructureIDs.StructureIdData> SystemStructureList = new List<StructureIDs.StructureIdData>();

                UriBuilder urlBuilder = new UriBuilder(@"https://esi.evetech.net/latest/characters/" + ID + "/search/");

                var esiQuery = HttpUtility.ParseQueryString(urlBuilder.Query);
                esiQuery["datasource"] = "tranquility";
                esiQuery["token"] = ESIAccessToken;
                esiQuery["categories"] = "structure";
                esiQuery["search"] = ms.Name;
                esiQuery["strict"] = "false";

                urlBuilder.Query = esiQuery.ToString();

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlBuilder.ToString());
                request.Method = WebRequestMethods.Http.Get;
                request.ContentType = "application/json";
                request.Timeout = 20000;
                request.Proxy = null;

                try
                {
                    HttpWebResponse esiResult = (HttpWebResponse)request.GetResponse();

                    if (esiResult.StatusCode != HttpStatusCode.OK)
                    {
                        continue;
                    }

                    Stream responseStream = esiResult.GetResponseStream();
                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        // Need to return this response
                        string strContent = sr.ReadToEnd();

                        StructureSearches.StructureSearch ss = StructureSearches.StructureSearch.FromJson(strContent);

                        if (ss == null || ss.Structure == null)
                            continue;


                        foreach (long l in ss.Structure)
                        {
                            // now search on each structure
                            UriBuilder urlStructureIDBuilder = new UriBuilder(@"https://esi.evetech.net/v1/universe/structures/" + l.ToString() + "/");

                            var esiStructureIDQuery = HttpUtility.ParseQueryString(urlStructureIDBuilder.Query);
                            esiStructureIDQuery["datasource"] = "tranquility";
                            esiStructureIDQuery["token"] = ESIAccessToken;

                            urlStructureIDBuilder.Query = esiStructureIDQuery.ToString();

                            HttpWebRequest sid_request = (HttpWebRequest)WebRequest.Create(urlStructureIDBuilder.ToString());
                            sid_request.Method = WebRequestMethods.Http.Get;
                            sid_request.ContentType = "application/json";
                            sid_request.Timeout = 20000;
                            sid_request.Proxy = null;

                            try
                            {
                                HttpWebResponse esi_sid_Result = (HttpWebResponse)sid_request.GetResponse();

                                Stream sid_responseStream = esi_sid_Result.GetResponseStream();
                                using (StreamReader sr2 = new StreamReader(sid_responseStream))
                                {

                                    // Need to return this response
                                    string strSIDContent = sr2.ReadToEnd();
                                    StructureIDs.StructureIdData sidd = StructureIDs.StructureIdData.FromJson(strSIDContent);
                                    if(sidd.Name != "")
                                    {
                                        SystemStructureList.Add(sidd);
                                    }

                                }
                            }
                            catch
                            {

                            }




                        }
                    }
                }
                catch (Exception)
                {
                }

                DockableStructures.Add(ms.Name, SystemStructureList);
                Thread.Sleep(10);
            }



        }
        

    }
}
