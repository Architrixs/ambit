using SkiaSharp;

namespace Ambit.Avalonia.Rendering;

/// <summary>
/// Renders a single decoration type onto a Skia canvas.
/// </summary>
public interface IDecorationRenderer
{
    /// <summary>
    /// Gets the decoration type identifier handled by this renderer.
    /// </summary>
    string TypeId { get; }

    /// <summary>
    /// Renders the decoration.
    /// </summary>
    /// <param name="canvas">The destination canvas.</param>
    /// <param name="decoration">The decoration to render.</param>
    /// <param name="owner">The owning region.</param>
    /// <param name="state">The transient render state.</param>
    /// <param name="transform">The coordinate transform.</param>
    /// <param name="resources">The shared Skia resource cache.</param>
    void Render(SKCanvas canvas, IDecoration decoration, IRegion owner, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources);
}
