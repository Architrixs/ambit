namespace Ambit;

/// <summary>
/// Represents immutable persisted region data.
/// </summary>
public interface IRegion
{
    /// <summary>
    /// Gets the unique region identifier.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the open-ended type identifier for the region.
    /// </summary>
    string TypeId { get; }

    /// <summary>
    /// Gets the normalized region vertices.
    /// </summary>
    IReadOnlyList<NormalizedPoint> Vertices { get; }

    /// <summary>
    /// Gets the attached decorations.
    /// </summary>
    IReadOnlyList<IDecoration> Decorations { get; }

    /// <summary>
    /// Gets the persisted visual styling.
    /// </summary>
    RegionStyle Style { get; }

    /// <summary>
    /// Gets the optional user-facing label text.
    /// </summary>
    string? Label { get; }
}
