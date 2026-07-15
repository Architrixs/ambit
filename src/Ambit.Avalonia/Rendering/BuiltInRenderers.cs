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
            BackgroundColorHex = "#1E293B",
            FontSize = 11d,
            Placement = LabelPlacement.TopLeft
        };

        var textPaint = resources.ConfigureTextPaint(resolvedStyle.TextColorHex, (float)resolvedStyle.FontSize);
        var backgroundPaint = resources.ConfigureLabelBackgroundPaint(resolvedStyle.BackgroundColorHex);
        var borderPaint = resources.ConfigureOverlayPaint(new SKColor(0x47, 0x55, 0x69), 1.0f); // Sleek slate border (#475569)

        var anchorPoint = RenderingUtilities.ToSkPoint(transform, anchor);
        var measuredWidth = textPaint.MeasureText(text);
        var paddingX = 8f;
        var paddingY = 4f;
        var gap = 6f;
        var width = measuredWidth + (paddingX * 2f);
        var height = textPaint.TextSize + (paddingY * 2f);

        float left, top;
        switch (resolvedStyle.Placement)
        {
            case LabelPlacement.TopRight:
                left = anchorPoint.X - width;
                top = anchorPoint.Y - height - gap;
                break;
            case LabelPlacement.BottomLeft:
                left = anchorPoint.X;
                top = anchorPoint.Y + gap;
                break;
            case LabelPlacement.BottomRight:
                left = anchorPoint.X - width;
                top = anchorPoint.Y + gap;
                break;
            case LabelPlacement.CenterInside:
                left = anchorPoint.X - width / 2f;
                top = anchorPoint.Y - height / 2f;
                break;
            case LabelPlacement.TopLeft:
            default:
                left = anchorPoint.X;
                top = anchorPoint.Y - height - gap;
                break;
        }

        var rect = new SKRect(left, top, left + width, top + height);

        if (backgroundPaint is not null)
        {
            canvas.DrawRoundRect(rect, 4f, 4f, backgroundPaint);
            canvas.DrawRoundRect(rect, 4f, 4f, borderPaint);
        }

        canvas.DrawText(text, rect.Left + paddingX, rect.Top + paddingY + textPaint.TextSize - 1f, textPaint);
    }
}
