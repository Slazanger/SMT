using Newtonsoft.Json;

namespace ESI.NET.Models.Wallet
{
    public class Wallet
    {
        [JsonProperty("division")]
        public int Division { get; set; }

        [JsonProperty("balance")]
        public decimal Balance { get; set; }
    }
}
