using System.Windows.Media;
using SMT.EVEData;

namespace SMT.Utils
{
    /// <summary>
    /// Pre-calculated rendering information for system data overlays.
    /// This class performs expensive calculations once and caches the results for efficient batched rendering.
    /// </summary>
    public class SystemDataRenderInfo
    {
        public double X { get; }
        public double Z { get; }
        public double DataScale { get; }
        public Brush DataBrush { get; }

        public SystemDataRenderInfo(
            EVEData.System system,
            double esiOverlayScale,
            bool showNPCKills,
            bool showPodKills,
            bool showShipKills,
            bool showShipJumps,
            bool showRattingDataAsDelta,
            bool showNegativeRattingDelta,
            Brush defaultDataBrush,
            Brush positiveDeltaBrush,
            Brush negativeDeltaBrush)
        {
            X = system.UniverseX;
            Z = system.UniverseY;

            // Calculate data scale based on active data type
            DataScale = CalculateDataScale(system, esiOverlayScale, showNPCKills, showPodKills, showShipKills, showShipJumps, showRattingDataAsDelta, showNegativeRattingDelta);

            // Determine brush based on data type and delta settings
            DataBrush = DetermineBrush(system, showNPCKills, showRattingDataAsDelta, showNegativeRattingDelta, defaultDataBrush, positiveDeltaBrush, negativeDeltaBrush);
        }

        private double CalculateDataScale(
            EVEData.System system,
            double esiOverlayScale,
            bool showNPCKills,
            bool showPodKills,
            bool showShipKills,
            bool showShipJumps,
            bool showRattingDataAsDelta,
            bool showNegativeRattingDelta)
        {
            double dataScale = 0;

            if (showNPCKills)
            {
                dataScale = system.NPCKillsLastHour * esiOverlayScale * 0.05f;

                if (showRattingDataAsDelta)
                {
                    if (!showNegativeRattingDelta)
                    {
                        dataScale = System.Math.Max(0, system.NPCKillsDeltaLastHour) * esiOverlayScale * 0.05f;
                    }
                    else
                    {
                        dataScale = System.Math.Abs(system.NPCKillsDeltaLastHour) * esiOverlayScale * 0.05f;
                    }
                }
            }

            if (showPodKills)
            {
                dataScale = system.PodKillsLastHour * esiOverlayScale * 2f;
            }

            if (showShipKills)
            {
                dataScale = system.ShipKillsLastHour * esiOverlayScale * 1f;
            }

            if (showShipJumps)
            {
                dataScale = system.JumpsLastHour * esiOverlayScale * 0.1f;
            }

            return dataScale;
        }

        private Brush DetermineBrush(
            EVEData.System system,
            bool showNPCKills,
            bool showRattingDataAsDelta,
            bool showNegativeRattingDelta,
            Brush defaultBrush,
            Brush positiveBrush,
            Brush negativeBrush)
        {
            // Only apply delta coloring for NPC kills with delta mode enabled
            if (showNPCKills && showRattingDataAsDelta && showNegativeRattingDelta)
            {
                if (system.NPCKillsDeltaLastHour > 0)
                {
                    return positiveBrush;
                }
                else if (system.NPCKillsDeltaLastHour < 0)
                {
                    return negativeBrush;
                }
            }

            return defaultBrush;
        }
    }
}
