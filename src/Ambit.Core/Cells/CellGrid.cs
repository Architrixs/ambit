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
        var row = point.Y >= 1d ? Rows - 1 : (int)Math.Floor(point.Y * Rows);
        var col = point.X >= 1d ? Columns - 1 : (int)Math.Floor(point.X * Columns);
        return (row, col);
    }
}
