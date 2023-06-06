using Eto.Drawing;

namespace PinBoard;

public class PinImage : IPinImage
{
    private readonly Image _source;
    private RectangleF _sourceRect;

    public PinImage(Image source, RectangleF? sourceRect = null)
    {
        _source = source;
        _sourceRect = sourceRect ?? new RectangleF(default, source.Size);
    }

    public bool IsIcon => false;

    public SizeF OriginalSize => _sourceRect.Size;

    public RectangleF? CropRect => _sourceRect;

    public RectangleF GetViewRect(IMatrix viewTransform, bool crop)
    {
        if (crop)
            return viewTransform.TransformRectangle(CenterRect(_sourceRect.Size, new PointF(_sourceRect.Size / 2)));
        return viewTransform.TransformRectangle(CenterRect(_source.Size, _sourceRect.Center));
    }

    public void Render(Graphics g, IMatrix viewTransform, bool crop)
    {
        if (crop)
            g.DrawImage(_source, _sourceRect, GetViewRect(viewTransform, true));
        else
            g.DrawImage(_source, GetViewRect(viewTransform, false));
    }

    public void Crop(RectangleF pinRect)
    {
        pinRect.Offset(_sourceRect.Center);
        _sourceRect = pinRect;
    }

    private static RectangleF CenterRect(SizeF size, PointF center)
    {
        var r = new RectangleF(size);
        r.Offset(-center);
        return r;
    }
}
