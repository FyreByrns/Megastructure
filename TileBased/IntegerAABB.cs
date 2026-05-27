namespace TileBased;

struct IntegerAABB(int left, int top, int right, int bottom) {
    public int Top = top, Left = left, Right = right, Bottom = bottom;

    public readonly int Width => Right - Left;
    public readonly int Height => Bottom - Top;

    public readonly int MidX => Left + (Width / 2);
    public readonly int MidY => Top + (Height / 2);

    public bool ContainsPoint(int x, int y) {
        return x >= Left && y >= Top && x < Right && y < Bottom;
        //return !(Top >= y && Left >= x && Right <= x && Bottom <= y);
    }

    /// <summary>
    /// Whether this AABB overlaps <paramref name="other"/> in any way.
    /// </summary>
    public bool Intersects(IntegerAABB other) {
        return !(Top > other.Bottom || Left > other.Right || Right < other.Left || Bottom < other.Top);
    }

    public bool CompletelyContains(IntegerAABB other) {
        return (Top <= other.Top && Left <= other.Left && Right >= other.Right && Bottom >= other.Bottom);
    }
}
