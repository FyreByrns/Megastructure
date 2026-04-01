namespace TileBased;

class Entity {
    public List<TilestackCoordinate> OwnedTiles = [];
    public List<Action<Entity>> TickActions = [];

    public TilestackCoordinate BaseCoordinate => OwnedTiles[0];

    public void Tick() {
        foreach (var action in TickActions) {
            action(this);
        }
    }

    /// <summary>
    /// Move the tiles associated with this entity.
    /// </summary>
    public void MoveTiles(TilestackCoordinate change, TilemapStack map) {
        TilestackCoordinate[] ignore = OwnedTiles.ToArray();

        foreach (var coord in OwnedTiles) {
            if (!map.CanMoveTile(coord, coord + change, ignore)) {
                if (map.CanMoveTile(coord, coord + change.OnlyX, ignore)) {
                    change = change.OnlyX;
                    continue;
                }
                if (map.CanMoveTile(coord, coord + change.OnlyY, ignore)) {
                    change = change.OnlyY;
                    continue;
                }
                if (map.CanMoveTile(coord, coord + change.OnlyZ, ignore)) {
                    change = change.OnlyZ;
                    continue;
                }
                return;
            }
        }

        map.MoveAllBy(OwnedTiles, change);

        for (int i = 0; i < OwnedTiles.Count; i++) {
            OwnedTiles[i] += change;
        }
    }

    public static Entity Construct(params TilestackCoordinate[] fromTiles) {
        Entity result = new();
        result.OwnedTiles = [.. fromTiles];
        return result;
    }
}
