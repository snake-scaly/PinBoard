using System.Text.RegularExpressions;
using Eto.Drawing;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace PinBoard;

public class Pin : ReactiveObject
{
    private RectangleF? _initialCrop;

    public Pin(Uri url, PointF center, float? scale = null, float? initialSize = null, RectangleF? initialCrop = null)
    {
        Url = url;
        Center = center;
        Scale = scale;
        InitialSize = initialSize;
        _initialCrop = initialCrop;

        if (url.IsFile)
            SetInitialImage(new Bitmap(url.LocalPath));
        else
            _ = LoadAsync(url);
    }

    public Pin(Image image, PointF center, float scale)
    {
        CurrentImage = new PinImage(image);
        Center = center;
        Scale = scale;
    }

    public Uri Url { get; }

    [Reactive]
    public PointF Center { get; set; }

    [Reactive]
    public float? Scale { get; set; }

    public SizeF OriginalSize => CurrentImage.OriginalSize;

    public bool CanResize => !CurrentImage.IsIcon;

    public bool CanCrop => !CurrentImage.IsIcon;

    public float? InitialSize { get; private set; }

    public RectangleF? CropRect => CurrentImage.CropRect ?? _initialCrop;

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

    private async Task LoadAsync(Uri url)
    {
        CurrentImage = new PinIcon(Bitmap.FromResource("PinBoard.Resources.url-icon.png"));

        try
        {
            var bitmap = await Task.Run(() => LoadSync(url)).ConfigureAwait(continueOnCapturedContext: true);
            SetInitialImage(bitmap);
        }
        catch (Exception e)
        {
            this.Log().Error(e, "Bitmap load failed");
            CurrentImage = new PinIcon(Bitmap.FromResource("PinBoard.Resources.url-error-icon.png"));
        }
    }

    private Bitmap LoadSync(Uri url)
    {
        this.Log().Info("Loading {url}", url);

        var httpClient = Locator.Current.GetService<HttpClient>();
        var response = httpClient!.GetAsync(url).GetAwaiter().GetResult();

        if (response.Content.Headers.ContentType?.ToString().StartsWith("image/") == true)
            return new Bitmap(response.Content.ReadAsStream());

        // Google search
        if (response.Content.Headers.ContentType?.ToString().StartsWith("text/html") == true)
        {
            var html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            this.Log().Debug("Got text/html {html}", html);

            var match = Regex.Match(html, @"imageUrl='([^?']*)");
            if (match.Success)
                return LoadSync(new Uri(match.Groups[1].Value));

            match = Regex.Match(html, @"<a href=""(https://[^?""]*).*?"">\1");
            if (match.Success)
                return LoadSync(new Uri(match.Groups[1].Value));

            throw new Exception("No match found");
        }

        throw new Exception($"Not an image: {response.Content.Headers.ContentType}\n{response.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
    }

    private void SetInitialImage(Image image)
    {
        CurrentImage = new PinImage(image, _initialCrop);

        if (InitialSize != null)
        {
            Scale = Math.Min(InitialSize.Value / image.Width, InitialSize.Value / image.Height);
            InitialSize = null;
        }
    }

    private IMatrix GetPinTransform()
    {
        var m = Matrix.Create();
        m.Translate(Center);
        m.Scale(Scale ?? 1f);
        return m;
    }

    private IMatrix GetPinViewTransform(IMatrix boardViewTransform)
    {
        var m = boardViewTransform.Clone();
        m.Prepend(GetPinTransform());
        return m;
    }
}
