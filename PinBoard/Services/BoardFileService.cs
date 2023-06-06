using System.Text.Json;
using System.Text.Json.Serialization;
using Eto.Drawing;

namespace PinBoard.Services;

public class BoardFileService : IBoardFileService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public void Load(Board board, string filename)
    {
        var json = File.ReadAllText(filename);
        var boardData = JsonSerializer.Deserialize<BoardDto>(json, _jsonOptions);
        var pins = boardData.Pins.Select(x => x.ToPin());

        board.Pins.Edit(
            x =>
            {
                x.Clear();
                x.AddRange(pins);
            });
    }

    public void Save(Board board, string filename)
    {
        var pinDtoList = board.Pins.Items.Select(PinDto.FromPin).ToList();
        var boardDto = new BoardDto { Pins = pinDtoList };
        var json = JsonSerializer.Serialize(boardDto, _jsonOptions);
        File.WriteAllText(filename, json);
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
                Url = pin.Url.ToString(),
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
            var location = Center.ToEto();

            if (Scale != null && Crop != null)
                return new Pin(url, location, scale: Scale, initialCrop: Crop.ToEto());
            if (InitialSize != null)
                return new Pin(url, location, initialSize: InitialSize);

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
