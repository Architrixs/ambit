namespace Ambit;

/// <summary>
/// Represents a serialized grid selection snapshot.
/// </summary>
public sealed class CellSelectionDto
{
    /// <summary>
    /// Gets or initializes the total grid row count.
    /// </summary>
    public required int Rows { get; init; }

    /// <summary>
    /// Gets or initializes the total grid column count.
    /// </summary>
    public required int Columns { get; init; }

    /// <summary>
    /// Gets or initializes the selected cells within the grid.
    /// </summary>
    public required IReadOnlyList<CellCoordinateDto> SelectedCells { get; init; }
}
