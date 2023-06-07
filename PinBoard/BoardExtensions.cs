using DynamicData;
using Eto.Drawing;
using PinBoard.Models;

namespace PinBoard;

public static class BoardExtensions
{
    public static void Add(this Board board, Uri url, RectangleF viewport)
    {
        var initialSize = Math.Min(viewport.Width / 2f, viewport.Height / 2f);
        var x = Random.Shared.NextSingle() * (viewport.Width - initialSize) + initialSize / 2 + viewport.Left;
        var y = Random.Shared.NextSingle() * (viewport.Height - initialSize) + initialSize / 2 + viewport.Top;
        board.Pins.Add(new Pin(url, new PointF(x, y), initialSize));
    }

    public static void Add(this Board board, Uri url, RectangleF viewport, PointF location)
    {
        var initialSize = Math.Min(viewport.Width / 2f, viewport.Height / 2f);
        board.Pins.Add(new Pin(url, location, initialSize));
    }

    public static void Add(this Board board, Image image, RectangleF viewport, PointF location)
    {
        var boardScale = viewport.Size / image.Size;
        board.Pins.Add(new Pin(image, location, Math.Min(boardScale.Width, boardScale.Height)));
    }
}
