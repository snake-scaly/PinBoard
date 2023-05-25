using Eto.Drawing;

namespace PinBoard;

public interface IPinImage
{
    bool IsIcon { get; }

    SizeF OriginalSize { get; }

    RectangleF GetViewRect(IMatrix viewTransform, bool crop);

    void Render(Graphics g, IMatrix viewTransform, bool crop);

    void Crop(RectangleF pinRect);
}
