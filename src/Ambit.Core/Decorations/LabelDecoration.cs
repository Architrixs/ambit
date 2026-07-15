namespace Ambit;

/// <summary>
/// Represents a non-interactive text label decoration attached to a region.
/// </summary>
public sealed class LabelDecoration : IAnchorableDecoration
{
    /// <summary>
    /// The built-in type identifier for label decorations.
    /// </summary>
    public const string LabelDecorationTypeId = "label-badge";

    /// <summary>
    /// Initializes a new instance of the <see cref="LabelDecoration"/> class.
    /// </summary>
    /// <param name="anchor">The anchor position in normalized region space.</param>
    /// <param name="text">The rendered label text.</param>
    public LabelDecoration(NormalizedPoint anchor, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Label text must be a non-empty string.", nameof(text));
        }

        Anchor = anchor;
        Text = text;
    }

    /// <inheritdoc />
    public string TypeId => LabelDecorationTypeId;

    /// <inheritdoc />
    public NormalizedPoint Anchor { get; set; }

    /// <inheritdoc />
    public bool IsInteractive => false;

    /// <summary>
    /// Gets the rendered label text.
    /// </summary>
    public string Text { get; }
}
