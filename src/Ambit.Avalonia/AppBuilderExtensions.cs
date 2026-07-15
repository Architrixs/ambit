using System;
using Avalonia;

namespace Ambit.Avalonia;

/// <summary>
/// Provides extension methods for integrating Ambit with an Avalonia <see cref="AppBuilder"/>.
/// </summary>
public static class AppBuilderExtensions
{
    /// <summary>
    /// Initializes the Ambit annotation and rendering library with default built-in region and decoration registries.
    /// </summary>
    /// <param name="builder">The application builder instance.</param>
    /// <param name="configure">An optional callback to fluently configure custom region types and default styling.</param>
    /// <returns>The configured application builder.</returns>
    public static AppBuilder UseAmbit(this AppBuilder builder, Action<AmbitConfiguration>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var config = AmbitConfiguration.Default;
        config.RegisterDefaults();

        configure?.Invoke(config);

        return builder;
    }
}
