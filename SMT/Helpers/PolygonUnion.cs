using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using Clipper2Lib;

namespace SMT.Helpers
{
    /// <summary>
    /// Boolean-unions simple polygon rings (e.g. Voronoi cells) into a WPF PathGeometry,
    /// preserving disconnected components and holes.
    /// </summary>
    internal static class PolygonUnion
    {
        /// <summary>Scale world coords to Clipper integer space. Cells are snapped to 2.5.</summary>
        private const double Scale = 100.0;

        public static PathGeometry UnionToPathGeometry(IEnumerable<IReadOnlyList<Vector2>> cellRings)
        {
            if(cellRings == null)
            {
                return null;
            }

            Paths64 subjects = new Paths64();
            foreach(IReadOnlyList<Vector2> ring in cellRings)
            {
                if(ring == null || ring.Count < 3)
                {
                    continue;
                }

                Path64 path = new Path64(ring.Count);
                foreach(Vector2 p in ring)
                {
                    path.Add(new Point64(p.X * Scale, p.Y * Scale));
                }

                subjects.Add(path);
            }

            if(subjects.Count == 0)
            {
                return null;
            }

            Clipper64 clipper = new Clipper64();
            clipper.AddSubject(subjects);
            PolyTree64 tree = new PolyTree64();
            if(!clipper.Execute(ClipType.Union, Clipper2Lib.FillRule.NonZero, tree) || tree.Count == 0)
            {
                return null;
            }

            PathGeometry geometry = new PathGeometry
            {
                FillRule = System.Windows.Media.FillRule.EvenOdd
            };
            AppendPolyTree(tree, geometry);

            return geometry.Figures.Count > 0 ? geometry : null;
        }

        private static void AppendPolyTree(PolyPath64 node, PathGeometry geometry)
        {
            for(int i = 0; i < node.Count; i++)
            {
                PolyPath64 child = node.Child(i);
                Path64 polygon = child.Polygon;
                if(polygon != null && polygon.Count >= 3)
                {
                    PathFigure figure = new PathFigure
                    {
                        IsClosed = true,
                        IsFilled = true,
                        StartPoint = ToPoint(polygon[0])
                    };

                    for(int j = 1; j < polygon.Count; j++)
                    {
                        figure.Segments.Add(new LineSegment(ToPoint(polygon[j]), true));
                    }

                    geometry.Figures.Add(figure);
                }

                AppendPolyTree(child, geometry);
            }
        }

        private static Point ToPoint(Point64 p)
        {
            return new Point(p.X / Scale, p.Y / Scale);
        }
    }
}
