using FluentAssertions;

namespace Ambit.Core.Tests;

public sealed class RegionTypeRegistryTests
{
    [Fact]
    public void Register_RejectsDuplicateRegionTypeIds()
    {
        var registry = new RegionTypeRegistry();
        registry.Register(new TestRegionFactory("test-region"));

        var action = () => registry.Register(new TestRegionFactory("test-region"));

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateRegion_UsesDecorationAndRegionFactoriesForRoundTripConstruction()
    {
        var registry = new RegionTypeRegistry();
        registry.Register(new TestDecorationFactory("direction-arrow"));
        registry.Register(new TestRegionFactory("line"));

        var dto = new RegionDto
        {
            Id = Guid.NewGuid(),
            TypeId = "line",
            Vertices = new[] { new NormalizedPoint(0.1, 0.2), new NormalizedPoint(0.8, 0.9) },
            Decorations =
            [
                new DecorationDto
                {
                    TypeId = "direction-arrow",
                    Anchor = new NormalizedPoint(0.4, 0.5),
                    IsInteractive = true,
                    Properties = new Dictionary<string, string?> { ["direction"] = "-1" },
                },
            ],
            Style = new RegionStyle
            {
                StrokeColorHex = "#00FF00",
            },
            Label = "tripwire",
            Properties = new Dictionary<string, string?> { ["kind"] = "demo" },
        };

        var region = registry.CreateRegion(dto);

        region.TypeId.Should().Be("line");
        region.Decorations.Should().ContainSingle();
        region.Decorations[0].TypeId.Should().Be("direction-arrow");
        region.Style.StrokeColorHex.Should().Be("#00FF00");
    }

    [Fact]
    public void ToDto_DelegatesSerializationToRegisteredFactories()
    {
        var registry = new RegionTypeRegistry();
        registry.Register(new TestDecorationFactory("badge"));
        registry.Register(new TestRegionFactory("star"));

        var region = new TestRegion(
            Guid.NewGuid(),
            "star",
            [new NormalizedPoint(0.2, 0.3)],
            [
                new TestDecoration(
                    "badge",
                    new NormalizedPoint(0.7, 0.8),
                    false,
                    new Dictionary<string, string?> { ["count"] = "5" })
            ],
            new RegionStyle { StrokeColorHex = "#123456" },
            "label",
            new Dictionary<string, string?> { ["points"] = "5" });

        var dto = registry.ToDto(region);

        dto.TypeId.Should().Be("star");
        dto.Decorations.Should().ContainSingle();
        dto.Decorations[0].Properties["count"].Should().Be("5");
        dto.Properties["points"].Should().Be("5");
    }

    [Fact]
    public void CellSelectionDto_CapturesGridDimensionsAndSelectedCells()
    {
        var dto = new CellSelectionDto
        {
            Rows = 4,
            Columns = 6,
            SelectedCells = [new CellCoordinateDto(1, 2), new CellCoordinateDto(3, 4)],
        };

        dto.Rows.Should().Be(4);
        dto.Columns.Should().Be(6);
        dto.SelectedCells.Should().ContainInOrder(new CellCoordinateDto(1, 2), new CellCoordinateDto(3, 4));
    }

    private sealed record TestDecoration(
        string TypeId,
        NormalizedPoint Anchor,
        bool IsInteractive,
        IReadOnlyDictionary<string, string?> Properties) : IDecoration;

    private sealed class TestDecorationFactory(string typeId) : IDecorationFactory
    {
        public string TypeId { get; } = typeId;

        public IDecoration Create(DecorationDto dto)
        {
            return new TestDecoration(dto.TypeId, dto.Anchor, dto.IsInteractive, dto.Properties);
        }

        public DecorationDto ToDto(IDecoration decoration)
        {
            var typedDecoration = (TestDecoration)decoration;

            return new DecorationDto
            {
                TypeId = typedDecoration.TypeId,
                Anchor = typedDecoration.Anchor,
                IsInteractive = typedDecoration.IsInteractive,
                Properties = typedDecoration.Properties,
            };
        }
    }

    private sealed class TestRegionFactory(string typeId) : IRegionFactory
    {
        public string TypeId { get; } = typeId;

        public IEditableRegion Create(RegionDto dto, IReadOnlyList<IDecoration> decorations)
        {
            return new TestRegion(dto.Id, dto.TypeId, dto.Vertices, decorations, dto.Style, dto.Label, dto.Properties);
        }

        public RegionDto ToDto(IRegion region, IReadOnlyList<DecorationDto> decorations)
        {
            var typedRegion = (TestRegion)region;

            return new RegionDto
            {
                Id = typedRegion.Id,
                TypeId = typedRegion.TypeId,
                Vertices = typedRegion.Vertices,
                Decorations = decorations,
                Style = typedRegion.Style,
                Label = typedRegion.Label,
                Properties = typedRegion.Properties,
            };
        }
    }

    private sealed class TestRegion(
        Guid id,
        string typeId,
        IReadOnlyList<NormalizedPoint> vertices,
        IReadOnlyList<IDecoration> decorations,
        RegionStyle style,
        string? label,
        IReadOnlyDictionary<string, string?> properties) : IEditableRegion
    {
        public Guid Id { get; } = id;

        public string TypeId { get; } = typeId;

        public IReadOnlyList<NormalizedPoint> Vertices { get; } = vertices;

        public IReadOnlyList<IDecoration> Decorations { get; } = decorations;

        public RegionStyle Style { get; } = style;

        public string? Label { get; } = label;

        public IReadOnlyDictionary<string, string?> Properties { get; } = properties;

        public IReadOnlyList<RegionHandle> GetHandles()
        {
            return Array.Empty<RegionHandle>();
        }

        public bool HitTestBody(NormalizedPoint point, double toleranceNormalized)
        {
            return false;
        }

        public void MoveHandle(int handleIndex, NormalizedPoint newPosition)
        {
        }

        public void Translate(NormalizedVector delta)
        {
        }
    }
}
