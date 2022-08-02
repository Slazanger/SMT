using Newtonsoft.Json;

namespace ESI.NET.Models.Universe
{
    public class Station
    {
        [JsonProperty("station_id")]
        public int StationId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("owner")]
        public int Owner { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("race_id")]
        public int RaceId { get; set; }

        [JsonProperty("position")]
        public Position Position { get; set; }

        [JsonProperty("system_id")]
        public int SystemId { get; set; }

        [JsonProperty("reprocessing_efficiency")]
        public decimal ReprocessingEfficiency { get; set; }

        [JsonProperty("reprocessing_stations_take")]
        public decimal ReprocessingStationsTake { get; set; }

        [JsonProperty("max_dockable_ship_volume")]
        public decimal MaxDockableShipVolume { get; set; }

        [JsonProperty("office_rental_cost")]
        public decimal OfficeRentalCost { get; set; }

        [JsonProperty("services")]
        public string[] Services { get; set; }
    }
}
