namespace Ambit;

/// <summary>
/// Describes an interactive handle exposed by a region or decoration.
/// </summary>
public readonly record struct RegionHandle
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegionHandle"/> struct.
    /// </summary>
    /// <param name="index">The stable handle index within the owning shape.</param>
    /// <param name="position">The handle position in normalized region space.</param>
    /// <param name="handleKind">The open-ended kind identifier for the handle.</param>
    public RegionHandle(int index, NormalizedPoint position, string handleKind)
    {
        Index = index;
        Position = position;
        HandleKind = ValidateHandleKind(handleKind);
    }

    /// <summary>
    /// Gets the stable handle index within the owning shape.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the handle position in normalized region space.
    /// </summary>
    public NormalizedPoint Position { get; }

    /// <summary>
    /// Gets the open-ended kind identifier for the handle.
    /// </summary>
    public string HandleKind { get; }

    private static string ValidateHandleKind(string handleKind)
    {
        if (string.IsNullOrWhiteSpace(handleKind))
        {
            throw new ArgumentException("Handle kind must be a non-empty string.", nameof(handleKind));
        }

        return handleKind;
    }
}
