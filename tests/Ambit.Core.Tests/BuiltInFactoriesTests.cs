using FluentAssertions;

namespace Ambit.Core.Tests;

public sealed class BuiltInFactoriesTests
{
    [Fact]
    public void RegisterBuiltInTypes_RegistersAllShippedFactories()
    {
        var registry = new RegionTypeRegistry().RegisterBuiltInTypes();

        registry.GetRegionFactory(RectangleRegion.RectangleTypeId).Should().BeOfType<RectangleRegionFactory>();
        registry.GetRegionFactory(PolygonRegion.PolygonTypeId).Should().BeOfType<PolygonRegionFactory>();
        registry.GetRegionFactory(PolylineRegion.PolylineTypeId).Should().BeOfType<PolylineRegionFactory>();
        registry.GetRegionFactory(LineRegion.LineTypeId).Should().BeOfType<LineRegionFactory>();
        registry.GetRegionFactory(EllipseRegion.EllipseTypeId).Should().BeOfType<EllipseRegionFactory>();
        registry.GetDecorationFactory(LabelDecoration.LabelDecorationTypeId).Should().BeOfType<LabelDecorationFactory>();
    }

    [Fact]
    public void BuiltInRoundTrip_PreservesAllGeometryStyleLabelAndDecorationState()
    {
        var registry = new RegionTypeRegistry().RegisterBuiltInTypes();
        var style = new RegionStyle
        {
            StrokeColorHex = "#112233",
            StrokeThickness = 3.5,
            FillColorHex = "#445566",
            FillOpacity = 0.4,
            LabelStyle = new LabelStyle
            {
                TextColorHex = "#FFFFFF",
                BackgroundColorHex = "#000000",
                FontSize = 14,
                AnchorOverride = new NormalizedPoint(0.3, 0.2),
            },
        };

        IRegion[] regions =
        [
            new RectangleRegion(
                new NormalizedPoint(0.1, 0.2),
                new NormalizedPoint(0.4, 0.6),
                style,
                decorations:
                [
                    new LabelDecoration(new NormalizedPoint(0.25, 0.2), "Dir1"),
                    new LabelDecoration(new NormalizedPoint(0.25, 0.15), "Rect"),
                ],
                label: "Rectangle",
                lockAspectRatio: true),
            new PolygonRegion(
                [
                    new NormalizedPoint(0.1, 0.1),
                    new NormalizedPoint(0.8, 0.1),
                    new NormalizedPoint(0.7, 0.5),
                    new NormalizedPoint(0.2, 0.6),
                ],
                style,
                decorations: [new LabelDecoration(new NormalizedPoint(0.4, 0.2), "Poly")],
                label: "Polygon"),
            new PolylineRegion(
                [
                    new NormalizedPoint(0.2, 0.2),
                    new NormalizedPoint(0.4, 0.3),
                    new NormalizedPoint(0.6, 0.5),
                ],
                style,
                label: "Polyline"),
            new LineRegion(
                new NormalizedPoint(0.2, 0.7),
                new NormalizedPoint(0.9, 0.7),
                style,
                decorations:
                [
                    new LabelDecoration(new NormalizedPoint(0.3, 0.7), "Dir2"),
                    new LabelDecoration(new NormalizedPoint(0.8, 0.7), "Dir3"),
                ],
                label: "Line"),
            new EllipseRegion(
                new NormalizedPoint(0.3, 0.2),
                new NormalizedPoint(0.9, 0.8),
                style,
                decorations: [new LabelDecoration(new NormalizedPoint(0.6, 0.15), "Ellipse")],
                label: "Ellipse",
                lockAspectRatio: true),
        ];

        foreach (var region in regions)
        {
            var dto = registry.ToDto(region);
            var recreated = registry.CreateRegion(dto);
            var recreatedDto = registry.ToDto(recreated);

            recreatedDto.TypeId.Should().Be(dto.TypeId);
            recreatedDto.Id.Should().Be(dto.Id);
            recreatedDto.Label.Should().Be(dto.Label);
            recreatedDto.Style.Should().BeSameAs(dto.Style);
            recreatedDto.Vertices.Should().Equal(dto.Vertices);
            recreatedDto.Properties.Should().Equal(dto.Properties);
            recreatedDto.Decorations.Should().HaveCount(dto.Decorations.Count);
            for (var index = 0; index < dto.Decorations.Count; index++)
            {
                recreatedDto.Decorations[index].TypeId.Should().Be(dto.Decorations[index].TypeId);
                recreatedDto.Decorations[index].Anchor.Should().Be(dto.Decorations[index].Anchor);
                recreatedDto.Decorations[index].IsInteractive.Should().Be(dto.Decorations[index].IsInteractive);
                recreatedDto.Decorations[index].Properties.Should().Equal(dto.Decorations[index].Properties);
            }
        }
    }
}
