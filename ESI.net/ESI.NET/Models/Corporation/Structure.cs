using ESI.NET.Enumerations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ESI.NET.Models.Corporation
{
    public class Structure
    {
        [JsonProperty("corporation_id")]
        public int CorporationId { get; set; }

        [JsonProperty("fuel_expires")]
        public DateTime FuelExpires { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("next_reinforce_apply")]
        public DateTime NextReinforceApply { get; set; }

        [JsonProperty("next_reinforce_hour")]
        public int NextReinforceHour { get; set; }

        [JsonProperty("profile_id")]
        public int ProfileId { get; set; }

        [JsonProperty("reinforce_hour")]
        public int ReinforceHour { get; set; }

        [JsonProperty("services")]
        public List<Service> Services { get; set; } = new List<Service>();

        [JsonProperty("state")]
        public StructureState State { get; set; }

        [JsonProperty("state_timer_end")]
        public DateTime StateTimerEnd { get; set; }

        [JsonProperty("state_timer_start")]
        public DateTime StateTimerStart { get; set; }

        [JsonProperty("structure_id")]
        public long StructureId { get; set; }

        [JsonProperty("system_id")]
        public int SystemId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("unanchors_at")]
        public DateTime UnanchorsAt { get; set; }
    }

    public class Service
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("state")]
        public StructureServiceState State { get; set; }
    }
}
