namespace Ambit;

/// <summary>
/// Represents a two-point editable line region.
/// </summary>
public sealed class LineRegion : IEditableRegion
{
    /// <summary>
    /// The built-in type identifier for line regions.
    /// </summary>
    public const string LineTypeId = "line";

    private const string VertexHandleKind = "vertex";
    private readonly IDecoration[] _decorations;
    private NormalizedPoint[] _vertices;

    /// <summary>
    /// Initializes a new instance of the <see cref="LineRegion"/> class.
    /// </summary>
    /// <param name="start">The start point.</param>
    /// <param name="end">The end point.</param>
    /// <param name="style">The persisted region style.</param>
    /// <param name="id">The optional region identifier.</param>
    /// <param name="decorations">The optional attached decorations.</param>
    /// <param name="label">The optional region label.</param>
    public LineRegion(
        NormalizedPoint start,
        NormalizedPoint end,
        RegionStyle style,
        Guid? id = null,
        IEnumerable<IDecoration>? decorations = null,
        string? label = null)
    {
        ArgumentNullException.ThrowIfNull(style);

        Id = id ?? Guid.NewGuid();
        Style = style;
        Label = label;
        _decorations = decorations?.ToArray() ?? Array.Empty<IDecoration>();
        _vertices = [start, end];
        UpdateDecorationAnchors();
    }

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public string TypeId => LineTypeId;

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
        return
        [
            new RegionHandle(0, _vertices[0], VertexHandleKind),
            new RegionHandle(1, _vertices[1], VertexHandleKind),
        ];
    }

    /// <inheritdoc />
    public bool HitTestBody(NormalizedPoint point, double toleranceNormalized)
    {
        return GeometryUtilities.IsPointNearSegment(point, _vertices[0], _vertices[1], toleranceNormalized);
    }

    /// <inheritdoc />
    public void MoveHandle(int handleIndex, NormalizedPoint newPosition)
    {
        if (handleIndex is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(handleIndex));
        }

        _vertices[handleIndex] = newPosition;
        UpdateDecorationAnchors();
    }

    /// <inheritdoc />
    public void Translate(NormalizedVector delta)
    {
        _vertices = GeometryUtilities.TranslateAll(_vertices, delta).ToArray();
        UpdateDecorationAnchors();
    }

    private void UpdateDecorationAnchors()
    {
        if (_vertices.Length < 2) return;
        var midPoint = new NormalizedPoint(
            (_vertices[0].X + _vertices[1].X) / 2d,
            (_vertices[0].Y + _vertices[1].Y) / 2d
        );

        foreach (var dec in _decorations)
        {
            if (dec is IAnchorableDecoration anchorable)
            {
                anchorable.Anchor = midPoint;
            }
            else
            {
                var prop = dec.GetType().GetProperty("Anchor");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(dec, midPoint);
                }
            }
        }
    }
}
