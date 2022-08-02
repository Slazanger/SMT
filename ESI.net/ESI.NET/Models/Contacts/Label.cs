using Newtonsoft.Json;

namespace ESI.NET.Models.Contacts
{
    public class Label
    {
        [JsonProperty("label_id")]
        public long LabelId { get; set; }

        [JsonProperty("label_name")]
        public string LabelName { get; set; }
    }
}
