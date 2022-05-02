// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using ZKBData;
//
//    var zkbData = ZkbData.FromJson(jsonString);

namespace ZKBData
{
    using System.Collections.Generic;
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public static class Serialize
    {
        public static string ToJson(this ZkbData self) => JsonConvert.SerializeObject(self, ZKBData.Converter.Settings);
    }

    public partial class Attacker
    {
        [JsonProperty("character_id")]
        public long CharacterId { get; set; }

        [JsonProperty("corporation_id")]
        public long CorporationId { get; set; }

        [JsonProperty("damage_done")]
        public long DamageDone { get; set; }

        [JsonProperty("faction_id")]
        public long FactionId { get; set; }

        [JsonProperty("final_blow")]
        public bool FinalBlow { get; set; }

        [JsonProperty("security_status")]
        public long SecurityStatus { get; set; }

        [JsonProperty("ship_type_id")]
        public long ShipTypeId { get; set; }

        [JsonProperty("weapon_type_id")]
        public long WeaponTypeId { get; set; }
    }

    public partial class Item
    {
        [JsonProperty("flag")]
        public long Flag { get; set; }

        [JsonProperty("item_type_id")]
        public long ItemTypeId { get; set; }

        [JsonProperty("quantity_destroyed")]
        public long? QuantityDestroyed { get; set; }

        [JsonProperty("quantity_dropped")]
        public long? QuantityDropped { get; set; }

        [JsonProperty("singleton")]
        public long Singleton { get; set; }
    }

    public partial class Killmail
    {
        [JsonProperty("attackers")]
        public List<Attacker> Attackers { get; set; }

        [JsonProperty("killmail_id")]
        public long KillmailId { get; set; }

        [JsonProperty("killmail_time")]
        public System.DateTimeOffset KillmailTime { get; set; }

        [JsonProperty("solar_system_id")]
        public long SolarSystemId { get; set; }

        [JsonProperty("victim")]
        public Victim Victim { get; set; }
    }

    public partial class Package
    {
        [JsonProperty("killID")]
        public long KillId { get; set; }

        [JsonProperty("killmail")]
        public Killmail Killmail { get; set; }

        [JsonProperty("zkb")]
        public Zkb Zkb { get; set; }
    }

    public partial class Position
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("z")]
        public double Z { get; set; }
    }

    public partial class Victim
    {
        [JsonProperty("alliance_id")]
        public long AllianceId { get; set; }

        [JsonProperty("character_id")]
        public long CharacterId { get; set; }

        [JsonProperty("corporation_id")]
        public long CorporationId { get; set; }

        [JsonProperty("damage_taken")]
        public long DamageTaken { get; set; }

        [JsonProperty("items")]
        public List<Item> Items { get; set; }

        [JsonProperty("position")]
        public Position Position { get; set; }

        [JsonProperty("ship_type_id")]
        public long ShipTypeId { get; set; }
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