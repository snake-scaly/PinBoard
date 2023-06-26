using System.Net;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using Eto.Drawing;
using Microsoft.Extensions.Logging;
using PinBoard.Models;
using ReactiveUI;

namespace PinBoard.ViewModels;

public sealed class PinViewModel : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PinViewModel> _logger;
    private readonly Subject<Unit> _updates = new();
    private readonly CompositeDisposable _disposables = new();

    private bool _updating;

    public PinViewModel(Pin pin, HttpClient httpClient, ILogger<PinViewModel> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        Pin = pin;

        if (pin.Image != null)
            SetImage(pin.Image);
        else if (pin.Url == null)
            Icon = Bitmap.FromResource("BoardView.Resources.file-error-icon.png");
        else if (pin.Url.IsFile)
            SetImage(new Bitmap(pin.Url.LocalPath));
        else
            _ = LoadAsync(pin.Url);

        pin.Updates.Subscribe(
            _ =>
            {
                if (!_updating)
                    _updates.OnNext(default);
            })
            .DisposeWith(_disposables);
    }

    public IObservable<Unit> Updates => _updates.AsObservable();

    public Pin Pin { get; }

    public Image? Image { get; private set; }

    public Image? Icon { get; private set; }

    public RectangleF? DisplayRect
    {
        get
        {
            if (Pin.CropRect == null)
                return null;
            var r = Pin.CropRect.Value * Pin.Scale!.Value;
            r.Center = Pin.Center;
            return r;
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    private async Task LoadAsync(Uri url)
    {
        Icon = Bitmap.FromResource("PinBoard.Resources.url-icon.png");

        try
        {
            var bitmap = await Observable.Start(() => LoadSync(url))
                .ObserveOn(RxApp.MainThreadScheduler);
            if (bitmap != null)
                SetImage(bitmap);
            else
                Icon = Bitmap.FromResource("PinBoard.Resources.url-error-icon.png");
        }
        catch (Exception e)
        {
            _logger.LogError("Bitmap load failed: {msg}", e.Message);
            Icon = Bitmap.FromResource("PinBoard.Resources.url-error-icon.png");
        }

        _updates.OnNext(default);
    }

    private Bitmap? LoadSync(Uri url)
    {
        _logger.LogInformation("Loading {url}", url);

        var response = _httpClient.GetAsync(url).GetAwaiter().GetResult();

        if (response.StatusCode is HttpStatusCode.Forbidden)
        {
            _logger.LogError("Forbidden");
            return null;
        }

        if (response.Content.Headers.ContentType?.ToString().StartsWith("image/") == true)
            return new Bitmap(response.Content.ReadAsStream());

        // Google search
        if (response.Content.Headers.ContentType?.ToString().StartsWith("text/html") == true)
        {
            var html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            _logger.LogDebug("Got text/html {html}", html);

            var match = Regex.Match(html, @"imageUrl='([^?']*)");
            if (match.Success)
                return LoadSync(new Uri(match.Groups[1].Value));

            match = Regex.Match(html, @"<a href=""(https://[^?""]*).*?"">\1");
            if (match.Success)
                return LoadSync(new Uri(match.Groups[1].Value));

            _logger.LogError("No image found in search");
            _logger.LogDebug("{html}", html);
            return null;
        }

        _logger.LogError("Unsupported content type {contentType}", response.Content.Headers.ContentType);
        return null;
    }

    private void SetImage(Image image)
    {
        _updating = true;

        try
        {
            (Image, Icon) = (image, null);
            var cropRect = new RectangleF(image.Size);

            Pin.Edit(
                pin =>
                {
                    if (pin.InitialSize != null)
                    {
                        pin.Scale = Math.Min(pin.InitialSize.Value / image.Width, pin.InitialSize.Value / image.Height);
                        pin.InitialSize = null;
                    }

                    if (pin.CropRect != null)
                    {
                        // Ensure that the crop rect does not exceed the loaded image.
                        cropRect.Restrict(pin.CropRect.Value);
                    }

                    pin.CropRect = cropRect;
                });
        }
        finally
        {
            _updating = false;
        }
    }
}
