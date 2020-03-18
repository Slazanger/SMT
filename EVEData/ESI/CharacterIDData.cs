// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using CharacterIDs;
//
//    var characterIdData = CharacterIdData.FromJson(jsonString);

namespace CharacterIDs
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Globalization;

    public static class Serialize
    {
        public static string ToJson(this CharacterIdData self) => JsonConvert.SerializeObject(self, CharacterIDs.Converter.Settings);
    }

    public partial class Character
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public partial class CharacterIdData
    {
        [JsonProperty("characters")]
        public Character[] Characters { get; set; }
    }

    public partial class CharacterIdData
    {
        public static CharacterIdData FromJson(string json) => JsonConvert.DeserializeObject<CharacterIdData>(json, CharacterIDs.Converter.Settings);
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