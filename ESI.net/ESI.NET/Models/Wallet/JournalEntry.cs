using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Wallet
{
    public class JournalEntry
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("balance")]
        public decimal Balance { get; set; }

        [JsonProperty("context_id")]
        public long ContextId { get; set; }

        [JsonProperty("context_id_type")]
        public string ContextIdType { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("first_party_id")]
        public int FirstPartyId { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("ref_type")]
        public string RefType { get; set; }

        [JsonProperty("second_party_id")]
        public int SecondPartyId { get; set; }

        [JsonProperty("tax")]
        public decimal Tax { get; set; }

        [JsonProperty("tax_receiver_id")]
        public int TaxReceiverId { get; set; }
    }
}
