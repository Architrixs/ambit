namespace Ambit;

/// <summary>
/// Registers the built-in Ambit region and decoration factories.
/// </summary>
public static class BuiltInFactoryRegistrationExtensions
{
    /// <summary>
    /// Registers all built-in region and decoration factories with the supplied registry.
    /// </summary>
    /// <param name="registry">The registry to populate.</param>
    /// <returns>The same registry instance.</returns>
    public static IRegionTypeRegistry RegisterBuiltInTypes(this IRegionTypeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(new RectangleRegionFactory());
        registry.Register(new PolygonRegionFactory());
        registry.Register(new PolylineRegionFactory());
        registry.Register(new LineRegionFactory());
        registry.Register(new EllipseRegionFactory());
        registry.Register(new LabelDecorationFactory());
        return registry;
    }
}
