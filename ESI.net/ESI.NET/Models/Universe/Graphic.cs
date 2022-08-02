using Newtonsoft.Json;

namespace ESI.NET.Models.Universe
{
    public class Graphic
    {
        [JsonProperty("graphic_id")]
        public int GraphicId { get; set; }

        [JsonProperty("graphic_file")]
        public string GraphicFile { get; set; }

        [JsonProperty("sof_race_name")]
        public string SofRaceName { get; set; }

        [JsonProperty("sof_fation_name")]
        public string SofFationName { get; set; }

        [JsonProperty("sof_dna")]
        public string SofDna { get; set; }

        [JsonProperty("sof_hull_name")]
        public string SofHullName { get; set; }

        [JsonProperty("collision_file")]
        public string CollisionFile { get; set; }

        [JsonProperty("icon_folder")]
        public string IconFolder { get; set; }

    }
}
