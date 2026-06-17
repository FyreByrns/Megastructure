namespace TileBased;

static class InputStrings {
    #region base bind names

    public const string INTERACT_WORLD_MAIN = "interact_world_main";
    public const string INTERACT_WORLD_SECONDARY = "interact_world_secondary";

    public const string MOVE_UP = "move_up";
    public const string MOVE_DOWN = "move_down";
    public const string MOVE_LEFT = "move_left";
    public const string MOVE_RIGHT = "move_right";

    public const string LAYER_UP = "layer_up";
    public const string LAYER_DOWN = "layer_down";

    public const string MODIFIER_MAIN = "modifier_main";
    public const string MODIFIER_SECONDARY = "modifier_secondary";

    #endregion base bind names

    #region hacky extension helper methods
    public static bool Pressed(this string s) => Instance.Pressed(s);
    public static bool JustPressed(this string s) => Instance.JustPressed(s);
    public static bool Released(this string s) => Instance.Released(s);
    public static bool JustReleased(this string s) => Instance.JustReleased(s);

    public static void Bind(this string s, Input input) => Instance.Bind(s, input);
    #endregion hacky extension helper methods
}