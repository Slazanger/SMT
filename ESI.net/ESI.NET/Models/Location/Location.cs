using Newtonsoft.Json;

namespace ESI.NET.Models.Location
{
    public class Location
    {
        [JsonProperty("solar_system_id")]
        public int SolarSystemId { get; set; }

        [JsonProperty("station_id")]
        public int StationId { get; set; }

        [JsonProperty("structure_id")]
        public long StructureId { get; set; }
    }
}
