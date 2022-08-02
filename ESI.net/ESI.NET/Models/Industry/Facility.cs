using Newtonsoft.Json;

namespace ESI.NET.Models.Industry
{
    public class Facility
    {
        [JsonProperty("facility_id")]
        public long FacilityId { get; set; }

        [JsonProperty("tax")]
        public decimal Tax { get; set; }

        [JsonProperty("owner_id")]
        public int OwnerId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("solar_system_id")]
        public int SolarSystemId { get; set; }

        [JsonProperty("region_id")]
        public int RegionId { get; set; }
    }
}
