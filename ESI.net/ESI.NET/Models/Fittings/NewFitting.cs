using Newtonsoft.Json;

namespace ESI.NET.Models.Fittings
{
    public class NewFitting
    {
        [JsonProperty("fitting_id")]
        public int FittingId { get; set; }
    }
}
