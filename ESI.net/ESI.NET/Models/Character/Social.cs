using Newtonsoft.Json;

namespace ESI.NET.Models.Character
{
    public class Social
    {
        [JsonProperty("add_contact_bad")]
        public long AddContactBad { get; set; }

        [JsonProperty("add_contact_good")]
        public long AddContactGood { get; set; }

        [JsonProperty("add_contact_high")]
        public long AddContactHigh { get; set; }

        [JsonProperty("add_contact_horrible")]
        public long AddContactHorrible { get; set; }

        [JsonProperty("add_contact_neutral")]
        public long AddContactNeutral { get; set; }

        [JsonProperty("add_note")]
        public long AddNote { get; set; }

        [JsonProperty("added_as_contact_bad")]
        public long AddedAsContactBad { get; set; }

        [JsonProperty("added_as_contact_good")]
        public long AddedAsContactGood { get; set; }

        [JsonProperty("added_as_contact_high")]
        public long AddedAsContactHigh { get; set; }

        [JsonProperty("added_as_contact_horrible")]
        public long AddedAsContactHorrible { get; set; }

        [JsonProperty("added_as_contact_neutral")]
        public long AddedAsContactNeutral { get; set; }

        [JsonProperty("calendar_event_created")]
        public long CalendarEventCreated { get; set; }

        [JsonProperty("chat_messages_alliance")]
        public long ChatMessagesAlliance { get; set; }

        [JsonProperty("chat_messages_constellation")]
        public long ChatMessagesConstellation { get; set; }

        [JsonProperty("chat_messages_corporation")]
        public long ChatMessagesCorporation { get; set; }

        [JsonProperty("chat_messages_fleet")]
        public long ChatMessagesFleet { get; set; }

        [JsonProperty("chat_messages_region")]
        public long ChatMessagesRegion { get; set; }

        [JsonProperty("chat_messages_solarsystem")]
        public long ChatMessagesSolarsystem { get; set; }

        [JsonProperty("chat_messages_warfaction")]
        public long ChatMessagesWarfaction { get; set; }

        [JsonProperty("chat_total_message_length")]
        public long ChatTotalMessageLength { get; set; }

        [JsonProperty("direct_trades")]
        public long DirectTrades { get; set; }

        [JsonProperty("fleet_broadcasts")]
        public long FleetBroadcasts { get; set; }

        [JsonProperty("fleet_joins")]
        public long FleetJoins { get; set; }

        [JsonProperty("mails_received")]
        public long MailsReceived { get; set; }

        [JsonProperty("mails_sent")]
        public long MailsSent { get; set; }
    }
}
