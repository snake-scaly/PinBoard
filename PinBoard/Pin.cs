using Eto.Drawing;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PinBoard;

public class Pin : ReactiveObject
{
    private readonly float _initialSize;

    public Pin(Uri url, PointF center, float initialSize)
    {
        _initialSize = initialSize;
        CurrentImage = new PinIcon(Bitmap.FromResource("PinBoard.Resources.file-icon.png"));
        Center = center;
        _ = LoadAsync(url);
    }

    private async Task LoadAsync(Uri url)
    {
        try
        {
            var image = await Task.Run(
                    () =>
                    {
                        Task.Delay(TimeSpan.FromSeconds(5)).Wait();
                        return new Bitmap(url.LocalPath);
                    })
                .ConfigureAwait(continueOnCapturedContext: true);

            CurrentImage = new PinImage(image);
            Scale = Math.Min(_initialSize / image.Width, _initialSize / image.Height);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            CurrentImage = new PinIcon(Bitmap.FromResource("PinBoard.Resources.file-error-icon.png"));
        }
    }

    [Reactive]
    public PointF Center { get; set; }

    [Reactive]
    public float Scale { get; set; } = 1;

    public SizeF OriginalSize => CurrentImage.OriginalSize;

    public bool CanResize => !CurrentImage.IsIcon;

    public bool CanCrop => !CurrentImage.IsIcon;

    [Reactive]
    private IPinImage CurrentImage { get; set; }

    public RectangleF GetViewBounds(IMatrix boardViewTransform, bool crop = true) => CurrentImage.GetViewRect(GetPinViewTransform(boardViewTransform), crop);

    public void Render(Graphics g, IMatrix boardViewTransform, bool crop = true) => CurrentImage.Render(g, GetPinViewTransform(boardViewTransform), crop);

    public void Crop(RectangleF boardRect)
    {
        var boardPinTransform = GetPinTransform().Inverse();
        CurrentImage.Crop(boardPinTransform.TransformRectangle(boardRect));
        Center = boardRect.Center;
    }

    private IMatrix GetPinTransform()
    {
        var m = Matrix.Create();
        m.Translate(Center);
        m.Scale(Scale);
        return m;
    }

    private IMatrix GetPinViewTransform(IMatrix boardViewTransform)
    {
        var m = boardViewTransform.Clone();
        m.Prepend(GetPinTransform());
        return m;
    }
}
