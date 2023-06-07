using Eto.Drawing;
using Eto.Forms;
using PinBoard.Models;

namespace PinBoard.Util;

public static class Utils
{
    public static HitZone HitTest(RectangleF rect, PointF point, float margin)
    {
        var all = rect;
        all.Inflate(margin, margin);
        if (!all.Contains(point))
            return HitZone.Outside;

        var left = Math.Abs(point.X - rect.Left) <= margin;
        var top = Math.Abs(point.Y - rect.Top) <= margin;
        var right = Math.Abs(point.X - rect.Right) <= margin;
        var bottom = Math.Abs(point.Y - rect.Bottom) <= margin;

        return (left, top, right, bottom) switch
        {
            (true, true, _, _) => HitZone.TopLeft,
            (_, true, true, _) => HitZone.TopRight,
            (_, _, true, true) => HitZone.BottomRight,
            (true, _, _, true) => HitZone.BottomLeft,
            (true, _, _, _) => HitZone.Left,
            (_, true, _, _) => HitZone.Top,
            (_, _, true, _) => HitZone.Right,
            (_, _, _, true) => HitZone.Bottom,
            _ => HitZone.Center
        };
    }

    public static Cursor HitZoneToCursor(HitZone hitZone)
    {
        return hitZone switch
        {
            HitZone.Center => Cursors.Move,
            HitZone.Left => Cursors.SizeLeft,
            HitZone.TopLeft => Cursors.SizeTopLeft,
            HitZone.Top => Cursors.SizeTop,
            HitZone.TopRight => Cursors.SizeTopRight,
            HitZone.Right => Cursors.SizeRight,
            HitZone.BottomRight => Cursors.SizeBottomRight,
            HitZone.Bottom => Cursors.SizeBottom,
            HitZone.BottomLeft => Cursors.SizeBottomLeft,
            _ => Cursors.Default
        };
    }
}
