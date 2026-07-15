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
    /// </summary>
    public static AmbitConfiguration Default => _defaultInstance.Value;

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
