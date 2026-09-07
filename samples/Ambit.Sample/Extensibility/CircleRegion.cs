using Ambit.Avalonia.Rendering;
using SkiaSharp;

namespace Ambit.Sample.Extensibility;

/// <summary>
/// Sample-only custom region. Not part of Ambit.Core.
/// </summary>
public sealed class CircleRegion : IEditableRegion
{
    public const string CircleTypeId = "circle";
    private readonly IDecoration[] _decorations;
    private readonly double _imageAspectRatio;

    public CircleRegion(
        NormalizedPoint center,
        double radius,
        double imageAspectRatio,
        RegionStyle style,
        Guid? id = null,
        IEnumerable<IDecoration>? decorations = null,
        string? label = null)
    {
        Id = id ?? Guid.NewGuid();
        _imageAspectRatio = imageAspectRatio > 0d ? imageAspectRatio : 1d;
        Center = center;
        Radius = Math.Clamp(radius, 0d, GetMaxRadius(center));
        Style = style;
        Label = label;
        _decorations = decorations?.ToArray() ?? Array.Empty<IDecoration>();
    }

    public Guid Id { get; }
    public string TypeId => CircleTypeId;
    public IReadOnlyList<NormalizedPoint> Vertices => new[] { Center, new NormalizedPoint(Center.X + Radius, Center.Y) };
    public IReadOnlyList<IDecoration> Decorations => _decorations;
    public RegionStyle Style { get; }
    public string? Label { get; }

    public NormalizedPoint Center { get; private set; }
    public double Radius { get; private set; }

    public NormalizedBounds Bounds => new(
        Math.Max(0, Center.X - Radius), Math.Max(0, Center.Y - GetVerticalRadius()),
        Math.Min(1, Center.X + Radius), Math.Min(1, Center.Y + GetVerticalRadius()));

    public IReadOnlyList<RegionHandle> GetHandles()
    {
        return new[]
        {
            new RegionHandle(0, Center, "vertex"),
            new RegionHandle(1, new NormalizedPoint(Center.X + Radius, Center.Y), "vertex"),
        };
    }

    public bool HitTestBody(NormalizedPoint point, double toleranceNormalized)
    {
        var radiusX = Radius + toleranceNormalized;
        var radiusY = GetVerticalRadius() + toleranceNormalized;
        if (radiusX <= double.Epsilon || radiusY <= double.Epsilon)
            return GeometryUtilities.Distance(point, Center) <= toleranceNormalized;
        var dx = (point.X - Center.X) / radiusX;
        var dy = (point.Y - Center.Y) / radiusY;
        return ((dx * dx) + (dy * dy)) <= 1d;
    }

    public void MoveHandle(int handleIndex, NormalizedPoint newPosition)
    {
        if (handleIndex == 0)
        {
            var verticalRadius = GetVerticalRadius();
            var clampedX = Math.Clamp(newPosition.X, Radius, 1 - Radius);
            var clampedY = Math.Clamp(newPosition.Y, verticalRadius, 1 - verticalRadius);
            Center = new NormalizedPoint(clampedX, clampedY);
        }
        else if (handleIndex == 1)
        {
            var raw = GetRadiusFromPoint(newPosition);
            Radius = Math.Clamp(raw, 0.01, Math.Max(0.01, GetMaxRadius(Center)));
        }
    }

    public void Translate(NormalizedVector delta)
    {
        var verticalRadius = GetVerticalRadius();
        var desired = new NormalizedPoint(Center.X + delta.Dx, Center.Y + delta.Dy);
        var clampedX = Math.Clamp(desired.X, Radius, 1 - Radius);
        var clampedY = Math.Clamp(desired.Y, verticalRadius, 1 - verticalRadius);
        Center = new NormalizedPoint(clampedX, clampedY);
        Radius = Math.Min(Radius, Math.Max(0.01, GetMaxRadius(Center)));
    }

    private double GetVerticalRadius() => Radius * _imageAspectRatio;
    private double GetMaxRadius(NormalizedPoint center)
    {
        var maxHorizontal = Math.Min(center.X, 1d - center.X);
        var maxVertical = Math.Min(center.Y, 1d - center.Y) / _imageAspectRatio;
        return Math.Max(0d, Math.Min(maxHorizontal, maxVertical));
    }
    private double GetRadiusFromPoint(NormalizedPoint point)
    {
        var dx = point.X - Center.X;
        var dy = (point.Y - Center.Y) / _imageAspectRatio;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}

public sealed class CircleRegionRenderer : IRegionRenderer
{
    public string TypeId => CircleRegion.CircleTypeId;
    public void Render(SKCanvas canvas, IRegion region, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources)
    {
        var circle = (CircleRegion)region;
        var centerPt = transform.ToControlSpace(circle.Center);
        var centerSk = new SKPoint((float)centerPt.X, (float)centerPt.Y);
        var edgePt = transform.ToControlSpace(new NormalizedPoint(circle.Center.X + circle.Radius, circle.Center.Y));
        var radiusPx = Math.Abs(new SKPoint((float)edgePt.X, (float)edgePt.Y).X - centerSk.X);
        var fillPaint = resources.ConfigureFillPaint(region.Style);
        if (fillPaint is not null) canvas.DrawCircle(centerSk, (float)radiusPx, fillPaint);
        canvas.DrawCircle(centerSk, (float)radiusPx, resources.ConfigureStrokePaint(region.Style));
        var overlayColor = region.Id == state.SelectedRegionId ? new SKColor(0x26, 0x80, 0xEB)
            : region.Id == state.HoveredRegionId ? new SKColor(0xFF, 0xC8, 0x3D) : SKColors.Transparent;
        if (overlayColor != SKColors.Transparent)
            canvas.DrawCircle(centerSk, (float)radiusPx, resources.ConfigureOverlayPaint(overlayColor, (float)region.Style.StrokeThickness + 1.5f));
    }
}

public sealed class CircleRegionFactory : IRegionFactory
{
    private readonly double _imageAspectRatio;
    public CircleRegionFactory(double imageAspectRatio) { _imageAspectRatio = imageAspectRatio > 0d ? imageAspectRatio : 1d; }
    public string TypeId => CircleRegion.CircleTypeId;

    public IEditableRegion Create(RegionDto dto, IReadOnlyList<IDecoration> decorations)
    {
        if (dto.Vertices.Count >= 2 && !dto.Properties.ContainsKey("radius"))
        {
            var center = dto.Vertices[0]; var edge = dto.Vertices[1];
            var dx = edge.X - center.X; var dy = (edge.Y - center.Y) / _imageAspectRatio;
            var requestedRadius = Math.Sqrt(dx*dx + dy*dy);
            var maxRadius = Math.Min(Math.Min(center.X, 1d - center.X), Math.Min(center.Y, 1d - center.Y) / _imageAspectRatio);
            var radius = Math.Clamp(requestedRadius, 0d, Math.Max(0d, maxRadius));
            return new CircleRegion(center, radius, _imageAspectRatio, dto.Style, dto.Id, decorations, dto.Label);
        }
        var r = dto.Properties.TryGetValue("radius", out var raw) && double.TryParse(raw, out var parsed) ? parsed : 0.15;
        return new CircleRegion(dto.Vertices[0], r, _imageAspectRatio, dto.Style, dto.Id, decorations, dto.Label);
    }

    public IEditableRegion CreateDraft(Guid id, NormalizedPoint a, NormalizedPoint b, RegionStyle style, IReadOnlyList<IDecoration> decorations)
    {
        var dx = b.X - a.X; var dy = (b.Y - a.Y) / _imageAspectRatio;
        var requestedRadius = Math.Sqrt(dx*dx + dy*dy);
        var maxRadius = Math.Min(Math.Min(a.X, 1d - a.X), Math.Min(a.Y, 1d - a.Y) / _imageAspectRatio);
        var radius = Math.Clamp(requestedRadius, 0d, Math.Max(0d, maxRadius));
        return new CircleRegion(a, radius, _imageAspectRatio, style, id, decorations, label: null);
    }

    public RegionDto ToDto(IRegion region, IReadOnlyList<DecorationDto> decorations)
    {
        var circle = (CircleRegion)region;
        return new RegionDto
        {
            Id = circle.Id, TypeId = circle.TypeId, Vertices = new[] { circle.Center },
            Decorations = decorations.ToArray(), Style = circle.Style, Label = circle.Label,
            Properties = new Dictionary<string, string?> { ["radius"] = circle.Radius.ToString() },
        };
    }
}
