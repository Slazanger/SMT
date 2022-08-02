using Newtonsoft.Json;

namespace ESI.NET.Models.Character
{
    public class Travel
    {
        [JsonProperty("acceleration_gate_activations")]
        public long AccelerationGateActivations { get; set; }

        [JsonProperty("align_to")]
        public long AlignTo { get; set; }

        [JsonProperty("distance_warped_high_sec")]
        public long DistanceWarpedHighSec { get; set; }

        [JsonProperty("distance_warped_low_sec")]
        public long DistanceWarpedLowSec { get; set; }

        [JsonProperty("distance_warped_null_sec")]
        public long DistanceWarpedNullSec { get; set; }

        [JsonProperty("distance_warped_wormhole")]
        public long DistanceWarpedWormhole { get; set; }

        [JsonProperty("docks_high_sec")]
        public long DocksHighSec { get; set; }

        [JsonProperty("docks_low_sec")]
        public long DocksLowSec { get; set; }

        [JsonProperty("docks_null_sec")]
        public long DocksNullSec { get; set; }

        [JsonProperty("jumps_stargate_high_sec")]
        public long JumpsStargateHighSec { get; set; }

        [JsonProperty("jumps_stargate_low_sec")]
        public long JumpsStargateLowSec { get; set; }

        [JsonProperty("jumps_stargate_null_sec")]
        public long JumpsStargateNullSec { get; set; }

        [JsonProperty("jumps_wormhole")]
        public long JumpsWormhole { get; set; }

        [JsonProperty("warps_high_sec")]
        public long WarpsHighSec { get; set; }

        [JsonProperty("warps_low_sec")]
        public long WarpsLowSec { get; set; }

        [JsonProperty("warps_null_sec")]
        public long WarpsNullSec { get; set; }

        [JsonProperty("warps_to_bookmark")]
        public long WarpsToBookmark { get; set; }

        [JsonProperty("warps_to_celestial")]
        public long WarpsToCelestial { get; set; }

        [JsonProperty("warps_to_fleet_member")]
        public long WarpsToFleetMember { get; set; }

        [JsonProperty("warps_to_scan_result")]
        public long WarpsToScanResult { get; set; }

        [JsonProperty("warps_wormhole")]
        public long WarpsWormhole { get; set; }
    }
}
