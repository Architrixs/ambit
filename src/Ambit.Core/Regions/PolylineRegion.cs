namespace Ambit;

/// <summary>
/// Represents an open polyline editable region.
/// </summary>
public sealed class PolylineRegion : IEditableRegion
{
    /// <summary>
    /// The built-in type identifier for polyline regions.
    /// </summary>
    public const string PolylineTypeId = "polyline";

    private const string VertexHandleKind = "vertex";
    private readonly IDecoration[] _decorations;
    private NormalizedPoint[] _vertices;

    /// <summary>
    /// Initializes a new instance of the <see cref="PolylineRegion"/> class.
    /// </summary>
    /// <param name="vertices">The ordered polyline vertices.</param>
    /// <param name="style">The persisted region style.</param>
    /// <param name="id">The optional region identifier.</param>
    /// <param name="decorations">The optional attached decorations.</param>
    /// <param name="label">The optional region label.</param>
    public PolylineRegion(
        IEnumerable<NormalizedPoint> vertices,
        RegionStyle style,
        Guid? id = null,
        IEnumerable<IDecoration>? decorations = null,
        string? label = null)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(style);

        _vertices = vertices.ToArray();
        if (_vertices.Length < 2)
        {
            throw new ArgumentException("A polyline requires at least two vertices.", nameof(vertices));
        }

        Id = id ?? Guid.NewGuid();
        Style = style;
        Label = label;
        _decorations = decorations?.ToArray() ?? Array.Empty<IDecoration>();
    }

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public string TypeId => PolylineTypeId;

    /// <inheritdoc />
    public IReadOnlyList<NormalizedPoint> Vertices => _vertices;

    /// <inheritdoc />
    public IReadOnlyList<IDecoration> Decorations => _decorations;

    /// <inheritdoc />
    public RegionStyle Style { get; }

    /// <inheritdoc />
    public string? Label { get; }

    /// <inheritdoc />
    public IReadOnlyList<RegionHandle> GetHandles()
    {
        var handles = new RegionHandle[_vertices.Length];
        for (var index = 0; index < _vertices.Length; index++)
        {
            handles[index] = new RegionHandle(index, _vertices[index], VertexHandleKind);
        }

        return handles;
    }

    /// <inheritdoc />
    public bool HitTestBody(NormalizedPoint point, double toleranceNormalized)
    {
        return GeometryUtilities.IsPointNearPolyline(point, _vertices, toleranceNormalized, closed: false);
    }

    /// <inheritdoc />
    public void MoveHandle(int handleIndex, NormalizedPoint newPosition)
    {
        if (handleIndex < 0 || handleIndex >= _vertices.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(handleIndex));
        }

        _vertices[handleIndex] = newPosition;
    }

    /// <inheritdoc />
    public void Translate(NormalizedVector delta)
    {
        _vertices = GeometryUtilities.TranslateAll(_vertices, delta).ToArray();
    }
}
