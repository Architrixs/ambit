namespace Ambit;

/// <summary>
/// Represents a region that supports hit-testing, handle editing, and translation.
/// </summary>
public interface IEditableRegion : IRegion, IHitTestable, IHandleProvider
{
    /// <summary>
    /// Translates the region by the specified normalized delta.
    /// </summary>
    /// <param name="delta">The translation delta.</param>
    void Translate(NormalizedVector delta);
}
