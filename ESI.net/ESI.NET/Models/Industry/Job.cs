using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Industry
{
    public class Job
    {
        [JsonProperty("job_id")]
        public int JobId { get; set; }

        [JsonProperty("installer_id")]
        public int InstallerId { get; set; }

        [JsonProperty("facility_id")]
        public long FacilityId { get; set; }

        [JsonProperty("station_id")]
        public long StationId { get; set; }

        [JsonProperty("activity_id")]
        public int ActivityId { get; set; }

        [JsonProperty("blueprint_id")]
        public long BlueprintId { get; set; }

        [JsonProperty("blueprint_type_id")]
        public int BlueprintTypeId { get; set; }

        [JsonProperty("blueprint_location_id")]
        public long BlueprintLocationId { get; set; }

        [JsonProperty("output_location_id")]
        public long OutputLocationId { get; set; }

        [JsonProperty("runs")]
        public int Runs { get; set; }

        [JsonProperty("cost")]
        public decimal Cost { get; set; }

        [JsonProperty("licensed_runs")]
        public int LicensedRuns { get; set; }

        [JsonProperty("probability")]
        public decimal Probability { get; set; }

        [JsonProperty("product_type_id")]
        public int ProductTypeId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("start_date")]
        public DateTime StartDate { get; set; }

        [JsonProperty("end_date")]
        public DateTime EndDate { get; set; }

        [JsonProperty("pause_date")]
        public DateTime PauseDate { get; set; }

        [JsonProperty("completed_date")]
        public DateTime CompletedDate { get; set; }

        [JsonProperty("completed_character_id")]
        public int CompletedCharacterId { get; set; }

        [JsonProperty("successful_runs")]
        public int SuccessfulRuns { get; set; }
    }
}
