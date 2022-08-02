using Newtonsoft.Json;

namespace ESI.NET.Models.Character
{
    public class Mining
    {
        [JsonProperty("drone_mine")]
        public long DroneMine { get; set; }

        [JsonProperty("ore_arkonor")]
        public long OreArkonor { get; set; }

        [JsonProperty("ore_bistot")]
        public long OreBistot { get; set; }

        [JsonProperty("ore_crokite")]
        public long OreCrokite { get; set; }

        [JsonProperty("ore_dark_ochre")]
        public long OreDarkOchre { get; set; }

        [JsonProperty("ore_gneiss")]
        public long OreGneiss { get; set; }

        [JsonProperty("ore_harvestable_cloud")]
        public long OreHarvestableCloud { get; set; }

        [JsonProperty("ore_hedbergite")]
        public long OreHedbergite { get; set; }

        [JsonProperty("ore_hemorphite")]
        public long OreHemorphite { get; set; }

        [JsonProperty("ore_ice")]
        public long OreIce { get; set; }

        [JsonProperty("ore_jaspet")]
        public long OreJaspet { get; set; }

        [JsonProperty("ore_kernite")]
        public long OreKernite { get; set; }

        [JsonProperty("ore_mercoxit")]
        public long OreMercoxit { get; set; }

        [JsonProperty("ore_omber")]
        public long OreOmber { get; set; }

        [JsonProperty("ore_plagioclase")]
        public long OrePlagioclase { get; set; }

        [JsonProperty("ore_pyroxeres")]
        public long OrePyroxeres { get; set; }

        [JsonProperty("ore_scordite")]
        public long OreScordite { get; set; }

        [JsonProperty("ore_spodumain")]
        public long OreSpodumain { get; set; }

        [JsonProperty("ore_veldspar")]
        public long OreVeldspar { get; set; }
    }
}
