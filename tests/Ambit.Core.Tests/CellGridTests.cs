using FluentAssertions;

namespace Ambit.Core.Tests;

public sealed class CellGridTests
{
    [Fact]
    public void HitTestCell_MapsCornersAndEdgesIntoExpectedCells()
    {
        var grid = new CellGrid(4, 5);

        grid.HitTestCell(new NormalizedPoint(0.0, 0.0)).Should().Be((0, 0));
        grid.HitTestCell(new NormalizedPoint(0.999, 0.999)).Should().Be((3, 4));
        grid.HitTestCell(new NormalizedPoint(1.0, 1.0)).Should().Be((3, 4));
        grid.HitTestCell(new NormalizedPoint(0.4, 0.5)).Should().Be((2, 2));
    }

    [Fact]
    public void CellSelectionStroke_SelectsAllNewlyVisitedCellsWhenStartingFromUnselectedCell()
    {
        var grid = new CellGrid(3, 3);
        var selected = new HashSet<(int Row, int Col)>();

        var stroke = new CellSelectionStroke(grid, selected, new NormalizedPoint(0.1, 0.1));
        stroke.Visit(new NormalizedPoint(0.5, 0.1));
        stroke.Visit(new NormalizedPoint(0.9, 0.1));
        stroke.Visit(new NormalizedPoint(0.9, 0.1)).Should().BeFalse();

        stroke.TargetSelectedState.Should().BeTrue();
        selected.Should().BeEquivalentTo([(0, 0), (0, 1), (0, 2)]);
    }

    [Fact]
    public void CellSelectionStroke_DeselectsAllNewlyVisitedCellsWhenStartingFromSelectedCell()
    {
        var grid = new CellGrid(2, 2);
        var selected = new HashSet<(int Row, int Col)> { (0, 0), (0, 1), (1, 0) };

        var stroke = new CellSelectionStroke(grid, selected, new NormalizedPoint(0.1, 0.1));
        stroke.Visit(new NormalizedPoint(0.9, 0.1));

        stroke.TargetSelectedState.Should().BeFalse();
        selected.Should().BeEquivalentTo([(1, 0)]);
    }
}
