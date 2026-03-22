namespace Utils.csDelaunay.Delaunay
{
    public class Triangle
    {
        public Triangle(Site a, Site b, Site c)
        {
            Sites = new List<Site>();
            Sites.Add(a);
            Sites.Add(b);
            Sites.Add(c);
        }

        public List<Site> Sites { get; }

        public void Dispose()
        {
            Sites.Clear();
        }
    }
}