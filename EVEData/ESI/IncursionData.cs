// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using IncursionData;
//
//    var incursionInfo = IncursionInfo.FromJson(jsonString);

namespace IncursionData
{
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public static class Serialize
    {
        public static string ToJson(this IncursionInfo[] self) => JsonConvert.SerializeObject(self, IncursionData.Converter.Settings);
    }

    public partial class IncursionInfo
    {
        [JsonProperty("constellation_id")]
        public long ConstellationId { get; set; }

        [JsonProperty("faction_id")]
        public long FactionId { get; set; }

        [JsonProperty("has_boss")]
        public bool HasBoss { get; set; }

        [JsonProperty("infested_solar_systems")]
        public long[] InfestedSolarSystems { get; set; }

        [JsonProperty("influence")]
        public double Influence { get; set; }

        [JsonProperty("staging_solar_system_id")]
        public long StagingSolarSystemId { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public partial class IncursionInfo
    {
        public static IncursionInfo[] FromJson(string json) => JsonConvert.DeserializeObject<IncursionInfo[]>(json, IncursionData.Converter.Settings);
    }

    internal static class Converter
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