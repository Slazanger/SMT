using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ESI.NET.Models.Corporation
{
    public class Outpost
    {
        [JsonProperty("owner_id")]
        public int OwnerId { get; set; }

        [JsonProperty("system_id")]
        public int SystemId { get; set; }

        [JsonProperty("docking_cost_per_ship_volume")]
        public decimal DockingCostPerShipVolume { get; set; }

        [JsonProperty("office_rental_cost")]
        public long OfficeRentalCost { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("reprocessing_efficiency")]
        public decimal ReprocessingEfficiency { get; set; }

        [JsonProperty("reprocessing_station_take")]
        public decimal ReprocessingStationTake { get; set; }

        [JsonProperty("standing_owner_id")]
        public int StandingOwnerId { get; set; }

        [JsonProperty("coordinates")]
        public Position Coordinates { get; set; }

        [JsonProperty("services")]
        public List<OutpostService> Services { get; set; } = new List<OutpostService>();
    }

    public class OutpostService
    {
        [JsonProperty("service_name")]
        public string ServiceName { get; set; }

        [JsonProperty("minimum_standing")]
        public decimal MinimumStanding { get; set; }

        [JsonProperty("surcharge_per_bad_standing")]
        public decimal SurchargePerBadStanding { get; set; }

        [JsonProperty("discount_per_good_standing")]
        public decimal DiscountPerGoodStanding { get; set; }
    }
}
