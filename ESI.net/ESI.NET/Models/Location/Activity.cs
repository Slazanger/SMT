using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Location
{
    public class Activity
    {
        [JsonProperty("online")]
        public bool Online { get; set; }

        [JsonProperty("last_login")]
        public DateTime LastLogin { get; set; }

        [JsonProperty("last_logout")]
        public DateTime LastLogout { get; set; }

        [JsonProperty("logins")]
        public int Logins { get; set; }
    }
}
