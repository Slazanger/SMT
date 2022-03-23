// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using StructureHunter;
//
//    var structures = Structures.FromJson(jsonString);

namespace StructureHunter
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public enum RegionName
    { AR00001, AR00002, AR00003, Aridia, BR00004, BR00005, BR00006, BR00007, BR00008, BlackRise, Branch, CR00009, CR00010, CR00011, CR00012, CR00013, CR00014, CR00015, Cache, Catch, CloudRing, CobaltEdge, Curse, DR00016, DR00017, DR00018, DR00019, DR00020, DR00021, DR00022, DR00023, Deklein, Delve, Derelik, Detorid, Devoid, Domain, ER00024, ER00025, ER00026, ER00027, ER00028, ER00029, Esoteria, Essence, EtheriumReach, Everyshore, FR00030, Fade, Feythabolis, Fountain, Geminate, Genesis, GreatWildlands, Heimatar, Immensea, Impass, Insmother, Kador, Khanid, KorAzor, Lonetrek, Malpais, Metropolis, MoldenHeath, Oasa, Omist, OuterPassage, OuterRing, ParagonSoul, PeriodBasis, PerrigenFalls, Placid, Providence, PureBlind, Querious, ScaldingPass, SinqLaison, Solitude, Stain, Syndicate, TashMurkon, Tenal, Tenerifis, TheBleakLands, TheCitadel, TheForge, TheKalevalaExpanse, TheSpire, Tribute, ValeOfTheSilent, Venal, VergeVendor, WickedCreek };

    public enum TypeName
    { Astrahus, Athanor, Azbel, Fortizar, Keepstar, Raitaru, Sotiyo, Tatara };

    public static class Serialize
    {
        public static string ToJson(this Dictionary<string, Structures> self) => JsonConvert.SerializeObject(self, StructureHunter.Converter.Settings);
    }

    public partial class Location
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("z")]
        public double Z { get; set; }
    }

    public partial class Structures
    {
        [JsonProperty("firstSeen")]
        public DateTimeOffset FirstSeen { get; set; }

        [JsonProperty("lastSeen")]
        public DateTimeOffset LastSeen { get; set; }

        [JsonProperty("location")]
        public Location Location { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("public")]
        public bool Public { get; set; }

        [JsonProperty("regionId")]
        public long RegionId { get; set; }

        [JsonProperty("regionName")]
        public RegionName RegionName { get; set; }

        [JsonProperty("systemId")]
        public long SystemId { get; set; }

        [JsonProperty("systemName")]
        public string SystemName { get; set; }

        [JsonProperty("typeId")]
        public long? TypeId { get; set; }

        [JsonProperty("typeName")]
        public TypeName? TypeName { get; set; }
    }

    public partial class Structures
    {
        public static Dictionary<string, Structures> FromJson(string json) => JsonConvert.DeserializeObject<Dictionary<string, Structures>>(json, StructureHunter.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                RegionNameConverter.Singleton,
                TypeNameConverter.Singleton,
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class RegionNameConverter : JsonConverter
    {
        public static readonly RegionNameConverter Singleton = new RegionNameConverter();

        public override bool CanConvert(Type t) => t == typeof(RegionName) || t == typeof(RegionName?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "A-R00001":
                    return RegionName.AR00001;

                case "A-R00002":
                    return RegionName.AR00002;

                case "A-R00003":
                    return RegionName.AR00003;

                case "Aridia":
                    return RegionName.Aridia;

                case "B-R00004":
                    return RegionName.BR00004;

                case "B-R00005":
                    return RegionName.BR00005;

                case "B-R00006":
                    return RegionName.BR00006;

                case "B-R00007":
                    return RegionName.BR00007;

                case "B-R00008":
                    return RegionName.BR00008;

                case "Black Rise":
                    return RegionName.BlackRise;

                case "Branch":
                    return RegionName.Branch;

                case "C-R00009":
                    return RegionName.CR00009;

                case "C-R00010":
                    return RegionName.CR00010;

                case "C-R00011":
                    return RegionName.CR00011;

                case "C-R00012":
                    return RegionName.CR00012;

                case "C-R00013":
                    return RegionName.CR00013;

                case "C-R00014":
                    return RegionName.CR00014;

                case "C-R00015":
                    return RegionName.CR00015;

                case "Cache":
                    return RegionName.Cache;

                case "Catch":
                    return RegionName.Catch;

                case "Cloud Ring":
                    return RegionName.CloudRing;

                case "Cobalt Edge":
                    return RegionName.CobaltEdge;

                case "Curse":
                    return RegionName.Curse;

                case "D-R00016":
                    return RegionName.DR00016;

                case "D-R00017":
                    return RegionName.DR00017;

                case "D-R00018":
                    return RegionName.DR00018;

                case "D-R00019":
                    return RegionName.DR00019;

                case "D-R00020":
                    return RegionName.DR00020;

                case "D-R00021":
                    return RegionName.DR00021;

                case "D-R00022":
                    return RegionName.DR00022;

                case "D-R00023":
                    return RegionName.DR00023;

                case "Deklein":
                    return RegionName.Deklein;

                case "Delve":
                    return RegionName.Delve;

                case "Derelik":
                    return RegionName.Derelik;

                case "Detorid":
                    return RegionName.Detorid;

                case "Devoid":
                    return RegionName.Devoid;

                case "Domain":
                    return RegionName.Domain;

                case "E-R00024":
                    return RegionName.ER00024;

                case "E-R00025":
                    return RegionName.ER00025;

                case "E-R00026":
                    return RegionName.ER00026;

                case "E-R00027":
                    return RegionName.ER00027;

                case "E-R00028":
                    return RegionName.ER00028;

                case "E-R00029":
                    return RegionName.ER00029;

                case "Esoteria":
                    return RegionName.Esoteria;

                case "Essence":
                    return RegionName.Essence;

                case "Etherium Reach":
                    return RegionName.EtheriumReach;

                case "Everyshore":
                    return RegionName.Everyshore;

                case "F-R00030":
                    return RegionName.FR00030;

                case "Fade":
                    return RegionName.Fade;

                case "Feythabolis":
                    return RegionName.Feythabolis;

                case "Fountain":
                    return RegionName.Fountain;

                case "Geminate":
                    return RegionName.Geminate;

                case "Genesis":
                    return RegionName.Genesis;

                case "Great Wildlands":
                    return RegionName.GreatWildlands;

                case "Heimatar":
                    return RegionName.Heimatar;

                case "Immensea":
                    return RegionName.Immensea;

                case "Impass":
                    return RegionName.Impass;

                case "Insmother":
                    return RegionName.Insmother;

                case "Kador":
                    return RegionName.Kador;

                case "Khanid":
                    return RegionName.Khanid;

                case "Kor-Azor":
                    return RegionName.KorAzor;

                case "Lonetrek":
                    return RegionName.Lonetrek;

                case "Malpais":
                    return RegionName.Malpais;

                case "Metropolis":
                    return RegionName.Metropolis;

                case "Molden Heath":
                    return RegionName.MoldenHeath;

                case "Oasa":
                    return RegionName.Oasa;

                case "Omist":
                    return RegionName.Omist;

                case "Outer Passage":
                    return RegionName.OuterPassage;

                case "Outer Ring":
                    return RegionName.OuterRing;

                case "Paragon Soul":
                    return RegionName.ParagonSoul;

                case "Period Basis":
                    return RegionName.PeriodBasis;

                case "Perrigen Falls":
                    return RegionName.PerrigenFalls;

                case "Placid":
                    return RegionName.Placid;

                case "Providence":
                    return RegionName.Providence;

                case "Pure Blind":
                    return RegionName.PureBlind;

                case "Querious":
                    return RegionName.Querious;

                case "Scalding Pass":
                    return RegionName.ScaldingPass;

                case "Sinq Laison":
                    return RegionName.SinqLaison;

                case "Solitude":
                    return RegionName.Solitude;

                case "Stain":
                    return RegionName.Stain;

                case "Syndicate":
                    return RegionName.Syndicate;

                case "Tash-Murkon":
                    return RegionName.TashMurkon;

                case "Tenal":
                    return RegionName.Tenal;

                case "Tenerifis":
                    return RegionName.Tenerifis;

                case "The Bleak Lands":
                    return RegionName.TheBleakLands;

                case "The Citadel":
                    return RegionName.TheCitadel;

                case "The Forge":
                    return RegionName.TheForge;

                case "The Kalevala Expanse":
                    return RegionName.TheKalevalaExpanse;

                case "The Spire":
                    return RegionName.TheSpire;

                case "Tribute":
                    return RegionName.Tribute;

                case "Vale of the Silent":
                    return RegionName.ValeOfTheSilent;

                case "Venal":
                    return RegionName.Venal;

                case "Verge Vendor":
                    return RegionName.VergeVendor;

                case "Wicked Creek":
                    return RegionName.WickedCreek;
            }
            throw new Exception("Cannot unmarshal type RegionName");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (RegionName)untypedValue;
            switch (value)
            {
                case RegionName.AR00001:
                    serializer.Serialize(writer, "A-R00001");
                    return;

                case RegionName.AR00002:
                    serializer.Serialize(writer, "A-R00002");
                    return;

                case RegionName.AR00003:
                    serializer.Serialize(writer, "A-R00003");
                    return;

                case RegionName.Aridia:
                    serializer.Serialize(writer, "Aridia");
                    return;

                case RegionName.BR00004:
                    serializer.Serialize(writer, "B-R00004");
                    return;

                case RegionName.BR00005:
                    serializer.Serialize(writer, "B-R00005");
                    return;

                case RegionName.BR00006:
                    serializer.Serialize(writer, "B-R00006");
                    return;

                case RegionName.BR00007:
                    serializer.Serialize(writer, "B-R00007");
                    return;

                case RegionName.BR00008:
                    serializer.Serialize(writer, "B-R00008");
                    return;

                case RegionName.BlackRise:
                    serializer.Serialize(writer, "Black Rise");
                    return;

                case RegionName.Branch:
                    serializer.Serialize(writer, "Branch");
                    return;

                case RegionName.CR00009:
                    serializer.Serialize(writer, "C-R00009");
                    return;

                case RegionName.CR00010:
                    serializer.Serialize(writer, "C-R00010");
                    return;

                case RegionName.CR00011:
                    serializer.Serialize(writer, "C-R00011");
                    return;

                case RegionName.CR00012:
                    serializer.Serialize(writer, "C-R00012");
                    return;

                case RegionName.CR00013:
                    serializer.Serialize(writer, "C-R00013");
                    return;

                case RegionName.CR00014:
                    serializer.Serialize(writer, "C-R00014");
                    return;

                case RegionName.CR00015:
                    serializer.Serialize(writer, "C-R00015");
                    return;

                case RegionName.Cache:
                    serializer.Serialize(writer, "Cache");
                    return;

                case RegionName.Catch:
                    serializer.Serialize(writer, "Catch");
                    return;

                case RegionName.CloudRing:
                    serializer.Serialize(writer, "Cloud Ring");
                    return;

                case RegionName.CobaltEdge:
                    serializer.Serialize(writer, "Cobalt Edge");
                    return;

                case RegionName.Curse:
                    serializer.Serialize(writer, "Curse");
                    return;

                case RegionName.DR00016:
                    serializer.Serialize(writer, "D-R00016");
                    return;

                case RegionName.DR00017:
                    serializer.Serialize(writer, "D-R00017");
                    return;

                case RegionName.DR00018:
                    serializer.Serialize(writer, "D-R00018");
                    return;

                case RegionName.DR00019:
                    serializer.Serialize(writer, "D-R00019");
                    return;

                case RegionName.DR00020:
                    serializer.Serialize(writer, "D-R00020");
                    return;

                case RegionName.DR00021:
                    serializer.Serialize(writer, "D-R00021");
                    return;

                case RegionName.DR00022:
                    serializer.Serialize(writer, "D-R00022");
                    return;

                case RegionName.DR00023:
                    serializer.Serialize(writer, "D-R00023");
                    return;

                case RegionName.Deklein:
                    serializer.Serialize(writer, "Deklein");
                    return;

                case RegionName.Delve:
                    serializer.Serialize(writer, "Delve");
                    return;

                case RegionName.Derelik:
                    serializer.Serialize(writer, "Derelik");
                    return;

                case RegionName.Detorid:
                    serializer.Serialize(writer, "Detorid");
                    return;

                case RegionName.Devoid:
                    serializer.Serialize(writer, "Devoid");
                    return;

                case RegionName.Domain:
                    serializer.Serialize(writer, "Domain");
                    return;

                case RegionName.ER00024:
                    serializer.Serialize(writer, "E-R00024");
                    return;

                case RegionName.ER00025:
                    serializer.Serialize(writer, "E-R00025");
                    return;

                case RegionName.ER00026:
                    serializer.Serialize(writer, "E-R00026");
                    return;

                case RegionName.ER00027:
                    serializer.Serialize(writer, "E-R00027");
                    return;

                case RegionName.ER00028:
                    serializer.Serialize(writer, "E-R00028");
                    return;

                case RegionName.ER00029:
                    serializer.Serialize(writer, "E-R00029");
                    return;

                case RegionName.Esoteria:
                    serializer.Serialize(writer, "Esoteria");
                    return;

                case RegionName.Essence:
                    serializer.Serialize(writer, "Essence");
                    return;

                case RegionName.EtheriumReach:
                    serializer.Serialize(writer, "Etherium Reach");
                    return;

                case RegionName.Everyshore:
                    serializer.Serialize(writer, "Everyshore");
                    return;

                case RegionName.FR00030:
                    serializer.Serialize(writer, "F-R00030");
                    return;

                case RegionName.Fade:
                    serializer.Serialize(writer, "Fade");
                    return;

                case RegionName.Feythabolis:
                    serializer.Serialize(writer, "Feythabolis");
                    return;

                case RegionName.Fountain:
                    serializer.Serialize(writer, "Fountain");
                    return;

                case RegionName.Geminate:
                    serializer.Serialize(writer, "Geminate");
                    return;

                case RegionName.Genesis:
                    serializer.Serialize(writer, "Genesis");
                    return;

                case RegionName.GreatWildlands:
                    serializer.Serialize(writer, "Great Wildlands");
                    return;

                case RegionName.Heimatar:
                    serializer.Serialize(writer, "Heimatar");
                    return;

                case RegionName.Immensea:
                    serializer.Serialize(writer, "Immensea");
                    return;

                case RegionName.Impass:
                    serializer.Serialize(writer, "Impass");
                    return;

                case RegionName.Insmother:
                    serializer.Serialize(writer, "Insmother");
                    return;

                case RegionName.Kador:
                    serializer.Serialize(writer, "Kador");
                    return;

                case RegionName.Khanid:
                    serializer.Serialize(writer, "Khanid");
                    return;

                case RegionName.KorAzor:
                    serializer.Serialize(writer, "Kor-Azor");
                    return;

                case RegionName.Lonetrek:
                    serializer.Serialize(writer, "Lonetrek");
                    return;

                case RegionName.Malpais:
                    serializer.Serialize(writer, "Malpais");
                    return;

                case RegionName.Metropolis:
                    serializer.Serialize(writer, "Metropolis");
                    return;

                case RegionName.MoldenHeath:
                    serializer.Serialize(writer, "Molden Heath");
                    return;

                case RegionName.Oasa:
                    serializer.Serialize(writer, "Oasa");
                    return;

                case RegionName.Omist:
                    serializer.Serialize(writer, "Omist");
                    return;

                case RegionName.OuterPassage:
                    serializer.Serialize(writer, "Outer Passage");
                    return;

                case RegionName.OuterRing:
                    serializer.Serialize(writer, "Outer Ring");
                    return;

                case RegionName.ParagonSoul:
                    serializer.Serialize(writer, "Paragon Soul");
                    return;

                case RegionName.PeriodBasis:
                    serializer.Serialize(writer, "Period Basis");
                    return;

                case RegionName.PerrigenFalls:
                    serializer.Serialize(writer, "Perrigen Falls");
                    return;

                case RegionName.Placid:
                    serializer.Serialize(writer, "Placid");
                    return;

                case RegionName.Providence:
                    serializer.Serialize(writer, "Providence");
                    return;

                case RegionName.PureBlind:
                    serializer.Serialize(writer, "Pure Blind");
                    return;

                case RegionName.Querious:
                    serializer.Serialize(writer, "Querious");
                    return;

                case RegionName.ScaldingPass:
                    serializer.Serialize(writer, "Scalding Pass");
                    return;

                case RegionName.SinqLaison:
                    serializer.Serialize(writer, "Sinq Laison");
                    return;

                case RegionName.Solitude:
                    serializer.Serialize(writer, "Solitude");
                    return;

                case RegionName.Stain:
                    serializer.Serialize(writer, "Stain");
                    return;

                case RegionName.Syndicate:
                    serializer.Serialize(writer, "Syndicate");
                    return;

                case RegionName.TashMurkon:
                    serializer.Serialize(writer, "Tash-Murkon");
                    return;

                case RegionName.Tenal:
                    serializer.Serialize(writer, "Tenal");
                    return;

                case RegionName.Tenerifis:
                    serializer.Serialize(writer, "Tenerifis");
                    return;

                case RegionName.TheBleakLands:
                    serializer.Serialize(writer, "The Bleak Lands");
                    return;

                case RegionName.TheCitadel:
                    serializer.Serialize(writer, "The Citadel");
                    return;

                case RegionName.TheForge:
                    serializer.Serialize(writer, "The Forge");
                    return;

                case RegionName.TheKalevalaExpanse:
                    serializer.Serialize(writer, "The Kalevala Expanse");
                    return;

                case RegionName.TheSpire:
                    serializer.Serialize(writer, "The Spire");
                    return;

                case RegionName.Tribute:
                    serializer.Serialize(writer, "Tribute");
                    return;

                case RegionName.ValeOfTheSilent:
                    serializer.Serialize(writer, "Vale of the Silent");
                    return;

                case RegionName.Venal:
                    serializer.Serialize(writer, "Venal");
                    return;

                case RegionName.VergeVendor:
                    serializer.Serialize(writer, "Verge Vendor");
                    return;

                case RegionName.WickedCreek:
                    serializer.Serialize(writer, "Wicked Creek");
                    return;
            }
            throw new Exception("Cannot marshal type RegionName");
        }
    }

    internal class TypeNameConverter : JsonConverter
    {
        public static readonly TypeNameConverter Singleton = new TypeNameConverter();

        public override bool CanConvert(Type t) => t == typeof(TypeName) || t == typeof(TypeName?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "Astrahus":
                    return TypeName.Astrahus;

                case "Athanor":
                    return TypeName.Athanor;

                case "Azbel":
                    return TypeName.Azbel;

                case "Fortizar":
                    return TypeName.Fortizar;

                case "Keepstar":
                    return TypeName.Keepstar;

                case "Raitaru":
                    return TypeName.Raitaru;

                case "Sotiyo":
                    return TypeName.Sotiyo;

                case "Tatara":
                    return TypeName.Tatara;
            }
            throw new Exception("Cannot unmarshal type TypeName");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (TypeName)untypedValue;
            switch (value)
            {
                case TypeName.Astrahus:
                    serializer.Serialize(writer, "Astrahus");
                    return;

                case TypeName.Athanor:
                    serializer.Serialize(writer, "Athanor");
                    return;

                case TypeName.Azbel:
                    serializer.Serialize(writer, "Azbel");
                    return;

                case TypeName.Fortizar:
                    serializer.Serialize(writer, "Fortizar");
                    return;

                case TypeName.Keepstar:
                    serializer.Serialize(writer, "Keepstar");
                    return;

                case TypeName.Raitaru:
                    serializer.Serialize(writer, "Raitaru");
                    return;

                case TypeName.Sotiyo:
                    serializer.Serialize(writer, "Sotiyo");
                    return;

                case TypeName.Tatara:
                    serializer.Serialize(writer, "Tatara");
                    return;
            }
            throw new Exception("Cannot marshal type TypeName");
        }
    }
}