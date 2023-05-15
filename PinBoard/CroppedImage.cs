using Eto.Drawing;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PinBoard;

public class CroppedImage : ReactiveObject
{
    public CroppedImage(Image source)
    {
        Source = source;
        SourceRect = new RectangleF(default, source.Size);
        this.WhenAnyValue(x => x.SourceRect.Width).ToPropertyEx(this, x => x.Width);
        this.WhenAnyValue(x => x.SourceRect.Height).ToPropertyEx(this, x => x.Height);
        this.WhenAnyValue(x => x.SourceRect.Size).ToPropertyEx(this, x => x.Size);
    }

    public Image Source { get; }

    [Reactive]
    public RectangleF SourceRect { get; set; }

    [ObservableAsProperty]
    public float Width { get; }

    [ObservableAsProperty]
    public float Height { get; }
    
    [ObservableAsProperty]
    public SizeF Size { get; }
}
