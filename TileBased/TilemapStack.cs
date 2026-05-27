namespace TileBased;

class TilemapStack {
    /// <summary>
    /// Tilemaps stacked spatially. 0 is the bottom, Length - 1 is the top.
    /// </summary>
    public Tilemap[] Stack;
    public Tile this[TilestackCoordinate index] {
        get {
            return Stack[index.Elevation][index.LayerCoordinate];
        }
        set {
            if (index.Elevation >= Layers) {
                return;
            }

            Stack[index.Elevation][index.LayerCoordinate] = value;
        }
    }

    public int Layers => Stack.Length;
    public int TopLayer => Stack.Length - 1;
    public bool HasCeiling = true;

    public IEnumerable<Tilemap> TopToBottom() {
        for (int i = Stack.Length - 1; i >= 0; i--) {
            yield return Stack[i];
        }
    }
    public IEnumerable<Tilemap> BottomToTop() {
        for (int i = 0; i < Stack.Length; i++) {
            yield return Stack[i];
        }
    }

    public record TileChange(TilestackCoordinate At, Tile To);
    public List<TilestackCoordinate> Removals = [];
    public List<TileChange> Changes = [];

    /// <summary>
    /// Register a tilemap change to happen at the end of this tick.
    /// </summary>
    public void RegisterChange(TilestackCoordinate at, Tile to) {
        if (to == Tile.NONE) {
            Removals.Add(at);
            return;
        }

        Changes.Add(new(at, to));
    }
    public void ApplyChanges() {
        if (Removals.Count <= 0 && Changes.Count <= 0) {
            return;
        } // don't bother trying to loop if there's nothing to loop over

        foreach (var removal in Removals) {
            this[removal] = Tile.NONE;
        }
        Removals.Clear();

        foreach (var change in Changes) {
            this[change.At] = change.To;
        }
        Changes.Clear();
        foreach (Tilemap t in Stack) {
            t.Tiles.Collapse();
        }
    }

    public bool CanMoveTile(TilestackCoordinate from, TilestackCoordinate to, params TilestackCoordinate[] ignoreTiles) {
        foreach (var coord in ignoreTiles) {
            if (coord == to)
                return true;
        }
        return TileManager.Movable(Stack[from.Elevation][from.LayerCoordinate]) && TileManager.CanMoveThrough(Stack[to.Elevation][to.LayerCoordinate]);
    }

    public void Erase(TilestackCoordinate where) {
        Stack[where.Elevation].Tiles.Remove(where.LayerCoordinate);
    }

    // TODO: Push tiles, emit events like damage, sound, etc.
    public void Change(TilestackCoordinate where, Tile to, bool silent = false) {
        RegisterChange(where, to);
    }

    public void Move(TilestackCoordinate from, TilestackCoordinate to) {
        if (this[from] == Tile.NONE) {
            return;
        }

        RegisterChange(from, Tile.NONE);
        RegisterChange(to, this[from]);
    }

    public void MoveAllBy(IEnumerable<TilestackCoordinate> tiles, TilestackCoordinate change) {
        foreach (var tile in tiles) {
            Move(tile, tile + change);
        }
    }

    /// <summary>
    /// Find the elevation the first solid tile from the top is in.
    /// "raycasts" downwards and returns the first hit z.
    /// </summary>
    public int FindTop(int x, int y) {
        int result = TopLayer;

        for (int i = result; i >= 0; i--) {
            Tile at = Stack[i][new(x, y)];

            if (!TileManager.CanMoveThrough(at)) {
                return i;
            }

            result = i;
        }

        return result;
    }

    public TilemapStack(int layers) {
        Stack = new Tilemap[layers];
        for (int i = 0; i < layers; i++) {
            Stack[i] = new Tilemap();
        }
    }
}
