namespace Ambit;

/// <summary>
/// Resolves region and decoration factories by open-ended type identifier.
/// </summary>
public interface IRegionTypeRegistry
{
    /// <summary>
    /// Gets the shared default global region and decoration type registry.
    /// Prefer <see cref="AmbitConfiguration.CreateRegistry"/> for isolation.
    /// </summary>
    [Obsolete("Prefer AmbitConfiguration.CreateRegistry() for isolation. Global Default may be removed in 1.0.")]
    public static IRegionTypeRegistry Default => RegionTypeRegistry.Default;

    /// <summary>
    /// Registers a region factory.
    /// </summary>
    /// <param name="factory">The region factory to register.</param>
    void Register(IRegionFactory factory);

    /// <summary>
    /// Registers a decoration factory.
    /// </summary>
    /// <param name="factory">The decoration factory to register.</param>
    void Register(IDecorationFactory factory);

    /// <summary>
    /// Resolves a region factory by type identifier.
    /// </summary>
    /// <param name="typeId">The region type identifier.</param>
    /// <returns>The matching region factory.</returns>
    IRegionFactory GetRegionFactory(string typeId);

    /// <summary>
    /// Resolves a decoration factory by type identifier.
    /// </summary>
    /// <param name="typeId">The decoration type identifier.</param>
    /// <returns>The matching decoration factory.</returns>
    IDecorationFactory GetDecorationFactory(string typeId);

    /// <summary>
    /// Materializes a runtime region from serialized data.
    /// </summary>
    /// <param name="dto">The serialized region data.</param>
    /// <returns>The runtime region instance.</returns>
    IEditableRegion CreateRegion(RegionDto dto);

    /// <summary>
    /// Converts a runtime region into serialized form.
    /// </summary>
    /// <param name="region">The runtime region instance.</param>
    /// <returns>The serialized region data.</returns>
    RegionDto ToDto(IRegion region);
}
