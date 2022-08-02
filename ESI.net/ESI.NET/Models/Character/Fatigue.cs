using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Character
{
    public class Fatigue
    {
        [JsonProperty("jump_fatigue_expire_date")]
        public DateTime JumpFatigueExpireDate { get; set; }

        [JsonProperty("last_jump_date")]
        public DateTime LastJumpDate { get; set; }

        [JsonProperty("last_update_date")]
        public DateTime LastUpdateDate { get; set; }
    }
}
