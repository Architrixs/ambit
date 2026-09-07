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
    public NormalizedBounds Bounds => GeometryUtilities.GetBounds(_vertices);

    private RegionHandle[]? _cachedHandles;

    /// <inheritdoc />
    public IReadOnlyList<RegionHandle> GetHandles()
    {
        if (_cachedHandles is not null)
        {
            return _cachedHandles;
        }

        _cachedHandles = new[]
        {
            new RegionHandle(0, _vertices[0], VertexHandleKind),
            new RegionHandle(1, _vertices[1], VertexHandleKind),
        };
        return _cachedHandles;
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

        var oldVertices = (NormalizedPoint[])_vertices.Clone();
        _vertices[handleIndex] = newPosition;
        UpdateDecorationAnchorsForHandleMove(oldVertices, _vertices);
        _cachedHandles = null;
    }

    /// <inheritdoc />
    public void Translate(NormalizedVector delta)
    {
        var adjusted = GeometryUtilities.ConstrainTranslationToUnitBounds(_vertices, delta);
        _vertices = GeometryUtilities.TranslateAll(_vertices, adjusted).ToArray();
        // Shift anchorable decorations by the same constrained delta so they stay relative to the line.
        foreach (var dec in _decorations)
        {
            if (dec is IAnchorableDecoration anchorable)
            {
                anchorable.Anchor = GeometryUtilities.Translate(anchorable.Anchor, adjusted);
            }
        }
        _cachedHandles = null;
    }

    private void UpdateDecorationAnchorsForHandleMove(NormalizedPoint[] oldVertices, NormalizedPoint[] newVertices)
    {
        if (oldVertices.Length < 2 || newVertices.Length < 2) return;
        // Preserve each decoration's fractional position along the line so independent
        // indicators near each endpoint stay near that endpoint after a handle drag.
        foreach (var dec in _decorations)
        {
            if (dec is not IAnchorableDecoration anchorable) continue;
            var oldT = GeometryUtilities.ProjectPointOntoSegment(anchorable.Anchor, oldVertices[0], oldVertices[1]).Parameter;
            var t = double.Clamp(oldT, 0d, 1d);
            anchorable.Anchor = new NormalizedPoint(
                newVertices[0].X + t * (newVertices[1].X - newVertices[0].X),
                newVertices[0].Y + t * (newVertices[1].Y - newVertices[0].Y));
        }
    }
}
