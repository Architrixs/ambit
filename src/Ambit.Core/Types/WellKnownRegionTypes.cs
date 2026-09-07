namespace Ambit;

/// <summary>
/// Well-known built-in region and decoration type ids for discoverability.
/// Prefer these over raw strings when setting <see cref="RegionEditController.ActiveDrawTypeId"/>.
/// </summary>
public static class WellKnownRegionTypes
{
    /// <inheritdoc cref="RectangleRegion.RectangleTypeId"/>
    public const string Rectangle = RectangleRegion.RectangleTypeId;
    /// <inheritdoc cref="EllipseRegion.EllipseTypeId"/>
    public const string Ellipse = EllipseRegion.EllipseTypeId;
    /// <inheritdoc cref="LineRegion.LineTypeId"/>
    public const string Line = LineRegion.LineTypeId;
    /// <inheritdoc cref="PolygonRegion.PolygonTypeId"/>
    public const string Polygon = PolygonRegion.PolygonTypeId;
    /// <inheritdoc cref="PolylineRegion.PolylineTypeId"/>
    public const string Polyline = PolylineRegion.PolylineTypeId;

    /// <summary>Sample-only custom region — not registered by default. See <c>samples/Ambit.Sample/Extensibility</c>.</summary>
    public const string Circle = "circle";
}

/// <summary>Well-known decoration type ids.</summary>
public static class WellKnownDecorationTypes
{
    /// <inheritdoc cref="LabelDecoration.LabelDecorationTypeId"/>
    public const string LabelBadge = LabelDecoration.LabelDecorationTypeId;
    /// <summary>Sample-only — registered in sample.</summary>
    public const string DirectionArrow = "direction-arrow";
    /// <summary>Sample-only — registered in sample.</summary>
    public const string CountBadge = "count-badge";
}
