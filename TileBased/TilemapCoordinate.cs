namespace TileBased;

struct TilemapCoordinate(int X, int Y) {
    public int X = X;
    public int Y = Y;

    public static bool operator ==(TilemapCoordinate lhs, TilemapCoordinate rhs) {
        return lhs.X == rhs.X && lhs.Y == rhs.Y;
    }
    public static bool operator !=(TilemapCoordinate lhs, TilemapCoordinate rhs) {
        return lhs.X != rhs.X || lhs.Y != rhs.Y;
    }
}
