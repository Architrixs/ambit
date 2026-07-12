namespace Ambit;

/// <summary>
/// Represents a serialized region instance.
/// </summary>
public sealed class RegionDto
{
    /// <summary>
    /// Gets or initializes the region identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or initializes the open-ended region type identifier.
    /// </summary>
    public required string TypeId { get; init; }

    /// <summary>
    /// Gets or initializes the normalized region vertices.
    /// </summary>
    public required IReadOnlyList<NormalizedPoint> Vertices { get; init; }

    /// <summary>
    /// Gets or initializes the serialized decorations attached to the region.
    /// </summary>
    public IReadOnlyList<DecorationDto> Decorations { get; init; } = Array.Empty<DecorationDto>();

    /// <summary>
    /// Gets or initializes the persisted region style.
    /// </summary>
    public required RegionStyle Style { get; init; }

    /// <summary>
    /// Gets or initializes the optional region label text.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Gets or initializes the region-specific serialized property bag.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Properties { get; init; } = new Dictionary<string, string?>();
}
