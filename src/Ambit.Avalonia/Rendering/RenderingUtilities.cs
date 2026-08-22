using SkiaSharp;

namespace Ambit.Avalonia.Rendering;

internal static class RenderingUtilities
{
    public static SKPoint ToSkPoint(ICoordinateTransform transform, NormalizedPoint point)
    {
        var controlPoint = transform.ToControlSpace(point);
        return new SKPoint((float)controlPoint.X, (float)controlPoint.Y);
    }

    public static SKRect GetControlRect(ICoordinateTransform transform)
    {
        var topLeft = transform.ToControlSpace(new NormalizedPoint(0d, 0d));
        var bottomRight = transform.ToControlSpace(new NormalizedPoint(1d, 1d));
        var left = (float)Math.Min(topLeft.X, bottomRight.X);
        var top = (float)Math.Min(topLeft.Y, bottomRight.Y);
        var right = (float)Math.Max(topLeft.X, bottomRight.X);
        var bottom = (float)Math.Max(topLeft.Y, bottomRight.Y);
        return new SKRect(left, top, right, bottom);
    }

    public static void BuildPath(SKPath path, IReadOnlyList<NormalizedPoint> vertices, ICoordinateTransform transform, bool closed)
    {
        path.Reset();
        if (vertices.Count == 0)
        {
            return;
        }

        path.MoveTo(ToSkPoint(transform, vertices[0]));
        for (var index = 1; index < vertices.Count; index++)
        {
            path.LineTo(ToSkPoint(transform, vertices[index]));
        }

        if (closed)
        {
            path.Close();
        }
    }

    public static SKColor GetOverlayColor(IRegion region, RegionRenderState state)
    {
        return region.Id == state.SelectedRegionId
            ? new SKColor(0x26, 0x80, 0xEB)
            : region.Id == state.HoveredRegionId
                ? new SKColor(0xFF, 0xC8, 0x3D)
                : SKColors.Transparent;
    }

    public static NormalizedPoint GetDefaultLabelAnchor(IRegion region)
    {
        // Preferred: use IEditableRegion.Bounds for accurate center (covers custom Circle etc.)
        if (region is IEditableRegion editable)
            return GetBoundsCenter(editable.Bounds);
        return region switch
        {
            RectangleRegion rectangle => GetBoundsCenter(rectangle.Bounds),
            EllipseRegion ellipse => GetBoundsCenter(ellipse.Bounds),
            LineRegion line => new NormalizedPoint(
                (line.Vertices[0].X + line.Vertices[1].X) / 2d,
                (line.Vertices[0].Y + line.Vertices[1].Y) / 2d),
            PolygonRegion polygon => GeometryUtilities.GetPolygonCentroid(polygon.Vertices),
            PolylineRegion polyline => GetAverage(polyline.Vertices),
            _ when region.Vertices.Count > 0 => GetAverage(region.Vertices),
            _ => new NormalizedPoint(0.5, 0.5),
        };
    }

    public static NormalizedPoint GetLabelAnchor(IRegion region, LabelPlacement placement)
    {
        if (region.Vertices.Count == 0)
        {
            return new NormalizedPoint(0.5, 0.5);
        }

        // Use IEditableRegion.Bounds when available (e.g. CircleRegion) for accurate anchor,
        // otherwise fall back to Vertices bounds
        NormalizedBounds bounds;
        if (region is IEditableRegion editable)
        {
            bounds = editable.Bounds;
        }
        else if (region.Vertices.Count > 0)
        {
            bounds = GeometryUtilities.GetBounds(region.Vertices);
        }
        else
        {
            bounds = new NormalizedBounds(0.5, 0.5, 0.5, 0.5);
        }

        return placement switch
        {
            LabelPlacement.TopLeft => new NormalizedPoint(bounds.Left, bounds.Top),
            LabelPlacement.TopRight => new NormalizedPoint(bounds.Right, bounds.Top),
            LabelPlacement.BottomLeft => new NormalizedPoint(bounds.Left, bounds.Bottom),
            LabelPlacement.BottomRight => new NormalizedPoint(bounds.Right, bounds.Bottom),
            LabelPlacement.CenterInside => new NormalizedPoint(bounds.Left + bounds.Width / 2d, bounds.Top + bounds.Height / 2d),
            _ => new NormalizedPoint(bounds.Left, bounds.Top)
        };
    }

    public static SKPoint GetDirectionVector(IRegion region, NormalizedPoint anchor)
    {
        if (region.Vertices.Count < 2)
        {
            return new SKPoint(1f, 0f);
        }

        var nearestStart = region.Vertices[0];
        var nearestEnd = region.Vertices[1];
        var nearestDistance = double.MaxValue;
        var segmentCount = region is PolygonRegion ? region.Vertices.Count : region.Vertices.Count - 1;

        for (var index = 0; index < segmentCount; index++)
        {
            var start = region.Vertices[index];
            var end = region.Vertices[(index + 1) % region.Vertices.Count];
            var distance = GeometryUtilities.DistanceToSegment(anchor, start, end);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestStart = start;
                nearestEnd = end;
            }
        }

        var dx = (float)(nearestEnd.X - nearestStart.X);
        var dy = (float)(nearestEnd.Y - nearestStart.Y);
        var length = MathF.Sqrt((dx * dx) + (dy * dy));
        return length <= float.Epsilon ? new SKPoint(1f, 0f) : new SKPoint(dx / length, dy / length);
    }

    private static NormalizedPoint GetBoundsCenter(NormalizedBounds bounds)
    {
        return new NormalizedPoint(bounds.Left + (bounds.Width / 2d), bounds.Top + (bounds.Height / 2d));
    }

    private static NormalizedPoint GetAverage(IReadOnlyList<NormalizedPoint> vertices)
    {
        double sumX = 0d;
        double sumY = 0d;
        for (var index = 0; index < vertices.Count; index++)
        {
            sumX += vertices[index].X;
            sumY += vertices[index].Y;
        }

        return new NormalizedPoint(sumX / vertices.Count, sumY / vertices.Count);
    }
}
