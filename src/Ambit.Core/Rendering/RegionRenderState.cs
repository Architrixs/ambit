namespace Ambit;

/// <summary>
/// Carries transient rendering state that is layered on top of persisted region data.
/// </summary>
public sealed class RegionRenderState
{
    /// <summary>
    /// Gets the identifier of the region currently under the pointer, if any.
    /// </summary>
    public Guid? HoveredRegionId { get; init; }

    /// <summary>
    /// Gets the identifier of the currently selected region, if any.
    /// </summary>
    public Guid? SelectedRegionId { get; init; }

    /// <summary>
    /// Gets the selected cell set for grid rendering, if any.
    /// </summary>
    public IReadOnlySet<(int Row, int Col)>? SelectedCells { get; init; }

    /// <summary>
    /// Gets the handle index currently under the pointer, if any.
    /// </summary>
    public int? HoveredHandleIndex { get; init; }
}
