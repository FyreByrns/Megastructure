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
    public bool HoldsBaseLeaves => !IsBaseLeaf && (TL.IsBaseLeaf && TR.IsBaseLeaf && BL.IsBaseLeaf && BR.IsBaseLeaf);
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
        if (Bounds.ContainsPoint(coordinate.X, coordinate.Y)) {
            if (IsBaseLeaf) {
                // if this leaf is the base leaf and the tile matches the current tile
                if (Tile == tile) {
                    return true;
                } // nothing needs to happen

                // if this leaf is the base leaf and its size is 1
                if (Bounds.Width == 1 && Bounds.Height == 1) {
                    Tile = tile;
                    return true;
                } // set this tile to the input tile

                // otherwise, need to split
                TL = new(new(Bounds.Left, Bounds.Top, Bounds.MidX, Bounds.MidY));
                TR = new(new(Bounds.MidX, Bounds.Top, Bounds.Right, Bounds.MidY));
                BL = new(new(Bounds.Left, Bounds.MidY, Bounds.MidX, Bounds.Bottom));
                BR = new(new(Bounds.MidX, Bounds.MidY, Bounds.Right, Bounds.Bottom));
            }

            // try to add to children
            if (TL!.SetTile(coordinate, tile)) { return true; }
            if (TR!.SetTile(coordinate, tile)) { return true; }
            if (BL!.SetTile(coordinate, tile)) { return true; }
            if (BR!.SetTile(coordinate, tile)) { return true; }

            // all failed, so the tile has not been set
            return false;
        }

        return false;
    }

    /// <summary>
    /// Attempt to collapse like children into a larger node.
    /// Returns whether or not a collapse occurred. 
    /// </summary>
    public bool Collapse() {
        if (IsBaseLeaf) { return false; }

        if(HoldsBaseLeaves) {
            if (TL.IsBaseLeaf && TR.IsBaseLeaf && BL.IsBaseLeaf && BR.IsBaseLeaf) {

            }
        }

        while (TL.Collapse() || TR.Collapse() || BL.Collapse() || BR.Collapse()) { 
            // loop while collapses may happen.
        }

        return false;
    }

    public TileQuadTree(IntegerAABB bounds) {
        Bounds = bounds;
    }
}

struct IntegerAABB(int left, int top, int right, int bottom) {
    public int Top = top, Left = left, Right = right, Bottom = bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;

    public int MidX => Left + Width / 2;
    public int MidY => Top + Height / 2;

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
