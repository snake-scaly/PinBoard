using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using Eto.Drawing;
using Eto.Forms;
using PinBoard.Models;
using PinBoard.Util;

namespace PinBoard.UI;

public class BoardButton : BoardControl
{
    private readonly Image _icon;
    private readonly Settings _settings;
    private readonly ICommand? _command;
    private readonly Subject<object?> _click = new();
    private object? _commandParameter;
    private bool _over;
    private bool _down;

    public BoardButton(string icon, Settings settings, ICommand? command = null, object? commandParameter = null)
    {
        _icon = Bitmap.FromResource($"PinBoard.Resources.{icon}");
        _settings = settings;
        _command = command;
        _commandParameter = commandParameter;

        if (command != null)
        {
            Observable.FromEventPattern(command, nameof(_command.CanExecuteChanged))
                .Subscribe(_ => Invalidate())
                .DisposeWith(Disposables);
        }
    }

    public IObservable<object?> Click => _click.AsObservable();

    public object? Parameter
    {
        get => _commandParameter;

        set
        {
            if (value != _commandParameter)
            {
                _commandParameter = value;
                Invalidate();
            }
        }
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        if (_command?.CanExecute(_commandParameter) == false)
            return;
        _over = new RectangleF(Size).Contains(e.Location);
        Invalidate();
    }

    public override void OnMouseLeave(MouseEventArgs e)
    {
        if (_command?.CanExecute(_commandParameter) == false)
            return;
        _over = false;
        Invalidate();
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (_command?.CanExecute(_commandParameter) == false)
            return;

        if (e.Buttons == MouseButtons.Primary)
        {
            _down = true;
            Invalidate();
        }
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        if (_command?.CanExecute(_commandParameter) == false)
            return;

        if (e.Buttons == MouseButtons.Primary)
        {
            _down = false;
            Invalidate();
            if (_over)
            {
                _click.OnNext(_commandParameter);
                _command?.Execute(_commandParameter);
            }
        }
    }

    public override void OnPaint(PaintEventArgs e)
    {
        var r = new RectangleF(Size);
        r.TopLeft -= 1;

        var path = GraphicsPath.GetRoundRect(r, 3);

        if (_over && _down)
            e.Graphics.FillPath(_settings.ButtonDownColor, path);
        else if (_over)
            e.Graphics.FillPath(_settings.ButtonHoverColor, path);
        else
            e.Graphics.FillPath(_settings.ButtonColor, path);

        e.Graphics.DrawImage(_icon, new PointF((Size - _icon.Size) / 2));

        if (_command?.CanExecute(_commandParameter) == false)
            e.Graphics.FillPath(Color.FromArgb(0, 0, 0, 128), path);

        if (_over)
            e.Graphics.DrawPath(new Pen(_settings.ButtonBorderColor, 2f), path);
        else
            e.Graphics.DrawPath(new Pen(_settings.ButtonBorderColor, 1.5f), path);
    }

    public override IDisposable OnContainerAttach(BoardControlContainer c)
    {
        return new CallbackDisposable(() => _over = _down = false);
    }
}
