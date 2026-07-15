namespace Ambit;

/// <summary>
/// Represents a decoration whose anchor position can be dynamically updated by its hosting region.
/// </summary>
public interface IAnchorableDecoration : IDecoration
{
    /// <summary>
    /// Gets or sets the decoration anchor in normalized region space.
    /// </summary>
    new NormalizedPoint Anchor { get; set; }
}
