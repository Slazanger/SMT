using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Wallet
{
    public class Transaction
    {
        [JsonProperty("transaction_id")]
        public long TransactionId { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("location_id")]
        public long LocationId { get; set; }

        [JsonProperty("unit_price")]
        public decimal UnitPrice { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("client_id")]
        public int ClientId { get; set; }

        [JsonProperty("is_buy")]
        public bool IsBuy { get; set; }

        [JsonProperty("is_personal")]
        public bool IsPersonal { get; set; }

        [JsonProperty("journal_ref_id")]
        public long JournalRefId { get; set; }
    }
}
