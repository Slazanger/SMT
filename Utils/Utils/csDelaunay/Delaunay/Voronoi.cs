using Utils.csDelaunay.Geom;

namespace Utils.csDelaunay.Delaunay
{
    public class Voronoi
    {
        private readonly Random weigthDistributor;

        // TODO generalize this so it doesn't have to be a rectangle;
        // then we can make the fractal voronois-within-voronois

        private SiteList sites;
        private List<Triangle> triangles;

        public Voronoi(List<Vector2f> points, Rectf plotBounds)
        {
            weigthDistributor = new Random();
            Init(points, plotBounds);
        }

        public Voronoi(List<Vector2f> points, Rectf plotBounds, int lloydIterations)
        {
            weigthDistributor = new Random();
            Init(points, plotBounds);
            LloydRelaxation(lloydIterations);
        }

        public List<Edge> Edges { get; private set; }

        public Rectf PlotBounds { get; private set; }

        public Dictionary<Vector2f, Site> SitesIndexedByLocation { get; private set; }

        public static int CompareByYThenX(Site s1, Site s2)
        {
            if (s1.y < s2.y) return -1;
            if (s1.y > s2.y) return 1;
            if (s1.x < s2.x) return -1;
            if (s1.x > s2.x) return 1;
            return 0;
        }

        public static int CompareByYThenX(Site s1, Vector2f s2)
        {
            if (s1.y < s2.y) return -1;
            if (s1.y > s2.y) return 1;
            if (s1.x < s2.x) return -1;
            if (s1.x > s2.x) return 1;
            return 0;
        }

        public List<Circle> Circles()
        {
            return sites.Circles();
        }

        public void Dispose()
        {
            sites.Dispose();
            sites = null;

            foreach (var t in triangles) t.Dispose();
            triangles.Clear();

            foreach (var e in Edges) e.Dispose();
            Edges.Clear();

            PlotBounds = Rectf.zero;
            SitesIndexedByLocation.Clear();
            SitesIndexedByLocation = null;
        }

        public List<Edge> HullEdges()
        {
            return Edges.FindAll(edge => edge.IsPartOfConvexHull());
        }

        public List<Vector2f> HullPointsInOrder()
        {
            var hullEdges = HullEdges();

            var points = new List<Vector2f>();
            if (hullEdges.Count == 0) return points;

            var reorderer = new EdgeReorderer(hullEdges, typeof(Site));
            hullEdges = reorderer.Edges;
            var orientations = reorderer.EdgeOrientations;
            reorderer.Dispose();

            LR orientation;
            for (var i = 0; i < hullEdges.Count; i++)
            {
                var edge = hullEdges[i];
                orientation = orientations[i];
                points.Add(edge.Site(orientation).Coord);
            }

            return points;
        }

        public void LloydRelaxation(int nbIterations)
        {
            // Reapeat the whole process for the number of iterations asked
            for (var i = 0; i < nbIterations; i++)
            {
                var newPoints = new List<Vector2f>();
                // Go thourgh all sites
                sites.ResetListIndex();
                var site = sites.Next();

                while (site != null)
                {
                    // Loop all corners of the site to calculate the centroid
                    var region = site.Region(PlotBounds);
                    if (region.Count < 1)
                    {
                        site = sites.Next();
                        continue;
                    }

                    var centroid = Vector2f.zero;
                    float signedArea = 0;
                    float x0 = 0;
                    float y0 = 0;
                    float x1 = 0;
                    float y1 = 0;
                    float a = 0;
                    // For all vertices except last
                    for (var j = 0; j < region.Count - 1; j++)
                    {
                        x0 = region[j].x;
                        y0 = region[j].y;
                        x1 = region[j + 1].x;
                        y1 = region[j + 1].y;
                        a = x0 * y1 - x1 * y0;
                        signedArea += a;
                        centroid.x += (x0 + x1) * a;
                        centroid.y += (y0 + y1) * a;
                    }

                    // Do last vertex
                    x0 = region[region.Count - 1].x;
                    y0 = region[region.Count - 1].y;
                    x1 = region[0].x;
                    y1 = region[0].y;
                    a = x0 * y1 - x1 * y0;
                    signedArea += a;
                    centroid.x += (x0 + x1) * a;
                    centroid.y += (y0 + y1) * a;

                    signedArea *= 0.5f;
                    centroid.x /= 6 * signedArea;
                    centroid.y /= 6 * signedArea;
                    // Move site to the centroid of its Voronoi cell
                    newPoints.Add(centroid);
                    site = sites.Next();
                }

                // Between each replacement of the cendroid of the cell,
                // we need to recompute Voronoi diagram:
                var origPlotBounds = PlotBounds;
                Dispose();
                Init(newPoints, origPlotBounds);
            }
        }

        public List<Vector2f> NeighborSitesForSite(Vector2f coord)
        {
            var points = new List<Vector2f>();
            Site site;
            if (SitesIndexedByLocation.TryGetValue(coord, out site))
            {
                var sites = site.NeighborSites();
                foreach (var neighbor in sites) points.Add(neighbor.Coord);
            }

            return points;
        }

        public List<Vector2f> Region(Vector2f p)
        {
            Site site;
            if (SitesIndexedByLocation.TryGetValue(p, out site)) return site.Region(PlotBounds);

            return new List<Vector2f>();
        }

        public List<List<Vector2f>> Regions()
        {
            return sites.Regions(PlotBounds);
        }

        public List<Vector2f> SiteCoords()
        {
            return sites.SiteCoords();
        }

        public List<LineSegment> VoronoiBoundarayForSite(Vector2f coord)
        {
            return LineSegment.VisibleLineSegments(Edge.SelectEdgesForSitePoint(coord, Edges));
        }

        public List<LineSegment> VoronoiDiagram()
        {
            return LineSegment.VisibleLineSegments(Edges);
        }

        private void AddSite(Vector2f p, int index)
        {
            var weigth = (float)weigthDistributor.NextDouble() * 100;
            var site = Site.Create(p, index, weigth);
            sites.Add(site);
            SitesIndexedByLocation[p] = site;
        }

        private void AddSites(List<Vector2f> points)
        {
            for (var i = 0; i < points.Count; i++) AddSite(points[i], i);
        }

        private void FortunesAlgorithm()
        {
            Site newSite, bottomSite, topSite, tempSite;
            Vertex v, vertex;
            var newIntStar = Vector2f.zero;
            LR leftRight;
            Halfedge lbnd, rbnd, llbnd, rrbnd, bisector;
            Edge edge;

            var dataBounds = sites.GetSitesBounds();

            var sqrtSitesNb = (int)Math.Sqrt(sites.Count() + 4);
            var heap = new HalfedgePriorityQueue(dataBounds.y, dataBounds.height, sqrtSitesNb);
            var edgeList = new EdgeList(dataBounds.x, dataBounds.width, sqrtSitesNb);
            var halfEdges = new List<Halfedge>();
            var vertices = new List<Vertex>();

            var bottomMostSite = sites.Next();
            newSite = sites.Next();

            while (true)
            {
                if (!heap.Empty()) newIntStar = heap.Min();

                if (newSite != null &&
                    (heap.Empty() || CompareByYThenX(newSite, newIntStar) < 0))
                {
                    // New site is smallest
                    //Debug.Log("smallest: new site " + newSite);

                    // Step 8:
                    lbnd = edgeList.EdgeListLeftNeighbor(newSite.Coord); // The halfedge just to the left of newSite
                    //UnityEngine.Debug.Log("lbnd: " + lbnd);
                    rbnd = lbnd.edgeListRightNeighbor; // The halfedge just to the right
                    //UnityEngine.Debug.Log("rbnd: " + rbnd);
                    bottomSite = RightRegion(lbnd, bottomMostSite); // This is the same as leftRegion(rbnd)
                    // This Site determines the region containing the new site
                    //UnityEngine.Debug.Log("new Site is in region of existing site: " + bottomSite);

                    // Step 9
                    edge = Edge.CreateBisectingEdge(bottomSite, newSite);
                    //UnityEngine.Debug.Log("new edge: " + edge);
                    Edges.Add(edge);

                    bisector = Halfedge.Create(edge, LR.LEFT);
                    halfEdges.Add(bisector);
                    // Inserting two halfedges into edgelist constitutes Step 10:
                    // Insert bisector to the right of lbnd:
                    EdgeList.Insert(lbnd, bisector);

                    // First half of Step 11:
                    if ((vertex = Vertex.Intersect(lbnd, bisector)) != null)
                    {
                        vertices.Add(vertex);
                        heap.Remove(lbnd);
                        lbnd.vertex = vertex;
                        lbnd.ystar = vertex.y + newSite.Dist(vertex);
                        heap.Insert(lbnd);
                    }

                    lbnd = bisector;
                    bisector = Halfedge.Create(edge, LR.RIGHT);
                    halfEdges.Add(bisector);
                    // Second halfedge for Step 10::
                    // Insert bisector to the right of lbnd:
                    EdgeList.Insert(lbnd, bisector);

                    // Second half of Step 11:
                    if ((vertex = Vertex.Intersect(bisector, rbnd)) != null)
                    {
                        vertices.Add(vertex);
                        bisector.vertex = vertex;
                        bisector.ystar = vertex.y + newSite.Dist(vertex);
                        heap.Insert(bisector);
                    }

                    newSite = sites.Next();
                }
                else if (!heap.Empty())
                {
                    // Intersection is smallest
                    lbnd = heap.ExtractMin();
                    llbnd = lbnd.edgeListLeftNeighbor;
                    rbnd = lbnd.edgeListRightNeighbor;
                    rrbnd = rbnd.edgeListRightNeighbor;
                    bottomSite = LeftRegion(lbnd, bottomMostSite);
                    topSite = RightRegion(rbnd, bottomMostSite);
                    // These three sites define a Delaunay triangle
                    // (not actually using these for anything...)
                    // triangles.Add(new Triangle(bottomSite, topSite, RightRegion(lbnd, bottomMostSite)));

                    v = lbnd.vertex;
                    v.SetIndex();
                    lbnd.edge.SetVertex(lbnd.leftRight, v);
                    rbnd.edge.SetVertex(rbnd.leftRight, v);
                    EdgeList.Remove(lbnd);
                    heap.Remove(rbnd);
                    EdgeList.Remove(rbnd);
                    leftRight = LR.LEFT;
                    if (bottomSite.y > topSite.y)
                    {
                        tempSite = bottomSite;
                        bottomSite = topSite;
                        topSite = tempSite;
                        leftRight = LR.RIGHT;
                    }

                    edge = Edge.CreateBisectingEdge(bottomSite, topSite);
                    Edges.Add(edge);
                    bisector = Halfedge.Create(edge, leftRight);
                    halfEdges.Add(bisector);
                    EdgeList.Insert(llbnd, bisector);
                    edge.SetVertex(LR.Other(leftRight), v);
                    if ((vertex = Vertex.Intersect(llbnd, bisector)) != null)
                    {
                        vertices.Add(vertex);
                        heap.Remove(llbnd);
                        llbnd.vertex = vertex;
                        llbnd.ystar = vertex.y + bottomSite.Dist(vertex);
                        heap.Insert(llbnd);
                    }

                    if ((vertex = Vertex.Intersect(bisector, rrbnd)) != null)
                    {
                        vertices.Add(vertex);
                        bisector.vertex = vertex;
                        bisector.ystar = vertex.y + bottomSite.Dist(vertex);
                        heap.Insert(bisector);
                    }
                }
                else
                {
                    break;
                }
            }

            // Heap should be empty now
            heap.Dispose();
            edgeList.Dispose();

            foreach (var halfedge in halfEdges) halfedge.ReallyDispose();
            halfEdges.Clear();

            // we need the vertices to clip the edges
            foreach (var e in Edges) e.ClipVertices(PlotBounds);
            // But we don't actually ever use them again!
            foreach (var ve in vertices) ve.Dispose();
            vertices.Clear();
        }

        private void Init(List<Vector2f> points, Rectf plotBounds)
        {
            sites = new SiteList();
            SitesIndexedByLocation = new Dictionary<Vector2f, Site>();
            AddSites(points);
            this.PlotBounds = plotBounds;
            triangles = new List<Triangle>();
            Edges = new List<Edge>();

            FortunesAlgorithm();
        }

        /*
        public List<LineSegment> DelaunayLinesForSite(Vector2f coord) {
            return DelaunayLinesForEdges(Edge.SelectEdgesForSitePoint(coord, edges));
        }*/
        /*
        public List<LineSegment> Hull() {
            return DelaunayLinesForEdges(HullEdges());
        }*/

        private static Site LeftRegion(Halfedge he, Site bottomMostSite)
        {
            var edge = he.edge;
            if (edge == null) return bottomMostSite;
            return edge.Site(he.leftRight);
        }

        private static Site RightRegion(Halfedge he, Site bottomMostSite)
        {
            var edge = he.edge;
            if (edge == null) return bottomMostSite;
            return edge.Site(LR.Other(he.leftRight));
        }
    }
}