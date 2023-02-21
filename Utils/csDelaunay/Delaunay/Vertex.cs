﻿namespace csDelaunay
{
    public class Vertex : ICoord
    {
        public static readonly Vertex VERTEX_AT_INFINITY = new Vertex(float.NaN, float.NaN);

        #region Pool

        private static int nVertices = 0;
        private static Queue<Vertex> pool = new Queue<Vertex>();

        private static Vertex Create(float x, float y)
        {
            if (float.IsNaN(x) || float.IsNaN(y))
            {
                return VERTEX_AT_INFINITY;
            }
            if (pool.Count > 0)
            {
                return pool.Dequeue().Init(x, y);
            }
            else
            {
                return new Vertex(x, y);
            }
        }

        #endregion Pool

        #region Object

        private Vector2f coord;
        private int vertexIndex;

        public Vertex(float x, float y)
        {
            Init(x, y);
        }

        public Vector2f Coord
        { get { return coord; } set { coord = value; } }

        public int VertexIndex
        { get { return vertexIndex; } }

        public float x
        { get { return coord.x; } }

        public float y
        { get { return coord.y; } }

        public static Vertex Intersect(Halfedge halfedge0, Halfedge halfedge1)
        {
            Edge edge, edge0, edge1;
            Halfedge halfedge;
            float determinant, intersectionX, intersectionY;
            bool rightOfSite;

            edge0 = halfedge0.edge;
            edge1 = halfedge1.edge;
            if (edge0 == null || edge1 == null)
            {
                return null;
            }
            if (edge0.RightSite == edge1.RightSite)
            {
                return null;
            }

            determinant = (edge0.a * edge1.b) - (edge0.b * edge1.a);
            if (Math.Abs(determinant) < 1E-10)
            {
                // The edges are parallel
                return null;
            }

            intersectionX = ((edge0.c * edge1.b) - (edge1.c * edge0.b)) / determinant;
            intersectionY = ((edge1.c * edge0.a) - (edge0.c * edge1.a)) / determinant;

            if (Voronoi.CompareByYThenX(edge0.RightSite, edge1.RightSite) < 0)
            {
                halfedge = halfedge0;
                edge = edge0;
            }
            else
            {
                halfedge = halfedge1;
                edge = edge1;
            }
            rightOfSite = intersectionX >= edge.RightSite.x;
            if ((rightOfSite && halfedge.leftRight == LR.LEFT) ||
                (!rightOfSite && halfedge.leftRight == LR.RIGHT))
            {
                return null;
            }

            return Vertex.Create(intersectionX, intersectionY);
        }

        public void Dispose()
        {
            coord = Vector2f.zero;
            pool.Enqueue(this);
        }

        public void SetIndex()
        {
            vertexIndex = nVertices++;
        }

        public override string ToString()
        {
            return "Vertex (" + vertexIndex + ")";
        }

        private Vertex Init(float x, float y)
        {
            coord = new Vector2f(x, y);

            return this;
        }

        /*
		 * This is the only way to make a Vertex
		 *
		 * @param halfedge0
		 * @param halfedge1
		 * @return
		 *
		 */

        #endregion Object
    }
}