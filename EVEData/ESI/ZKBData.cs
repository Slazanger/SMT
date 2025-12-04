// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using ZKBData;
//
//    var zkbData = ZkbData.FromJson(jsonString);

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ZKBData
{
    public static class Serialize
    {
        public static string ToJson(this ZkbData self) => JsonConvert.SerializeObject(self, ZKBData.Converter.Settings);
    }

    public partial class Package
    {
        [JsonProperty("killID")]
        public long KillId { get; set; }

        [JsonProperty("zkb")]
        public Zkb Zkb { get; set; }
    }


    public partial class Zkb
    {
        [JsonProperty("awox")]
        public bool Awox { get; set; }

        [JsonProperty("fittedValue")]
        public double FittedValue { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("locationID")]
        public long LocationId { get; set; }

        [JsonProperty("npc")]
        public bool Npc { get; set; }

        [JsonProperty("points")]
        public long Points { get; set; }

        [JsonProperty("solo")]
        public bool Solo { get; set; }

        [JsonProperty("totalValue")]
        public double TotalValue { get; set; }
    }

    public partial class ZkbData
    {
        [JsonProperty("package")]
        public Package Package { get; set; }
    }

    public partial class ZkbData
    {
        public static ZkbData FromJson(string json) => JsonConvert.DeserializeObject<ZkbData>(json, ZKBData.Converter.Settings);
    }

    internal class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}