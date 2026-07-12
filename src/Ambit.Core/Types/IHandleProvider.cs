namespace Ambit;

/// <summary>
/// Exposes editable handles for a region or decoration owner.
/// </summary>
public interface IHandleProvider
{
    /// <summary>
    /// Gets the current handle set.
    /// </summary>
    /// <returns>The handles exposed by the owner.</returns>
    IReadOnlyList<RegionHandle> GetHandles();

    /// <summary>
    /// Moves the specified handle to a new normalized position.
    /// </summary>
    /// <param name="handleIndex">The handle index to move.</param>
    /// <param name="newPosition">The target normalized position.</param>
    void MoveHandle(int handleIndex, NormalizedPoint newPosition);
}
