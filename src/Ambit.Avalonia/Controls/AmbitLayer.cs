using Avalonia.Controls;
using Avalonia.Media;

namespace Ambit.Avalonia.Controls;

/// <summary>
/// Represents a transparent overlay layer hosted within an <see cref="AmbitViewer"/>.
/// </summary>
public abstract class AmbitLayer : Control
{
    private ICoordinateTransform? _transform;

    /// <summary>
    /// Gets the coordinate transformation supplied by the parent <see cref="AmbitViewer"/>.
    /// </summary>
    protected ICoordinateTransform? Transform => _transform;

    /// <summary>
    /// Initializes a new instance of the <see cref="AmbitLayer"/> class.
    /// </summary>
    protected AmbitLayer()
    {
        ClipToBounds = true;
    }

    /// <summary>
    /// Invoked when the parent <see cref="AmbitViewer"/> updates its coordinate transformation.
    /// </summary>
    /// <param name="transform">The updated coordinate transform.</param>
    public virtual void OnViewerTransformChanged(ICoordinateTransform transform)
    {
        _transform = transform;
        InvalidateVisual();
    }
}
