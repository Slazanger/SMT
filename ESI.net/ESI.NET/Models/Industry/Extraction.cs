using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Industry
{
    public class Extraction
    {
        [JsonProperty("structure_id")]
        public long StructureId { get; set; }

        [JsonProperty("moon_id")]
        public int MoonId { get; set; }

        [JsonProperty("extraction_start_time")]
        public DateTime ExtractionStartTime { get; set; }

        [JsonProperty("chunk_arrival_time")]
        public DateTime ChunkArrivalTime { get; set; }

        [JsonProperty("natural_decay_time")]
        public DateTime NaturalDecayTime { get; set; }
    }
}
