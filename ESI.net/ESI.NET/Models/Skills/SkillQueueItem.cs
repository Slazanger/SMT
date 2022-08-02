using Newtonsoft.Json;

namespace ESI.NET.Models.Skills
{
    public class SkillQueueItem
    {
        [JsonProperty("skill_id")]
        public int SkillId { get; set; }

        [JsonProperty("finish_date")]
        public string FinishDate { get; set; }

        [JsonProperty("start_date")]
        public string StartDate { get; set; }

        [JsonProperty("finished_level")]
        public int FinishedLevel { get; set; }

        [JsonProperty("queue_position")]
        public int QueuePosition { get; set; }

        [JsonProperty("training_start_sp")]
        public int TrainingStartSp { get; set; }

        [JsonProperty("level_end_sp")]
        public int LevelEndSp { get; set; }

        [JsonProperty("level_start_sp")]
        public int LevelStartSp { get; set; }
    }
}
