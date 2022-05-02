// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using IncursionData;
//
//    var structureIdData = StructureIdData.FromJson(jsonString);

namespace StructureIDs
{
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public static class Serialize
    {
        public static string ToJson(this StructureIdData self) => JsonConvert.SerializeObject(self, StructureIDs.Converter.Settings);
    }

    public partial class Position
    {
        [JsonProperty("x")]
        public long X { get; set; }

        [JsonProperty("y")]
        public long Y { get; set; }

        [JsonProperty("z")]
        public long Z { get; set; }
    }

    public partial class StructureIdData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("position")]
        public Position Position { get; set; }

        [JsonProperty("solar_system_id")]
        public long SolarSystemId { get; set; }

        [JsonProperty("type_id")]
        public long TypeId { get; set; }
    }

    public partial class StructureIdData
    {
        public static StructureIdData FromJson(string json) => JsonConvert.DeserializeObject<StructureIdData>(json, StructureIDs.Converter.Settings);
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