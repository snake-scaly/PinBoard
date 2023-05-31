using System.Text.RegularExpressions;
using Eto.Drawing;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace PinBoard;

public class Pin : ReactiveObject
{
    public Pin(Uri url, PointF center, float initialSize)
    {
        Center = center;

        if (url.IsFile)
            SetInitialImage(new Bitmap(url.LocalPath), initialSize);
        else
            _ = LoadAsync(url, initialSize);
    }

    public Pin(Image image, PointF center, float scale)
    {
        CurrentImage = new PinImage(image);
        Center = center;
        Scale = scale;
    }

    private async Task LoadAsync(Uri url, float initialSize)
    {
        CurrentImage = new PinIcon(Bitmap.FromResource("PinBoard.Resources.url-icon.png"));

        try
        {
            var bitmap = await Task.Run(() => LoadSync(url)).ConfigureAwait(continueOnCapturedContext: true);
            SetInitialImage(bitmap, initialSize);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            CurrentImage = new PinIcon(Bitmap.FromResource("PinBoard.Resources.url-error-icon.png"));
        }
    }

    private static Bitmap LoadSync(Uri url)
    {
        Console.WriteLine($"Loading {url}");

        var httpClient = Locator.Current.GetService<HttpClient>();
        var response = httpClient!.GetAsync(url).GetAwaiter().GetResult();

        if (response.Content.Headers.ContentType?.ToString().StartsWith("image/") == true)
            return new Bitmap(response.Content.ReadAsStream());

        // Google search
        if (response.Content.Headers.ContentType?.ToString().StartsWith("text/html") == true)
        {
            var html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var match = Regex.Match(html, @"imageUrl='([^']*?)'");
            if (match.Success)
                return LoadSync(new Uri(match.Groups[1].Value));
        }

        throw new Exception($"Not an image: {response.Content.Headers.ContentType}\n{response.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
    }

    private void SetInitialImage(Image image, float initialSize)
    {
        CurrentImage = new PinImage(image);
        Scale = Math.Min(initialSize / image.Width, initialSize / image.Height);
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
