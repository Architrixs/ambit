using System;
using System.Collections.Generic;
using System.Linq;
using Ambit.Avalonia.Heatmaps;
using Ambit.Avalonia.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Ambit.Avalonia.Controls;

/// <summary>
/// Renders a passive region overlay using a single custom draw operation within an <see cref="AmbitViewer"/>.
/// </summary>
public sealed class RegionOverlayControl : AmbitViewer, IDisposable
{
    private readonly RegionOverlayRenderer _renderer;
    private readonly OverlayLayer _overlayLayer;
    private IReadOnlyList<IRegion> _regions = Array.Empty<IRegion>();
    private RegionRenderState _renderState = new();
    private HeatmapLayer? _heatmap;
    private int _contentVersion;
    private ulong _regionFingerprint;
    private ulong _stateFingerprint;
    private ulong _heatmapFingerprint;

    /// <summary>
    /// Gets the duration of the last render pass in milliseconds.
    /// </summary>
    public double LastRenderTimeMs => _renderer.LastRenderTimeMs;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegionOverlayControl"/> class.
    /// </summary>
    public RegionOverlayControl()
    {
        _renderer = new RegionOverlayRenderer();
        _overlayLayer = new OverlayLayer(this);
        Children.Add(_overlayLayer);
    }

    /// <summary>
    /// Updates the rendered region collection and invalidates only when the content changes.
    /// </summary>
    /// <param name="regions">The new region collection.</param>
    /// <returns><see langword="true"/> when the control state changed and a redraw was scheduled; otherwise <see langword="false"/>.</returns>
    public bool UpdateRegions(IReadOnlyList<IRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(regions);

        var fingerprint = ComputeRegionFingerprint(regions);
        if (fingerprint == _regionFingerprint)
        {
            return false;
        }

        _regionFingerprint = fingerprint;
        _regions = regions.ToArray();
        _contentVersion++;
        _overlayLayer.InvalidateVisual();
        return true;
    }

    /// <summary>
    /// Updates the transient render state and invalidates only when it changes.
    /// </summary>
    /// <param name="state">The new render state.</param>
    /// <returns><see langword="true"/> when the control state changed and a redraw was scheduled; otherwise <see langword="false"/>.</returns>
    public bool UpdateRenderState(RegionRenderState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var fingerprint = ComputeRenderStateFingerprint(state);
        if (fingerprint == _stateFingerprint)
        {
            return false;
        }

        _stateFingerprint = fingerprint;
        _renderState = state;
        _contentVersion++;
        _overlayLayer.InvalidateVisual();
        return true;
    }

    /// <summary>
    /// Updates the optional heatmap layer and invalidates only when it changes.
    /// </summary>
    /// <param name="heatmap">The new heatmap layer, or <see langword="null"/> to remove the current layer.</param>
    /// <returns><see langword="true"/> when the control state changed and a redraw was scheduled; otherwise <see langword="false"/>.</returns>
    public bool UpdateHeatmap(HeatmapLayer? heatmap)
    {
        var fingerprint = ComputeHeatmapFingerprint(heatmap);
        if (fingerprint == _heatmapFingerprint)
        {
            return false;
        }

        _heatmapFingerprint = fingerprint;
        _heatmap = heatmap;
        _contentVersion++;
        _overlayLayer.InvalidateVisual();
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _renderer.Dispose();
    }

    private static ulong ComputeRegionFingerprint(IReadOnlyList<IRegion> regions)
    {
        var hashCode = new HashCode();
        hashCode.Add(regions.Count);
        for (var index = 0; index < regions.Count; index++)
        {
            var region = regions[index];
            hashCode.Add(region.Id);
            hashCode.Add(region.TypeId, StringComparer.Ordinal);
            hashCode.Add(region.Label, StringComparer.Ordinal);
            for (var vertexIndex = 0; vertexIndex < region.Vertices.Count; vertexIndex++)
            {
                hashCode.Add(region.Vertices[vertexIndex].X);
                hashCode.Add(region.Vertices[vertexIndex].Y);
            }

            var style = region.Style;
            hashCode.Add(style.StrokeColorHex, StringComparer.Ordinal);
            hashCode.Add(style.StrokeThickness);
            hashCode.Add(style.FillColorHex, StringComparer.Ordinal);
            hashCode.Add(style.FillOpacity);
            if (style.StrokeDashPattern is not null)
            {
                for (var dashIndex = 0; dashIndex < style.StrokeDashPattern.Length; dashIndex++)
                {
                    hashCode.Add(style.StrokeDashPattern[dashIndex]);
                }
            }

            if (style.LabelStyle is not null)
            {
                hashCode.Add(style.LabelStyle.TextColorHex, StringComparer.Ordinal);
                hashCode.Add(style.LabelStyle.BackgroundColorHex, StringComparer.Ordinal);
                hashCode.Add(style.LabelStyle.FontSize);
                if (style.LabelStyle.AnchorOverride is { } anchor)
                {
                    hashCode.Add(anchor.X);
                    hashCode.Add(anchor.Y);
                }
            }

            hashCode.Add(style.DefaultHandleStyle.FillColorHex, StringComparer.Ordinal);
            hashCode.Add(style.DefaultHandleStyle.StrokeColorHex, StringComparer.Ordinal);
            hashCode.Add(style.DefaultHandleStyle.RadiusPixels);

            for (var decorationIndex = 0; decorationIndex < region.Decorations.Count; decorationIndex++)
            {
                var decoration = region.Decorations[decorationIndex];
                hashCode.Add(decoration.TypeId, StringComparer.Ordinal);
                hashCode.Add(decoration.Anchor.X);
                hashCode.Add(decoration.Anchor.Y);
                hashCode.Add(decoration.IsInteractive);
            }
        }

        return (ulong)hashCode.ToHashCode();
    }

    private static ulong ComputeRenderStateFingerprint(RegionRenderState state)
    {
        var hashCode = new HashCode();
        hashCode.Add(state.HoveredRegionId);
        hashCode.Add(state.SelectedRegionId);
        hashCode.Add(state.HoveredHandleIndex);
        if (state.SelectedCells is not null)
        {
            foreach (var cell in state.SelectedCells.OrderBy(static cell => cell.Row).ThenBy(static cell => cell.Col))
            {
                hashCode.Add(cell.Row);
                hashCode.Add(cell.Col);
            }
        }

        return (ulong)hashCode.ToHashCode();
    }

    private static ulong ComputeHeatmapFingerprint(HeatmapLayer? heatmap)
    {
        if (heatmap is null)
        {
            return 0UL;
        }

        var hashCode = new HashCode();
        hashCode.Add(heatmap.Rows);
        hashCode.Add(heatmap.Columns);
        hashCode.Add(heatmap.Opacity);
        for (var index = 0; index < heatmap.Intensities.Count; index++)
        {
            hashCode.Add(heatmap.Intensities[index]);
        }

        if (heatmap.ColorLut is not null)
        {
            for (var index = 0; index < heatmap.ColorLut.Count; index++)
            {
                hashCode.Add(heatmap.ColorLut[index]);
            }
        }

        return (ulong)hashCode.ToHashCode();
    }

    private sealed class OverlayLayer : AmbitLayer
    {
        private readonly RegionOverlayControl _control;
        private readonly DrawOperation _drawOperation;

        public OverlayLayer(RegionOverlayControl control)
        {
            _control = control;
            _drawOperation = new DrawOperation(_control, this);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            context.Custom(_drawOperation);
        }
    }

    private sealed class DrawOperation : ICustomDrawOperation
    {
        private readonly RegionOverlayControl _control;
        private readonly OverlayLayer _layer;

        public DrawOperation(RegionOverlayControl control, OverlayLayer layer)
        {
            _control = control;
            _layer = layer;
        }

        public Rect Bounds => new(_layer.Bounds.Size);
        public void Dispose() { }
        public bool Equals(ICustomDrawOperation? other) => false;
        public bool HitTest(Point p) => false;

        public void Render(ImmediateDrawingContext context)
        {
            var transform = _control.CoordinateTransform;
            if (transform == null) return;

            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null) return;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            canvas.Save();

            _control._renderer.Render(canvas, _control._regions, _control._renderState, transform, heatmap: _control._heatmap, cellGrid: null, backgroundImage: null);

            canvas.Restore();
        }
    }
}
