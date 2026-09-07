using Ambit.Avalonia.Rendering;
using SkiaSharp;

namespace Ambit.Sample.Extensibility;

/// <summary>
/// Sample-only toggle decoration. Two instances on one line give independent in/out arrows.
/// </summary>
public sealed class DirectionIndicatorDecoration : IToggleDecoration
{
    public const string DirectionIndicatorTypeId = "direction-arrow";
    public DirectionIndicatorDecoration(NormalizedPoint anchor, int directionSign = 1)
    {
        Anchor = anchor; DirectionSign = NormalizeDirectionSign(directionSign);
    }
    public string TypeId => DirectionIndicatorTypeId;
    public NormalizedPoint Anchor { get; set; }
    public bool IsInteractive => true;
    public int DirectionSign { get; set; }
    public string? StrokeColorHex { get; set; }
    public string? FillColorHex { get; set; }
    public float? ArrowSize { get; set; }
    public void Toggle() => DirectionSign = DirectionSign switch { 1 => -1, -1 => 2, 2 => 0, _ => 1 };
    private static int NormalizeDirectionSign(int v) => v switch { 1=>1,-1=>-1,2=>2,0=>0,_=> throw new ArgumentOutOfRangeException(nameof(v),"Direction sign must be 1,-1,2,0.") };
}

public sealed class DirectionIndicatorDecorationRenderer : IDecorationRenderer
{
    public string TypeId => DirectionIndicatorDecoration.DirectionIndicatorTypeId;
    private static SKPoint ToSk(ICoordinateTransform t, NormalizedPoint p) { var cp=t.ToControlSpace(p); return new SKPoint((float)cp.X,(float)cp.Y); }
    private static SKPoint GetDir(IRegion region, NormalizedPoint anchor)
    {
        if (region.Vertices.Count<2) return new SKPoint(1,0);
        var nearestStart=region.Vertices[0]; var nearestEnd=region.Vertices[1]; var best=double.MaxValue;
        var segs= region is PolygonRegion ? region.Vertices.Count : region.Vertices.Count-1;
        for(var i=0;i<segs;i++){var s=region.Vertices[i]; var e=region.Vertices[(i+1)%region.Vertices.Count]; var d=GeometryUtilities.DistanceToSegment(anchor,s,e); if(d<best){best=d; nearestStart=s; nearestEnd=e;}}
        var dx=(float)(nearestEnd.X-nearestStart.X); var dy=(float)(nearestEnd.Y-nearestStart.Y); var len=MathF.Sqrt(dx*dx+dy*dy); return len<=float.Epsilon?new SKPoint(1,0):new SKPoint(dx/len,dy/len);
    }
    public void Render(SKCanvas canvas, IDecoration decoration, IRegion owner, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources)
    {
        var d=(DirectionIndicatorDecoration)decoration; var anchor=ToSk(transform,d.Anchor); var dir=GetDir(owner,d.Anchor);
        var scale=d.ArrowSize??1f; var sign=d.DirectionSign; var perpA=new SKPoint(-dir.Y,dir.X); var perpB=new SKPoint(dir.Y,-dir.X);
        if(sign==1) DrawArrow(canvas,anchor,perpA,dir,scale,resources,d);
        else if(sign==-1) DrawArrow(canvas,anchor,perpB,dir,scale,resources,d);
        else if(sign==2){ DrawArrow(canvas,anchor,perpA,dir,scale,resources,d); DrawArrow(canvas,anchor,perpB,dir,scale,resources,d); }
        else { var c=d.StrokeColorHex!=null?SKColor.Parse(d.StrokeColorHex):new SKColor(0x94,0xA3,0xB8); resources.StrokePaint.Color=c; resources.StrokePaint.StrokeWidth=2f*scale; resources.StrokePaint.Style=SKPaintStyle.Stroke; resources.StrokePaint.PathEffect=null; var tick=5f*scale; canvas.DrawLine(new SKPoint(anchor.X-perpA.X*tick,anchor.Y-perpA.Y*tick), new SKPoint(anchor.X+perpA.X*tick,anchor.Y+perpA.Y*tick), resources.StrokePaint); }
    }
    private static void DrawArrow(SKCanvas canvas, SKPoint anchor, SKPoint dir, SKPoint segDir, float scale, SkiaRenderResources r, DirectionIndicatorDecoration d)
    {
        float len=14f*scale, wingBack=5f*scale, wingOut=3.5f*scale;
        var tip=new SKPoint(anchor.X+dir.X*len,anchor.Y+dir.Y*len);
        var left=new SKPoint(tip.X-dir.X*wingBack+segDir.X*wingOut, tip.Y-dir.Y*wingBack+segDir.Y*wingOut);
        var right=new SKPoint(tip.X-dir.X*wingBack-segDir.X*wingOut, tip.Y-dir.Y*wingBack-segDir.Y*wingOut);
        var c=d.StrokeColorHex!=null?SKColor.Parse(d.StrokeColorHex):new SKColor(0xF5,0x9E,0x0B);
        r.StrokePaint.Color=c; r.StrokePaint.StrokeWidth=2f*scale; r.StrokePaint.Style=SKPaintStyle.Stroke; r.StrokePaint.PathEffect=null;
        canvas.DrawLine(anchor,tip,r.StrokePaint); canvas.DrawLine(tip,left,r.StrokePaint); canvas.DrawLine(tip,right,r.StrokePaint);
    }
}

public sealed class DirectionIndicatorDecorationFactory : IDecorationFactory
{
    private const string DirectionSignProperty="directionSign", StrokeColorProperty="strokeColor", FillColorProperty="fillColor", ArrowSizeProperty="arrowSize";
    public string TypeId => DirectionIndicatorDecoration.DirectionIndicatorTypeId;
    public IDecoration Create(DecorationDto dto)
    {
        var sign=dto.Properties.TryGetValue(DirectionSignProperty,out var raw)&&int.TryParse(raw,out var p)?p:1;
        var sc=dto.Properties.TryGetValue(StrokeColorProperty,out var s)?s:null;
        var fc=dto.Properties.TryGetValue(FillColorProperty,out var f)?f:null;
        var arrow=dto.Properties.TryGetValue(ArrowSizeProperty,out var sz)&&float.TryParse(sz,out var v)?(float?)v:null;
        return new DirectionIndicatorDecoration(dto.Anchor,sign){ StrokeColorHex=sc, FillColorHex=fc, ArrowSize=arrow };
    }
    public DecorationDto ToDto(IDecoration decoration)
    {
        var ind=(DirectionIndicatorDecoration)decoration;
        return new DecorationDto{ TypeId=ind.TypeId, Anchor=ind.Anchor, IsInteractive=ind.IsInteractive,
            Properties=new Dictionary<string,string?>{[DirectionSignProperty]=ind.DirectionSign.ToString(),[StrokeColorProperty]=ind.StrokeColorHex,[FillColorProperty]=ind.FillColorHex,[ArrowSizeProperty]=ind.ArrowSize?.ToString()}};
    }
}
