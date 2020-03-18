// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using EveTrace;
//
//    var eveTraceFleetInfo = EveTraceFleetInfo.FromJson(jsonString);

namespace EveTrace
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;
    using System.Globalization;

    public static class Serialize
    {
        public static string ToJson(this EveTraceFleetInfo self) => JsonConvert.SerializeObject(self, EveTrace.Converter.Settings);
    }

    public partial class EveTraceFleetInfo
    {
        [JsonProperty("fleetInstances")]
        public FleetInstance[] FleetInstances { get; set; }
    }

    public partial class EveTraceFleetInfo
    {
        public static EveTraceFleetInfo FromJson(string json) => JsonConvert.DeserializeObject<EveTraceFleetInfo>(json, EveTrace.Converter.Settings);
    }

    public partial class FleetInstance
    {
        [JsonProperty("firstSeen")]
        public DateTimeOffset FirstSeen { get; set; }

        [JsonProperty("fleetParticipants")]
        public FleetParticipant[] FleetParticipants { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("killCount")]
        public long KillCount { get; set; }

        [JsonProperty("lastSeen")]
        public DateTimeOffset LastSeen { get; set; }

        [JsonProperty("pilotCount")]
        public long PilotCount { get; set; }

        [JsonProperty("previousSystems")]
        public PreviousSystem[] PreviousSystems { get; set; }

        [JsonProperty("system")]
        public long System { get; set; }
    }

    public partial class FleetParticipant
    {
        [JsonProperty("corporationId")]
        public long CorporationId { get; set; }

        [JsonProperty("currentlyDead")]
        public bool CurrentlyDead { get; set; }

        [JsonProperty("killCount")]
        public long KillCount { get; set; }

        [JsonProperty("pilotID")]
        public long PilotId { get; set; }

        [JsonProperty("shipType")]
        public long ShipType { get; set; }
    }

    public partial class PreviousSystem
    {
        [JsonProperty("item1")]
        public DateTimeOffset Item1 { get; set; }

        [JsonProperty("item2")]
        public long Item2 { get; set; }
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