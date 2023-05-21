using Eto.Drawing;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PinBoard;

public class Pin : ReactiveObject
{
    [Reactive]
    public CroppedImage Image { get; set; }

    [Reactive]
    public PointF Center { get; set; }

    [Reactive]
    public float Scale { get; set; } = 1;

    public RectangleF Bounds => new(Image.SourceRect.Size * Scale) { Center = Center };
}
