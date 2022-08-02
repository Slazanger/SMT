using Newtonsoft.Json;

namespace ESI.NET.Models.Opportunities
{
    public class Group
    {
        [JsonProperty("group_id")]
        public int GroupId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("notification")]
        public string Notification { get; set; }

        [JsonProperty("required_tasks")]
        public int[] RequiredTasks { get; set; }

        [JsonProperty("connected_groups")]
        public int[] ConnectedGroups { get; set; }
    }
}
