using FluentAssertions;

namespace Ambit.Core.Tests;

public sealed class DecorationTests
{

    [Fact]
    public void LabelDecoration_IsNonInteractiveAndStoresText()
    {
        var decoration = new LabelDecoration(new NormalizedPoint(0.3, 0.4), "North Gate");

        decoration.IsInteractive.Should().BeFalse();
        decoration.Text.Should().Be("North Gate");
        decoration.TypeId.Should().Be(LabelDecoration.LabelDecorationTypeId);
    }
}
