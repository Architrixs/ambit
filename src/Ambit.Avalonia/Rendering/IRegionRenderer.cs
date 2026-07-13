using SkiaSharp;

namespace Ambit.Avalonia.Rendering;

/// <summary>
/// Renders a single region type onto a Skia canvas.
/// </summary>
public interface IRegionRenderer
{
    /// <summary>
    /// Gets the region type identifier handled by this renderer.
    /// </summary>
    string TypeId { get; }

    /// <summary>
    /// Renders the region.
    /// </summary>
    /// <param name="canvas">The destination canvas.</param>
    /// <param name="region">The region to render.</param>
    /// <param name="state">The transient render state.</param>
    /// <param name="transform">The coordinate transform.</param>
    /// <param name="resources">The shared Skia resource cache.</param>
    void Render(SKCanvas canvas, IRegion region, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources);
}
