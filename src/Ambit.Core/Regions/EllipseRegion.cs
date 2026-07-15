namespace Ambit;

/// <summary>
/// Represents an editable ellipse defined by an axis-aligned bounding rectangle.
/// </summary>
public sealed class EllipseRegion : IEditableRegion
{
    /// <summary>
    /// The built-in type identifier for ellipse regions.
    /// </summary>
    public const string EllipseTypeId = "ellipse";

    private const int TopLeftHandleIndex = 0;
    private const int TopRightHandleIndex = 1;
    private const int BottomRightHandleIndex = 2;
    private const int BottomLeftHandleIndex = 3;
    private const string CornerHandleKind = "corner";

    private readonly IDecoration[] _decorations;
    private NormalizedPoint[] _vertices;

    /// <summary>
    /// Initializes a new instance of the <see cref="EllipseRegion"/> class.
    /// </summary>
    /// <param name="firstCorner">The first bounding-box corner.</param>
    /// <param name="secondCorner">The second bounding-box corner.</param>
    /// <param name="style">The persisted region style.</param>
    /// <param name="id">The optional region identifier.</param>
    /// <param name="decorations">The optional attached decorations.</param>
    /// <param name="label">The optional region label.</param>
    /// <param name="lockAspectRatio">A value indicating whether corner-handle drags preserve the current aspect ratio.</param>
    public EllipseRegion(
        NormalizedPoint firstCorner,
        NormalizedPoint secondCorner,
        RegionStyle style,
        Guid? id = null,
        IEnumerable<IDecoration>? decorations = null,
        string? label = null,
        bool lockAspectRatio = false)
    {
        ArgumentNullException.ThrowIfNull(style);

        Id = id ?? Guid.NewGuid();
        Style = style;
        Label = label;
        LockAspectRatio = lockAspectRatio;
        _decorations = decorations?.ToArray() ?? Array.Empty<IDecoration>();
        _vertices = CreateVertices(firstCorner, secondCorner);
    }

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public string TypeId => EllipseTypeId;

    /// <inheritdoc />
    public IReadOnlyList<NormalizedPoint> Vertices => _vertices;

    /// <inheritdoc />
    public IReadOnlyList<IDecoration> Decorations => _decorations;

    /// <inheritdoc />
    public RegionStyle Style { get; }

    /// <inheritdoc />
    public string? Label { get; }

    /// <summary>
    /// Gets or sets a value indicating whether corner-handle drags preserve the current aspect ratio.
    /// </summary>
    public bool LockAspectRatio { get; set; }

    /// <summary>
    /// Gets the current ellipse bounding box.
    /// </summary>
    public NormalizedBounds Bounds => GeometryUtilities.GetBounds(_vertices);

    /// <inheritdoc />
    public IReadOnlyList<RegionHandle> GetHandles()
    {
        var bounds = Bounds;
        return
        [
            new RegionHandle(TopLeftHandleIndex,     new NormalizedPoint(bounds.Left,  bounds.Top),    CornerHandleKind),
            new RegionHandle(TopRightHandleIndex,    new NormalizedPoint(bounds.Right, bounds.Top),    CornerHandleKind),
            new RegionHandle(BottomRightHandleIndex, new NormalizedPoint(bounds.Right, bounds.Bottom), CornerHandleKind),
            new RegionHandle(BottomLeftHandleIndex,  new NormalizedPoint(bounds.Left,  bounds.Bottom), CornerHandleKind),
        ];
    }

    /// <inheritdoc />
    public bool HitTestBody(NormalizedPoint point, double toleranceNormalized)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(toleranceNormalized);

        var bounds = Bounds;
        var radiusX = bounds.Width / 2d;
        var radiusY = bounds.Height / 2d;
        var centerX = bounds.Left + radiusX;
        var centerY = bounds.Top + radiusY;

        if (radiusX <= double.Epsilon && radiusY <= double.Epsilon)
        {
            return GeometryUtilities.Distance(point, new NormalizedPoint(centerX, centerY)) <= toleranceNormalized;
        }

        if (radiusX <= double.Epsilon)
        {
            return GeometryUtilities.IsPointNearSegment(
                point,
                new NormalizedPoint(centerX, bounds.Top),
                new NormalizedPoint(centerX, bounds.Bottom),
                toleranceNormalized);
        }

        if (radiusY <= double.Epsilon)
        {
            return GeometryUtilities.IsPointNearSegment(
                point,
                new NormalizedPoint(bounds.Left, centerY),
                new NormalizedPoint(bounds.Right, centerY),
                toleranceNormalized);
        }

        var expandedRadiusX = radiusX + toleranceNormalized;
        var expandedRadiusY = radiusY + toleranceNormalized;
        var normalizedX = (point.X - centerX) / expandedRadiusX;
        var normalizedY = (point.Y - centerY) / expandedRadiusY;
        return ((normalizedX * normalizedX) + (normalizedY * normalizedY)) <= 1d;
    }

    /// <inheritdoc />
    public void MoveHandle(int handleIndex, NormalizedPoint newPosition)
    {
        if (handleIndex is < TopLeftHandleIndex or > BottomLeftHandleIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(handleIndex));
        }

        if (LockAspectRatio)
        {
            MoveLockedCorner(handleIndex, newPosition);
        }
        else
        {
            MoveCornerFree(handleIndex, newPosition);
        }
    }

    /// <inheritdoc />
    public void Translate(NormalizedVector delta)
    {
        _vertices = GeometryUtilities.TranslateAll(_vertices, delta).ToArray();
    }

    private void MoveCornerFree(int handleIndex, NormalizedPoint newPosition)
    {
        if (handleIndex == TopLeftHandleIndex)
        {
            _vertices[0] = newPosition;
        }
        else if (handleIndex == BottomRightHandleIndex)
        {
            _vertices[1] = newPosition;
        }
        else if (handleIndex == TopRightHandleIndex)
        {
            _vertices[1] = new NormalizedPoint(newPosition.X, _vertices[1].Y);
            _vertices[0] = new NormalizedPoint(_vertices[0].X, newPosition.Y);
        }
        else // BottomLeft
        {
            _vertices[0] = new NormalizedPoint(newPosition.X, _vertices[0].Y);
            _vertices[1] = new NormalizedPoint(_vertices[1].X, newPosition.Y);
        }
    }

    private void MoveLockedCorner(int handleIndex, NormalizedPoint newPosition)
    {
        var bounds = Bounds;
        var width = bounds.Width;
        var height = bounds.Height;
        if (width <= double.Epsilon || height <= double.Epsilon)
        {
            MoveCornerFree(handleIndex, newPosition);
            return;
        }

        var aspectRatio = width / height;
        var anchor = handleIndex switch
        {
            TopLeftHandleIndex     => _vertices[1],
            TopRightHandleIndex    => new NormalizedPoint(_vertices[0].X, _vertices[1].Y),
            BottomRightHandleIndex => _vertices[0],
            _                      => new NormalizedPoint(_vertices[1].X, _vertices[0].Y), // BottomLeft
        };

        var xDirection = newPosition.X >= anchor.X ? 1d : -1d;
        var yDirection = newPosition.Y >= anchor.Y ? 1d : -1d;
        var requestedWidth = Math.Abs(newPosition.X - anchor.X);
        var requestedHeight = Math.Abs(newPosition.Y - anchor.Y);

        var widthDriven = BuildLockedBounds(anchor, xDirection, yDirection, requestedWidth, requestedWidth / aspectRatio);
        var heightDriven = BuildLockedBounds(anchor, xDirection, yDirection, requestedHeight * aspectRatio, requestedHeight);

        var requestedCorner = newPosition;
        var widthDrivenCorner = GetMovedCorner(widthDriven, handleIndex);
        var heightDrivenCorner = GetMovedCorner(heightDriven, handleIndex);

        var widthDistance = GeometryUtilities.DistanceSquared(widthDrivenCorner, requestedCorner);
        var heightDistance = GeometryUtilities.DistanceSquared(heightDrivenCorner, requestedCorner);

        var chosen = widthDistance <= heightDistance ? widthDriven : heightDriven;

        var w = chosen.Width;
        var h = chosen.Height;
        var dragged = new NormalizedPoint(anchor.X + xDirection * w, anchor.Y + yDirection * h);

        if (handleIndex == TopLeftHandleIndex)
        {
            _vertices[0] = dragged;
            _vertices[1] = anchor;
        }
        else if (handleIndex == BottomRightHandleIndex)
        {
            _vertices[1] = dragged;
            _vertices[0] = anchor;
        }
        else if (handleIndex == TopRightHandleIndex)
        {
            _vertices[0] = new NormalizedPoint(anchor.X, dragged.Y);
            _vertices[1] = new NormalizedPoint(dragged.X, anchor.Y);
        }
        else // BottomLeft
        {
            _vertices[0] = new NormalizedPoint(dragged.X, anchor.Y);
            _vertices[1] = new NormalizedPoint(anchor.X, dragged.Y);
        }
    }

    private static NormalizedBounds BuildLockedBounds(
        NormalizedPoint anchor,
        double xDirection,
        double yDirection,
        double requestedWidth,
        double requestedHeight)
    {
        var maxWidth = xDirection > 0d ? 1d - anchor.X : anchor.X;
        var maxHeight = yDirection > 0d ? 1d - anchor.Y : anchor.Y;
        var width = requestedWidth;
        var height = requestedHeight;

        if (width > maxWidth || height > maxHeight)
        {
            var widthScale = width <= double.Epsilon ? 1d : maxWidth / width;
            var heightScale = height <= double.Epsilon ? 1d : maxHeight / height;
            var scale = Math.Min(widthScale, heightScale);
            width *= scale;
            height *= scale;
        }

        var movedCorner = new NormalizedPoint(anchor.X + (xDirection * width), anchor.Y + (yDirection * height));
        return NormalizeBounds(anchor.X, anchor.Y, movedCorner.X, movedCorner.Y);
    }

    private static NormalizedPoint GetMovedCorner(NormalizedBounds bounds, int handleIndex)
    {
        return handleIndex switch
        {
            TopLeftHandleIndex => new NormalizedPoint(bounds.Left, bounds.Top),
            TopRightHandleIndex => new NormalizedPoint(bounds.Right, bounds.Top),
            BottomRightHandleIndex => new NormalizedPoint(bounds.Right, bounds.Bottom),
            BottomLeftHandleIndex => new NormalizedPoint(bounds.Left, bounds.Bottom),
            _ => throw new ArgumentOutOfRangeException(nameof(handleIndex)),
        };
    }

    private static NormalizedPoint[] CreateVertices(NormalizedPoint firstCorner, NormalizedPoint secondCorner)
    {
        var bounds = GeometryUtilities.GetBounds([firstCorner, secondCorner]);
        return
        [
            new NormalizedPoint(bounds.Left, bounds.Top),
            new NormalizedPoint(bounds.Right, bounds.Bottom),
        ];
    }

    private static NormalizedBounds NormalizeBounds(double left, double top, double right, double bottom)
    {
        return new NormalizedBounds(
            Math.Min(left, right),
            Math.Min(top, bottom),
            Math.Max(left, right),
            Math.Max(top, bottom));
    }
}
