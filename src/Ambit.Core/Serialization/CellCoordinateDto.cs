namespace Ambit;

/// <summary>
/// Represents a selected grid cell in serialized form.
/// </summary>
public readonly record struct CellCoordinateDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CellCoordinateDto"/> struct.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    public CellCoordinateDto(int row, int column)
    {
        Row = row;
        Column = column;
    }

    /// <summary>
    /// Gets the zero-based row index.
    /// </summary>
    public int Row { get; }

    /// <summary>
    /// Gets the zero-based column index.
    /// </summary>
    public int Column { get; }
}
