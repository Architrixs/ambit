using System;

namespace Ambit;

/// <summary>
/// Provides developer-friendly configuration and fluent setup options for the Ambit library.
/// </summary>
public sealed class AmbitConfiguration
{
    private static readonly Lazy<AmbitConfiguration> _defaultInstance = new(() => new AmbitConfiguration().RegisterDefaults());

    /// <summary>
    /// Gets the global default configuration instance.
    /// Prefer <see cref="Create"/> + instance registry for testability / isolation;
    /// <see cref="Default"/> is retained for single-registry apps and legacy code.
    /// </summary>
    [Obsolete("Prefer AmbitConfiguration.Create() + CreateRegistry() for isolation. Default remains for single-registry apps but may be removed in 1.0.")]
    public static AmbitConfiguration Default => _defaultInstance.Value;

    /// <summary>
    /// Creates a new isolated configuration with its own <see cref="IRegionTypeRegistry"/>.
    /// Use this instead of <see cref="Default"/> when you need per-control or per-test isolation.
    /// </summary>
    public static AmbitConfiguration Create()
    {
        var cfg = new AmbitConfiguration();
        // Do not auto-register globals — caller decides registry target.
        return cfg;
    }

    /// <summary>
    /// Creates an isolated <see cref="IRegionTypeRegistry"/> pre-populated with built-ins.
    /// Example: <c>var registry = AmbitConfiguration.CreateRegistry(); registry.Register(myFactory);</c>
    /// </summary>
    public static IRegionTypeRegistry CreateRegistry()
    {
        return new RegionTypeRegistry().RegisterBuiltInTypes();
    }

    /// <summary>
    /// Gets or sets the default region style applied when creating new regions without explicit styling.
    /// </summary>
    public RegionStyle DefaultRegionStyle { get; set; } = new()
    {
        StrokeColorHex = "#38BDF8", // Vibrant sky blue
        StrokeThickness = 2.0d,
        FillColorHex = "#0EA5E9",
        FillOpacity = 0.25d
    };

    /// <summary>
    /// Registers all built-in region types and decorations with the default global registry.
    /// </summary>
    /// <returns>The current configuration instance for chaining.</returns>
    public AmbitConfiguration RegisterDefaults()
    {
        IRegionTypeRegistry.Default.RegisterBuiltInTypes();
        return this;
    }

    /// <summary>
    /// Fluently registers a custom region factory with the global registry.
    /// For isolated registries, use <c>registry.Register(factory)</c> directly.
    /// </summary>
    /// <param name="factory">The region factory to register.</param>
    /// <returns>The current configuration instance for chaining.</returns>
    public AmbitConfiguration RegisterRegionType(IRegionFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        IRegionTypeRegistry.Default.Register(factory);
        return this;
    }

    /// <summary>
    /// Fluently registers a custom decoration factory with the global registry.
    /// For isolated registries, use <c>registry.Register(factory)</c> directly.
    /// </summary>
    /// <param name="factory">The decoration factory to register.</param>
    /// <returns>The current configuration instance for chaining.</returns>
    public AmbitConfiguration RegisterDecorationType(IDecorationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        IRegionTypeRegistry.Default.Register(factory);
        return this;
    }

    /// <summary>
    /// Registers a factory on a specific registry (isolated / non-global). Preferred for new code.
    /// </summary>
    public AmbitConfiguration RegisterRegionType(IRegionTypeRegistry registry, IRegionFactory factory)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(factory);
        registry.Register(factory);
        return this;
    }

    /// <summary>
    /// Registers a decoration factory on a specific registry.
    /// </summary>
    public AmbitConfiguration RegisterDecorationType(IRegionTypeRegistry registry, IDecorationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(factory);
        registry.Register(factory);
        return this;
    }

    /// <summary>
    /// Configures the default region style used by new controllers and regions.
    /// </summary>
    /// <param name="configure">An action modifying the default region style.</param>
    /// <returns>The current configuration instance for chaining.</returns>
    public AmbitConfiguration ConfigureDefaultStyle(Action<RegionStyle> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(DefaultRegionStyle);
        return this;
    }
}
