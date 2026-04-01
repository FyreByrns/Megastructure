global using static TileBased.TileManager;

using PixelEngine;

namespace TileBased;

static class TileManager {
    public static int TileSize = 3;

    public static Sprite Empty = new(TileSize, TileSize);
    public static Dictionary<Tile, Sprite> TileGraphics = [];
    public static Dictionary<Tile, Sprite[,]> MultiSprites = [];

    public static void RegisterTileGraphic(Tile tile, Sprite sprite) {
        // if the tile has multiple textures, use those
        if (sprite.Width != TileSize || sprite.Height != TileSize) {
            int xVariants = sprite.Width / TileSize;
            int yVariants = sprite.Height / TileSize;
            MultiSprites[tile] = new Sprite[xVariants, yVariants];

            // copy the subsprites of the overall tile sprite into their own tile-sized sprites
            for (int x = 0; x < xVariants; x++) {
                for (int y = 0; y < yVariants; y++) {
                    Sprite littlePiece = new(TileSize, TileSize);
                    Instance.PushDrawTarget(littlePiece);
                    Instance.DrawPartialSprite(Point.Origin, sprite, new(x * TileSize, y * TileSize), TileSize, TileSize);
                    Instance.PopDrawTarget();

                    MultiSprites[tile][x, y] = littlePiece;
                }
            }

            return;
        }

        TileGraphics[tile] = sprite;
    }

    public static bool HasMultiSprite(Tile tile) {
        return MultiSprites.ContainsKey(tile);
    }
    public static int GetXRepeats(Tile tile) {
        if (HasMultiSprite(tile)) {
            return MultiSprites[tile].GetLength(0);
        }
        return 0;
    }
    public static int GetYRepeats(Tile tile) {
        if (HasMultiSprite(tile)) {
            return MultiSprites[tile].GetLength(1);
        }
        return 0;
    }

    /// <summary>
    /// Get the sprite corresponding to the given tile.
    /// </summary>
    public static Sprite GetSprite(Tile tile, int x = 0, int y = 0) {
        if (HasMultiSprite(tile)) {
            int xTilingIndex = x % GetXRepeats(tile);
            int yTilingIndex = y % GetYRepeats(tile);

            return MultiSprites[tile][xTilingIndex, yTilingIndex];
        }

        if (TileGraphics.TryGetValue(tile, out Sprite? value)) {
            return value;
        }

        return Empty;
    }

    // TODO: actually look up whether or not the tile is movable
    public static bool Movable(Tile tile) {
        return true;
    }

    // TODO: have multiple tiletypes that count as "empty" for moving through them
    public static bool CanMoveThrough(Tile tile) {
        return tile == Tile.NONE;
    }
}
