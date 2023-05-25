using Eto.Drawing;

namespace PinBoard;

public class PinIcon : IPinImage
{
    private readonly Image _source;

    public PinIcon(Image source)
    {
        _source = source;
    }

    public bool IsIcon => true;

    public SizeF OriginalSize => _source.Size;

    public RectangleF GetViewRect(IMatrix viewTransform, bool crop)
    {
        return new RectangleF(_source.Size) { Center = viewTransform.TransformPoint(default) };
    }

    public void Render(Graphics g, IMatrix viewTransform, bool crop)
    {
        g.DrawImage(_source, viewTransform.TransformPoint(default) - _source.Size / 2);
    }

    public void Crop(RectangleF pinRect)
    {
        throw new InvalidOperationException();
    }
}
