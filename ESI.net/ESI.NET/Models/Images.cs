using Newtonsoft.Json;
    
namespace ESI.NET.Models
{
    public class Images
    {
        [JsonProperty("px512x512")]
        public string x512 { get; set; }

        [JsonProperty("px256x256")]
        public string x256 { get; set; }

        [JsonProperty("px128x128")]
        public string x128 { get; set; }

        [JsonProperty("px64x64")]
        public string x64 { get; set; }
    }
}
