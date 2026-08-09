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
        /// <summary>Scale world coords to Clipper integer space. Cells are snapped to 1.0.</summary>
        private const double Scale = 100.0;

        /// <summary>Default inset in map units so adjacent same-standing alliances show a background gap.</summary>
        public const double DefaultInsetDelta = -1.75;

        public static PathGeometry UnionToPathGeometry(IEnumerable<IReadOnlyList<Vector2>> cellRings, double insetDelta = DefaultInsetDelta)
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

            Paths64 united = Clipper.PolyTreeToPaths64(tree);
            if(united == null || united.Count == 0)
            {
                return null;
            }

            if(insetDelta != 0.0)
            {
                // InflatePaths delta is in the same integer space as Path64 coordinates.
                united = Clipper.InflatePaths(united, insetDelta * Scale, JoinType.Round, EndType.Polygon);
                if(united == null || united.Count == 0)
                {
                    return null;
                }
            }

            PathGeometry geometry = new PathGeometry
            {
                FillRule = System.Windows.Media.FillRule.EvenOdd
            };
            AppendPaths(united, geometry);

            return geometry.Figures.Count > 0 ? geometry : null;
        }

        private static void AppendPaths(Paths64 paths, PathGeometry geometry)
        {
            foreach(Path64 polygon in paths)
            {
                if(polygon == null || polygon.Count < 3)
                {
                    continue;
                }

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
        }

        private static Point ToPoint(Point64 p)
        {
            return new Point(p.X / Scale, p.Y / Scale);
        }
    }
}
