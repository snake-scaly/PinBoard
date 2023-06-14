using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Eto.Drawing;
using Eto.Forms;
using PinBoard.Models;

namespace PinBoard.UI;

public class BoardButton : BoardControl
{
    private readonly Settings _settings;
    private readonly Subject<Unit> _click = new();
    private bool _over;
    private bool _down;

    public BoardButton(Settings settings)
    {
        _settings = settings;
    }

    public IObservable<Unit> Click => _click.AsObservable();
    public Color Color { get; set; }

    public override void OnMouseMove(MouseEventArgs e)
    {
        _over = new RectangleF(Size).Contains(e.Location);
        Invalidate();
    }

    public override void OnMouseLeave(MouseEventArgs e)
    {
        _over = false;
        Invalidate();
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        _down = true;
        Invalidate();
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        _down = false;
        Invalidate();
        if (_over)
            _click.OnNext(default);
    }

    public override void OnPaint(PaintEventArgs e)
    {
        var path = GraphicsPath.GetRoundRect(new RectangleF(Size), 3);

        if (_over && _down)
            e.Graphics.FillPath(_settings.ButtonDownColor, path);
        else if (_over)
            e.Graphics.FillPath(_settings.ButtonHoverColor, path);
        else
            e.Graphics.FillPath(_settings.ButtonColor, path);

        if (_over)
            e.Graphics.DrawPath(new Pen(_settings.ButtonBorderColor, 2f), path);
        else
            e.Graphics.DrawPath(new Pen(_settings.ButtonBorderColor, 1.5f), path);
    }
}
