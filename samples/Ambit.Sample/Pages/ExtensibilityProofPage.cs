using System;
using System.Collections.Generic;
using System.Linq;
using Ambit.Avalonia.Controls;
using Ambit.Avalonia.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SkiaSharp;

namespace Ambit.Sample.Pages;

// --- Custom Circle Region Implementation ---
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
        return dist <= (Radius + toleranceNormalized);
    }

    public void MoveHandle(int handleIndex, NormalizedPoint newPosition)
    {
        if (handleIndex == 0)
        {
            Center = newPosition;
        }
        else if (handleIndex == 1)
        {
            Radius = Math.Max(0.01, Math.Abs(newPosition.X - Center.X));
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

// --- Custom Circle Factory (for DTO mapping) ---
public sealed class CircleRegionFactory : IRegionFactory
{
    public string TypeId => CircleRegion.CircleTypeId;

    public IEditableRegion Create(RegionDto dto, IReadOnlyList<IDecoration> decorations)
    {
        var radius = dto.Properties.TryGetValue("radius", out var raw) && double.TryParse(raw, out var parsed) ? parsed : 0.15;
        return new CircleRegion(dto.Vertices[0], radius, dto.Style, dto.Id, decorations, dto.Label);
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
        badgePaint.Color = new SKColor(239, 68, 68); // Red

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

// --- Page UI class ---
public sealed class ExtensibilityProofPage : UserControl
{
    public ExtensibilityProofPage()
    {
        // 1. Setup the Registries
        var renderRegistry = new RegionRenderRegistry().RegisterBuiltInRenderers();
        renderRegistry.Register(new CircleRegionRenderer());
        renderRegistry.Register(new CountBadgeDecorationRenderer());

        var renderer = new RegionOverlayRenderer(renderRegistry);

        var controller = new RegionEditController();
        var editor = new RegionEditorControl(controller, renderer)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        // 2. Add instances of custom region and decoration
        var style = new RegionStyle { StrokeColorHex = "#EC4899", FillColorHex = "#F472B6", FillOpacity = 0.2 };
        var customDec = new CountBadgeDecoration(new NormalizedPoint(0.5, 0.5), 18);
        var circle = new CircleRegion(
            new NormalizedPoint(0.5, 0.5),
            0.15,
            style,
            decorations: new[] { customDec },
            label: "Custom Circle");

        controller.SetRegions(new IEditableRegion[] { circle });

        // Left controls panel
        var controlsPanel = new StackPanel
        {
            Width = 240,
            Spacing = 12,
            Margin = new Thickness(16),
            VerticalAlignment = VerticalAlignment.Top,
        };

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Extensibility Proof",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var description = new TextBlock
        {
            Text = "Demonstrates the Open-Closed Principle (OCP):\n\n" +
                   "A custom 'CircleRegion' and 'CountBadgeDecoration' were added entirely within the sample project. " +
                   "They are registered via API registration with no changes made to Ambit.Core or Ambit.Avalonia.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.LightGray,
            FontSize = 13,
        };
        controlsPanel.Children.Add(description);

        var note = new TextBlock
        {
            Text = "*Note: The pink circle is editable (drag the center handle to move, edge handle to change radius) and displays a custom red numeric badge '18' at its center.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.DarkGray,
            FontSize = 12,
            Margin = new Thickness(0, 16, 0, 0),
        };
        controlsPanel.Children.Add(note);

        var canvasContainer = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1A1A1A")),
            BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(16),
            Child = editor,
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(270) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(controlsPanel, 0);
        Grid.SetColumn(canvasContainer, 1);

        grid.Children.Add(controlsPanel);
        grid.Children.Add(canvasContainer);

        Content = grid;
    }
}
