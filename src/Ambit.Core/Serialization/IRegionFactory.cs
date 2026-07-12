namespace Ambit;

/// <summary>
/// Creates and serializes a single region type.
/// </summary>
public interface IRegionFactory
{
    /// <summary>
    /// Gets the open-ended region type identifier handled by this factory.
    /// </summary>
    string TypeId { get; }

    /// <summary>
    /// Creates a runtime region instance from serialized data.
    /// </summary>
    /// <param name="dto">The serialized region data.</param>
    /// <param name="decorations">The already materialized runtime decorations for the region.</param>
    /// <returns>The runtime region instance.</returns>
    IEditableRegion Create(RegionDto dto, IReadOnlyList<IDecoration> decorations);

    /// <summary>
    /// Converts a runtime region into serialized form.
    /// </summary>
    /// <param name="region">The runtime region instance.</param>
    /// <param name="decorations">The already serialized decorations for the region.</param>
    /// <returns>The serialized region data.</returns>
    RegionDto ToDto(IRegion region, IReadOnlyList<DecorationDto> decorations);
}
