namespace Ambit;

/// <summary>
/// Provides the default in-memory implementation of <see cref="IRegionTypeRegistry"/>.
/// </summary>
public sealed class RegionTypeRegistry : IRegionTypeRegistry
{
    /// <summary>
    /// Gets the default global instance of the region and decoration registry.
    /// </summary>
    public static RegionTypeRegistry Default { get; } = new();

    private readonly Dictionary<string, IRegionFactory> _regionFactories = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IDecorationFactory> _decorationFactories = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public void Register(IRegionFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        RegisterFactory(factory.TypeId, factory, _regionFactories, "region");
    }

    /// <inheritdoc />
    public void Register(IDecorationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        RegisterFactory(factory.TypeId, factory, _decorationFactories, "decoration");
    }

    /// <summary>
    /// Attempts to register a region factory; returns <c>false</c> if a factory with the same TypeId already exists.
    /// Use this for idempotent startup / sample extensibility without risking <see cref="InvalidOperationException"/>.
    /// </summary>
    public bool TryRegister(IRegionFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ValidateTypeId(factory.TypeId);
        return _regionFactories.TryAdd(factory.TypeId, factory);
    }

    /// <summary>
    /// Attempts to register a decoration factory; returns <c>false</c> if already registered.
    /// </summary>
    public bool TryRegister(IDecorationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ValidateTypeId(factory.TypeId);
        return _decorationFactories.TryAdd(factory.TypeId, factory);
    }

    /// <summary>
    /// Registers or replaces a region factory. Useful for overriding a built-in type in tests/samples.
    /// </summary>
    public void RegisterOrReplace(IRegionFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ValidateTypeId(factory.TypeId);
        _regionFactories[factory.TypeId] = factory;
    }

    /// <summary>
    /// Registers or replaces a decoration factory.
    /// </summary>
    public void RegisterOrReplace(IDecorationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ValidateTypeId(factory.TypeId);
        _decorationFactories[factory.TypeId] = factory;
    }

    /// <inheritdoc />
    public IRegionFactory GetRegionFactory(string typeId)
    {
        return GetFactory(typeId, _regionFactories, "region");
    }

    /// <inheritdoc />
    public IDecorationFactory GetDecorationFactory(string typeId)
    {
        return GetFactory(typeId, _decorationFactories, "decoration");
    }

    /// <inheritdoc />
    public IEditableRegion CreateRegion(RegionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var decorationFactoryInputs = dto.Decorations;
        if (decorationFactoryInputs.Count == 0)
        {
            return GetRegionFactory(dto.TypeId).Create(dto, Array.Empty<IDecoration>());
        }

        var decorations = new IDecoration[decorationFactoryInputs.Count];
        for (var index = 0; index < decorationFactoryInputs.Count; index++)
        {
            var decorationDto = decorationFactoryInputs[index];
            decorations[index] = GetDecorationFactory(decorationDto.TypeId).Create(decorationDto);
        }

        return GetRegionFactory(dto.TypeId).Create(dto, decorations);
    }

    /// <inheritdoc />
    public RegionDto ToDto(IRegion region)
    {
        ArgumentNullException.ThrowIfNull(region);

        var sourceDecorations = region.Decorations;
        if (sourceDecorations.Count == 0)
        {
            return GetRegionFactory(region.TypeId).ToDto(region, Array.Empty<DecorationDto>());
        }

        var decorationDtos = new DecorationDto[sourceDecorations.Count];
        for (var index = 0; index < sourceDecorations.Count; index++)
        {
            var decoration = sourceDecorations[index];
            decorationDtos[index] = GetDecorationFactory(decoration.TypeId).ToDto(decoration);
        }

        return GetRegionFactory(region.TypeId).ToDto(region, decorationDtos);
    }

    private static void RegisterFactory<TFactory>(string typeId, TFactory factory, IDictionary<string, TFactory> storage, string category)
    {
        ValidateTypeId(typeId);

        if (!storage.TryAdd(typeId, factory))
        {
            throw new InvalidOperationException($"A {category} factory is already registered for '{typeId}'.");
        }
    }

    private static TFactory GetFactory<TFactory>(string typeId, IReadOnlyDictionary<string, TFactory> storage, string category)
    {
        ValidateTypeId(typeId);

        if (!storage.TryGetValue(typeId, out var factory))
        {
            throw new KeyNotFoundException($"No {category} factory is registered for '{typeId}'.");
        }

        return factory;
    }

    private static void ValidateTypeId(string typeId)
    {
        if (string.IsNullOrWhiteSpace(typeId))
        {
            throw new ArgumentException("Type identifier must be a non-empty string.", nameof(typeId));
        }
    }
}
