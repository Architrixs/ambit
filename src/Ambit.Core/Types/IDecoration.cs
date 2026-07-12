namespace Ambit;

/// <summary>
/// Represents an adornment attached to a region.
/// </summary>
public interface IDecoration
{
    /// <summary>
    /// Gets the open-ended type identifier for the decoration.
    /// </summary>
    string TypeId { get; }

    /// <summary>
    /// Gets the decoration anchor in normalized region space.
    /// </summary>
    NormalizedPoint Anchor { get; }

    /// <summary>
    /// Gets a value indicating whether the decoration participates in interaction.
    /// </summary>
    bool IsInteractive { get; }
}
