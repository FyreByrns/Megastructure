namespace TileBased;

struct TilestackCoordinate(int x, int y, int z) {
    public TilemapCoordinate LayerCoordinate = new(x, y);
    public int Elevation = z;

    public TilestackCoordinate OnlyX => new(LayerCoordinate.X, 0, 0);
    public TilestackCoordinate OnlyY => new(0, LayerCoordinate.Y, 0);
    public TilestackCoordinate OnlyZ => new(0, 0, Elevation);

    public static bool operator== (TilestackCoordinate lhs, TilestackCoordinate rhs) {
        return lhs.LayerCoordinate == rhs.LayerCoordinate && lhs.Elevation == rhs.Elevation;
    }
    public static bool operator !=(TilestackCoordinate lhs, TilestackCoordinate rhs) {
        return lhs.LayerCoordinate != rhs.LayerCoordinate || lhs.Elevation != rhs.Elevation;
    }

    public static TilestackCoordinate operator +(TilestackCoordinate a, TilestackCoordinate b) {
        return new(a.LayerCoordinate.X + b.LayerCoordinate.X, a.LayerCoordinate.Y + b.LayerCoordinate.Y, a.Elevation + b.Elevation);
    }
}
