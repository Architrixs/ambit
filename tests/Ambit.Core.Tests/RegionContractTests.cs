using FluentAssertions;

namespace Ambit.Core.Tests;

public sealed class RegionContractTests
{
    [Fact]
    public void RegionHandle_RejectsBlankHandleKind()
    {
        var action = () => new RegionHandle(0, new NormalizedPoint(0.5, 0.5), " ");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RegionStyle_UsesExpectedDefaultVisualValues()
    {
        var style = new RegionStyle
        {
            StrokeColorHex = "#FF0000",
        };

        style.StrokeThickness.Should().Be(2.0);
        style.FillOpacity.Should().Be(0.25);
        style.DefaultHandleStyle.Should().BeSameAs(HandleStyle.Default);
        style.LabelStyle.Should().BeNull();
    }

    [Fact]
    public void RegionRenderState_AllowsIndependentTransientSelections()
    {
        var hoveredId = Guid.NewGuid();
        var selectedId = Guid.NewGuid();
        var cells = new HashSet<(int Row, int Col)> { (1, 2) };

        var state = new RegionRenderState
        {
            HoveredRegionId = hoveredId,
            SelectedRegionId = selectedId,
            HoveredHandleIndex = 3,
            SelectedCells = cells,
        };

        state.HoveredRegionId.Should().Be(hoveredId);
        state.SelectedRegionId.Should().Be(selectedId);
        state.HoveredHandleIndex.Should().Be(3);
        state.SelectedCells.Should().BeSameAs(cells);
    }
}
