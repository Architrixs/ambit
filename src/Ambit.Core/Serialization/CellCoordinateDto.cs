namespace Ambit;

/// <summary>
/// Represents a selected grid cell in serialized form.
/// </summary>
/// <param name="row">The zero-based row index.</param>
/// <param name="column">The zero-based column index.</param>
public readonly record struct CellCoordinateDto(int row, int column)
{
    /// <summary>
    /// Gets the zero-based row index.
    /// </summary>
    public int Row { get; } = row;

    /// <summary>
    /// Gets the zero-based column index.
    /// </summary>
    public int Column { get; } = column;
}
