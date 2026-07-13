namespace Ambit;

/// <summary>
/// Identifies the current state of the region edit state machine.
/// </summary>
public enum RegionEditState
{
    /// <summary>
    /// No active pointer interaction or hover.
    /// </summary>
    Idle,

    /// <summary>
    /// The pointer is hovering over a region, handle, or interactive decoration.
    /// </summary>
    Hover,

    /// <summary>
    /// A region handle is being dragged to resize or reshape.
    /// </summary>
    DraggingHandle,

    /// <summary>
    /// A region body is being dragged to translate.
    /// </summary>
    DraggingRegion,

    /// <summary>
    /// A new region is being drawn.
    /// </summary>
    DrawingNewRegion,

    /// <summary>
    /// Cell grid cells are being painted via a drag stroke.
    /// </summary>
    PaintingCells,
}
