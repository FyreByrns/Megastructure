global using static TileBased.Engine;

using PixelEngine;

namespace TileBased;

class Engine : Game {
    static Engine _instance;
    public static Engine Instance {
        get => _instance ??= new();
    }

    public float TicksPerSecond = 1f / 60f;
    float _frameAccumulator = 0;
    int Scroll = 0;

    public Sprite Background;

    public int ScreenTileWidth => ScreenWidth / TileSize;
    public int ScreenTileHeight => ScreenHeight / TileSize;
    public int TileMouseX => MouseX / TileSize + TileScreenMinX;
    public int TileMouseY => MouseY / TileSize + TileScreenMinY;

    public int TileScreenMinX = 0;
    public int TileScreenMinY = 0;
    public int TileScreenMaxX = 0;
    public int TileScreenMaxY = 0;
    public IntegerAABB ScreenBounds => new(TileScreenMinX, TileScreenMinY, TileScreenMaxX, TileScreenMaxY);

    bool screenTilesChanging = false;
    public bool ScreenTilesChanged = false;

    /// <summary>
    /// The level the player is currently in.
    /// </summary>
    public TilemapStack CurrentFloor;
    public EntityManager Entities;

    /// <summary>
    /// What layer is currently being targeted.
    /// </summary>
    public int TargetedLayer;

    public void MarkTilesDirty(IEnumerable<TilestackCoordinate> dirtyTiles) {
        foreach (var tile in dirtyTiles) {
            if (ScreenBounds.ContainsPoint(tile.LayerCoordinate.X, tile.LayerCoordinate.Y)) {
                screenTilesChanging = true;
                break;
            }
        }
    }
    public bool ChangeTile(TilestackCoordinate location, Tile to, bool silent = false) {
        try {
            CurrentFloor.Change(location, to, silent);

            if (ScreenBounds.ContainsPoint(location.LayerCoordinate.X, location.LayerCoordinate.Y)) {
                screenTilesChanging = true;
            }
        }
        catch (Exception e) {
            Console.WriteLine($"error changing tile: {e}");
            return false;
        }
        return true;
    }
    public void ApplyTileChanges() {
        CurrentFloor.ApplyChanges();

        if (screenTilesChanging) {
            ScreenTilesChanged = true;
            screenTilesChanging = false;
        }
    }

    public override void OnMouseScroll() {
        Scroll = (int)MouseScroll;
    }
    void Tick(float elapsed) {
        if (GetKey(Key.Q).Down) { Scroll = 1; }
        if (GetKey(Key.E).Down) { Scroll = -1; }
        TargetedLayer = CurrentFloor.FindTop(TileMouseX, TileMouseY);

        if (TargetedLayer < 0) { TargetedLayer = 0; }
        if (TargetedLayer >= CurrentFloor.Layers) { TargetedLayer = CurrentFloor.TopLayer; }

        Scroll = 0;

        TickEntities();

        // interact
        if (GetMouse(Mouse.Left).Down) {
            ChangeTile(new(TileMouseX, TileMouseY, TargetedLayer + 1), Tile.Meat);
        }

        // apply all tile changes in a single step
        ApplyTileChanges();
    }
    void TickEntities() {
        foreach (Entity entity in Entities.Entities) {
            entity.Tick();
        }
    }

    void Render(float elapsed) {
        PushDrawTarget(Background);
        DrawTiles();
        PopDrawTarget();
        DrawSprite(default, Background);

        DrawUI();
    }
    void DrawTiles() {
        int tileMouseX = MouseX / TileSize;
        int tileMouseY = MouseY / TileSize;

        if (ScreenTilesChanged) {
            int layer = 0;
            foreach (Tilemap map in CurrentFloor.BottomToTop()) {
                Tile[,] rect = map.GetWithinRect(new(TileScreenMinX, TileScreenMinY), TileScreenMaxX - TileScreenMinX, TileScreenMaxY - TileScreenMinY);

                // draw tiles
                for (int x = 0; x < rect.GetLength(0); x++) {
                    for (int y = 0; y < rect.GetLength(1); y++) {
                        Tile tile = rect[x, y];

                        // don't draw empty tiles.
                        if (tile == Tile.NONE) {
                            if (layer == 0) {
                                FillRect(new(x * TileSize, y * TileSize), TileSize, TileSize, Pixel.Empty);
                            }

                            continue;
                        }

                        DrawSprite(new(x * TileSize, y * TileSize), GetSprite(tile, x + TileScreenMinX, y + TileScreenMinY));
                    }
                }
                // draw edges 
                for (int x = 0; x < rect.GetLength(0); x++) {
                    for (int y = 0; y < rect.GetLength(1); y++) {
                        Tile tile = rect[x, y];
                        if (tile == Tile.NONE) { continue; }

                        int up = y - 1;
                        int down = y + 1;
                        int left = x - 1;
                        int right = x + 1;

                        if (up < 0) { up = 0; }
                        while (down >= rect.GetLength(1)) { down--; }
                        if (left < 0) { left = 0; }
                        while (right >= rect.GetLength(0)) { right--; }

                        Tile tUp = rect[x, up];
                        Tile tDown = rect[x, down];
                        Tile tLeft = rect[left, y];
                        Tile tRight = rect[right, y];

                        int l = x * TileSize - 1;
                        int t = y * TileSize - 1;
                        int r = x * TileSize + TileSize;
                        int b = y * TileSize + TileSize;

                        if (tUp != tile) { DrawLine(new(l, t), new(r, t), Pixel.Presets.DarkGrey); }
                        if (tDown != tile) { DrawLine(new(l, b), new(r, b), Pixel.Presets.DarkGrey); }
                        if (tLeft != tile) { DrawLine(new(l, t), new(l, b), Pixel.Presets.DarkGrey); }
                        if (tRight != tile) { DrawLine(new(r, t), new(r, b), Pixel.Presets.DarkGrey); }
                    }
                }

                layer++;
            }

            // reset flag
            ScreenTilesChanged = false;
        }
    }
    void DrawUI() {
        int uiSizeX = 20;
        int uiSizeY = 40;
        FillRect(new(ScreenWidth - uiSizeX, ScreenHeight - uiSizeY), uiSizeX, uiSizeY, Pixel.Presets.DarkBlue);

        Draw(ScreenWidth - 1, ScreenHeight - TargetedLayer - 1, Pixel.Presets.White);
        DrawText(new(ScreenWidth - 10, ScreenHeight - 10), $"{TargetedLayer}", Pixel.Presets.White);

        // cursor
        Draw((MouseX / TileSize * TileSize) + TileSize / 2, (MouseY / TileSize * TileSize) + TileSize / 2, Pixel.Presets.Mint);

        // line
        //DrawLine(new((MouseX / TileSize * TileSize) + TileSize / 2, (MouseY / TileSize * TileSize) + TileSize / 2), new(ScreenWidth / 2, ScreenHeight / 2), Pixel.Presets.White);

        // stack at cursor
        for (int i = 0; i < CurrentFloor.Layers; i++) {
            DrawSprite(new(ScreenWidth - 15, ScreenHeight - TileSize * (i + 1)), GetSprite(CurrentFloor.Stack[i][new(TileMouseX, TileMouseY)]));
            if (i == TargetedLayer) {
                DrawLine(new(ScreenWidth - 16, ScreenHeight - TileSize * (i + 1)), new(ScreenWidth - 16, ScreenHeight - TileSize * i - 1), Pixel.Presets.Mint);
            }
        }
    }

    public override void OnCreate() {
        TileScreenMaxX = ScreenTileWidth;
        TileScreenMaxY = ScreenTileHeight;

        Sprite floor = new(6, 6);
        Pixel colourA = new(30, 30, 60);
        Pixel colourB = new(30, 60, 30);

        Instance.PushDrawTarget(floor);

        for (int x = 0; x < floor.Width; x++) {
            for (int y = 0; y < floor.Height; y++) {
                if ((x / 3 + y / 3) % 2 == 0) {
                    Draw(x, y, colourA);
                }
                else {
                    Draw(x, y, colourB);
                }
            }
        }

        RegisterTileGraphic(Tile.BaseFloorTile, floor);
        Instance.PopDrawTarget();

        Sprite meat = new(3, 3);
        Instance.PushDrawTarget(meat);
        Instance.Clear(Pixel.Presets.DarkRed);
        RegisterTileGraphic(Tile.Meat, meat);
        Instance.PopDrawTarget();

        Background = new(ScreenWidth, ScreenHeight);

        CurrentFloor = new(10);
        for (int x = 0; x < 200; x++) {
            for (int y = 0; y < 200; y++) {
                ChangeTile(new(x, y, 0), Tile.BaseFloorTile);
            }
        }

        for (int x = 0; x < 5; x++) {
            for (int y = 0; y < 30; y++) {
                for (int z = 0; z < 5; z++) {
                    ChangeTile(new(18 + x, 6 + y, z), Tile.BaseFloorTile);
                }
            }
        }

        Entities = new();

        ChangeTile(new(0, 0, 1), Tile.Meat);
        ChangeTile(new(0, 0, 2), Tile.Meat);

        Entity player = Entity.Construct(
            new(0, 0, 1), new(0, 0, 2)
            );
        Action<Entity> centerCamera = e => {
            TilestackCoordinate baseCoordinate = e.BaseCoordinate;

            TileScreenMinX = baseCoordinate.LayerCoordinate.X - ScreenTileWidth / 2;
            TileScreenMinY = baseCoordinate.LayerCoordinate.Y - ScreenTileHeight / 2;
            TileScreenMaxX = TileScreenMinX + ScreenTileWidth;
            TileScreenMaxY = TileScreenMinY + ScreenTileHeight;
        };

        player.TickActions.Add(PlayerMove);
        player.TickActions.Add(centerCamera);

        Entities.Entities.Add(player);
    }

    bool playerMoved = false;
    int _playerMoveAccumulator = 0;
    int PlayerMoveTicks = 10;
    int xMove = 0;
    int yMove = 0;
    public void PlayerMove(Entity entity) {
        _playerMoveAccumulator++;

        if (GetKey(Key.W).Down) { yMove += -1; }
        if (GetKey(Key.S).Down) { yMove += +1; }
        if (GetKey(Key.A).Down) { xMove += -1; }
        if (GetKey(Key.D).Down) { xMove += +1; }

        if (playerMoved && _playerMoveAccumulator <= PlayerMoveTicks / 2) {
            xMove = 0;
            yMove = 0;
        } // don't persist player movement from a previous move

        while (_playerMoveAccumulator >= PlayerMoveTicks) {
            _playerMoveAccumulator -= PlayerMoveTicks;

            playerMoved = false;
            if (xMove != 0 || yMove != 0) {
                entity.MoveTiles(new(Math.Sign(xMove), Math.Sign(yMove), 0), CurrentFloor);
                xMove = 0;
                yMove = 0;
                playerMoved = true;
            }
        }
    }

    public override void OnUpdate(float elapsed) {
        _frameAccumulator += elapsed;
        while (_frameAccumulator >= TicksPerSecond) {
            Tick(elapsed);
            _frameAccumulator -= TicksPerSecond;
        }
        Render(elapsed);
    }

    public Stack<Sprite> DrawTargetStack = new();
    public void PushDrawTarget(Sprite target) {
        DrawTargetStack.Push(DrawTarget);
        DrawTarget = target;
    }
    public void PopDrawTarget() {
        if (DrawTargetStack.TryPop(out Sprite last)) {
            DrawTarget = last;
        }
        else {
            DrawTarget = null;
        }
    }

    public Engine() {
        Construct(400, 225, 4, 4);
    }
}
