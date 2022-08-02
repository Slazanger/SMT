using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ESI.NET.Models.Character
{
    public class ChatChannel
    {
        [JsonProperty("allowed")]
        public List<ChannelUser> Allowed { get; set; } = new List<ChannelUser>();

        [JsonProperty("blocked")]
        public List<DeniedUser> Blocked { get; set; } = new List<DeniedUser>();

        [JsonProperty("channel_id")]
        public long Id { get; set; }

        [JsonProperty("comparison_key")]
        public string ComparisonKey { get; set; }

        [JsonProperty("has_password")]
        public bool HasPassword { get; set; }

        [JsonProperty("motd")]
        public string MOTD { get; set; }

        [JsonProperty("muted")]
        public List<DeniedUser> Muted { get; set; } = new List<DeniedUser>();

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("operators")]
        public List<ChannelUser> Operators { get; set; } = new List<ChannelUser>();

        [JsonProperty("owner_id")]
        public long OwnerId { get; set; }
    }


    public class ChannelUser
    {
        [JsonProperty("accessor_id")]
        public long Id { get; set; }

        [JsonProperty("accessor_type")]
        public string AccessorType { get; set; }
    }

    public class DeniedUser : ChannelUser
    {
        [JsonProperty("end_at")]
        public DateTime EndAt { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
}
