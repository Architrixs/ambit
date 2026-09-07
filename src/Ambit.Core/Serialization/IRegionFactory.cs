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
    /// Creates a draft region for interactive drawing between two normalized points.
    /// Default implementation builds a two-vertex <see cref="RegionDto"/> and delegates to <see cref="Create"/>.
    /// Override for custom shapes that need different handling.
    /// </summary>
    /// <param name="id">The draft region id.</param>
    /// <param name="a">The first normalized point (anchor).</param>
    /// <param name="b">The second normalized point (live cursor).</param>
    /// <param name="style">The region style.</param>
    /// <param name="decorations">The decorations for the draft.</param>
    /// <returns>The draft region instance.</returns>
    virtual IEditableRegion CreateDraft(Guid id, NormalizedPoint a, NormalizedPoint b, RegionStyle style, IReadOnlyList<IDecoration> decorations)
    {
        var dto = new RegionDto
        {
            Id = id,
            TypeId = TypeId,
            Vertices = new[] { a, b },
            Decorations = Array.Empty<DecorationDto>(),
            Style = style,
            Label = null,
            Properties = new Dictionary<string, string?>(),
        };
        return Create(dto, decorations);
    }

    /// <summary>
    /// Converts a runtime region into serialized form.
    /// </summary>
    /// <param name="region">The runtime region instance.</param>
    /// <param name="decorations">The already serialized decorations for the region.</param>
    /// <returns>The serialized region data.</returns>
    RegionDto ToDto(IRegion region, IReadOnlyList<DecorationDto> decorations);
}
