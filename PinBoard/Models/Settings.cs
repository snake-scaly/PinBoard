using Eto.Drawing;

namespace PinBoard.Models;

public class Settings
{
    public Color BackgroundColor { get; set; } = Color.FromArgb(35, 40, 60);
    public float DragMargin { get; set; } = 10;
}
