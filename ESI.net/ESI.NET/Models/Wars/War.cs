using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ESI.NET.Models.Wars
{
    public class War
    {
        [JsonProperty("aggressor")]
        public Combatant Aggressor { get; set; }

        [JsonProperty("allies")]
        public List<Ally> Allies { get; set; }

        [JsonProperty("declared")]
        public DateTime Declared { get; set; }

        [JsonProperty("defender")]
        public Combatant Defender { get; set; }

        [JsonProperty("finished")]
        public DateTime Finished { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("mutual")]
        public bool Mutual { get; set; }

        [JsonProperty("open_for_allies")]
        public bool OpenForAllies { get; set; }

        [JsonProperty("retracted")]
        public DateTime Retracted { get; set; }

        [JsonProperty("started")]
        public DateTime Started { get; set; }
    }



    public class Combatant
    {
        [JsonProperty("alliance_id")]
        public int AllianceId { get; set; }

        [JsonProperty("corporation_id")]
        public int CorporationId { get; set; }

        [JsonProperty("isk_destroyed")]
        public float IskDestroyed { get; set; }

        [JsonProperty("ships_killed")]
        public int ShipsKilled { get; set; }
    }

    public class Ally
    {
        [JsonProperty("alliance_id")]
        public int AllianceId { get; set; }

        [JsonProperty("corporation_id")]
        public int CorporationId { get; set; }
    }
}
