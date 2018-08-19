// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using SovStructures;
//
//    var sovStructures = SovStructure.FromJson(jsonString);

namespace SovStructures
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class SovStructure
    {
        [JsonProperty("alliance_id")]
        public long AllianceId { get; set; }

        [JsonProperty("solar_system_id")]
        public long SolarSystemId { get; set; }

        [JsonProperty("structure_id")]
        public long StructureId { get; set; }

        [JsonProperty("structure_type_id")]
        public long StructureTypeId { get; set; }

        [JsonProperty("vulnerability_occupancy_level", NullValueHandling = NullValueHandling.Ignore)]
        public double? VulnerabilityOccupancyLevel { get; set; }

        [JsonProperty("vulnerable_end_time", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? VulnerableEndTime { get; set; }

        [JsonProperty("vulnerable_start_time", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? VulnerableStartTime { get; set; }
    }

    public partial class SovStructure
    {
        public static SovStructure[] FromJson(string json) => JsonConvert.DeserializeObject<SovStructure[]>(json, StructureHunter.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this SovStructure[] self) => JsonConvert.SerializeObject(self, StructureHunter.Converter.Settings);
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