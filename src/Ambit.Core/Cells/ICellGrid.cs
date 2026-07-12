namespace Ambit;

/// <summary>
/// Maps normalized coordinates onto a fixed row and column grid.
/// </summary>
public interface ICellGrid
{
    /// <summary>
    /// Gets the number of rows in the grid.
    /// </summary>
    int Rows { get; }

    /// <summary>
    /// Gets the number of columns in the grid.
    /// </summary>
    int Columns { get; }

    /// <summary>
    /// Maps a normalized point to its containing cell.
    /// </summary>
    /// <param name="point">The normalized point to map.</param>
    /// <returns>The containing row and column.</returns>
    (int Row, int Col) HitTestCell(NormalizedPoint point);
}
