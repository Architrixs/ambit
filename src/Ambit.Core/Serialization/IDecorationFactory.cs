namespace Ambit;

/// <summary>
/// Creates and serializes a single decoration type.
/// </summary>
public interface IDecorationFactory
{
    /// <summary>
    /// Gets the open-ended decoration type identifier handled by this factory.
    /// </summary>
    string TypeId { get; }

    /// <summary>
    /// Creates a runtime decoration instance from serialized data.
    /// </summary>
    /// <param name="dto">The serialized decoration data.</param>
    /// <returns>The runtime decoration instance.</returns>
    IDecoration Create(DecorationDto dto);

    /// <summary>
    /// Converts a runtime decoration into serialized form.
    /// </summary>
    /// <param name="decoration">The runtime decoration instance.</param>
    /// <returns>The serialized decoration data.</returns>
    DecorationDto ToDto(IDecoration decoration);
}
