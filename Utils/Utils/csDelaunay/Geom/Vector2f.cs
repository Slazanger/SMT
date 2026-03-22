// Recreation of the UnityEngine.Vector2, so it can be used in other thread

namespace Utils.csDelaunay.Geom;

public struct Vector2f
{
    public static readonly Vector2f down = new(0, -1);
    public static readonly Vector2f left = new(-1, 0);
    public static readonly Vector2f one = new(1, 1);
    public static readonly Vector2f right = new(1, 0);
    public static readonly Vector2f up = new(0, 1);
    public static readonly Vector2f zero = new(0, 0);
    public float x, y;

    public Vector2f(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public Vector2f(double x, double y)
    {
        this.x = (float)x;
        this.y = (float)y;
    }

    public float magnitude => (float)Math.Sqrt(x * x + y * y);

    public static float DistanceSquare(Vector2f a, Vector2f b)
    {
        var cx = b.x - a.x;
        var cy = b.y - a.y;
        return cx * cx + cy * cy;
    }

    public static Vector2f Max(Vector2f a, Vector2f b)
    {
        return new Vector2f(Math.Max(a.x, b.x), Math.Max(a.y, b.y));
    }

    public static Vector2f Min(Vector2f a, Vector2f b)
    {
        return new Vector2f(Math.Min(a.x, b.x), Math.Min(a.y, b.y));
    }

    public static Vector2f Normalize(Vector2f a)
    {
        var magnitude = a.magnitude;
        return new Vector2f(a.x / magnitude, a.y / magnitude);
    }

    public static Vector2f operator -(Vector2f a, Vector2f b)
    {
        return new Vector2f(a.x - b.x, a.y - b.y);
    }

    public static bool operator !=(Vector2f a, Vector2f b)
    {
        return a.x != b.x ||
               a.y != b.y;
    }

    public static Vector2f operator *(Vector2f a, int i)
    {
        return new Vector2f(a.x * i, a.y * i);
    }

    public static Vector2f operator +(Vector2f a, Vector2f b)
    {
        return new Vector2f(a.x + b.x, a.y + b.y);
    }

    public static bool operator ==(Vector2f a, Vector2f b)
    {
        return a.x == b.x &&
               a.y == b.y;
    }

    public float DistanceSquare(Vector2f v)
    {
        return DistanceSquare(this, v);
    }

    public override bool Equals(object other)
    {
        if (!(other is Vector2f)) return false;
        var v = (Vector2f)other;
        return x == v.x &&
               y == v.y;
    }

    public override int GetHashCode()
    {
        return x.GetHashCode() ^ (y.GetHashCode() << 2);
    }

    public void Normalize()
    {
        var magnitude = this.magnitude;
        x /= magnitude;
        y /= magnitude;
    }

    public override string ToString()
    {
        return string.Format("[Vector2f]" + x + "," + y);
    }
}