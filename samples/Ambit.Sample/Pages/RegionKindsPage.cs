using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Ambit.Avalonia.Controls;
using Ambit.Avalonia.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SkiaSharp;

namespace Ambit.Sample.Pages;

// --- Custom Circle Region (from Extensibility Proof) ---
public sealed class CircleRegion : IEditableRegion
{
    public const string CircleTypeId = "circle";
    private readonly IDecoration[] _decorations;

    public CircleRegion(
        NormalizedPoint center,
        double radius,
        RegionStyle style,
        Guid? id = null,
        IEnumerable<IDecoration>? decorations = null,
        string? label = null)
    {
        Id = id ?? Guid.NewGuid();
        Center = center;
        Radius = radius;
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
        Math.Max(0, Center.X - Radius), Math.Max(0, Center.Y - Radius),
        Math.Min(1, Center.X + Radius), Math.Min(1, Center.Y + Radius));

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
        var dist = GeometryUtilities.Distance(point, Center);
        if (!string.IsNullOrWhiteSpace(Style.FillColorHex) && Style.FillOpacity > 0)
        {
            return dist <= (Radius + toleranceNormalized);
        }
        return Math.Abs(dist - Radius) <= toleranceNormalized;
    }

    public void MoveHandle(int handleIndex, NormalizedPoint newPosition)
    {
        if (handleIndex == 0)
        {
            Center = newPosition;
        }
        else if (handleIndex == 1)
        {
            Radius = Math.Max(0.01, GeometryUtilities.Distance(newPosition, Center));
        }
    }

    public void Translate(NormalizedVector delta)
    {
        Center = GeometryUtilities.Translate(Center, delta);
    }
}

// --- Custom Circle Renderer ---
public sealed class CircleRegionRenderer : IRegionRenderer
{
    public string TypeId => CircleRegion.CircleTypeId;

    public void Render(SKCanvas canvas, IRegion region, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources)
    {
        var circle = (CircleRegion)region;
        var centerPt = transform.ToControlSpace(circle.Center);
        var centerSk = new SKPoint((float)centerPt.X, (float)centerPt.Y);
        
        var edgePt = transform.ToControlSpace(new NormalizedPoint(circle.Center.X + circle.Radius, circle.Center.Y));
        var edgeSk = new SKPoint((float)edgePt.X, (float)edgePt.Y);
        var radiusPx = Math.Abs(edgeSk.X - centerSk.X);

        var fillPaint = resources.ConfigureFillPaint(region.Style);
        if (fillPaint is not null)
        {
            canvas.DrawCircle(centerSk, (float)radiusPx, fillPaint);
        }

        canvas.DrawCircle(centerSk, (float)radiusPx, resources.ConfigureStrokePaint(region.Style));

        var overlayColor = region.Id == state.SelectedRegionId
            ? new SKColor(0x26, 0x80, 0xEB)
            : region.Id == state.HoveredRegionId
                ? new SKColor(0xFF, 0xC8, 0x3D)
                : SKColors.Transparent;

        if (overlayColor != SKColors.Transparent)
        {
            canvas.DrawCircle(centerSk, (float)radiusPx, resources.ConfigureOverlayPaint(overlayColor, (float)region.Style.StrokeThickness + 1.5f));
        }
    }
}

// --- Custom Circle Factory ---
public sealed class CircleRegionFactory : IRegionFactory
{
    public string TypeId => CircleRegion.CircleTypeId;

    public IEditableRegion Create(RegionDto dto, IReadOnlyList<IDecoration> decorations)
    {
        // Handle 2-vertex draft from controller (a and b are drag corners) — make it feel like dragging a bounding box
        if (dto.Vertices.Count >= 2 && !dto.Properties.ContainsKey("radius"))
        {
            var a = dto.Vertices[0];
            var b = dto.Vertices[1];
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var center = new NormalizedPoint((a.X + b.X) / 2, (a.Y + b.Y) / 2);
            var radius = Math.Min(Math.Abs(dx), Math.Abs(dy)) / 2;
            if (radius < 0.01) radius = GeometryUtilities.Distance(a, b) / 2;
            if (radius > 1e-6)
                return new CircleRegion(center, radius, dto.Style, dto.Id, decorations, dto.Label ?? "Circle");
        }
        var r = dto.Properties.TryGetValue("radius", out var raw) && double.TryParse(raw, out var parsed) ? parsed : 0.15;
        return new CircleRegion(dto.Vertices[0], r, dto.Style, dto.Id, decorations, dto.Label);
    }

    public RegionDto ToDto(IRegion region, IReadOnlyList<DecorationDto> decorations)
    {
        var circle = (CircleRegion)region;
        return new RegionDto
        {
            Id = circle.Id,
            TypeId = circle.TypeId,
            Vertices = new[] { circle.Center },
            Decorations = decorations.ToArray(),
            Style = circle.Style,
            Label = circle.Label,
            Properties = new Dictionary<string, string?> { ["radius"] = circle.Radius.ToString() },
        };
    }
}

// --- Custom Direction Indicator Decoration ---
public sealed class DirectionIndicatorDecoration : IToggleDecoration
{
    public const string DirectionIndicatorTypeId = "direction-arrow";

    public DirectionIndicatorDecoration(NormalizedPoint anchor, int directionSign = 1)
    {
        Anchor = anchor;
        DirectionSign = NormalizeDirectionSign(directionSign);
    }

    public string TypeId => DirectionIndicatorTypeId;
    public NormalizedPoint Anchor { get; set; }
    public bool IsInteractive => true;
    public int DirectionSign { get; set; }
    public string? StrokeColorHex { get; set; }
    public string? FillColorHex { get; set; }
    public float? ArrowSize { get; set; }

    public void Toggle()
    {
        DirectionSign = DirectionSign switch
        {
            1 => -1,
            -1 => 2,
            2 => 0,
            _ => 1,
        };
    }

    private static int NormalizeDirectionSign(int directionSign)
    {
        return directionSign switch
        {
            1 => 1,
            -1 => -1,
            2 => 2,
            0 => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(directionSign), "Direction sign must be 1, -1, 2, or 0."),
        };
    }
}

// --- Custom Direction Indicator Decoration Renderer ---
public sealed class DirectionIndicatorDecorationRenderer : IDecorationRenderer
{
    public string TypeId => DirectionIndicatorDecoration.DirectionIndicatorTypeId;

    private static SKPoint ToSkPoint(ICoordinateTransform transform, NormalizedPoint p)
    {
        var cp = transform.ToControlSpace(p);
        return new SKPoint((float)cp.X, (float)cp.Y);
    }

    private static SKPoint GetDirectionVector(IRegion region, NormalizedPoint anchor)
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

    public void Render(SKCanvas canvas, IDecoration decoration, IRegion owner, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources)
    {
        var directionIndicator = (DirectionIndicatorDecoration)decoration;
        var anchor = ToSkPoint(transform, directionIndicator.Anchor);
        var direction = GetDirectionVector(owner, directionIndicator.Anchor);

        var scale = directionIndicator.ArrowSize ?? 1.0f;
        var sign = directionIndicator.DirectionSign;

        var perpA = new SKPoint(-direction.Y, direction.X);
        var perpB = new SKPoint(direction.Y, -direction.X);

        if (sign == 1)
        {
            DrawArrow(canvas, anchor, perpA, direction, scale, resources, directionIndicator);
        }
        else if (sign == -1)
        {
            DrawArrow(canvas, anchor, perpB, direction, scale, resources, directionIndicator);
        }
        else if (sign == 2)
        {
            DrawArrow(canvas, anchor, perpA, direction, scale, resources, directionIndicator);
            DrawArrow(canvas, anchor, perpB, direction, scale, resources, directionIndicator);
        }
        else
        {
            var strokeColor = directionIndicator.StrokeColorHex != null ? SKColor.Parse(directionIndicator.StrokeColorHex) : new SKColor(0x94, 0xA3, 0xB8);
            resources.StrokePaint.Color = strokeColor;
            resources.StrokePaint.StrokeWidth = 2.0f * scale;
            resources.StrokePaint.Style = SKPaintStyle.Stroke;
            resources.StrokePaint.PathEffect = null;

            var tickLength = 5f * scale;
            canvas.DrawLine(
                new SKPoint(anchor.X - perpA.X * tickLength, anchor.Y - perpA.Y * tickLength),
                new SKPoint(anchor.X + perpA.X * tickLength, anchor.Y + perpA.Y * tickLength),
                resources.StrokePaint);
        }
    }

    private static void DrawArrow(
        SKCanvas canvas,
        SKPoint anchor,
        SKPoint dir,
        SKPoint segmentDir,
        float scale,
        SkiaRenderResources resources,
        DirectionIndicatorDecoration decoration)
    {
        float arrowLength = 14f * scale;
        float wingBack = 5f * scale;
        float wingOut = 3.5f * scale;

        var tip = new SKPoint(anchor.X + (dir.X * arrowLength), anchor.Y + (dir.Y * arrowLength));
        var leftWing = new SKPoint(tip.X - dir.X * wingBack + segmentDir.X * wingOut, tip.Y - dir.Y * wingBack + segmentDir.Y * wingOut);
        var rightWing = new SKPoint(tip.X - dir.X * wingBack - segmentDir.X * wingOut, tip.Y - dir.Y * wingBack - segmentDir.Y * wingOut);

        var strokeColor = decoration.StrokeColorHex != null ? SKColor.Parse(decoration.StrokeColorHex) : new SKColor(0xF5, 0x9E, 0x0B);

        resources.StrokePaint.Color = strokeColor;
        resources.StrokePaint.StrokeWidth = 2.0f * scale;
        resources.StrokePaint.Style = SKPaintStyle.Stroke;
        resources.StrokePaint.PathEffect = null;

        canvas.DrawLine(anchor, tip, resources.StrokePaint);
        canvas.DrawLine(tip, leftWing, resources.StrokePaint);
        canvas.DrawLine(tip, rightWing, resources.StrokePaint);
    }
}

// --- Custom Direction Indicator Decoration Factory ---
public sealed class DirectionIndicatorDecorationFactory : IDecorationFactory
{
    private const string DirectionSignProperty = "directionSign";
    private const string StrokeColorProperty = "strokeColor";
    private const string FillColorProperty = "fillColor";
    private const string ArrowSizeProperty = "arrowSize";

    public string TypeId => DirectionIndicatorDecoration.DirectionIndicatorTypeId;

    public IDecoration Create(DecorationDto dto)
    {
        var directionSign = dto.Properties.TryGetValue(DirectionSignProperty, out var rawValue)
            && int.TryParse(rawValue, out var parsedValue)
            ? parsedValue
            : 1;

        var strokeColor = dto.Properties.TryGetValue(StrokeColorProperty, out var sc) ? sc : null;
        var fillColor = dto.Properties.TryGetValue(FillColorProperty, out var fc) ? fc : null;
        var arrowSize = dto.Properties.TryGetValue(ArrowSizeProperty, out var sz) && float.TryParse(sz, out var s) ? (float?)s : null;

        return new DirectionIndicatorDecoration(dto.Anchor, directionSign)
        {
            StrokeColorHex = strokeColor,
            FillColorHex = fillColor,
            ArrowSize = arrowSize,
        };
    }

    public DecorationDto ToDto(IDecoration decoration)
    {
        var indicator = decoration as DirectionIndicatorDecoration
            ?? throw new ArgumentException("Decoration must be a DirectionIndicatorDecoration.", nameof(decoration));

        return new DecorationDto
        {
            TypeId = indicator.TypeId,
            Anchor = indicator.Anchor,
            IsInteractive = indicator.IsInteractive,
            Properties = new Dictionary<string, string?>
            {
                [DirectionSignProperty] = indicator.DirectionSign.ToString(),
                [StrokeColorProperty] = indicator.StrokeColorHex,
                [FillColorProperty] = indicator.FillColorHex,
                [ArrowSizeProperty] = indicator.ArrowSize?.ToString(),
            },
        };
    }
}

// --- Custom Count Badge Decoration ---
public sealed class CountBadgeDecoration : IDecoration
{
    public const string CountBadgeTypeId = "count-badge";

    public CountBadgeDecoration(NormalizedPoint anchor, int count)
    {
        Anchor = anchor;
        Count = count;
    }

    public string TypeId => CountBadgeTypeId;
    public NormalizedPoint Anchor { get; }
    public bool IsInteractive => false;
    public int Count { get; }
}

// --- Custom Count Badge Renderer ---
public sealed class CountBadgeDecorationRenderer : IDecorationRenderer
{
    public string TypeId => CountBadgeDecoration.CountBadgeTypeId;

    public void Render(SKCanvas canvas, IDecoration decoration, IRegion owner, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources)
    {
        var badge = (CountBadgeDecoration)decoration;
        var anchorControl = transform.ToControlSpace(badge.Anchor);
        var anchorPt = new SKPoint((float)anchorControl.X, (float)anchorControl.Y);

        var badgePaint = resources.FillPaint;
        badgePaint.Color = new SKColor(239, 68, 68);

        var strokePaint = resources.StrokePaint;
        strokePaint.Color = SKColors.White;
        strokePaint.StrokeWidth = 1.5f;
        strokePaint.PathEffect = null;

        canvas.DrawCircle(anchorPt, 10f, badgePaint);
        canvas.DrawCircle(anchorPt, 10f, strokePaint);

        var textPaint = resources.ConfigureTextPaint("#FFFFFF", 11f);
        var text = badge.Count.ToString();
        var width = textPaint.MeasureText(text);
        canvas.DrawText(text, anchorPt.X - (width / 2f), anchorPt.Y + 4f, textPaint);
    }
}

// --- Custom Count Badge Factory ---
public sealed class CountBadgeDecorationFactory : IDecorationFactory
{
    public string TypeId => CountBadgeDecoration.CountBadgeTypeId;

    public IDecoration Create(DecorationDto dto)
    {
        var count = dto.Properties.TryGetValue("count", out var raw) && int.TryParse(raw, out var parsed) ? parsed : 0;
        return new CountBadgeDecoration(dto.Anchor, count);
    }

    public DecorationDto ToDto(IDecoration decoration)
    {
        var badge = (CountBadgeDecoration)decoration;
        return new DecorationDto
        {
            TypeId = badge.TypeId,
            Anchor = badge.Anchor,
            IsInteractive = badge.IsInteractive,
            Properties = new Dictionary<string, string?> { ["count"] = badge.Count.ToString() },
        };
    }
}

// --- Main Page Implementation ---
public sealed class RegionKindsPage : UserControl, IDisposable
{
    private readonly RegionEditorControl _editor;
    private readonly RegionEditController _controller;
    private readonly IRegionTypeRegistry _typeRegistry;
    private readonly HashSet<Guid> _knownRegionIds = new();

    // UI Elements
    private readonly CheckBox _aspectRatioLockCheckbox;
    private readonly Border _propertiesCard;
    private readonly TextBlock _noSelectionText;
    private readonly StackPanel _propertiesStack;
    private readonly TextBox _labelTextbox;
    private readonly ComboBox _placementCombobox;
    private readonly ComboBox _labelFontSizeCombobox;
    private readonly ComboBox _labelTextColorCombobox;
    private readonly ComboBox _labelBgColorCombobox;
    private readonly ComboBox _colorCombobox;
    private readonly ComboBox _thicknessCombobox;
    private readonly ComboBox _strokeStyleCombobox;
    private readonly Button _cycleArrowsButton;
    private readonly TextBox _jsonTextBox;

    private bool _isPopulatingUi;

    // Color definitions
    private static readonly (string Name, string Hex)[] ColorsList =
    {
        ("Blue", "#3B82F6"),
        ("Green", "#10B981"),
        ("Red", "#EF4444"),
        ("Yellow", "#F59E0B"),
        ("Pink", "#EC4899"),
        ("Purple", "#8B5CF6"),
        ("Slate", "#64748B"),
    };

    public RegionKindsPage()
    {
        // 1. Setup registries — showcase OCP extensibility: DirectionIndicatorDecoration
        // and CountBadgeDecoration are NOT built into Ambit.Core; they are registered
        // here purely as sample-level extensions to prove the registry pattern.
        _typeRegistry = new RegionTypeRegistry().RegisterBuiltInTypes();
        _typeRegistry.Register(new CircleRegionFactory());
        _typeRegistry.Register(new CountBadgeDecorationFactory());
        _typeRegistry.Register(new DirectionIndicatorDecorationFactory());

        var renderRegistry = new RegionRenderRegistry().RegisterBuiltInRenderers();
        renderRegistry.Register(new CircleRegionRenderer());
        renderRegistry.Register(new CountBadgeDecorationRenderer());
        renderRegistry.Register(new DirectionIndicatorDecorationRenderer());
        var customRenderer = new RegionOverlayRenderer(renderRegistry);

        _controller = new RegionEditController
        {
            RegionTypeRegistry = _typeRegistry, // P4: registry-driven drawing for custom types (e.g. Circle)
        };
        _editor = new RegionEditorControl(_controller, customRenderer)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            BackgroundImage = SharedAssets.SchoenbrunnSKBitmap,
            IsPanZoomEnabled = true,
        };

        // 2. Prepopulate regions with labels and styles
        var defaultStyle = new RegionStyle { StrokeColorHex = "#3B82F6", FillColorHex = "#3B82F6", FillOpacity = 0.15 };
        var activeStyle = new RegionStyle
        {
            StrokeColorHex = "#10B981",
            FillColorHex = "#10B981",
            FillOpacity = 0.15,
            StrokeDashPattern = new double[] { 6.0, 4.0 },
            LabelStyle = new LabelStyle { TextColorHex = "#FFFFFF", BackgroundColorHex = "#064E3B", FontSize = 12d, Placement = LabelPlacement.TopRight }
        };
        var lineStyle = new RegionStyle { StrokeColorHex = "#F59E0B", StrokeThickness = 3.0 };

        var rect = new RectangleRegion(new NormalizedPoint(0.1, 0.1), new NormalizedPoint(0.4, 0.45), defaultStyle, label: "Zone Alpha");
        _knownRegionIds.Add(rect.Id);

        var lineStart = new NormalizedPoint(0.2, 0.7);
        var lineEnd = new NormalizedPoint(0.8, 0.7);
        var arrowDec = new DirectionIndicatorDecoration(
            new NormalizedPoint((lineStart.X + lineEnd.X) / 2, (lineStart.Y + lineEnd.Y) / 2),
            directionSign: 1);
        var tripwire = new LineRegion(
            lineStart,
            lineEnd,
            lineStyle,
            decorations: new IDecoration[] { arrowDec },
            label: "ANPR Tripwire");
        _knownRegionIds.Add(tripwire.Id);

        var activeZone = new RectangleRegion(
            new NormalizedPoint(0.5, 0.15),
            new NormalizedPoint(0.8, 0.45),
            activeStyle,
            label: "Detection Zone");
        _knownRegionIds.Add(activeZone.Id);

        _controller.SetRegions(new IEditableRegion[] { rect, tripwire, activeZone });

        // 3. Sidebar Controls (compact 220px layout)
        var controlsPanel = new StackPanel { Spacing = 10, Margin = new Thickness(0) };

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Editor & Drawings",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 4),
        });
        // OCP proof banner — visible confirmation that custom types required zero library edits
        controlsPanel.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.Parse("#ECFDF5")),
            BorderBrush = new SolidColorBrush(Color.Parse("#A7F3D0")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 6),
            Margin = new Thickness(0, 0, 0, 6),
            Child = new TextBlock
            {
                Text = "OCP proof: CircleRegion + DirectionIndicatorDecoration/CountBadge are registered only here — zero Ambit.Core/Avalonia files modified.",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.Parse("#065F46")),
                TextWrapping = TextWrapping.Wrap,
            }
        });

        // Draw Mode Selector
        controlsPanel.Children.Add(new TextBlock { Text = "Draw Mode:", FontSize = 12, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        var selectMode = new RadioButton { Content = "Select / Edit Mode", IsChecked = true, FontSize = 12 };
        var rectMode = new RadioButton { Content = "Draw Rectangle", FontSize = 12 };
        var ellipseMode = new RadioButton { Content = "Draw Ellipse", FontSize = 12 };
        var lineMode = new RadioButton { Content = "Draw Line", FontSize = 12 };
        var polylineMode = new RadioButton { Content = "Draw Polyline", FontSize = 12 };
        var polygonMode = new RadioButton { Content = "Draw Polygon", FontSize = 12 };
        var circleMode = new RadioButton { Content = "Draw Custom Circle", FontSize = 12 };

        selectMode.IsCheckedChanged += (_, _) => { if (selectMode.IsChecked == true) _controller.ActiveDrawTypeId = null; };
        rectMode.IsCheckedChanged += (_, _) => { if (rectMode.IsChecked == true) _controller.ActiveDrawTypeId = RectangleRegion.RectangleTypeId; };
        ellipseMode.IsCheckedChanged += (_, _) => { if (ellipseMode.IsChecked == true) _controller.ActiveDrawTypeId = EllipseRegion.EllipseTypeId; };
        lineMode.IsCheckedChanged += (_, _) => { if (lineMode.IsChecked == true) _controller.ActiveDrawTypeId = LineRegion.LineTypeId; };
        polylineMode.IsCheckedChanged += (_, _) => { if (polylineMode.IsChecked == true) _controller.ActiveDrawTypeId = PolylineRegion.PolylineTypeId; };
        polygonMode.IsCheckedChanged += (_, _) => { if (polygonMode.IsChecked == true) _controller.ActiveDrawTypeId = PolygonRegion.PolygonTypeId; };
        circleMode.IsCheckedChanged += (_, _) => { if (circleMode.IsChecked == true) _controller.ActiveDrawTypeId = CircleRegion.CircleTypeId; };

        controlsPanel.Children.Add(selectMode);
        controlsPanel.Children.Add(rectMode);
        controlsPanel.Children.Add(ellipseMode);
        controlsPanel.Children.Add(lineMode);
        controlsPanel.Children.Add(polylineMode);
        controlsPanel.Children.Add(polygonMode);
        controlsPanel.Children.Add(circleMode);

        _aspectRatioLockCheckbox = new CheckBox { Content = "Lock Aspect Ratio", IsChecked = false, Margin = new Thickness(0, 2, 0, 0), FontSize = 12 };
        _aspectRatioLockCheckbox.IsCheckedChanged += (_, _) => ApplyAspectLock();
        controlsPanel.Children.Add(_aspectRatioLockCheckbox);

        controlsPanel.Children.Add(new Separator { Background = new SolidColorBrush(Color.Parse("#E2E8F0")), Margin = new Thickness(0, 4, 0, 4) });

        // Properties Editor Panel
        _propertiesStack = new StackPanel { Spacing = 8 };

        _propertiesStack.Children.Add(new TextBlock { Text = "Label Text:", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        _labelTextbox = new TextBox { Watermark = "Label Text", FontSize = 12 };
        _labelTextbox.TextChanged += (s, e) => UpdateSelectedRegionLabel();
        _propertiesStack.Children.Add(_labelTextbox);

        _propertiesStack.Children.Add(new TextBlock { Text = "Label Placement:", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        _placementCombobox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = Enum.GetNames<LabelPlacement>(),
            SelectedIndex = 0,
        };
        _placementCombobox.SelectionChanged += (s, e) => UpdateSelectedRegionLabelPlacement();
        _propertiesStack.Children.Add(_placementCombobox);

        _propertiesStack.Children.Add(new TextBlock { Text = "Label Font Size:", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        _labelFontSizeCombobox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "10", "11", "12", "14", "16" },
            SelectedIndex = 1,
        };
        _labelFontSizeCombobox.SelectionChanged += (s, e) => UpdateSelectedRegionLabelStyle();
        _propertiesStack.Children.Add(_labelFontSizeCombobox);

        _propertiesStack.Children.Add(new TextBlock { Text = "Label Text Color:", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        _labelTextColorCombobox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "White", "Black", "Yellow", "Cyan" },
            SelectedIndex = 0,
        };
        _labelTextColorCombobox.SelectionChanged += (s, e) => UpdateSelectedRegionLabelStyle();
        _propertiesStack.Children.Add(_labelTextColorCombobox);

        _propertiesStack.Children.Add(new TextBlock { Text = "Label Background:", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        _labelBgColorCombobox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "Dark", "Blue", "Green", "Transparent" },
            SelectedIndex = 0,
        };
        _labelBgColorCombobox.SelectionChanged += (s, e) => UpdateSelectedRegionLabelStyle();
        _propertiesStack.Children.Add(_labelBgColorCombobox);

        _propertiesStack.Children.Add(new TextBlock { Text = "Color Theme:", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        _colorCombobox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = ColorsList.Select(c => c.Name).ToList(),
            SelectedIndex = 0,
        };
        _colorCombobox.SelectionChanged += (s, e) => UpdateSelectedRegionStyle();
        _propertiesStack.Children.Add(_colorCombobox);

        _propertiesStack.Children.Add(new TextBlock { Text = "Stroke Width:", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        _thicknessCombobox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "1 px", "2 px", "3 px", "5 px", "8 px" },
            SelectedIndex = 1,
        };
        _thicknessCombobox.SelectionChanged += (s, e) => UpdateSelectedRegionStyle();
        _propertiesStack.Children.Add(_thicknessCombobox);

        _propertiesStack.Children.Add(new TextBlock { Text = "Stroke Style:", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        _strokeStyleCombobox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "Solid", "Dashed" },
            SelectedIndex = 0,
        };
        _strokeStyleCombobox.SelectionChanged += (s, e) => UpdateSelectedRegionStyle();
        _propertiesStack.Children.Add(_strokeStyleCombobox);

        _cycleArrowsButton = new Button
        {
            Content = "Cycle Arrow Direction",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 4, 0, 0),
        };
        _cycleArrowsButton.Click += (s, e) => CycleSelectedLineArrows();
        _propertiesStack.Children.Add(_cycleArrowsButton);

        _noSelectionText = new TextBlock
        {
            Text = "Select a shape on the canvas to configure its style and label properties.",
            FontStyle = FontStyle.Italic,
            Foreground = new SolidColorBrush(Color.Parse("#64748B")),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            LineHeight = 16,
            Margin = new Thickness(0, 4, 0, 4),
        };

        _propertiesCard = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#F8FAFC")),
            BorderBrush = new SolidColorBrush(Color.Parse("#E2E8F0")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Child = new Grid
            {
                Children = { _noSelectionText, _propertiesStack }
            }
        };
        controlsPanel.Children.Add(_propertiesCard);

        controlsPanel.Children.Add(new Separator { Background = new SolidColorBrush(Color.Parse("#E2E8F0")), Margin = new Thickness(0, 4, 0, 4) });

        // Actions
        var fitButton = new Button { Content = "Fit to Canvas", HorizontalAlignment = HorizontalAlignment.Stretch };
        fitButton.Click += (_, _) => _editor.FitToCanvas();
        controlsPanel.Children.Add(fitButton);

        var clearButton = new Button { Content = "Clear All Regions", HorizontalAlignment = HorizontalAlignment.Stretch };
        clearButton.Click += (_, _) =>
        {
            _controller.SetRegions(Array.Empty<IEditableRegion>());
            _controller.CancelActiveOperation();
        };
        controlsPanel.Children.Add(clearButton);

        // Compact JSON Serialization Expander
        _jsonTextBox = new TextBox
        {
            Height = 110,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            IsReadOnly = true,
            FontSize = 9,
            Background = new SolidColorBrush(Color.Parse("#F1F5F9")),
            BorderBrush = new SolidColorBrush(Color.Parse("#E2E8F0")),
        };

        var saveButton = new Button { Content = "Save JSON", Margin = new Thickness(0, 0, 4, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
        Grid.SetColumn(saveButton, 0);
        saveButton.Click += (_, _) => SaveToJSON();

        var loadButton = new Button { Content = "Load JSON", Margin = new Thickness(4, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
        Grid.SetColumn(loadButton, 1);
        loadButton.Click += (_, _) => LoadFromJSON();

        var jsonButtonsGrid = new Grid();
        jsonButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        jsonButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        jsonButtonsGrid.Children.Add(saveButton);
        jsonButtonsGrid.Children.Add(loadButton);

        var jsonPanel = new StackPanel
        {
            Spacing = 8,
            Children = { jsonButtonsGrid, _jsonTextBox }
        };

        var jsonExpander = new Expander
        {
            Header = "DTO Serialization / JSON",
            Content = jsonPanel,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 4, 0, 0),
        };
        controlsPanel.Children.Add(jsonExpander);

        var scrollViewer = new ScrollViewer
        {
            Content = controlsPanel,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        };

        var card = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#FFFFFF")),
            BorderBrush = new SolidColorBrush(Color.Parse("#E2E8F0")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(12),
            Padding = new Thickness(12),
            Child = scrollViewer,
        };

        // Right preview container
        var canvasContainer = SharedAssets.CreatePreviewContainer(_editor);
        canvasContainer.Margin = new Thickness(12);

        // Page layout grid (compact 220px control bar)
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(card, 0);
        Grid.SetColumn(canvasContainer, 1);

        grid.Children.Add(card);
        grid.Children.Add(canvasContainer);

        Content = grid;

        // Controller Event Subscriptions
        _controller.RegionsChanged += OnRegionsChanged;
        _controller.RenderStateChanged += OnRenderStateChanged;

        // Initial setup
        UpdateSelectionUi();
        SaveToJSON();
    }

    private void ApplyAspectLock()
    {
        var isLocked = _aspectRatioLockCheckbox.IsChecked == true;
        foreach (var region in _controller.Regions)
        {
            if (region is RectangleRegion r) r.LockAspectRatio = isLocked;
            if (region is EllipseRegion el) el.LockAspectRatio = isLocked;
        }
        if (_controller.DrawingRegion is RectangleRegion dr) dr.LockAspectRatio = isLocked;
        if (_controller.DrawingRegion is EllipseRegion del) del.LockAspectRatio = isLocked;
        _editor.InvalidateVisual();
    }

    private void OnRegionsChanged(object? sender, EventArgs e)
    {
        AutoConfigureNewRegions();
        SaveToJSON();
    }

    private void OnRenderStateChanged(object? sender, EventArgs e)
    {
        UpdateSelectionUi();
    }

    private void AutoConfigureNewRegions()
    {
        var regions = _controller.Regions.ToList();
        var changed = false;

        for (var i = 0; i < regions.Count; i++)
        {
            var region = regions[i];
            if (!_knownRegionIds.Contains(region.Id))
            {
                _knownRegionIds.Add(region.Id);

                // Auto-configure with default label and styling
                var defaultLabel = $"Zone {_knownRegionIds.Count}";
                var defaultStyle = new RegionStyle
                {
                    StrokeColorHex = "#3B82F6",
                    StrokeThickness = 2.0,
                    FillColorHex = "#3B82F6",
                    FillOpacity = 0.15,
                    LabelStyle = new LabelStyle
                    {
                        TextColorHex = "#FFFFFF",
                        BackgroundColorHex = "#FFFFFF",
                        FontSize = 11.0,
                        Placement = LabelPlacement.TopLeft
                    }
                };

                var dto = _typeRegistry.ToDto(region);
                var decorations = dto.Decorations;

                // For line regions, attach a direction indicator at the line midpoint
                if (region.TypeId == LineRegion.LineTypeId && dto.Vertices.Count >= 2)
                {
                    var mid = new NormalizedPoint(
                        (dto.Vertices[0].X + dto.Vertices[1].X) / 2,
                        (dto.Vertices[0].Y + dto.Vertices[1].Y) / 2);
                    decorations = new[]
                    {
                        new DecorationDto
                        {
                            TypeId = DirectionIndicatorDecoration.DirectionIndicatorTypeId,
                            Anchor = mid,
                            IsInteractive = true,
                            Properties = new Dictionary<string, string?> { ["directionSign"] = "1" }
                        }
                    };
                }

                var configuredDto = new RegionDto
                {
                    Id = dto.Id,
                    TypeId = dto.TypeId,
                    Vertices = dto.Vertices,
                    Decorations = decorations,
                    Style = defaultStyle,
                    Label = defaultLabel,
                    Properties = dto.Properties
                };

                var configuredRegion = _typeRegistry.CreateRegion(configuredDto);
                regions[i] = configuredRegion;
                changed = true;
            }
        }

        if (changed)
        {
            _controller.RegionsChanged -= OnRegionsChanged;
            _controller.SetRegions(regions);
            _controller.RegionsChanged += OnRegionsChanged;
        }
    }

    private void UpdateSelectionUi()
    {
        var selectedId = _controller.SelectedRegionId;
        if (selectedId == null)
        {
            _noSelectionText.IsVisible = true;
            _propertiesStack.IsVisible = false;
            return;
        }

        var region = _controller.Regions.FirstOrDefault(r => r.Id == selectedId);
        if (region == null)
        {
            _noSelectionText.IsVisible = true;
            _propertiesStack.IsVisible = false;
            return;
        }

        _noSelectionText.IsVisible = false;
        _propertiesStack.IsVisible = true;

        _isPopulatingUi = true;

        // 1. Label
        _labelTextbox.Text = region.Label ?? "";

        // 2. Placement
        var placement = region.Style.LabelStyle?.Placement ?? LabelPlacement.TopLeft;
        _placementCombobox.SelectedIndex = (int)placement;

        // 3. Color
        var currentHex = region.Style.StrokeColorHex.ToUpperInvariant();
        var colorIdx = Array.FindIndex(ColorsList, c => c.Hex.Equals(currentHex, StringComparison.OrdinalIgnoreCase));
        _colorCombobox.SelectedIndex = colorIdx >= 0 ? colorIdx : 0;

        // 4. Thickness
        var thickness = region.Style.StrokeThickness;
        _thicknessCombobox.SelectedIndex = thickness switch
        {
            1.0d => 0,
            2.0d => 1,
            3.0d => 2,
            5.0d => 3,
            8.0d => 4,
            _ => 1
        };

        // 5. Stroke style
        var isDashed = region.Style.StrokeDashPattern is not null;
        _strokeStyleCombobox.SelectedIndex = isDashed ? 1 : 0;

        // 6. Label font size
        var fontSize = region.Style.LabelStyle?.FontSize ?? 11.0;
        _labelFontSizeCombobox.SelectedIndex = fontSize switch { 10.0 => 0, 11.0 => 1, 12.0 => 2, 14.0 => 3, 16.0 => 4, _ => 1 };

        // 7. Label text color
        var txtHex = (region.Style.LabelStyle?.TextColorHex ?? "#FFFFFF").ToUpperInvariant();
        _labelTextColorCombobox.SelectedIndex = txtHex switch { "#000000" => 1, "#FBBF24" => 2, "#06B6D4" => 3, _ => 0 };

        // 8. Label background
        var bgHex = region.Style.LabelStyle?.BackgroundColorHex;
        _labelBgColorCombobox.SelectedIndex = bgHex == null ? 3 : bgHex.ToUpperInvariant() switch { "#3B82F6" => 1, "#10B981" => 2, _ => 0 };

        // 9. Arrow visibility (Line only)
        _cycleArrowsButton.IsVisible = region.TypeId == LineRegion.LineTypeId;

        _isPopulatingUi = false;
    }

    private static RegionDto CloneWithLabel(RegionDto dto, string? label)
    {
        return new RegionDto
        {
            Id = dto.Id,
            TypeId = dto.TypeId,
            Vertices = dto.Vertices,
            Decorations = dto.Decorations,
            Style = dto.Style,
            Label = label,
            Properties = dto.Properties
        };
    }

    private static RegionDto CloneWithStyle(RegionDto dto, RegionStyle style)
    {
        return new RegionDto
        {
            Id = dto.Id,
            TypeId = dto.TypeId,
            Vertices = dto.Vertices,
            Decorations = dto.Decorations,
            Style = style,
            Label = dto.Label,
            Properties = dto.Properties
        };
    }

    private static RegionDto CloneWithDecorations(RegionDto dto, DecorationDto[] decorations)
    {
        return new RegionDto
        {
            Id = dto.Id,
            TypeId = dto.TypeId,
            Vertices = dto.Vertices,
            Decorations = decorations,
            Style = dto.Style,
            Label = dto.Label,
            Properties = dto.Properties
        };
    }

    private void UpdateSelectedRegion(Func<RegionDto, RegionDto> modifyFunc)
    {
        var selectedId = _controller.SelectedRegionId;
        if (selectedId == null) return;

        var regions = _controller.Regions.ToList();
        var index = regions.FindIndex(r => r.Id == selectedId);
        if (index < 0) return;

        var oldRegion = regions[index];
        var dto = _typeRegistry.ToDto(oldRegion);
        
        var newDto = modifyFunc(dto);

        var newRegion = _typeRegistry.CreateRegion(newDto);
        regions[index] = newRegion;

        _controller.RegionsChanged -= OnRegionsChanged;
        _controller.SetRegions(regions);
        _controller.RegionsChanged += OnRegionsChanged;

        _editor.InvalidateVisual();
        SaveToJSON();
    }

    private void UpdateSelectedRegionLabel()
    {
        if (_isPopulatingUi) return;
        UpdateSelectedRegion(dto => CloneWithLabel(dto, _labelTextbox.Text));
    }

    private void UpdateSelectedRegionLabelPlacement()
    {
        if (_isPopulatingUi) return;
        var selectedPlacement = (LabelPlacement)_placementCombobox.SelectedIndex;

        UpdateSelectedRegion(dto =>
        {
            var ls = dto.Style.LabelStyle ?? new LabelStyle { TextColorHex = "#FFFFFF", BackgroundColorHex = "#1E293B" };
            var newStyle = dto.Style.With(labelStyle: ls.With(placement: selectedPlacement));
            return CloneWithStyle(dto, newStyle);
        });
    }

    private void UpdateSelectedRegionLabelStyle()
    {
        if (_isPopulatingUi) return;
        var fontSize = _labelFontSizeCombobox.SelectedIndex switch { 0 => 10.0, 1 => 11.0, 2 => 12.0, 3 => 14.0, 4 => 16.0, _ => 11.0 };
        var textColor = _labelTextColorCombobox.SelectedIndex switch { 1 => "#000000", 2 => "#FBBF24", 3 => "#06B6D4", _ => "#FFFFFF" };
        var bgIdx = _labelBgColorCombobox.SelectedIndex;
        string? bgColor = bgIdx switch { 1 => "#3B82F6", 2 => "#10B981", 3 => null, _ => "#1E293B" };

        UpdateSelectedRegion(dto =>
        {
            var ls = dto.Style.LabelStyle ?? new LabelStyle { TextColorHex = "#FFFFFF", BackgroundColorHex = "#1E293B", FontSize = 11.0 };
            var newLs = ls.With(textColorHex: textColor, backgroundColorHex: bgColor, clearBackgroundColorHex: bgColor == null, fontSize: fontSize);
            var newStyle = dto.Style.With(labelStyle: newLs);
            return CloneWithStyle(dto, newStyle);
        });
    }

    private void UpdateSelectedRegionStyle()
    {
        if (_isPopulatingUi) return;

        var chosenColor = ColorsList[Math.Max(0, _colorCombobox.SelectedIndex)];
        var thickness = _thicknessCombobox.SelectedIndex switch
        {
            0 => 1.0,
            1 => 2.0,
            2 => 3.0,
            3 => 5.0,
            4 => 8.0,
            _ => 2.0
        };
        var isDashed = _strokeStyleCombobox.SelectedIndex == 1;

        UpdateSelectedRegion(dto =>
        {
            var newStyle = new RegionStyle
            {
                StrokeColorHex = chosenColor.Hex,
                StrokeThickness = thickness,
                StrokeDashPattern = isDashed ? new double[] { 6.0, 4.0 } : null,
                FillColorHex = chosenColor.Hex,
                FillOpacity = dto.Style.FillOpacity,
                DefaultHandleStyle = dto.Style.DefaultHandleStyle,
                LabelStyle = dto.Style.LabelStyle
            };
            return CloneWithStyle(dto, newStyle);
        });
    }

    private void CycleSelectedLineArrows()
    {
        if (_isPopulatingUi) return;

        UpdateSelectedRegion(dto =>
        {
            var decs = dto.Decorations.ToList();
            var index = decs.FindIndex(d => d.TypeId == DirectionIndicatorDecoration.DirectionIndicatorTypeId);
            if (index >= 0)
            {
                var dec = decs[index];
                var sign = dec.Properties.TryGetValue("directionSign", out var s) && int.TryParse(s, out var parsed) ? parsed : 1;
                var nextSign = sign switch
                {
                    1 => -1,
                    -1 => 2,
                    2 => 0,
                    _ => 1
                };

                decs[index] = new DecorationDto
                {
                    TypeId = dec.TypeId,
                    Anchor = dec.Anchor,
                    IsInteractive = dec.IsInteractive,
                    Properties = new Dictionary<string, string?>(dec.Properties)
                    {
                        ["directionSign"] = nextSign.ToString()
                    }
                };
                return CloneWithDecorations(dto, decs.ToArray());
            }
            return dto;
        });
    }

    private void SaveToJSON()
    {
        try
        {
#pragma warning disable IL2026
            var dtos = _controller.Regions.Select(r => _typeRegistry.ToDto(r)).ToList();
            var options = new JsonSerializerOptions { WriteIndented = true };
            _jsonTextBox.Text = JsonSerializer.Serialize(dtos, options);
#pragma warning restore IL2026
        }
        catch (Exception ex)
        {
            _jsonTextBox.Text = $"Error serializing: {ex.Message}";
        }
    }

    private void LoadFromJSON()
    {
        try
        {
            var json = _jsonTextBox.Text;
            if (string.IsNullOrWhiteSpace(json)) return;

#pragma warning disable IL2026
            var dtos = JsonSerializer.Deserialize<List<RegionDto>>(json);
#pragma warning restore IL2026
            if (dtos == null) return;

            var regions = dtos.Select(dto => _typeRegistry.CreateRegion(dto)).ToList();
            
            _knownRegionIds.Clear();
            foreach (var r in regions)
            {
                _knownRegionIds.Add(r.Id);
            }

            _controller.RegionsChanged -= OnRegionsChanged;
            _controller.SetRegions(regions);
            _controller.RegionsChanged += OnRegionsChanged;
            
            _controller.CancelActiveOperation();
            UpdateSelectionUi();
        }
        catch (Exception ex)
        {
            _jsonTextBox.Text = $"Error deserializing: {ex.Message}";
        }
    }

    public void Dispose()
    {
        _controller.RegionsChanged -= OnRegionsChanged;
        _controller.RenderStateChanged -= OnRenderStateChanged;
        _editor.Dispose();
    }
}
