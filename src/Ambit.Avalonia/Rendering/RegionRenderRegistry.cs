namespace Ambit.Avalonia.Rendering;

/// <summary>
/// Resolves region and decoration renderers by open-ended type identifier.
/// </summary>
public sealed class RegionRenderRegistry
{
    private readonly Dictionary<string, IRegionRenderer> _regionRenderers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IDecorationRenderer> _decorationRenderers = new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a region renderer.
    /// </summary>
    /// <param name="renderer">The renderer to register.</param>
    public void Register(IRegionRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        Register(renderer.TypeId, renderer, _regionRenderers, "region");
    }

    /// <summary>
    /// Registers a decoration renderer.
    /// </summary>
    /// <param name="renderer">The renderer to register.</param>
    public void Register(IDecorationRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        Register(renderer.TypeId, renderer, _decorationRenderers, "decoration");
    }

    /// <summary>
    /// Resolves a region renderer by type identifier.
    /// </summary>
    /// <param name="typeId">The region type identifier.</param>
    /// <returns>The matching renderer.</returns>
    public IRegionRenderer GetRegionRenderer(string typeId)
    {
        return Get(typeId, _regionRenderers, "region");
    }

    /// <summary>
    /// Resolves a decoration renderer by type identifier.
    /// </summary>
    /// <param name="typeId">The decoration type identifier.</param>
    /// <returns>The matching renderer.</returns>
    public IDecorationRenderer GetDecorationRenderer(string typeId)
    {
        return Get(typeId, _decorationRenderers, "decoration");
    }

    private static void Register<TValue>(string typeId, TValue renderer, IDictionary<string, TValue> target, string category)
    {
        if (string.IsNullOrWhiteSpace(typeId))
        {
            throw new ArgumentException("Type identifier must be a non-empty string.", nameof(typeId));
        }

        if (!target.TryAdd(typeId, renderer))
        {
            throw new InvalidOperationException($"A {category} renderer is already registered for '{typeId}'.");
        }
    }

    private static TValue Get<TValue>(string typeId, IReadOnlyDictionary<string, TValue> source, string category)
    {
        if (string.IsNullOrWhiteSpace(typeId))
        {
            throw new ArgumentException("Type identifier must be a non-empty string.", nameof(typeId));
        }

        if (!source.TryGetValue(typeId, out var value))
        {
            throw new KeyNotFoundException($"No {category} renderer is registered for '{typeId}'.");
        }

        return value;
    }
}
