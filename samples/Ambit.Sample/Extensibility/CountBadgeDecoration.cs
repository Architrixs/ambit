using Ambit.Avalonia.Rendering;
using SkiaSharp;

namespace Ambit.Sample.Extensibility;

/// <summary>Sample-only non-interactive badge decoration.</summary>
public sealed class CountBadgeDecoration : IDecoration
{
    public const string CountBadgeTypeId = "count-badge";
    public CountBadgeDecoration(NormalizedPoint anchor, int count) { Anchor=anchor; Count=count; }
    public string TypeId => CountBadgeTypeId;
    public NormalizedPoint Anchor { get; }
    public bool IsInteractive => false;
    public int Count { get; }
}

public sealed class CountBadgeDecorationRenderer : IDecorationRenderer
{
    public string TypeId => CountBadgeDecoration.CountBadgeTypeId;
    public void Render(SKCanvas canvas, IDecoration decoration, IRegion owner, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources)
    {
        var badge=(CountBadgeDecoration)decoration; var cp=transform.ToControlSpace(badge.Anchor); var pt=new SKPoint((float)cp.X,(float)cp.Y);
        var fill=resources.FillPaint; fill.Color=new SKColor(239,68,68);
        var stroke=resources.StrokePaint; stroke.Color=SKColors.White; stroke.StrokeWidth=1.5f; stroke.PathEffect=null;
        canvas.DrawCircle(pt,10f,fill); canvas.DrawCircle(pt,10f,stroke);
        var tp=resources.ConfigureTextPaint("#FFFFFF",11f); var text=badge.Count.ToString(); var w=tp.MeasureText(text);
        canvas.DrawText(text, pt.X - w/2f, pt.Y + 4f, tp);
    }
}

public sealed class CountBadgeDecorationFactory : IDecorationFactory
{
    public string TypeId => CountBadgeDecoration.CountBadgeTypeId;
    public IDecoration Create(DecorationDto dto)
    {
        var count=dto.Properties.TryGetValue("count",out var raw)&&int.TryParse(raw,out var p)?p:0;
        return new CountBadgeDecoration(dto.Anchor,count);
    }
    public DecorationDto ToDto(IDecoration decoration)
    {
        var badge=(CountBadgeDecoration)decoration;
        return new DecorationDto{ TypeId=badge.TypeId, Anchor=badge.Anchor, IsInteractive=badge.IsInteractive, Properties=new Dictionary<string,string?>{["count"]=badge.Count.ToString()}};
    }
}
