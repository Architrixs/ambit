namespace Ambit;

/// <summary>
/// Represents a serialized decoration instance.
/// </summary>
public sealed class DecorationDto
{
    /// <summary>
    /// Gets or initializes the open-ended decoration type identifier.
    /// </summary>
    public required string TypeId { get; init; }

    /// <summary>
    /// Gets or initializes the decoration anchor in normalized region space.
    /// </summary>
    public required NormalizedPoint Anchor { get; init; }

    /// <summary>
    /// Gets or initializes a value indicating whether the decoration participates in interaction.
    /// </summary>
    public required bool IsInteractive { get; init; }

    /// <summary>
    /// Gets or initializes the decoration-specific serialized property bag.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Properties { get; init; } = new Dictionary<string, string?>();
}
