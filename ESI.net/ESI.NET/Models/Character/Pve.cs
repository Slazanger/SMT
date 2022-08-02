using Newtonsoft.Json;

namespace ESI.NET.Models.Character
{
    public class Pve
    {
        [JsonProperty("dungeons_completed_agent")]
        public long DungeonsCompletedAgent { get; set; }

        [JsonProperty("dungeons_completed_distribution")]
        public long DungeonsCompletedDistribution { get; set; }

        [JsonProperty("missions_succeeded")]
        public long MissionsSucceeded { get; set; }

        [JsonProperty("missions_succeeded_epic_arc")]
        public long MissionsSucceededEpicArc { get; set; }
    }
}
