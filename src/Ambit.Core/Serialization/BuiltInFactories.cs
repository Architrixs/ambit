namespace Ambit;

/// <summary>
/// Serializes and materializes <see cref="RectangleRegion"/> instances.
/// </summary>
public sealed class RectangleRegionFactory : IRegionFactory
{
    private const string LockAspectRatioProperty = "lockAspectRatio";

    /// <inheritdoc />
    public string TypeId => RectangleRegion.RectangleTypeId;

    /// <inheritdoc />
    public IEditableRegion Create(RegionDto dto, IReadOnlyList<IDecoration> decorations)
    {
        BuiltInFactoryHelpers.ValidateVertexCount(dto, expectedCount: 2);
        return new RectangleRegion(
            dto.Vertices[0],
            dto.Vertices[1],
            dto.Style,
            dto.Id,
            decorations,
            dto.Label,
            BuiltInFactoryHelpers.GetBooleanProperty(dto.Properties, LockAspectRatioProperty));
    }

    /// <inheritdoc />
    public RegionDto ToDto(IRegion region, IReadOnlyList<DecorationDto> decorations)
    {
        var rectangle = region as RectangleRegion ?? throw new ArgumentException("Region must be a RectangleRegion.", nameof(region));
        return new RegionDto
        {
            Id = rectangle.Id,
            TypeId = rectangle.TypeId,
            Vertices = rectangle.Vertices.ToArray(),
            Decorations = decorations.ToArray(),
            Style = rectangle.Style,
            Label = rectangle.Label,
            Properties = new Dictionary<string, string?> { [LockAspectRatioProperty] = rectangle.LockAspectRatio.ToString() },
        };
    }
}

/// <summary>
/// Serializes and materializes <see cref="PolygonRegion"/> instances.
/// </summary>
public sealed class PolygonRegionFactory : IRegionFactory
{
    /// <inheritdoc />
    public string TypeId => PolygonRegion.PolygonTypeId;

    /// <inheritdoc />
    public IEditableRegion Create(RegionDto dto, IReadOnlyList<IDecoration> decorations)
    {
        if (dto.Vertices.Count < 3)
        {
            throw new ArgumentException("Polygon DTOs require at least three vertices.", nameof(dto));
        }

        return new PolygonRegion(dto.Vertices, dto.Style, dto.Id, decorations, dto.Label);
    }

    /// <inheritdoc />
    public RegionDto ToDto(IRegion region, IReadOnlyList<DecorationDto> decorations)
    {
        var polygon = region as PolygonRegion ?? throw new ArgumentException("Region must be a PolygonRegion.", nameof(region));
        return BuiltInFactoryHelpers.CreateVertexRegionDto(polygon, decorations);
    }
}

/// <summary>
/// Serializes and materializes <see cref="PolylineRegion"/> instances.
/// </summary>
public sealed class PolylineRegionFactory : IRegionFactory
{
    /// <inheritdoc />
    public string TypeId => PolylineRegion.PolylineTypeId;

    /// <inheritdoc />
    public IEditableRegion Create(RegionDto dto, IReadOnlyList<IDecoration> decorations)
    {
        if (dto.Vertices.Count < 2)
        {
            throw new ArgumentException("Polyline DTOs require at least two vertices.", nameof(dto));
        }

        return new PolylineRegion(dto.Vertices, dto.Style, dto.Id, decorations, dto.Label);
    }

    /// <inheritdoc />
    public RegionDto ToDto(IRegion region, IReadOnlyList<DecorationDto> decorations)
    {
        var polyline = region as PolylineRegion ?? throw new ArgumentException("Region must be a PolylineRegion.", nameof(region));
        return BuiltInFactoryHelpers.CreateVertexRegionDto(polyline, decorations);
    }
}

/// <summary>
/// Serializes and materializes <see cref="LineRegion"/> instances.
/// </summary>
public sealed class LineRegionFactory : IRegionFactory
{
    /// <inheritdoc />
    public string TypeId => LineRegion.LineTypeId;

    /// <inheritdoc />
    public IEditableRegion Create(RegionDto dto, IReadOnlyList<IDecoration> decorations)
    {
        BuiltInFactoryHelpers.ValidateVertexCount(dto, expectedCount: 2);
        return new LineRegion(dto.Vertices[0], dto.Vertices[1], dto.Style, dto.Id, decorations, dto.Label);
    }

    /// <inheritdoc />
    public RegionDto ToDto(IRegion region, IReadOnlyList<DecorationDto> decorations)
    {
        var line = region as LineRegion ?? throw new ArgumentException("Region must be a LineRegion.", nameof(region));
        return BuiltInFactoryHelpers.CreateVertexRegionDto(line, decorations);
    }
}

/// <summary>
/// Serializes and materializes <see cref="EllipseRegion"/> instances.
/// </summary>
public sealed class EllipseRegionFactory : IRegionFactory
{
    private const string LockAspectRatioProperty = "lockAspectRatio";

    /// <inheritdoc />
    public string TypeId => EllipseRegion.EllipseTypeId;

    /// <inheritdoc />
    public IEditableRegion Create(RegionDto dto, IReadOnlyList<IDecoration> decorations)
    {
        BuiltInFactoryHelpers.ValidateVertexCount(dto, expectedCount: 2);
        return new EllipseRegion(
            dto.Vertices[0],
            dto.Vertices[1],
            dto.Style,
            dto.Id,
            decorations,
            dto.Label,
            BuiltInFactoryHelpers.GetBooleanProperty(dto.Properties, LockAspectRatioProperty));
    }

    /// <inheritdoc />
    public RegionDto ToDto(IRegion region, IReadOnlyList<DecorationDto> decorations)
    {
        var ellipse = region as EllipseRegion ?? throw new ArgumentException("Region must be an EllipseRegion.", nameof(region));
        return new RegionDto
        {
            Id = ellipse.Id,
            TypeId = ellipse.TypeId,
            Vertices = ellipse.Vertices.ToArray(),
            Decorations = decorations.ToArray(),
            Style = ellipse.Style,
            Label = ellipse.Label,
            Properties = new Dictionary<string, string?> { [LockAspectRatioProperty] = ellipse.LockAspectRatio.ToString() },
        };
    }
}


/// <summary>
/// Serializes and materializes <see cref="LabelDecoration"/> instances.
/// </summary>
public sealed class LabelDecorationFactory : IDecorationFactory
{
    private const string TextProperty = "text";

    /// <inheritdoc />
    public string TypeId => LabelDecoration.LabelDecorationTypeId;

    /// <inheritdoc />
    public IDecoration Create(DecorationDto dto)
    {
        if (!dto.Properties.TryGetValue(TextProperty, out var text) || string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Label decoration DTOs require a text property.", nameof(dto));
        }

        return new LabelDecoration(dto.Anchor, text);
    }

    /// <inheritdoc />
    public DecorationDto ToDto(IDecoration decoration)
    {
        var label = decoration as LabelDecoration ?? throw new ArgumentException("Decoration must be a LabelDecoration.", nameof(decoration));
        return new DecorationDto
        {
            TypeId = label.TypeId,
            Anchor = label.Anchor,
            IsInteractive = label.IsInteractive,
            Properties = new Dictionary<string, string?> { [TextProperty] = label.Text },
        };
    }
}

internal static class BuiltInFactoryHelpers
{
    public static RegionDto CreateVertexRegionDto(IRegion region, IReadOnlyList<DecorationDto> decorations)
    {
        return new RegionDto
        {
            Id = region.Id,
            TypeId = region.TypeId,
            Vertices = region.Vertices.ToArray(),
            Decorations = decorations.ToArray(),
            Style = region.Style,
            Label = region.Label,
            Properties = new Dictionary<string, string?>(),
        };
    }

    public static bool GetBooleanProperty(IReadOnlyDictionary<string, string?> properties, string key)
    {
        return properties.TryGetValue(key, out var rawValue) && bool.TryParse(rawValue, out var parsedValue) && parsedValue;
    }

    public static void ValidateVertexCount(RegionDto dto, int expectedCount)
    {
        if (dto.Vertices.Count != expectedCount)
        {
            throw new ArgumentException($"Expected exactly {expectedCount} vertices for '{dto.TypeId}'.", nameof(dto));
        }
    }
}
