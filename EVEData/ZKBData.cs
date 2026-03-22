// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using ZKBData;
//
//    var r2z2Data = R2Z2Data.FromJson(jsonString);

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EVEData;

public partial class SequenceData
{
    [JsonProperty("sequence")] public long Sequence { get; set; }

    public static SequenceData FromJson(string json)
    {
        return JsonConvert.DeserializeObject<SequenceData>(json, Converter.Settings);
    }
}

public partial class R2Z2Data
{
    [JsonProperty("killmail_id")] public long KillmailId { get; set; }

    [JsonProperty("hash")] public string Hash { get; set; }

    [JsonProperty("esi")] public EsiData Esi { get; set; }

    [JsonProperty("zkb")] public Zkb Zkb { get; set; }

    [JsonProperty("uploaded_at")] public long UploadedAt { get; set; }

    [JsonProperty("sequence_id")] public long SequenceId { get; set; }

    public static R2Z2Data FromJson(string json)
    {
        return JsonConvert.DeserializeObject<R2Z2Data>(json, Converter.Settings);
    }
}

public partial class EsiData
{
    [JsonProperty("killmail_id")] public long KillmailId { get; set; }

    [JsonProperty("killmail_time")] public DateTimeOffset KillmailTime { get; set; }

    [JsonProperty("solar_system_id")] public long SolarSystemId { get; set; }

    [JsonProperty("victim")] public Victim Victim { get; set; }
}

public partial class Victim
{
    [JsonProperty("alliance_id", NullValueHandling = NullValueHandling.Ignore)]
    public int AllianceId { get; set; }

    [JsonProperty("character_id", NullValueHandling = NullValueHandling.Ignore)]
    public int CharacterId { get; set; }

    [JsonProperty("corporation_id", NullValueHandling = NullValueHandling.Ignore)]
    public int CorporationId { get; set; }

    [JsonProperty("damage_taken")] public long DamageTaken { get; set; }

    [JsonProperty("ship_type_id")] public int ShipTypeId { get; set; }
}

public partial class Zkb
{
    [JsonProperty("locationID")] public long LocationId { get; set; }

    [JsonProperty("hash")] public string Hash { get; set; }

    [JsonProperty("fittedValue")] public double FittedValue { get; set; }

    [JsonProperty("droppedValue")] public double DroppedValue { get; set; }

    [JsonProperty("destroyedValue")] public double DestroyedValue { get; set; }

    [JsonProperty("totalValue")] public double TotalValue { get; set; }

    [JsonProperty("points")] public long Points { get; set; }

    [JsonProperty("npc")] public bool Npc { get; set; }

    [JsonProperty("solo")] public bool Solo { get; set; }

    [JsonProperty("awox")] public bool Awox { get; set; }
}

internal class Converter
{
    public static readonly JsonSerializerSettings Settings = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters =
        {
            new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
        }
    };
}