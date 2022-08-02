using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Character
{
    public class Agent
    {
        [JsonProperty("agent_id")]
        public int AgentId { get; set; }

        [JsonProperty("points_per_day")]
        public float PointsPerDay { get; set; }

        [JsonProperty("remainder_points")]
        public float RemainderPoints { get; set; }

        [JsonProperty("skill_type_id")]
        public int SkillTypeId { get; set; }

        [JsonProperty("started_at")]
        public DateTime StartedAt { get; set; }
    }
}
