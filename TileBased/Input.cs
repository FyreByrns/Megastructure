using PixelEngine;

namespace TileBased;

class Input {
    public enum InputMode {
        NONE = 0,

        All,
        Any,
    }

    public Mouse[]? MouseBinds;
    public Scroll[]? ScrollBinds;
    public Key[]? KeyBinds;

    public InputMode Mode = InputMode.All;
    /// <summary>
    /// Whether this input prevents all inputs after this one from accepting any binds in this input.
    /// </summary>
    public bool Exclusive = false;

    public bool LastState = false;
    public bool Active = false;

    public bool Pressed => Active; // currently down
    public bool JustPressed => Pressed && !LastState; // currently down but wasn't
    public bool Released => !Active; // currently up
    public bool JustReleased => Released && LastState; // currently up but wasn't

    public Input(Mouse mouse) { MouseBinds = [mouse]; }
    public Input(Scroll scroll) { ScrollBinds = [scroll]; }
    public Input(Key key) { KeyBinds = [key]; }

    public Input(params Mouse[] mouseBinds) { MouseBinds = mouseBinds; }
    public Input(params Scroll[] scrollBinds) { ScrollBinds = scrollBinds; }
    public Input(params Key[] keyBinds) { KeyBinds = keyBinds; }
    public Input(params object[] binds) {
        HashSet<Mouse> mice = [];
        HashSet<Scroll> scrolls = [];
        HashSet<Key> keys = [];

        foreach (var item in binds) {
            if (item is Mouse m) { mice.Add(m); continue; }
            if (item is Scroll s) { scrolls.Add(s); continue; }
            if (item is Key k) { keys.Add(k); continue; }
        }
        MouseBinds = mice.ToArray();
        ScrollBinds = scrolls.ToArray();
        KeyBinds = keys.ToArray();
    }
}
