using SkiaSharp;

namespace Ambit.Avalonia.Rendering;

/// <summary>
/// Registers the built-in region and decoration renderers.
/// </summary>
public static class BuiltInRenderers
{
    /// <summary>
    /// Registers all built-in region and decoration renderers.
    /// </summary>
    /// <param name="registry">The registry to populate.</param>
    /// <returns>The same registry instance.</returns>
    public static RegionRenderRegistry RegisterBuiltInRenderers(this RegionRenderRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(new RectangleRegionRenderer());
        registry.Register(new PolygonRegionRenderer());
        registry.Register(new PolylineRegionRenderer());
        registry.Register(new LineRegionRenderer());
        registry.Register(new EllipseRegionRenderer());
        registry.Register(new DirectionIndicatorDecorationRenderer());
        registry.Register(new LabelDecorationRenderer());
        return registry;
    }
}

public sealed class RectangleRegionRenderer : IRegionRenderer
{
    public string TypeId => RectangleRegion.RectangleTypeId;

    public void Render(SKCanvas canvas, IRegion region, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources)
    {
        var rectangle = (RectangleRegion)region;
        var bounds = rectangle.Bounds;
        var topLeft = RenderingUtilities.ToSkPoint(transform, new NormalizedPoint(bounds.Left, bounds.Top));
        var bottomRight = RenderingUtilities.ToSkPoint(transform, new NormalizedPoint(bounds.Right, bounds.Bottom));
        var rect = new SKRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);

        var fillPaint = resources.ConfigureFillPaint(region.Style);
        if (fillPaint is not null)
        {
            canvas.DrawRect(rect, fillPaint);
        }

        canvas.DrawRect(rect, resources.ConfigureStrokePaint(region.Style));

        var overlayColor = RenderingUtilities.GetOverlayColor(region, state);
        if (overlayColor != SKColors.Transparent)
        {
            canvas.DrawRect(rect, resources.ConfigureOverlayPaint(overlayColor, (float)region.Style.StrokeThickness + 1.5f));
        }
    }
}

public sealed class PolygonRegionRenderer : IRegionRenderer
{
    public string TypeId => PolygonRegion.PolygonTypeId;

    public void Render(SKCanvas canvas, IRegion region, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources)
    {
        var path = resources.SharedPath;
        RenderingUtilities.BuildPath(path, region.Vertices, transform, closed: true);

        var fillPaint = resources.ConfigureFillPaint(region.Style);
        if (fillPaint is not null)
        {
            canvas.DrawPath(path, fillPaint);
        }

        canvas.DrawPath(path, resources.ConfigureStrokePaint(region.Style));
        var overlayColor = RenderingUtilities.GetOverlayColor(region, state);
        if (overlayColor != SKColors.Transparent)
        {
            canvas.DrawPath(path, resources.ConfigureOverlayPaint(overlayColor, (float)region.Style.StrokeThickness + 1.5f));
        }
    }
}

public sealed class PolylineRegionRenderer : IRegionRenderer
{
    public string TypeId => PolylineRegion.PolylineTypeId;

    public void Render(SKCanvas canvas, IRegion region, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources)
    {
        var path = resources.SharedPath;
        RenderingUtilities.BuildPath(path, region.Vertices, transform, closed: false);
        canvas.DrawPath(path, resources.ConfigureStrokePaint(region.Style));

        var overlayColor = RenderingUtilities.GetOverlayColor(region, state);
        if (overlayColor != SKColors.Transparent)
        {
            canvas.DrawPath(path, resources.ConfigureOverlayPaint(overlayColor, (float)region.Style.StrokeThickness + 1.5f));
        }
    }
}

public sealed class LineRegionRenderer : IRegionRenderer
{
    public string TypeId => LineRegion.LineTypeId;

    public void Render(SKCanvas canvas, IRegion region, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources)
    {
        var start = RenderingUtilities.ToSkPoint(transform, region.Vertices[0]);
        var end = RenderingUtilities.ToSkPoint(transform, region.Vertices[1]);

        canvas.DrawLine(start, end, resources.ConfigureStrokePaint(region.Style));

        var overlayColor = RenderingUtilities.GetOverlayColor(region, state);
        if (overlayColor != SKColors.Transparent)
        {
            canvas.DrawLine(start, end, resources.ConfigureOverlayPaint(overlayColor, (float)region.Style.StrokeThickness + 1.5f));
        }
    }
}

public sealed class EllipseRegionRenderer : IRegionRenderer
{
    public string TypeId => EllipseRegion.EllipseTypeId;

    public void Render(SKCanvas canvas, IRegion region, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources)
    {
        var ellipse = (EllipseRegion)region;
        var bounds = ellipse.Bounds;
        var topLeft = RenderingUtilities.ToSkPoint(transform, new NormalizedPoint(bounds.Left, bounds.Top));
        var bottomRight = RenderingUtilities.ToSkPoint(transform, new NormalizedPoint(bounds.Right, bounds.Bottom));
        var rect = new SKRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);

        var fillPaint = resources.ConfigureFillPaint(region.Style);
        if (fillPaint is not null)
        {
            canvas.DrawOval(rect, fillPaint);
        }

        canvas.DrawOval(rect, resources.ConfigureStrokePaint(region.Style));
        var overlayColor = RenderingUtilities.GetOverlayColor(region, state);
        if (overlayColor != SKColors.Transparent)
        {
            canvas.DrawOval(rect, resources.ConfigureOverlayPaint(overlayColor, (float)region.Style.StrokeThickness + 1.5f));
        }
    }
}

public sealed class DirectionIndicatorDecorationRenderer : IDecorationRenderer
{
    public string TypeId => DirectionIndicatorDecoration.DirectionIndicatorTypeId;

    public void Render(SKCanvas canvas, IDecoration decoration, IRegion owner, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources)
    {
        var directionIndicator = (DirectionIndicatorDecoration)decoration;
        var anchor = RenderingUtilities.ToSkPoint(transform, directionIndicator.Anchor);
        var direction = RenderingUtilities.GetDirectionVector(owner, directionIndicator.Anchor);
        if (directionIndicator.DirectionSign < 0)
        {
            direction = new SKPoint(-direction.X, -direction.Y);
        }

        var orthogonal = new SKPoint(-direction.Y, direction.X);
        const float arrowLength = 16f;
        const float arrowWidth = 6f;

        var tip = new SKPoint(anchor.X + (direction.X * arrowLength), anchor.Y + (direction.Y * arrowLength));
        var baseCenter = new SKPoint(anchor.X - (direction.X * 6f), anchor.Y - (direction.Y * 6f));
        var left = new SKPoint(baseCenter.X + (orthogonal.X * arrowWidth), baseCenter.Y + (orthogonal.Y * arrowWidth));
        var right = new SKPoint(baseCenter.X - (orthogonal.X * arrowWidth), baseCenter.Y - (orthogonal.Y * arrowWidth));

        var path = resources.SharedPath;
        path.Reset();
        path.MoveTo(tip);
        path.LineTo(left);
        path.LineTo(right);
        path.Close();

        resources.FillPaint.Color = new SKColor(0xF6, 0xB7, 0x26, 220);
        resources.StrokePaint.Color = new SKColor(0x7A, 0x4D, 0x00);
        resources.StrokePaint.StrokeWidth = 1.5f;
        resources.StrokePaint.PathEffect = null;

        canvas.DrawPath(path, resources.FillPaint);
        canvas.DrawPath(path, resources.StrokePaint);
    }
}

public sealed class LabelDecorationRenderer : IDecorationRenderer
{
    public string TypeId => LabelDecoration.LabelDecorationTypeId;

    public void Render(SKCanvas canvas, IDecoration decoration, IRegion owner, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources)
    {
        var labelDecoration = (LabelDecoration)decoration;
        DrawLabel(canvas, labelDecoration.Anchor, labelDecoration.Text, owner.Style.LabelStyle, transform, resources);
    }

    internal static void DrawLabel(
        SKCanvas canvas,
        NormalizedPoint anchor,
        string text,
        LabelStyle? style,
        ICoordinateTransform transform,
        SkiaRenderResources resources)
    {
        var resolvedStyle = style ?? new LabelStyle
        {
            TextColorHex = "#FFFFFF",
            BackgroundColorHex = "#202020",
            FontSize = 12d,
        };

        var textPaint = resources.ConfigureTextPaint(resolvedStyle.TextColorHex, (float)resolvedStyle.FontSize);
        var backgroundPaint = resources.ConfigureLabelBackgroundPaint(resolvedStyle.BackgroundColorHex);
        var anchorPoint = RenderingUtilities.ToSkPoint(transform, anchor);
        var measuredWidth = textPaint.MeasureText(text);
        var padding = 4f;
        var height = textPaint.TextSize + (padding * 2f);
        var rect = new SKRect(anchorPoint.X, anchorPoint.Y, anchorPoint.X + measuredWidth + (padding * 2f), anchorPoint.Y + height);

        if (backgroundPaint is not null)
        {
            canvas.DrawRoundRect(rect, 6f, 6f, backgroundPaint);
        }

        canvas.DrawText(text, rect.Left + padding, rect.Top + padding + textPaint.TextSize, textPaint);
    }
}
