using Eto.Drawing;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PinBoard;

public class Pin : ReactiveObject
{
    [Reactive]
    public PinImage Image { get; set; }

    [Reactive]
    public PointF Center { get; set; }

    [Reactive]
    public float Scale { get; set; } = 1;

    public RectangleF Bounds => new(Image.SourceRect.Size * Scale) { Center = Center };

    public SizeF OriginalSize => Image.SourceRect.Size;

    public bool CanResize => !Image.IsIcon;

    public bool CanCrop => !Image.IsIcon;

    public RectangleF GetBounds(IMatrix boardViewTransform) => Image.GetViewRect(GetImageViewTransform(boardViewTransform));

    public void Render(Graphics g, IMatrix boardViewTransform) => Image.Render(g, GetImageViewTransform(boardViewTransform));

    private IMatrix GetImageViewTransform(IMatrix boardViewTransform)
    {
        var imageViewTransform = boardViewTransform.Clone();
        imageViewTransform.Translate(Center);
        imageViewTransform.Scale(Scale);
        return imageViewTransform;
    }
}
