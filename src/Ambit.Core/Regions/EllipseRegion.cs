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
    private const int TopEdgeHandleIndex = 4;
    private const int RightEdgeHandleIndex = 5;
    private const int BottomEdgeHandleIndex = 6;
    private const int LeftEdgeHandleIndex = 7;
    private const string CornerHandleKind = "corner";
    private const string EdgeMidpointHandleKind = "edge-midpoint";

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
        var centerX = bounds.Left + (bounds.Width / 2d);
        var centerY = bounds.Top + (bounds.Height / 2d);

        return
        [
            new RegionHandle(TopLeftHandleIndex, new NormalizedPoint(bounds.Left, bounds.Top), CornerHandleKind),
            new RegionHandle(TopRightHandleIndex, new NormalizedPoint(bounds.Right, bounds.Top), CornerHandleKind),
            new RegionHandle(BottomRightHandleIndex, new NormalizedPoint(bounds.Right, bounds.Bottom), CornerHandleKind),
            new RegionHandle(BottomLeftHandleIndex, new NormalizedPoint(bounds.Left, bounds.Bottom), CornerHandleKind),
            new RegionHandle(TopEdgeHandleIndex, new NormalizedPoint(centerX, bounds.Top), EdgeMidpointHandleKind),
            new RegionHandle(RightEdgeHandleIndex, new NormalizedPoint(bounds.Right, centerY), EdgeMidpointHandleKind),
            new RegionHandle(BottomEdgeHandleIndex, new NormalizedPoint(centerX, bounds.Bottom), EdgeMidpointHandleKind),
            new RegionHandle(LeftEdgeHandleIndex, new NormalizedPoint(bounds.Left, centerY), EdgeMidpointHandleKind),
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
        if (handleIndex is < TopLeftHandleIndex or > LeftEdgeHandleIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(handleIndex));
        }

        var currentBounds = Bounds;
        var updatedBounds = LockAspectRatio && IsCornerHandle(handleIndex)
            ? MoveLockedCorner(currentBounds, handleIndex, newPosition)
            : MoveUnlockedHandle(currentBounds, handleIndex, newPosition);

        _vertices = CreateVertices(
            new NormalizedPoint(updatedBounds.Left, updatedBounds.Top),
            new NormalizedPoint(updatedBounds.Right, updatedBounds.Bottom));
    }

    /// <inheritdoc />
    public void Translate(NormalizedVector delta)
    {
        _vertices = GeometryUtilities.TranslateAll(_vertices, delta).ToArray();
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

    private static bool IsCornerHandle(int handleIndex)
    {
        return handleIndex is TopLeftHandleIndex or TopRightHandleIndex or BottomRightHandleIndex or BottomLeftHandleIndex;
    }

    private static NormalizedBounds MoveUnlockedHandle(NormalizedBounds bounds, int handleIndex, NormalizedPoint newPosition)
    {
        var left = bounds.Left;
        var top = bounds.Top;
        var right = bounds.Right;
        var bottom = bounds.Bottom;

        switch (handleIndex)
        {
            case TopLeftHandleIndex:
                left = newPosition.X;
                top = newPosition.Y;
                break;
            case TopRightHandleIndex:
                right = newPosition.X;
                top = newPosition.Y;
                break;
            case BottomRightHandleIndex:
                right = newPosition.X;
                bottom = newPosition.Y;
                break;
            case BottomLeftHandleIndex:
                left = newPosition.X;
                bottom = newPosition.Y;
                break;
            case TopEdgeHandleIndex:
                top = newPosition.Y;
                break;
            case RightEdgeHandleIndex:
                right = newPosition.X;
                break;
            case BottomEdgeHandleIndex:
                bottom = newPosition.Y;
                break;
            case LeftEdgeHandleIndex:
                left = newPosition.X;
                break;
        }

        return NormalizeBounds(left, top, right, bottom);
    }

    private static NormalizedBounds MoveLockedCorner(NormalizedBounds bounds, int handleIndex, NormalizedPoint newPosition)
    {
        var width = bounds.Width;
        var height = bounds.Height;
        if (width <= double.Epsilon || height <= double.Epsilon)
        {
            return MoveUnlockedHandle(bounds, handleIndex, newPosition);
        }

        var aspectRatio = width / height;
        var anchor = GetOppositeCorner(bounds, handleIndex);
        var xDirection = handleIndex is TopLeftHandleIndex or BottomLeftHandleIndex ? -1d : 1d;
        var yDirection = handleIndex is TopLeftHandleIndex or TopRightHandleIndex ? -1d : 1d;
        var requestedWidth = Math.Abs(newPosition.X - anchor.X);
        var requestedHeight = Math.Abs(newPosition.Y - anchor.Y);

        var widthDriven = BuildLockedBounds(anchor, xDirection, yDirection, requestedWidth, requestedWidth / aspectRatio);
        var heightDriven = BuildLockedBounds(anchor, xDirection, yDirection, requestedHeight * aspectRatio, requestedHeight);

        var requestedCorner = newPosition;
        var widthDrivenCorner = GetMovedCorner(widthDriven, handleIndex);
        var heightDrivenCorner = GetMovedCorner(heightDriven, handleIndex);

        var widthDistance = GeometryUtilities.DistanceSquared(widthDrivenCorner, requestedCorner);
        var heightDistance = GeometryUtilities.DistanceSquared(heightDrivenCorner, requestedCorner);

        return widthDistance <= heightDistance ? widthDriven : heightDriven;
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

    private static NormalizedPoint GetOppositeCorner(NormalizedBounds bounds, int handleIndex)
    {
        return handleIndex switch
        {
            TopLeftHandleIndex => new NormalizedPoint(bounds.Right, bounds.Bottom),
            TopRightHandleIndex => new NormalizedPoint(bounds.Left, bounds.Bottom),
            BottomRightHandleIndex => new NormalizedPoint(bounds.Left, bounds.Top),
            BottomLeftHandleIndex => new NormalizedPoint(bounds.Right, bounds.Top),
            _ => throw new ArgumentOutOfRangeException(nameof(handleIndex)),
        };
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

    private static NormalizedBounds NormalizeBounds(double left, double top, double right, double bottom)
    {
        return new NormalizedBounds(
            Math.Min(left, right),
            Math.Min(top, bottom),
            Math.Max(left, right),
            Math.Max(top, bottom));
    }
}
