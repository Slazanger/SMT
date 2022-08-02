using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ESI.NET.Models.Character
{
    public class Title
    {
        [JsonProperty("title_id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
