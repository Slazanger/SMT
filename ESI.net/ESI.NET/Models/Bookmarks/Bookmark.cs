using Newtonsoft.Json;

namespace ESI.NET.Models.Bookmarks
{
    public class Bookmark
    {
        [JsonProperty("bookmark_id")]
        public int BookmarkId { get; set; }

        [JsonProperty("folder_id")]
        public int FolderId { get; set; }

        [JsonProperty("created")]
        public string Created { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }

        [JsonProperty("location_id")]
        public int LocationId { get; set; }

        [JsonProperty("creator_id")]
        public int CreatorId { get; set; }

        [JsonProperty("item")]
        public Item Item { get; set; }

        [JsonProperty("coordinates")]
        public Position Coordinates { get; set; }
    }

    public class Item
    {
        [JsonProperty("item_id")]
        public long ItemId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }
    }
}
