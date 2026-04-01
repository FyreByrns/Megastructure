using System.Security.AccessControl;

namespace TileBased;

struct Tilemap {
    public Dictionary<TilemapCoordinate, Tile> Tiles;

    public Tile[,] GetWithinRect(TilemapCoordinate topLeft, int width, int height) {
        if (Tiles.Count == 0) { return new Tile[0, 0]; }
        Tile[,] result = new Tile[width, height];

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                result[x, y] = Tiles.TryGetValue(new(x + topLeft.X, y + topLeft.Y), out Tile t) ? t : Tile.NONE;
            }
        }

        return result;
    }

    public Tilemap() {
        Tiles = [];
    }

    public Tile this[TilemapCoordinate t] {
        get {
            if (Tiles.TryGetValue(t, out Tile result)) { return result; }
            return Tile.NONE;
        }
        set {
            if (value == Tile.NONE) {
                Tiles.Remove(t);
                return;
            }

            Tiles[t] = value;
        }
    }
}

class TileQuadTree {
/*
    Quadtree that subdivides to the individual tile level.
    Collapses like leaves into larger leaves if possible.

    Worst-case: checkerboard of unlike tiles
    Best-case: large plane of like tiles
*/

    public Tile Tile = Tile.NONE;
    public bool IsBaseLeaf => TL == null || TR == null || BL == null || BR == null;
    TileQuadTree? TL, TR, BL, BR;

    public IntegerAABB Bounds;

    public bool ContainsPoint(TilemapCoordinate coordinate) {
        return Bounds.ContainsPoint(coordinate.X, coordinate.Y);
    }
    public bool Intersects(IntegerAABB aabb) {
        return Bounds.Intersects(aabb);
    }
    public bool IsCompletelyContainedWithin(IntegerAABB aabb) {
        return aabb.CompletelyContains(Bounds);
    }

    public Tile GetTile(TilemapCoordinate coordinate) {
        if (Bounds.ContainsPoint(coordinate.X, coordinate.Y)) {
            if (IsBaseLeaf) {
                return Tile;
            }

            if (TL.ContainsPoint(coordinate)) { return TL.GetTile(coordinate); }
            if (TR.ContainsPoint(coordinate)) { return TR.GetTile(coordinate); }
            if (BL.ContainsPoint(coordinate)) { return BL.GetTile(coordinate); }
            if (BR.ContainsPoint(coordinate)) { return BR.GetTile(coordinate); }
        }

        return Tile.NONE;
    }

    public bool SetTile(TilemapCoordinate coordinate, Tile tile) {
        if(Bounds.ContainsPoint(coordinate.X, coordinate.Y)) {
            if(IsBaseLeaf) {
                // if this leaf is the base leaf and the tile matches the current tile
                if (Tile == tile) { return true; }


            }
        }

        return false;
    }
}

struct IntegerAABB(int top, int left, int right, int bottom) {
    public int Top = top, Left = left, Right = right, Bottom = bottom;

    public bool ContainsPoint(int x, int y) {
        return !(Top >= y && Left >= x && Right <= x && Bottom <= y);
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
