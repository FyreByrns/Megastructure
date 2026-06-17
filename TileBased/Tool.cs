namespace TileBased;

/// <summary>
/// Base class for chaining together getting information from the player and world, then acting on it.
/// Has the ability to provide context and instructions for what inputs will do.
/// </summary>
class Tool {
    public Tool? Previous;
    public Tool? Next;

    /// <summary>
    /// Holds inputs and their corresponding hint strings
    /// </summary>
    public Dictionary<string, string> HintsByInput = [];
    public Dictionary<string, string> SimplifiedInputNames = [];
    /// <summary>
    /// General help text.
    /// </summary>
    public string Hint = "";

    public virtual void Tick() { }
}
