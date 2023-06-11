using Eto.Drawing;

namespace PinBoard.Models;

public class Settings
{
    public Color BackgroundColor { get; set; } = Color.FromArgb(35, 40, 60);
    public Color ButtonColor { get; set; } = Color.FromArgb(22, 26, 42);
    public Color ButtonHoverColor{ get; set; } = Color.FromArgb(43, 48, 69);
    public Color ButtonDownColor{ get; set; } = Color.FromArgb(52, 59, 85);
    public Color ButtonBorderColor{ get; set; } = Color.FromArgb(193, 192, 214);
    public float DragMargin { get; set; } = 10;
}
