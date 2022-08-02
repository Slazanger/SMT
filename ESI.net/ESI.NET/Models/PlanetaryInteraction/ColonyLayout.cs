using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ESI.NET.Models.PlanetaryInteraction
{
    public class ColonyLayout
    {
        [JsonProperty("links")]
        public List<Link> Links { get; set; } = new List<Link>();

        [JsonProperty("pins")]
        public List<Pin> Pins { get; set; } = new List<Pin>();

        [JsonProperty("routes")]
        public List<Route> Routes { get; set; } = new List<Route>();
    }

    #region Level 1 nested classes
    public class Link
    {
        [JsonProperty("destination_pin_id")]
        public long DestinationPinId { get; set; }

        [JsonProperty("link_level")]
        public int LinkLevel { get; set; }

        [JsonProperty("source_pin_id")]
        public long SourcePinId { get; set; }
    }

    public class Pin
    {
        [JsonProperty("contents")]
        public List<Content> Contents { get; set; } = new List<Content>();

        [JsonProperty("expiry_time")]
        public DateTime ExpirationTime { get; set; }

        [JsonProperty("extractor_details")]
        public Extractor Extractor { get; set; }

        [JsonProperty("factory_details")]
        public Factory Factory { get; set; }

        [JsonProperty("install_time")]
        public DateTime InstallTime { get; set; }

        [JsonProperty("last_cycle_start")]
        public string LastCycleStart { get; set; }

        [JsonProperty("latitude")]
        public decimal Latitude { get; set; }

        [JsonProperty("longitude")]
        public decimal Longitude { get; set; }

        [JsonProperty("pin_id")]
        public long PinId { get; set; }

        [JsonProperty("schematic_id")]
        public long SchematicId { get; set; }

        [JsonProperty("type_id")]
        public long TypeId { get; set; }
    }

    public class Route
    {
        [JsonProperty("content_type_id")]
        public long ContentTypeId { get; set; }

        [JsonProperty("destination_pin_id")]
        public long DestinationPinId { get; set; }

        [JsonProperty("quantity")]
        public long Quantity { get; set; }

        [JsonProperty("route_id")]
        public long RouteId { get; set; }

        [JsonProperty("source_pin_id")]
        public long SourcePinId { get; set; }

        [JsonProperty("waypoints")]
        public long[] Waypoints { get; set; }
    }
    #endregion

    #region Level 2 nested classes
    public class Content
    {
        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("type_id")]
        public long TypeId { get; set; }
    }

    public class Extractor
    {
        [JsonProperty("cycle_time")]
        public int CycleTime { get; set; }

        [JsonProperty("head_radius")]
        public decimal HeadRadius { get; set; }

        [JsonProperty("heads")]
        public List<Head> Heads { get; set; } = new List<Head>();

        [JsonProperty("product_type_id")]
        public long ProductTypeId { get; set; }

        [JsonProperty("qty_per_cycle")]
        public int QuantityPerCycle { get; set; }
    }
    #endregion

    #region Level 3 nested classes
    public class Factory
    {
        [JsonProperty("schematic_id")]
        public long SchematicId { get; set; }
    }

    public class Head
    {
        [JsonProperty("head_id")]
        public long Id { get; set; }

        [JsonProperty("latitude")]
        public decimal Latitude { get; set; }

        [JsonProperty("longitude")]
        public decimal Longitude { get; set; }
    }
    #endregion
}
