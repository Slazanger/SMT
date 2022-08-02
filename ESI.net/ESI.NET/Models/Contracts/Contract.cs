using ESI.NET.Enumerations;
using Newtonsoft.Json;
using System;

namespace ESI.NET.Models.Contracts
{
    public class Contract
    {
        [JsonProperty("contract_id")]
        public int ContractId { get; set; }

        [JsonProperty("issuer_id")]
        public int IssuerId { get; set; }

        [JsonProperty("issuer_corporation_id")]
        public int IssuerCorporationId { get; set; }

        [JsonProperty("assignee_id")]
        public int AssigneeId { get; set; }

        [JsonProperty("acceptor_id")]
        public int AcceptorId { get; set; }

        [JsonProperty("start_location_id")]
        public long StartLocationId { get; set; }

        [JsonProperty("end_location_id")]
        public long EndLocationId { get; set; }

        [JsonProperty("type")]
        public ContractType Type { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("for_corporation")]
        public bool ForCorporation { get; set; }

        [JsonProperty("availability")]
        public string Availability { get; set; }

        [JsonProperty("date_issued")]
        public DateTime DateIssued { get; set; }

        [JsonProperty("date_expired")]
        public DateTime DateExpired { get; set; }

        [JsonProperty("date_accepted")]
        public DateTime DateAccepted { get; set; }

        [JsonProperty("days_to_complete")]
        public int DaysToComplete { get; set; }

        [JsonProperty("date_completed")]
        public DateTime DateCompleted { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("reward")]
        public decimal Reward { get; set; }

        [JsonProperty("collateral")]
        public decimal Collateral { get; set; }

        [JsonProperty("buyout")]
        public decimal Buyout { get; set; }

        [JsonProperty("volume")]
        public decimal Volume { get; set; }
    }
}
