namespace TileBased;

static class InputStrings {
    #region base bind names

    public const string MOVE_UP = "move_up";
    public const string MOVE_DOWN = "move_down";
    public const string MOVE_LEFT = "move_left";
    public const string MOVE_RIGHT = "move_right";

    #endregion base bind names

    #region hacky extension helper methods
    public static bool Pressed(this string s) => Instance.Pressed(s);
    public static bool JustPressed(this string s) => Instance.JustPressed(s);
    public static bool Released(this string s) => Instance.Released(s);
    public static bool JustReleased(this string s) => Instance.JustReleased(s);

    public static void Bind(this string s, Input input) => Instance.Bind(s, input);
    #endregion hacky extension helper methods
}