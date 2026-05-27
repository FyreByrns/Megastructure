namespace TileBased;

struct Tilemap {
    //public Dictionary<TilemapCoordinate, Tile> Tiles;
    public TileQuadTree Tiles;

    public Tile[,] GetWithinRect(TilemapCoordinate topLeft, int width, int height) {
        //if (Tiles.Count == 0) { return new Tile[0, 0]; }
        Tile[,] result = new Tile[width, height];

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                result[x, y] = Tiles.GetTile(new(x + topLeft.X, y + topLeft.Y));
            }
        }

        return result;
    }

    public Tilemap() {
        Tiles = new(new(short.MinValue, short.MinValue, short.MaxValue, short.MaxValue));
    }

    public Tile this[TilemapCoordinate t] {
        get {
            return Tiles.GetTile(t);
        }
        set {
            Tiles.SetTile(t, value);
        }
    }
}
