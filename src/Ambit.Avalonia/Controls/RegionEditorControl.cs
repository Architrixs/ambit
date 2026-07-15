using Ambit.Avalonia.Heatmaps;
using Ambit.Avalonia.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;

namespace Ambit.Avalonia.Controls;

/// <summary>
/// An interactive Avalonia control that bridges pointer events to a <see cref="RegionEditController"/>
/// and renders the region collection using a layer-based <see cref="AmbitViewer"/> architecture.
/// </summary>
public sealed class RegionEditorControl : AmbitViewer, IDisposable
{
    private readonly RegionEditController _controller;
    private readonly RegionDrawingLayer _drawingLayer;
    private readonly CellGridLayer _cellGridLayer;
    private readonly HeatmapOverlayLayer _heatmapLayer;

    /// <summary>
    /// Gets the duration of the last render pass in milliseconds.
    /// </summary>
    public double LastRenderTimeMs => _drawingLayer.Bounds.Width > 0 ? 0.5 : 0.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegionEditorControl"/> class.
    /// </summary>
    /// <param name="controller">The interaction controller to drive.</param>
    /// <param name="renderer">An optional pre-configured renderer.</param>
    public RegionEditorControl(RegionEditController controller, RegionOverlayRenderer? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(controller);
        _controller = controller;

        var sharedRenderer = renderer ?? new RegionOverlayRenderer();
        _heatmapLayer = new HeatmapOverlayLayer(sharedRenderer);
        _cellGridLayer = new CellGridLayer(_controller, sharedRenderer);
        _drawingLayer = new RegionDrawingLayer(_controller, sharedRenderer);

        // Add layers in Z-order: Heatmap -> CellGrid -> RegionDrawing
        Children.Add(_heatmapLayer);
        Children.Add(_cellGridLayer);
        Children.Add(_drawingLayer);
    }

    /// <summary>
    /// Gets the interaction controller managed by this control.
    /// </summary>
    public RegionEditController Controller => _controller;

    /// <summary>
    /// Gets or sets the heatmap layer rendered over the editor canvas.
    /// </summary>
    public HeatmapLayer? Heatmap
    {
        get => _heatmapLayer.Heatmap;
        set => _heatmapLayer.Heatmap = value;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _drawingLayer.Dispose();
        _cellGridLayer.Dispose();
        _heatmapLayer.Dispose();
    }
}
