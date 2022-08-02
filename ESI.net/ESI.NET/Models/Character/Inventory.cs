using Newtonsoft.Json;

namespace ESI.NET.Models.Character
{
    public class Inventory
    {
        [JsonProperty("abandon_loot_quantity")]
        public long AbandonLootQuantity { get; set; }

        [JsonProperty("trash_item_quantity")]
        public long TrashItemQuantity { get; set; }
    }
}
