namespace Ambit;

/// <summary>
/// Represents a body that can be hit-tested in normalized region space.
/// </summary>
public interface IHitTestable
{
    /// <summary>
    /// Tests whether the specified normalized point intersects the region body.
    /// </summary>
    /// <param name="point">The normalized point to test.</param>
    /// <param name="toleranceNormalized">The tolerance already converted to normalized space.</param>
    /// <returns><see langword="true"/> when the point intersects the body; otherwise <see langword="false"/>.</returns>
    bool HitTestBody(NormalizedPoint point, double toleranceNormalized);
}
