namespace Ambit;

/// <summary>
/// Provides a fixed-topology normalized cell grid.
/// </summary>
public sealed class CellGrid : ICellGrid
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CellGrid"/> class.
    /// </summary>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of columns.</param>
    public CellGrid(int rows, int columns)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);

        Rows = rows;
        Columns = columns;
    }

    /// <inheritdoc />
    public int Rows { get; }

    /// <inheritdoc />
    public int Columns { get; }

    /// <inheritdoc />
    public (int Row, int Col) HitTestCell(NormalizedPoint point)
    {
        // Clamp so out-of-bounds drags (including negative) don't produce -1 indices.
        var clampedX = Math.Clamp(point.X, 0d, 1d);
        var clampedY = Math.Clamp(point.Y, 0d, 1d);
        var row = clampedY >= 1d ? Rows - 1 : (int)Math.Floor(clampedY * Rows);
        var col = clampedX >= 1d ? Columns - 1 : (int)Math.Floor(clampedX * Columns);
        return (row, col);
    }
}
