// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using IncursionData;
//
//    var structureSearch = StructureSearch.FromJson(jsonString);

namespace StructureSearches
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Globalization;

    public static class Serialize
    {
        public static string ToJson(this StructureSearch self) => JsonConvert.SerializeObject(self, StructureSearches.Converter.Settings);
    }

    public partial class StructureSearch
    {
        [JsonProperty("structure")]
        public long[] Structure { get; set; }
    }

    public partial class StructureSearch
    {
        public static StructureSearch FromJson(string json) => JsonConvert.DeserializeObject<StructureSearch>(json, StructureSearches.Converter.Settings);
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