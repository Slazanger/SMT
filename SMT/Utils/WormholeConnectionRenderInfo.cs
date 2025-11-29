using System.Windows.Media;
using SMT.EVEData;

namespace SMT.Utils
{
    /// <summary>
    /// Rendering information for wormhole connections (Thera/Turnur)
    /// </summary>
    public class WormholeConnectionRenderInfo
    {
        public EVEData.System System { get; }
        public Brush Brush { get; }
        public Pen Pen { get; }

        public WormholeConnectionRenderInfo(EVEData.System system, Brush brush, Pen pen)
        {
            System = system;
            Brush = brush;
            Pen = pen;
        }
    }
}
