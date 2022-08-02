using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ESI.NET.Models.Character
{
    public class Medal
    {
        [JsonProperty("corporation_id")]
        public int CorporationId { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("graphics")]
        public List<GraphicLayer> Graphics { get; set; } = new List<GraphicLayer>();

        [JsonProperty("issuer_id")]
        public int IssuerId { get; set; }

        [JsonProperty("medal_id")]
        public int MedalId { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public class GraphicLayer
    {
        [JsonProperty("color")]
        public int Color { get; set; }

        [JsonProperty("graphic")]
        public string Graphic { get; set; }

        [JsonProperty("layer")]
        public int Layer { get; set; }

        [JsonProperty("part")]
        public int Part { get; set; }
    }
}
