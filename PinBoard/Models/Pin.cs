using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Eto.Drawing;

namespace PinBoard.Models;

public class Pin
{
    private readonly Subject<Unit> _updates = new();

    public Pin(Uri url, PointF center, float initialSize)
    {
        Url = url;
        Center = center;
        InitialSize = initialSize;
    }

    public Pin(Uri url, PointF center, float scale, RectangleF cropRect)
    {
        Url = url;
        Center = center;
        Scale = scale;
        CropRect = cropRect;
    }

    public Pin(Image image, PointF center, float scale, RectangleF? cropRect = null)
    {
        Image = image;
        Center = center;
        Scale = scale;
        CropRect = cropRect ?? new RectangleF(image.Size);
    }

    public IObservable<Unit> Updates => _updates.AsObservable();
    public Uri? Url { get; }
    public Image? Image { get; }
    public float? InitialSize { get; private set; }
    public PointF Center { get; private set; }
    public float? Scale { get; private set; }
    public RectangleF? CropRect { get; private set; }

    public void Edit(Action<IMutablePin> editAction)
    {
        editAction(new Mutable(this));
        _updates.OnNext(default);
    }

    public interface IMutablePin
    {
        float? InitialSize { get; set; }
        PointF Center { get; set; }
        float? Scale { get; set; }
        RectangleF? CropRect { get; set; }
    }
    
    private class Mutable : IMutablePin
    {
        private readonly Pin _pin;

        public Mutable(Pin pin)
        {
            _pin = pin;
        }

        public float? InitialSize
        {
            get => _pin.InitialSize;
            set => _pin.InitialSize = value;
        }

        public PointF Center
        {
            get => _pin.Center;
            set => _pin.Center = value;
        }

        public float? Scale
        {
            get => _pin.Scale;
            set => _pin.Scale = value;
        }

        public RectangleF? CropRect
        {
            get => _pin.CropRect;
            set => _pin.CropRect = value;
        }
    }
}
