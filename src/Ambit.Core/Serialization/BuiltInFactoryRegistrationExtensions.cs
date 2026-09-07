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

        TryRegister(registry, new RectangleRegionFactory());
        TryRegister(registry, new PolygonRegionFactory());
        TryRegister(registry, new PolylineRegionFactory());
        TryRegister(registry, new LineRegionFactory());
        TryRegister(registry, new EllipseRegionFactory());
        TryRegister(registry, new LabelDecorationFactory());
        return registry;
    }

    private static void TryRegister(IRegionTypeRegistry registry, IRegionFactory factory)
    {
        if (registry is RegionTypeRegistry concrete)
        {
            concrete.TryRegister(factory);
            return;
        }
        try { registry.Register(factory); } catch (InvalidOperationException) { }
    }

    private static void TryRegister(IRegionTypeRegistry registry, IDecorationFactory factory)
    {
        if (registry is RegionTypeRegistry concrete)
        {
            concrete.TryRegister(factory);
            return;
        }
        try { registry.Register(factory); } catch (InvalidOperationException) { }
    }
}
