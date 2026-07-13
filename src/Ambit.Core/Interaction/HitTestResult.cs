namespace Ambit;

/// <summary>
/// Describes the result of a hit test against the region collection.
/// </summary>
public sealed class HitTestResult
{
    private HitTestResult()
    {
    }

    /// <summary>
    /// Gets the kind of element that was hit.
    /// </summary>
    public HitTestKind Kind { get; private init; }

    /// <summary>
    /// Gets the region that was hit, if any.
    /// </summary>
    public IEditableRegion? Region { get; private init; }

    /// <summary>
    /// Gets the handle index that was hit, if <see cref="Kind"/> is <see cref="HitTestKind.Handle"/>.
    /// </summary>
    public int? HandleIndex { get; private init; }

    /// <summary>
    /// Gets the decoration that was hit, if <see cref="Kind"/> is <see cref="HitTestKind.Decoration"/>.
    /// </summary>
    public IDecoration? Decoration { get; private init; }

    /// <summary>
    /// Gets the index of the decoration within the region's decoration list.
    /// </summary>
    public int? DecorationIndex { get; private init; }

    /// <summary>
    /// Creates a hit result for empty space.
    /// </summary>
    /// <returns>A background hit result.</returns>
    public static HitTestResult Background() => new() { Kind = HitTestKind.Background };

    /// <summary>
    /// Creates a hit result for a region handle.
    /// </summary>
    /// <param name="region">The owning region.</param>
    /// <param name="handleIndex">The handle index within the region.</param>
    /// <returns>A handle hit result.</returns>
    public static HitTestResult Handle(IEditableRegion region, int handleIndex) =>
        new() { Kind = HitTestKind.Handle, Region = region, HandleIndex = handleIndex };

    /// <summary>
    /// Creates a hit result for an interactive decoration.
    /// </summary>
    /// <param name="region">The owning region.</param>
    /// <param name="decoration">The decoration that was hit.</param>
    /// <param name="decorationIndex">The index of the decoration in the region's decoration list.</param>
    /// <returns>A decoration hit result.</returns>
    public static HitTestResult DecorationHit(IEditableRegion region, IDecoration decoration, int decorationIndex) =>
        new() { Kind = HitTestKind.Decoration, Region = region, Decoration = decoration, DecorationIndex = decorationIndex };

    /// <summary>
    /// Creates a hit result for a region body.
    /// </summary>
    /// <param name="region">The region whose body was hit.</param>
    /// <returns>A body hit result.</returns>
    public static HitTestResult Body(IEditableRegion region) =>
        new() { Kind = HitTestKind.Body, Region = region };
}

/// <summary>
/// Identifies the kind of element struck by a hit test.
/// </summary>
public enum HitTestKind
{
    /// <summary>
    /// No interactive element was hit.
    /// </summary>
    Background,

    /// <summary>
    /// An interactive decoration anchor was hit.
    /// </summary>
    Decoration,

    /// <summary>
    /// A region handle was hit.
    /// </summary>
    Handle,

    /// <summary>
    /// A region body was hit.
    /// </summary>
    Body,
}
