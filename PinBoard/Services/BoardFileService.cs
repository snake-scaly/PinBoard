using System.Text.Json;
using System.Text.Json.Serialization;
using Eto.Drawing;
using Microsoft.Extensions.Logging;
using PinBoard.Models;

namespace PinBoard.Services;

public class BoardFileService : IBoardFileService
{
    private readonly ILogger<BoardFileService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public BoardFileService(ILogger<BoardFileService> logger)
    {
        _logger = logger;
    }

    public void Load(Board board, string filename)
    {
        var json = File.ReadAllText(filename);

        var boardDto = JsonSerializer.Deserialize<BoardDto>(json, _jsonOptions);
        if (boardDto?.Pins == null)
            throw new BoardFormatException("Invalid file format");

        var pins = boardDto.Pins.Select(x => x.ToPin());

        board.Pins.Edit(
            x =>
            {
                x.Clear();
                x.AddRange(pins);
            });

        board.Filename = filename;
        board.Modified = false;
    }

    public void Save(Board board, string filename)
    {
        if (board.Pins.Items.Any(x => x.Url == null))
            _logger.LogWarning("Unable to save image-only pins, skipping");

        var pinDtoList = board.Pins.Items.Where(x => x.Url != null).Select(PinDto.FromPin).ToList();
        var boardDto = new BoardDto { Pins = pinDtoList };
        var json = JsonSerializer.Serialize(boardDto, _jsonOptions);
        File.WriteAllText(filename, json);

        board.Filename = filename;
        board.Modified = false;
    }

    private class BoardDto
    {
        public List<PinDto>? Pins { get; set; }
    }

    private class PinDto
    {
        public string? Url { get; set; }
        public Point? Center { get; set; }
        public float? Scale { get; set; }
        public Rect? Crop { get; set; }
        public float? InitialSize { get; set; }

        public static PinDto FromPin(Pin pin)
        {
            return new PinDto
            {
                Url = pin.Url?.ToString(),
                Center = Point.FromEto(pin.Center),
                Scale = pin.Scale,
                Crop = pin.CropRect == null ? null : Rect.FromEto(pin.CropRect.Value),
                InitialSize = pin.InitialSize,
            };
        }

        public Pin ToPin()
        {
            if (Url == null)
                throw new BoardFormatException($"{nameof(Url)} is missing");
            if (Center == null)
                throw new BoardFormatException($"{nameof(Center)} is missing");

            var url = new Uri(Url);
            var center = Center.ToEto();

            if (Scale != null && Crop != null)
                return new Pin(url, center, Scale.Value, Crop.ToEto());
            if (InitialSize != null)
                return new Pin(url, center, InitialSize.Value);

            throw new BoardFormatException($"{nameof(Scale)}/{nameof(Crop)} or {nameof(InitialSize)} is required");
        }
    }

    private class Point
    {
        public float X { get; set; }
        public float Y { get; set; }

        public static Point FromEto(PointF p) => new() { X = p.X, Y = p.Y };
        public PointF ToEto() => new() { X = X, Y = Y };
    }

    private class Rect
    {
        public float Left { get; set; }
        public float Top { get; set; }
        public float Right { get; set; }
        public float Bottom { get; set; }

        public static Rect FromEto(RectangleF r) => new() { Left = r.Left, Top = r.Top, Right = r.Right, Bottom = r.Bottom };
        public RectangleF ToEto() => new() { Left = Left, Top = Top, Right = Right, Bottom = Bottom };
    }
}
