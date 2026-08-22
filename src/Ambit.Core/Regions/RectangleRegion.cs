namespace Ambit;

/// <summary>
/// Represents an axis-aligned rectangular editable region.
/// </summary>
public sealed class RectangleRegion : IEditableRegion
{
    /// <summary>The built-in type identifier for rectangle regions.</summary>
    public const string RectangleTypeId = "rectangle";

    // Handle index constants – these are stable across flips.
    // Each index identifies the *logical role* of a corner as seen by the user,
    // not a fixed position. After every move we re-derive which physical corner
    // is TL/TR/BR/BL from the actual stored vertices.
    private const int TopLeftHandleIndex     = 0;
    private const int TopRightHandleIndex    = 1;
    private const int BottomRightHandleIndex = 2;
    private const int BottomLeftHandleIndex  = 3;
    private const string CornerHandleKind    = "corner";

    private readonly IDecoration[] _decorations;

    // We store the rectangle as two raw corners (firstCorner, secondCorner) so that
    // handle dragging can freely flip the rectangle by just updating one corner.
    // _corners[0] is always the corner the user first clicked (or the top-left after
    // construction); _corners[1] is the diagonally opposite corner.
    // GetHandles() derives the four logical corners from min/max of these two points.
    private NormalizedPoint _corner0;
    private NormalizedPoint _corner1;

    /// <summary>
    /// Initializes a new instance of the <see cref="RectangleRegion"/> class.
    /// </summary>
    public RectangleRegion(
        NormalizedPoint firstCorner,
        NormalizedPoint secondCorner,
        RegionStyle style,
        Guid? id = null,
        IEnumerable<IDecoration>? decorations = null,
        string? label = null,
        bool lockAspectRatio = false)
    {
        ArgumentNullException.ThrowIfNull(style);

        Id              = id ?? Guid.NewGuid();
        Style           = style;
        Label           = label;
        LockAspectRatio = lockAspectRatio;
        _decorations    = decorations?.ToArray() ?? Array.Empty<IDecoration>();

        // Normalise: _corner0 = top-left, _corner1 = bottom-right initially.
        var b = GeometryUtilities.GetBounds([firstCorner, secondCorner]);
        _corner0 = new NormalizedPoint(b.Left,  b.Top);
        _corner1 = new NormalizedPoint(b.Right, b.Bottom);
    }

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public string TypeId => RectangleTypeId;

    /// <inheritdoc />
    public IReadOnlyList<NormalizedPoint> Vertices =>
    [
        new NormalizedPoint(Bounds.Left,  Bounds.Top),
        new NormalizedPoint(Bounds.Right, Bounds.Bottom),
    ];

    /// <inheritdoc />
    public IReadOnlyList<IDecoration> Decorations => _decorations;

    /// <inheritdoc />
    public RegionStyle Style { get; }

    /// <inheritdoc />
    public string? Label { get; }

    /// <summary>Gets or sets whether corner drags preserve the current aspect ratio.</summary>
    public bool LockAspectRatio { get; set; }

    /// <summary>Gets the current rectangle bounds (always normalised: left≤right, top≤bottom).</summary>
    public NormalizedBounds Bounds => GeometryUtilities.GetBounds([_corner0, _corner1]);

    private RegionHandle[]? _cachedHandles;

    // ── IHandleProvider ────────────────────────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<RegionHandle> GetHandles()
    {
        if (_cachedHandles is not null)
        {
            return _cachedHandles;
        }

        var b = Bounds;
        _cachedHandles = new[]
        {
            new RegionHandle(TopLeftHandleIndex,     new NormalizedPoint(b.Left,  b.Top),    CornerHandleKind),
            new RegionHandle(TopRightHandleIndex,    new NormalizedPoint(b.Right, b.Top),    CornerHandleKind),
            new RegionHandle(BottomRightHandleIndex, new NormalizedPoint(b.Right, b.Bottom), CornerHandleKind),
            new RegionHandle(BottomLeftHandleIndex,  new NormalizedPoint(b.Left,  b.Bottom), CornerHandleKind),
        };
        return _cachedHandles;
    }

    // ── IHitTestable ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public bool HitTestBody(NormalizedPoint point, double toleranceNormalized)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(toleranceNormalized);
        var b = Bounds;
        return point.X >= b.Left   - toleranceNormalized
            && point.X <= b.Right  + toleranceNormalized
            && point.Y >= b.Top    - toleranceNormalized
            && point.Y <= b.Bottom + toleranceNormalized;
    }

    // ── IEditableRegion ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public void MoveHandle(int handleIndex, NormalizedPoint newPosition)
    {
        if (handleIndex is < TopLeftHandleIndex or > BottomLeftHandleIndex)
            throw new ArgumentOutOfRangeException(nameof(handleIndex));

        if (LockAspectRatio)
        {
            MoveLockedCorner(handleIndex, newPosition);
        }
        else
        {
            MoveCornerFree(handleIndex, newPosition);
        }
        _cachedHandles = null;
    }

    /// <inheritdoc />
    public void Translate(NormalizedVector delta)
    {
        var b = Bounds;
        var dx = delta.Dx;
        var dy = delta.Dy;

        if (b.Left   + dx < 0d) dx = -b.Left;
        else if (b.Right  + dx > 1d) dx = 1d - b.Right;
        if (b.Top    + dy < 0d) dy = -b.Top;
        else if (b.Bottom + dy > 1d) dy = 1d - b.Bottom;

        var d = new NormalizedVector(dx, dy);
        _corner0 = GeometryUtilities.Translate(_corner0, d);
        _corner1 = GeometryUtilities.Translate(_corner1, d);
        _cachedHandles = null;
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Moves the logical corner identified by <paramref name="handleIndex"/> to
    /// <paramref name="newPosition"/>, keeping the diagonally opposite corner fixed.
    /// The stored _corner0/_corner1 are updated so the visual shape and handles stay
    /// consistent even when the rectangle flips.
    /// </summary>
    private void MoveCornerFree(int handleIndex, NormalizedPoint newPosition)
    {
        if (handleIndex == TopLeftHandleIndex)
        {
            _corner0 = newPosition;
        }
        else if (handleIndex == BottomRightHandleIndex)
        {
            _corner1 = newPosition;
        }
        else if (handleIndex == TopRightHandleIndex)
        {
            _corner1 = new NormalizedPoint(newPosition.X, _corner1.Y);
            _corner0 = new NormalizedPoint(_corner0.X, newPosition.Y);
        }
        else // BottomLeft
        {
            _corner0 = new NormalizedPoint(newPosition.X, _corner0.Y);
            _corner1 = new NormalizedPoint(_corner1.X, newPosition.Y);
        }
    }

    private void MoveLockedCorner(int handleIndex, NormalizedPoint newPosition)
    {
        var b      = Bounds;
        var width  = b.Width;
        var height = b.Height;

        if (width <= double.Epsilon || height <= double.Epsilon)
        {
            MoveCornerFree(handleIndex, newPosition);
            return;
        }

        var aspectRatio = width / height;
        var anchor = handleIndex switch
        {
            TopLeftHandleIndex     => _corner1,
            TopRightHandleIndex    => new NormalizedPoint(_corner0.X, _corner1.Y),
            BottomRightHandleIndex => _corner0,
            _                      => new NormalizedPoint(_corner1.X, _corner0.Y), // BottomLeft
        };

        var xDir = newPosition.X >= anchor.X ? 1d : -1d;
        var yDir = newPosition.Y >= anchor.Y ? 1d : -1d;

        var reqW = Math.Abs(newPosition.X - anchor.X);
        var reqH = Math.Abs(newPosition.Y - anchor.Y);

        var wDriven = BuildLockedBounds(anchor, xDir, yDir, reqW, reqW / aspectRatio);
        var hDriven = BuildLockedBounds(anchor, xDir, yDir, reqH * aspectRatio, reqH);

        // Pick whichever driven result is closest to the requested position.
        var wCorner = GetDraggedCorner(wDriven, handleIndex);
        var hCorner = GetDraggedCorner(hDriven, handleIndex);
        var chosen  = GeometryUtilities.DistanceSquared(wCorner, newPosition) <=
                      GeometryUtilities.DistanceSquared(hCorner, newPosition)
                      ? wDriven : hDriven;

        var w = chosen.Width;
        var h = chosen.Height;
        var dragged = new NormalizedPoint(anchor.X + xDir * w, anchor.Y + yDir * h);

        if (handleIndex == TopLeftHandleIndex)
        {
            _corner0 = dragged;
            _corner1 = anchor;
        }
        else if (handleIndex == BottomRightHandleIndex)
        {
            _corner1 = dragged;
            _corner0 = anchor;
        }
        else if (handleIndex == TopRightHandleIndex)
        {
            _corner0 = new NormalizedPoint(anchor.X, dragged.Y);
            _corner1 = new NormalizedPoint(dragged.X, anchor.Y);
        }
        else // BottomLeft
        {
            _corner0 = new NormalizedPoint(dragged.X, anchor.Y);
            _corner1 = new NormalizedPoint(anchor.X, dragged.Y);
        }
    }

    private static NormalizedBounds BuildLockedBounds(
        NormalizedPoint anchor,
        double xDir, double yDir,
        double reqW, double reqH)
    {
        var maxW = xDir > 0d ? 1d - anchor.X : anchor.X;
        var maxH = yDir > 0d ? 1d - anchor.Y : anchor.Y;
        var w    = reqW;
        var h    = reqH;

        if (w > maxW || h > maxH)
        {
            var ws    = w <= double.Epsilon ? 1d : maxW / w;
            var hs    = h <= double.Epsilon ? 1d : maxH / h;
            var scale = Math.Min(ws, hs);
            w *= scale;
            h *= scale;
        }

        var moved = new NormalizedPoint(anchor.X + xDir * w, anchor.Y + yDir * h);
        return new NormalizedBounds(
            Math.Min(anchor.X, moved.X), Math.Min(anchor.Y, moved.Y),
            Math.Max(anchor.X, moved.X), Math.Max(anchor.Y, moved.Y));
    }

    private static NormalizedPoint GetDraggedCorner(NormalizedBounds b, int handleIndex) =>
        handleIndex switch
        {
            TopLeftHandleIndex     => new NormalizedPoint(b.Left,  b.Top),
            TopRightHandleIndex    => new NormalizedPoint(b.Right, b.Top),
            BottomRightHandleIndex => new NormalizedPoint(b.Right, b.Bottom),
            _                     => new NormalizedPoint(b.Left,  b.Bottom),
        };
}
