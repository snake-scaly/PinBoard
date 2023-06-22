using Eto.Drawing;

namespace PinBoard.UI;

public static class ButtonPlacementHelper
{
    public static void PlaceButtons(PointF anchor, RectangleF viewBounds, params BoardControl[] buttons)
    {
        const int buttonSpacing = 4;
        const int viewPadding = 10;

        var buttonsSize = buttons.Aggregate(
            default(SizeF),
            (size, button) => new SizeF(size.Width + button.Size.Width, Math.Max(size.Height, button.Size.Height)));
        buttonsSize += new SizeF(buttonSpacing * (buttons.Length - 1), 0);

        var topLeft = new PointF(anchor.X - buttonsSize.Width / 2, anchor.Y - buttonsSize.Height);

        viewBounds.Inset(viewPadding);
        viewBounds.BottomRight -= buttonsSize;
        topLeft.Restrict(viewBounds);

        foreach (var b in buttons)
        {
            b.Location = topLeft;
            b.Invalidate();
            topLeft.X += b.Size.Width + buttonSpacing;
        }
    }
}
