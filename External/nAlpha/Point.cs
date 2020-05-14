using System;

namespace nAlpha
{
    public struct Point : IEquatable<Point>
    {
        public bool Equals(Point other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Point && Equals((Point)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(Point left, Point right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Point left, Point right)
        {
            return !left.Equals(right);
        }

        public double X { get; private set; }
        public double Y { get; private set; }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"X={X}; Y={Y}";
        }

        public double DistanceTo(Point p)
        {
            double val = (X - p.X) * (X - p.X) + (Y - p.Y) * (Y - p.Y);
            return Math.Sqrt(val);
        }

        public Point CenterTo(Point p)
        {
            return new Point((X + p.X) / 2, (Y + p.Y) / 2);
        }

        public Point VectorTo(Point p)
        {
            double d = DistanceTo(p);
            return new Point((p.X - X) / d,
                (p.Y - Y) / d);
        }
    }
}