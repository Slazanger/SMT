namespace Utils.csDelaunay.Geom;

public struct Rectf
{
    public static readonly Rectf one = new(1, 1, 1, 1);
    public static readonly Rectf zero = new(0, 0, 0, 0);
    public float x, y, width, height;

    public Rectf(float x, float y, float width, float height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    public float bottom => y + height;

    public Vector2f bottomRight => new(right, bottom);

    public float left => x;

    public float right => x + width;

    public float top => y;

    public Vector2f topLeft => new(left, top);
}