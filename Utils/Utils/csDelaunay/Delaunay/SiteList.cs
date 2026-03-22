using Utils.csDelaunay.Geom;

namespace Utils.csDelaunay.Delaunay
{
    public class SiteList
    {
        private readonly List<Site> sites;
        private int currentIndex;
        private bool sorted;

        public SiteList()
        {
            sites = new List<Site>();
            sorted = false;
        }

        public int Add(Site site)
        {
            sorted = false;
            sites.Add(site);
            return sites.Count;
        }

        public List<Circle> Circles()
        {
            var circles = new List<Circle>();
            foreach (var site in sites)
            {
                float radius = 0;
                var nearestEdge = site.NearestEdge();

                if (!nearestEdge.IsPartOfConvexHull()) radius = nearestEdge.SitesDistance() * 0.5f;
                circles.Add(new Circle(site.x, site.y, radius));
            }

            return circles;
        }

        public int Count()
        {
            return sites.Count;
        }

        public void Dispose()
        {
            sites.Clear();
        }

        public Rectf GetSitesBounds()
        {
            if (!sorted)
            {
                SortList();
                ResetListIndex();
            }

            float xmin, xmax, ymin, ymax;
            if (sites.Count == 0) return Rectf.zero;
            xmin = float.MaxValue;
            xmax = float.MinValue;
            foreach (var site in sites)
            {
                if (site.x < xmin) xmin = site.x;
                if (site.x > xmax) xmax = site.x;
            }

            // here's where we assume that the sites have been sorted on y:
            ymin = sites[0].y;
            ymax = sites[sites.Count - 1].y;

            return new Rectf(xmin, ymin, xmax - xmin, ymax - ymin);
        }

        public Site Next()
        {
            if (!sorted) throw new Exception("SiteList.Next(): sites have not been sorted");
            if (currentIndex < sites.Count) return sites[currentIndex++];

            return null;
        }

        public List<List<Vector2f>> Regions(Rectf plotBounds)
        {
            var regions = new List<List<Vector2f>>();
            foreach (var site in sites) regions.Add(site.Region(plotBounds));
            return regions;
        }

        public void ResetListIndex()
        {
            currentIndex = 0;
        }

        public List<Vector2f> SiteCoords()
        {
            var coords = new List<Vector2f>();
            foreach (var site in sites) coords.Add(site.Coord);

            return coords;
        }

        /*
         *
         * @return the largest circle centered at each site that fits in its region;
         * if the region is infinite, return a circle of radius 0.
         */

        public void SortList()
        {
            Site.SortSites(sites);
            sorted = true;
        }
    }
}