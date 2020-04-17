using System;

namespace nAlpha
{
    public class Shape
    {
        public Shape(Point[] vertices, Tuple<int, int>[] edges)
        {
            Vertices = vertices;
            Edges = edges;
        }

        public Point[] Vertices { get; private set; }
        public Tuple<int, int>[] Edges { get; private set; }
    }
}