using Utils.csDelaunay.Delaunay;

namespace Utils.csDelaunay.Geom
{
    public class LineSegment
    {
        public Vector2f p0;

        public Vector2f p1;

        public LineSegment(Vector2f p0, Vector2f p1)
        {
            this.p0 = p0;
            this.p1 = p1;
        }

        public static float CompareLengths(LineSegment edge0, LineSegment edge1)
        {
            return -CompareLengths_MAX(edge0, edge1);
        }

        public static float CompareLengths_MAX(LineSegment segment0, LineSegment segment1)
        {
            var length0 = (segment0.p0 - segment0.p1).magnitude;
            var length1 = (segment1.p0 - segment1.p1).magnitude;
            if (length0 < length1) return 1;
            if (length0 > length1) return -1;
            return 0;
        }

        public static List<LineSegment> VisibleLineSegments(List<Edge> edges)
        {
            var segments = new List<LineSegment>();

            foreach (var edge in edges)
                if (edge.Visible())
                {
                    var p1 = edge.ClippedEnds[LR.LEFT];
                    var p2 = edge.ClippedEnds[LR.RIGHT];
                    segments.Add(new LineSegment(p1, p2));
                }

            return segments;
        }
    }
}