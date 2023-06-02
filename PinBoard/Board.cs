using DynamicData;
using Eto.Drawing;

namespace PinBoard;

public class Board
{
    public SourceList<Pin> Pins { get; } = new();

    public void Add(Uri url, RectangleF viewport)
    {
        var size = Math.Min(viewport.Width / 2f, viewport.Height / 2f);
        var x = Random.Shared.NextSingle() * (viewport.Width - size) + size / 2 + viewport.Left;
        var y = Random.Shared.NextSingle() * (viewport.Height - size) + size / 2 + viewport.Top;
        Pins.Add(new Pin(url, new PointF(x, y), size));
    }

    public void Add(Uri url, RectangleF viewport, PointF location)
    {
        var size = Math.Min(viewport.Width / 2f, viewport.Height / 2f);
        Pins.Add(new Pin(url, location, size));
    }

    public void Add(Image image, RectangleF viewport, PointF location)
    {
        var boardScale = viewport.Size / image.Size;
        Pins.Add(new Pin(image, location, Math.Min(boardScale.Width, boardScale.Height)));
    }
}
