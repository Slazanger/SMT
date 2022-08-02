using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ESI.NET.Models.Sovereignty
{
    public class Campaign
    {
        [JsonProperty("attackers_score")]
        public double AttackersScore { get; set; }

        [JsonProperty("campaign_id")]
        public int CampaignId { get; set; }

        [JsonProperty("constellation_id")]
        public int ConstellationId { get; set; }

        [JsonProperty("defender_id")]
        public int DefenderId { get; set; }

        [JsonProperty("defender_score")]
        public double DefenderScore { get; set; }

        [JsonProperty("event_type")]
        public string EventType { get; set; }

        [JsonProperty("participants")]
        public List<Participants> Participants { get; set; } = new List<Participants>();

        [JsonProperty("solar_system_id")]
        public int SolarSystemId { get; set; }

        [JsonProperty("start_time")]
        public DateTime StartTime { get; set; }

        [JsonProperty("structure_id")]
        public long StructureId { get; set; }
    }

    public class Participants
    {
        [JsonProperty("alliance_id")]
        public int AllianceId { get; set; }

        [JsonProperty("score")]
        public double Score { get; set; }
    }
}
