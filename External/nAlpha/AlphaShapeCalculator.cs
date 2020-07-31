using System;
using System.Collections.Generic;
using System.Linq;

namespace nAlpha
{
    public class AlphaShapeCalculator
    {
        public double Alpha { get; set; }
        public bool CloseShape { get; set; }
        public double Radius => 1 / Alpha;

        private List<Tuple<int, int>> resultingEdges = new List<Tuple<int, int>>();
        private List<Point> resultingVertices = new List<Point>();
        private Point[] points;

        public Shape CalculateShape(Point[] points)
        {
            SetData(points);
            CalculateShape();
            if (CloseShape)
            {
                CloseShapeImpl();
            }
            return GetShape();
        }

        private void CloseShapeImpl()
        {
            var vertexCounter = CountVertices();
            var vertexIndices = vertexCounter.GetIndicesByCount(1);
            AddClosingEdges(vertexIndices);
        }

        private void AddClosingEdges(int[] vertexIndices)
        {
            foreach (var vertexIndex in vertexIndices)
            {
                var nearestPendingVertex = GetNearestPendingVertex(vertexIndices, vertexIndex);
                AddEdge(resultingVertices[vertexIndex], resultingVertices[nearestPendingVertex]);
            }
        }

        private void SetData(Point[] points)
        {
            resultingEdges.Clear();
            resultingVertices.Clear();
            this.points = points;
        }

        private void CalculateShape()
        {
            foreach (var point in points)
            {
                ProcessPoint(point);
            }
        }

        private VertexCounter CountVertices()
        {
            VertexCounter counter = new VertexCounter();

            foreach (var edge in resultingEdges)
            {
                counter.IncreaseForIndex(edge.Item1);
                counter.IncreaseForIndex(edge.Item2);
            }

            return counter;
        }

        private int GetNearestPendingVertex(int[] vertices, int vertexIndex)
        {
            var vertexPoint = GetVertex(vertexIndex);
            var vertexIndicesWithDistance =
                vertices.Where(v => v != vertexIndex).Select(v => new { Index = v, Distance = resultingVertices[v].DistanceTo(vertexPoint) });
            return vertexIndicesWithDistance.Aggregate((a, b) => a.Distance < b.Distance ? a : b).Index;
        }

        private Point GetVertex(int vertexIndex)
        {
            return resultingVertices[vertexIndex];
        }

        private void ProcessPoint(Point point)
        {
            foreach (var otherPoint in NearbyPoints(point))
            {
                Tuple<Point, Point> alphaDiskCenters = CalculateAlphaDiskCenters(point, otherPoint);

                if (!DoOtherPointsFallWithinDisk(alphaDiskCenters.Item1, point, otherPoint)
                    || !DoOtherPointsFallWithinDisk(alphaDiskCenters.Item2, point, otherPoint))
                {
                    AddEdge(point, otherPoint);
                }
            }
        }

        private bool DoOtherPointsFallWithinDisk(Point center, Point p1, Point p2)
        {
            return NearbyPoints(center).Count(p => p != p1 && p != p2) > 0;
        }

        private void AddEdge(Point p1, Point p2)
        {
            int indexP1;
            int indexP2;

            indexP1 = AddVertex(p1);
            indexP2 = AddVertex(p2);

            AddEdge(indexP1, indexP2);
        }

        private void AddEdge(int indexP1, int indexP2)
        {
            if (!resultingEdges.Contains(new Tuple<int, int>(indexP1, indexP2))
                && !resultingEdges.Contains(new Tuple<int, int>(indexP2, indexP1)))
                resultingEdges.Add(new Tuple<int, int>(indexP1, indexP2));
        }

        private int AddVertex(Point p)
        {
            int index;
            if (!resultingVertices.Contains(p))
            {
                resultingVertices.Add(p);
            }
            index = resultingVertices.IndexOf(p);
            return index;
        }

        private Point[] NearbyPoints(Point point)
        {
            var nearbyPoints = points.Where(p => p.DistanceTo(point) <= Radius && p != point).ToArray();
            return nearbyPoints;

            //return points;
        }

        private Tuple<Point, Point> CalculateAlphaDiskCenters(Point p1, Point p2)
        {
            double distanceBetweenPoints = p1.DistanceTo(p2);
            double distanceFromConnectionLine = Math.Sqrt(Radius * Radius - distanceBetweenPoints * distanceBetweenPoints / 4);

            Point centerOfConnectionLine = p1.CenterTo(p2);
            Point vector = p1.VectorTo(p2);

            return GetAlphaDiskCenters(vector, centerOfConnectionLine, distanceFromConnectionLine);
        }

        private static Tuple<Point, Point> GetAlphaDiskCenters(Point vector, Point center, double distanceFromConnectionLine)
        {
            Point normalVector = new Point(vector.Y, -vector.X);
            return
                new Tuple<Point, Point>(
                    new Point(center.X + normalVector.X * distanceFromConnectionLine,
                        center.Y + normalVector.Y * distanceFromConnectionLine),
                    new Point(center.X - normalVector.X * distanceFromConnectionLine,
                        center.Y - normalVector.Y * distanceFromConnectionLine));
        }

        private Shape GetShape()
        {
            return new Shape(resultingVertices.ToArray(), resultingEdges.ToArray());
        }
    }
}