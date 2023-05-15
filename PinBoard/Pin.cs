using System.Reactive.Linq;
using Eto.Drawing;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PinBoard;

public class Pin : ReactiveObject
{
    public Pin()
    {
        var stateObservable = this.WhenAnyValue(x => x.Image, x => x.Center, x => x.Scale);

        stateObservable
            .Select(_ => Image == null ? default : new RectangleF(Image.SourceRect.Size * Scale) { Center = Center })
            .ToPropertyEx(this, x => x.Bounds);

        Update = stateObservable.Select(_ => true);
    }

    [Reactive]
    public CroppedImage Image { get; set; }

    [Reactive]
    public PointF Center { get; set; }

    [Reactive]
    public float Scale { get; set; } = 1;

    [ObservableAsProperty]
    public RectangleF Bounds { get; }

    public IObservable<bool> Update { get; }
}
