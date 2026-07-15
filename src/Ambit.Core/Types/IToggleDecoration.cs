namespace Ambit;

/// <summary>
/// Represents an interactive decoration that can switch between internal states.
/// </summary>
public interface IToggleDecoration : IAnchorableDecoration
{
    /// <summary>
    /// Toggles the decoration state.
    /// </summary>
    void Toggle();
}
