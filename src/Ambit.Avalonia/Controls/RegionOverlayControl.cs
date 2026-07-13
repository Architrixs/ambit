using System.Runtime.CompilerServices;
using Ambit.Avalonia.Heatmaps;
using Ambit.Avalonia.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Ambit.Avalonia.Controls;

/// <summary>
/// Renders a passive region overlay using a single custom draw operation.
/// </summary>
public sealed class RegionOverlayControl : Control, IDisposable
{
    private readonly RegionOverlayRenderer _renderer;
    private readonly RegionOverlayDrawOperation _drawOperation;
    private readonly BoundsCoordinateTransform _boundsTransform = new();
    private IReadOnlyList<IRegion> _regions = Array.Empty<IRegion>();
    private RegionRenderState _renderState = new();
    private HeatmapLayer? _heatmap;
    private ICoordinateTransform? _coordinateTransform;
    private int _contentVersion;
    private ulong _regionFingerprint;
    private ulong _stateFingerprint;
    private ulong _heatmapFingerprint;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegionOverlayControl"/> class.
    /// </summary>
    public RegionOverlayControl()
    {
        _renderer = new RegionOverlayRenderer();
        _drawOperation = new RegionOverlayDrawOperation(_renderer);
        ClipToBounds = true;
    }

    /// <summary>
    /// Gets or sets the coordinate transform used to map normalized geometry into control pixels.
    /// When unset, the control uses its own bounds as a direct unit-square transform.
    /// </summary>
    public ICoordinateTransform? CoordinateTransform
    {
        get => _coordinateTransform;
        set
        {
            if (ReferenceEquals(_coordinateTransform, value))
            {
                return;
            }

            _coordinateTransform = value;
            InvalidateVisual();
        }
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
        InvalidateVisual();
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
        InvalidateVisual();
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
        InvalidateVisual();
        return true;
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        _boundsTransform.Update(Bounds);
        _drawOperation.Update(
            new Rect(Bounds.Size),
            _regions,
            _renderState,
            _coordinateTransform ?? _boundsTransform,
            _heatmap,
            _contentVersion);
        context.Custom(_drawOperation);
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

                switch (decoration)
                {
                    case DirectionIndicatorDecoration directionIndicator:
                        hashCode.Add(directionIndicator.DirectionSign);
                        break;
                    case LabelDecoration labelDecoration:
                        hashCode.Add(labelDecoration.Text, StringComparer.Ordinal);
                        break;
                }
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

    private sealed class BoundsCoordinateTransform : ICoordinateTransform
    {
        public static BoundsCoordinateTransform Empty { get; } = new();

        private Rect _bounds;

        public void Update(Rect bounds)
        {
            _bounds = bounds;
        }

        public ControlPoint ToControlSpace(NormalizedPoint p)
        {
            return new ControlPoint(_bounds.Left + (p.X * _bounds.Width), _bounds.Top + (p.Y * _bounds.Height));
        }

        public NormalizedPoint ToNormalizedSpace(ControlPoint controlPoint)
        {
            var x = _bounds.Width <= double.Epsilon ? 0d : (controlPoint.X - _bounds.Left) / _bounds.Width;
            var y = _bounds.Height <= double.Epsilon ? 0d : (controlPoint.Y - _bounds.Top) / _bounds.Height;
            return new NormalizedPoint(x, y);
        }
    }
}
