namespace TileBased;

class TileQuadTree {
    /*
        Quadtree that subdivides to the individual tile level.
        Collapses like leaves into larger leaves if possible.

        Worst-case: checkerboard of unlike tiles
        Best-case: large plane of like tiles
    */

    public Tile this[TilemapCoordinate t] {
        get {
            return GetTile(t);
        }
        set {
            SetTile(t, value);
        }
    }

    public Tile Tile = Tile.NONE;
    TileQuadTree? TL, TR, BL, BR;
    public bool HasLeaves => !HasNoLeaves;
    public bool HasNoLeaves => TL == null || TR == null || BL == null || BR == null;

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

    public bool Subdivide() {
        if (Bounds.Width == 1) { // if it's already the smallest
            return false;
        } // can't divide

        if (HasNoLeaves) { // only subdivide if there are no leaves yet
            TL = new(new(Bounds.Left, Bounds.Top, Bounds.MidX, Bounds.MidY));
            TR = new(new(Bounds.MidX, Bounds.Top, Bounds.Right, Bounds.MidY));
            BL = new(new(Bounds.Left, Bounds.MidY, Bounds.MidX, Bounds.Bottom));
            BR = new(new(Bounds.MidX, Bounds.MidY, Bounds.Right, Bounds.Bottom));

            return true;
        }

        return false;
    }

    public Tile GetTile(TilemapCoordinate coordinate) {


        if (ContainsPoint(coordinate)) { // if the coordinate belongs to this quad
            if (HasNoLeaves) { // if there are no leaves
                return Tile;
            } // just return the contained tile
            // .. otherwise query subquads
            if (TL!.ContainsPoint(coordinate)) { return TL.GetTile(coordinate); }
            if (TR!.ContainsPoint(coordinate)) { return TR.GetTile(coordinate); }
            if (BL!.ContainsPoint(coordinate)) { return BL.GetTile(coordinate); }
            if (BR!.ContainsPoint(coordinate)) { return BR.GetTile(coordinate); }
        }

        return Tile.NONE;
    }

    public bool SetTile(TilemapCoordinate coordinate, Tile tile) {
        if (ContainsPoint(coordinate)) { // if the coordinate belongs to this quad
            if (HasNoLeaves) { // if this quad has no leaves
                if (Tile == tile) { // if the tile is already the tile
                    Console.WriteLine($"Quad hit with {tile} at ({coordinate.X}, {coordinate.Y}) in [({Bounds.Left}, {Bounds.Top}), ({Bounds.Right}, {Bounds.Bottom}) w{Bounds.Width}]");
                    return true;
                } // don't do anything, it's already good

                if (Bounds.Width == 1) { // and this quad is as small as possible
                    Tile = tile;
                    //Console.WriteLine($"Set {tile} at ({coordinate.X}, {coordinate.Y}) in [({Bounds.Left}, {Bounds.Top}), ({Bounds.Right}, {Bounds.Bottom})] (width {Bounds.Width})");
                    return true;

                } // set the tile
                else { // if the quad has no leaves and is not as small as possible
                    Subdivide();
                    TL!.Tile = Tile;
                    TR!.Tile = Tile;
                    BL!.Tile = Tile;
                    BR!.Tile = Tile;
                } // subdivide
            }

            // attempt to add to children
            if (TL!.ContainsPoint(coordinate)) { TL.SetTile(coordinate, tile); return true; }
            if (TR!.ContainsPoint(coordinate)) { TR.SetTile(coordinate, tile); return true; }
            if (BL!.ContainsPoint(coordinate)) { BL.SetTile(coordinate, tile); return true; }
            if (BR!.ContainsPoint(coordinate)) { BR.SetTile(coordinate, tile); return true; }
        }

        // total failure
        Console.WriteLine($"Failure to set {tile} at ({coordinate.X}, {coordinate.Y}) [({Bounds.Left}, {Bounds.Top}), ({Bounds.Right}, {Bounds.Bottom}) w{Bounds.Width}]");
        return false;
    }

    public void Remove(TilemapCoordinate coordinate) {
        SetTile(coordinate, Tile.NONE);
        Collapse();
    }

    /// <summary>
    /// Attempt to collapse like children into a larger node.
    /// Returns whether or not a collapse occurred. 
    /// </summary>
    public bool Collapse() {
        if (HasNoLeaves) { return false; }

        if (HasLeaves) {
            TL!.Collapse();
            TR!.Collapse();
            BL!.Collapse();
            BR!.Collapse();

            if (TL.HasLeaves || TR.HasLeaves || BL.HasLeaves || BR.HasLeaves) {
                return false;
            }

            if (TL.Tile == TR.Tile &&
                TL.Tile == BL.Tile &&
                TL.Tile == BR.Tile) {

                Tile = TL.Tile;
                TL = null;
                TR = null;
                BL = null;
                BR = null;

                return true;
            }
        }

        return false;
    }

    public TileQuadTree(IntegerAABB bounds) {
        Bounds = bounds;
    }
}
