namespace TileBased;

class BasicTileCursor : Tool {
    public override void Tick() {
        Hint = $"Target: {Instance.TileMouseX}, {Instance.TileMouseY}xy {Instance.TargetedLayer}z";

        if (LAYER_UP.JustPressed()) {
            Instance.TargetedLayer++;
        }
        if (LAYER_DOWN.JustPressed()) {
            Instance.TargetedLayer--;
        }
        if (MODIFIER_MAIN.Pressed()) {
            Instance.TargetedLayer = Instance.CurrentFloor.FindTop(Instance.TileMouseX, Instance.TileMouseY);
        }

        if (INTERACT_WORLD_MAIN.Pressed()) {
            Instance.ChangeTile(new(Instance.TileMouseX, Instance.TileMouseY, Instance.TargetedLayer), Tile.Meat);
        }
        if (INTERACT_WORLD_SECONDARY.Pressed()) {
            Instance.ChangeTile(new(Instance.TileMouseX, Instance.TileMouseY, Instance.TargetedLayer), Tile.NONE);
        }

        if(MODIFIER_SECONDARY.JustPressed()) {
            Instance.ShowToolInputHints = !Instance.ShowToolInputHints;
        }
    }

    public BasicTileCursor() {
        HintsByInput = new() {
            { MODIFIER_MAIN, "Snap to top."},
            { LAYER_UP, "Move cursor up." },
            { LAYER_DOWN, "Move cursor down." },
            { INTERACT_WORLD_MAIN, "Set tile." },
            { INTERACT_WORLD_SECONDARY, "Delete tile." },
            { MODIFIER_SECONDARY, "Toggle this." },
        };
    }
}
