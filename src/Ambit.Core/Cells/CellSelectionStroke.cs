namespace Ambit;

/// <summary>
/// Applies the drag-paint selection semantics for a single grid stroke.
/// </summary>
public sealed class CellSelectionStroke
{
    private readonly ICellGrid _grid;
    private readonly ISet<(int Row, int Col)> _selectedCells;
    private readonly HashSet<(int Row, int Col)> _visitedCells = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="CellSelectionStroke"/> class.
    /// </summary>
    /// <param name="grid">The grid to paint against.</param>
    /// <param name="selectedCells">The mutable selected-cell set.</param>
    /// <param name="startPoint">The normalized stroke start point.</param>
    public CellSelectionStroke(ICellGrid grid, ISet<(int Row, int Col)> selectedCells, NormalizedPoint startPoint)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(selectedCells);

        _grid = grid;
        _selectedCells = selectedCells;
        StartCell = grid.HitTestCell(startPoint);
        TargetSelectedState = !selectedCells.Contains(StartCell);
        VisitCell(StartCell);
    }

    /// <summary>
    /// Gets the first cell touched by the stroke.
    /// </summary>
    public (int Row, int Col) StartCell { get; }

    /// <summary>
    /// Gets a value indicating whether the stroke is selecting or deselecting cells.
    /// </summary>
    public bool TargetSelectedState { get; }

    /// <summary>
    /// Visits the cell containing the specified point.
    /// </summary>
    /// <param name="point">The normalized point to map and apply.</param>
    /// <returns><see langword="true"/> when the stroke changed selection state for a newly visited cell; otherwise <see langword="false"/>.</returns>
    public bool Visit(NormalizedPoint point)
    {
        return VisitCell(_grid.HitTestCell(point));
    }

    /// <summary>
    /// Visits a concrete cell directly.
    /// </summary>
    /// <param name="cell">The cell to apply.</param>
    /// <returns><see langword="true"/> when the stroke changed selection state for a newly visited cell; otherwise <see langword="false"/>.</returns>
    public bool VisitCell((int Row, int Col) cell)
    {
        if (!_visitedCells.Add(cell))
        {
            return false;
        }

        if (TargetSelectedState)
        {
            _selectedCells.Add(cell);
        }
        else
        {
            _selectedCells.Remove(cell);
        }

        return true;
    }
}
