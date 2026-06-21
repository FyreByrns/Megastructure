global using static TileBased.Engine;
global using static TileBased.InputStrings;

using PixelEngine;

using Hexa.NET.ImGui;

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

    /// <summary>
    /// What inputs to update each 
    /// </summary>
    public Dictionary<string, Input> BoundInputs = [];
    public void Bind(string name, Input input) {
        BoundInputs[name] = input;
    }
    public bool GetInput(string name, out Input? input) {
        return BoundInputs.TryGetValue(name, out input);
    }
    public bool Pressed(string name) => GetInput(name, out var i) && i!.Pressed;
    public bool JustPressed(string name) => GetInput(name, out var i) && i!.JustPressed;
    public bool Released(string name) => GetInput(name, out var i) && i!.Released;
    public bool JustReleased(string name) => GetInput(name, out var i) && i!.JustReleased;

    public Tool CurrentTool = new BasicTileCursor();
    public bool ShowToolInputHints = true;

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

    public record TileShaperParams(Tile Target, TilemapStack world, int RandomSeed);
    public delegate bool TileShaper(TileShaperParams parameters, TilestackCoordinate start, TilestackCoordinate end, params int[] extraParams);
    public bool ChangeTiles(TileShaper shape, TilestackCoordinate start, TilestackCoordinate end, Tile to, bool silent = false) {
        bool anyFailures = false;

        shape(new(to, CurrentFloor, 0), start, end);

        return !anyFailures;
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
        TickInputs();
        TickTool();

        if (TargetedLayer < 0) { TargetedLayer = 0; }
        if (TargetedLayer >= CurrentFloor.Layers) { TargetedLayer = CurrentFloor.TopLayer; }

        Scroll = 0;

        TickEntities();

        // apply all tile changes in a single step
        ApplyTileChanges();
    }
    void TickInputs() {
        // for tracking which binds have been consumed by exclusive inputs
        HashSet<Mouse> consumedMouseBinds = [];
        HashSet<Scroll> consumedScrollBinds = [];
        HashSet<Key> consumedKeyBinds = [];

        foreach (var input in BoundInputs.Values) {
            input.LastState = input.Active;

            bool anyBindsMet = false;

            bool allMouseBindsMet = true;
            bool allScrollBindsMet = true;
            bool allKeyBindsMet = true;

            foreach (var m in input.MouseBinds ?? []) {
                if (consumedMouseBinds.Contains(m)) {
                    goto inputInactive;
                }

                if (GetMouse(m).Down) { anyBindsMet = true; }
                if (!GetMouse(m).Down) {
                    allMouseBindsMet = false;
                }
            }
            foreach (var s in input.ScrollBinds ?? []) {
                if (consumedScrollBinds.Contains(s)) {
                    goto inputInactive;
                }

                if ((int)s == Scroll) { anyBindsMet = true; }
                if (!((int)s == Scroll)) {
                    allScrollBindsMet = false;
                }
            }
            foreach (var k in input.KeyBinds ?? []) {
                if (consumedKeyBinds.Contains(k)) {
                    goto inputInactive;
                }

                if (GetKey(k).Down) { anyBindsMet = true; }
                if (!GetKey(k).Down) {
                    allKeyBindsMet = false;
                }
            }

            if (anyBindsMet) {
                if (input.Mode == Input.InputMode.All && allMouseBindsMet && allScrollBindsMet && allKeyBindsMet) {
                    goto inputActive;
                }

                if (input.Mode == Input.InputMode.Any) {
                    goto inputActive;
                }
            }

        // end-of-check, if nothing has explicitly set the input state to active,
        // .. this falls into inputInactive, setting the input to active and continuing.
        // the only way to activate the input is an explicit goto inputActive.
        inputInactive:
            input.Active = false;
            continue;
        inputActive:
            if (input.Exclusive) {
                foreach (var m in input.MouseBinds ?? [])
                    consumedMouseBinds.Add(m);
                foreach (var s in input.ScrollBinds ?? [])
                    consumedScrollBinds.Add(s);
                foreach (var k in input.KeyBinds ?? [])
                    consumedKeyBinds.Add(k);
            }
            input.Active = true;
        }
    }
    void TickTool() {
        CurrentTool!.Tick();
    }
    void TickEntities() {
        foreach (Entity entity in Entities.Entities) {
            entity.Tick();
        }
    }

    void Render(float elapsed) {
        DrawTiles();
        DrawSprite(default, Background);

        //ImGui.ShowDemoWindow();

        ImGui.Begin("WOAGFG");
        ImGui.Text("testing");
        ImGui.End();

        DrawUI();
    }
    void DrawTiles(bool force = false) {
        if (force || ScreenTilesChanged) {
            PushDrawTarget(Background);

            int tileMouseX = MouseX / TileSize;
            int tileMouseY = MouseY / TileSize;

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
            PopDrawTarget();
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

        // tool hint
        DrawText(new(1, 1), CurrentTool!.Hint, Pixel.Presets.White);
        // tool actions
        if (ShowToolInputHints) {
            int actionIndex = 0;
            foreach (var (input, action) in CurrentTool.HintsByInput) {
                DrawText(new(2, 8 + 8 * actionIndex), $"{input}: {action}", Pixel.Presets.White);
                actionIndex++;
            }
        }

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
        INTERACT_WORLD_MAIN.Bind(new(Mouse.Left));
        INTERACT_WORLD_SECONDARY.Bind(new(Mouse.Right));

        MOVE_UP.Bind(new(Key.W));
        MOVE_DOWN.Bind(new(Key.S));
        MOVE_LEFT.Bind(new(Key.A));
        MOVE_RIGHT.Bind(new(Key.D));

        LAYER_UP.Bind(new(PixelEngine.Scroll.Down, Key.Q) { Mode = Input.InputMode.Any });
        LAYER_DOWN.Bind(new(PixelEngine.Scroll.Up, Key.E) { Mode = Input.InputMode.Any });

        MODIFIER_MAIN.Bind(new(Key.Control));
        MODIFIER_SECONDARY.Bind(new(Key.SemiColon));

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

        void centerCamera(Entity e) {
            TilestackCoordinate baseCoordinate = e.BaseCoordinate;

            TileScreenMinX = baseCoordinate.LayerCoordinate.X - ScreenTileWidth / 2;
            TileScreenMinY = baseCoordinate.LayerCoordinate.Y - ScreenTileHeight / 2;
            TileScreenMaxX = TileScreenMinX + ScreenTileWidth;
            TileScreenMaxY = TileScreenMinY + ScreenTileHeight;
        }

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

        if (MOVE_UP.Pressed()) { yMove += -1; }
        if (MOVE_DOWN.Pressed()) { yMove += +1; }
        if (MOVE_LEFT.Pressed()) { xMove += -1; }
        if (MOVE_RIGHT.Pressed()) { xMove += +1; }

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
