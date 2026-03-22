using Utils.csDelaunay.Geom;

namespace Utils.csDelaunay.Delaunay
{
    public class BoundsCheck
    {
        public const int BOTTOM = 2;
        public const int LEFT = 4;
        public const int RIGHT = 8;

        public const int TOP = 1;
        /*
         *
         * @param point
         * @param bounds
         * @return an int with the appropriate bits set if the Point lies on the corresponding bounds lines
         */

        public static int Check(Vector2f point, Rectf bounds)
        {
            var value = 0;
            if (point.x == bounds.left) value |= LEFT;
            if (point.x == bounds.right) value |= RIGHT;
            if (point.y == bounds.top) value |= TOP;
            if (point.y == bounds.bottom) value |= BOTTOM;

            return value;
        }
    }

    public class Site : ICoord
    {
        private const float EPSILON = 0.005f;
        private static readonly Queue<Site> pool = new();

        private Vector2f coord;

        // which end of each edge hooks up with the previous edge in edges:
        private List<LR> edgeOrientations;

        // The edges that define this Site's Voronoi region:

        // ordered list of points that define the region clipped to bounds:
        private List<Vector2f> region;

        public Site(Vector2f p, int index, float weigth)
        {
            Init(p, index, weigth);
        }

        public List<Edge> Edges { get; private set; }

        public int SiteIndex { get; set; }

        public float Weigth { get; private set; }

        public float x => coord.x;

        public float y => coord.y;

        public Vector2f Coord
        {
            get => coord;
            set => coord = value;
        }

        public static Site Create(Vector2f p, int index, float weigth)
        {
            if (pool.Count > 0) return pool.Dequeue().Init(p, index, weigth);

            return new Site(p, index, weigth);
        }

        public static void SortSites(List<Site> sites)
        {
            sites.Sort(delegate(Site s0, Site s1)
            {
                var returnValue = Voronoi.CompareByYThenX(s0, s1);

                int tempIndex;

                if (returnValue == -1)
                {
                    if (s0.SiteIndex > s1.SiteIndex)
                    {
                        tempIndex = s0.SiteIndex;
                        s0.SiteIndex = s1.SiteIndex;
                        s1.SiteIndex = tempIndex;
                    }
                }
                else if (returnValue == 1)
                {
                    if (s1.SiteIndex > s0.SiteIndex)
                    {
                        tempIndex = s1.SiteIndex;
                        s1.SiteIndex = s0.SiteIndex;
                        s0.SiteIndex = tempIndex;
                    }
                }

                return returnValue;
            });
        }

        public void AddEdge(Edge edge)
        {
            Edges.Add(edge);
        }

        public static int Compare(Site s1, Site s2)
        {
            return s1.CompareTo(s2);
        }

        public int CompareTo(Site s1)
        {
            var returnValue = Voronoi.CompareByYThenX(this, s1);

            int tempIndex;

            if (returnValue == -1)
            {
                if (SiteIndex > s1.SiteIndex)
                {
                    tempIndex = SiteIndex;
                    SiteIndex = s1.SiteIndex;
                    s1.SiteIndex = tempIndex;
                }
            }
            else if (returnValue == 1)
            {
                if (s1.SiteIndex > SiteIndex)
                {
                    tempIndex = s1.SiteIndex;
                    s1.SiteIndex = SiteIndex;
                    SiteIndex = tempIndex;
                }
            }

            return returnValue;
        }

        public void Dispose()
        {
            Clear();
            pool.Enqueue(this);
        }

        public float Dist(ICoord p)
        {
            return (Coord - p.Coord).magnitude;
        }

        public Edge NearestEdge()
        {
            Edges.Sort(Edge.CompareSitesDistances);
            return Edges[0];
        }

        public List<Site> NeighborSites()
        {
            if (Edges == null || Edges.Count == 0) return new List<Site>();
            if (edgeOrientations == null) ReorderEdges();
            var list = new List<Site>();
            foreach (var edge in Edges) list.Add(NeighborSite(edge));
            return list;
        }

        public List<Vector2f> Region(Rectf clippingBounds)
        {
            if (Edges == null || Edges.Count == 0) return new List<Vector2f>();
            if (edgeOrientations == null)
            {
                ReorderEdges();
                region = ClipToBounds(clippingBounds);
                if (new Polygon(region).PolyWinding() == Winding.CLOCKWISE) region.Reverse();
            }

            return region;
        }

        public override string ToString()
        {
            return "Site " + SiteIndex + ": " + coord;
        }

        private static bool CloseEnough(Vector2f p0, Vector2f p1)
        {
            return (p0 - p1).magnitude < EPSILON;
        }

        private void Clear()
        {
            if (Edges != null)
            {
                Edges.Clear();
                Edges = null;
            }

            if (edgeOrientations != null)
            {
                edgeOrientations.Clear();
                edgeOrientations = null;
            }

            if (region != null)
            {
                region.Clear();
                region = null;
            }
        }

        private List<Vector2f> ClipToBounds(Rectf bounds)
        {
            var points = new List<Vector2f>();
            var n = Edges.Count;
            var i = 0;
            Edge edge;

            while (i < n && !Edges[i].Visible()) i++;

            if (i == n)
                // No edges visible
                return new List<Vector2f>();
            edge = Edges[i];
            var orientation = edgeOrientations[i];
            points.Add(edge.ClippedEnds[orientation]);
            points.Add(edge.ClippedEnds[LR.Other(orientation)]);

            for (var j = i + 1; j < n; j++)
            {
                edge = Edges[j];
                if (!edge.Visible()) continue;
                Connect(ref points, j, bounds);
            }

            // Close up the polygon by adding another corner point of the bounds if needed:
            Connect(ref points, i, bounds, true);

            return points;
        }

        private void Connect(ref List<Vector2f> points, int j, Rectf bounds, bool closingUp = false)
        {
            var rightPoint = points[points.Count - 1];
            var newEdge = Edges[j];
            var newOrientation = edgeOrientations[j];

            // The point that must be conected to rightPoint:
            var newPoint = newEdge.ClippedEnds[newOrientation];

            if (!CloseEnough(rightPoint, newPoint))
            {
                // The points do not coincide, so they must have been clipped at the bounds;
                // see if they are on the same border of the bounds:
                if (rightPoint.x != newPoint.x && rightPoint.y != newPoint.y)
                {
                    // They are on different borders of the bounds;
                    // insert one or two corners of bounds as needed to hook them up:
                    // (NOTE this will not be correct if the region should take up more than
                    // half of the bounds rect, for then we will have gone the wrong way
                    // around the bounds and included the smaller part rather than the larger)
                    var rightCheck = BoundsCheck.Check(rightPoint, bounds);
                    var newCheck = BoundsCheck.Check(newPoint, bounds);
                    float px, py;
                    if ((rightCheck & BoundsCheck.RIGHT) != 0)
                    {
                        px = bounds.right;

                        if ((newCheck & BoundsCheck.BOTTOM) != 0)
                        {
                            py = bounds.bottom;
                            points.Add(new Vector2f(px, py));
                        }
                        else if ((newCheck & BoundsCheck.TOP) != 0)
                        {
                            py = bounds.top;
                            points.Add(new Vector2f(px, py));
                        }
                        else if ((newCheck & BoundsCheck.LEFT) != 0)
                        {
                            if (rightPoint.y - bounds.y + newPoint.y - bounds.y < bounds.height)
                                py = bounds.top;
                            else
                                py = bounds.bottom;
                            points.Add(new Vector2f(px, py));
                            points.Add(new Vector2f(bounds.left, py));
                        }
                    }
                    else if ((rightCheck & BoundsCheck.LEFT) != 0)
                    {
                        px = bounds.left;

                        if ((newCheck & BoundsCheck.BOTTOM) != 0)
                        {
                            py = bounds.bottom;
                            points.Add(new Vector2f(px, py));
                        }
                        else if ((newCheck & BoundsCheck.TOP) != 0)
                        {
                            py = bounds.top;
                            points.Add(new Vector2f(px, py));
                        }
                        else if ((newCheck & BoundsCheck.RIGHT) != 0)
                        {
                            if (rightPoint.y - bounds.y + newPoint.y - bounds.y < bounds.height)
                                py = bounds.top;
                            else
                                py = bounds.bottom;
                            points.Add(new Vector2f(px, py));
                            points.Add(new Vector2f(bounds.right, py));
                        }
                    }
                    else if ((rightCheck & BoundsCheck.TOP) != 0)
                    {
                        py = bounds.top;

                        if ((newCheck & BoundsCheck.RIGHT) != 0)
                        {
                            px = bounds.right;
                            points.Add(new Vector2f(px, py));
                        }
                        else if ((newCheck & BoundsCheck.LEFT) != 0)
                        {
                            px = bounds.left;
                            points.Add(new Vector2f(px, py));
                        }
                        else if ((newCheck & BoundsCheck.BOTTOM) != 0)
                        {
                            if (rightPoint.x - bounds.x + newPoint.x - bounds.x < bounds.width)
                                px = bounds.left;
                            else
                                px = bounds.right;
                            points.Add(new Vector2f(px, py));
                            points.Add(new Vector2f(px, bounds.bottom));
                        }
                    }
                    else if ((rightCheck & BoundsCheck.BOTTOM) != 0)
                    {
                        py = bounds.bottom;

                        if ((newCheck & BoundsCheck.RIGHT) != 0)
                        {
                            px = bounds.right;
                            points.Add(new Vector2f(px, py));
                        }
                        else if ((newCheck & BoundsCheck.LEFT) != 0)
                        {
                            px = bounds.left;
                            points.Add(new Vector2f(px, py));
                        }
                        else if ((newCheck & BoundsCheck.TOP) != 0)
                        {
                            if (rightPoint.x - bounds.x + newPoint.x - bounds.x < bounds.width)
                                px = bounds.left;
                            else
                                px = bounds.right;
                            points.Add(new Vector2f(px, py));
                            points.Add(new Vector2f(px, bounds.top));
                        }
                    }
                }

                if (closingUp)
                    // newEdge's ends have already been added
                    return;
                points.Add(newPoint);
            }

            var newRightPoint = newEdge.ClippedEnds[LR.Other(newOrientation)];
            if (!CloseEnough(points[0], newRightPoint)) points.Add(newRightPoint);
        }

        private Site Init(Vector2f p, int index, float weigth)
        {
            coord = p;
            SiteIndex = index;
            this.Weigth = weigth;
            Edges = new List<Edge>();
            region = null;

            return this;
        }

        private Site NeighborSite(Edge edge)
        {
            if (this == edge.LeftSite) return edge.RightSite;
            if (this == edge.RightSite) return edge.LeftSite;
            return null;
        }

        private void ReorderEdges()
        {
            var reorderer = new EdgeReorderer(Edges, typeof(Vertex));
            Edges = reorderer.Edges;
            edgeOrientations = reorderer.EdgeOrientations;
            reorderer.Dispose();
        }
    }
}