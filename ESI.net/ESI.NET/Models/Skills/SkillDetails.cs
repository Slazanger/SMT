using Newtonsoft.Json;
using System.Collections.Generic;

namespace ESI.NET.Models.Skills
{
    public class SkillDetails
    {
        [JsonProperty("skills")]
        public List<Skill> Skills { get; set; } = new List<Skill>();

        [JsonProperty("total_sp")]
        public long TotalSp { get; set; }

        [JsonProperty("unallocated_sp")]
        public int UnallocatedSp { get; set; }
    }

    public class Skill
    {
        [JsonProperty("skill_id")]
        public int SkillId { get; set; }

        [JsonProperty("skillpoints_in_skill")]
        public long SkillpointsInSkill { get; set; }

        [JsonProperty("trained_skill_level")]
        public int TrainedSkillLevel { get; set; }

        [JsonProperty("active_skill_level")]
        public int ActiveSkillLevel { get; set; }
    }
}
